using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SesliAsistan.Business.DTOs.Learning;

namespace SesliAsistan.Business.Speech;

public record QuizValidationResult(bool IsValid, string? ErrorMessage = null);

public interface IQuizValidator
{
    QuizValidationResult ValidateQuestion(QuizQuestionGeneratedDto question, string sourceText);
}

public class QuizValidator : IQuizValidator
{
    public QuizValidationResult ValidateQuestion(QuizQuestionGeneratedDto question, string sourceText)
    {
        if (question is null)
            return new QuizValidationResult(false, "Soru nesnesi boş olamaz.");

        // 1. Soru metni kontrolü
        if (string.IsNullOrWhiteSpace(question.QuestionText))
            return new QuizValidationResult(false, "Soru metni boş olamaz.");

        // 2. Tam 4 seçenek kontrolü
        if (question.Options is null || question.Options.Count != 4)
            return new QuizValidationResult(false, "Soru tam 4 seçenek içermelidir.");

        // 3. Seçeneklerin boş olmaması kontrolü
        if (question.Options.Any(string.IsNullOrWhiteSpace))
            return new QuizValidationResult(false, "Seçenek metinleri boş olamaz.");

        // 4. Tekrar eden seçenek kontrolü
        var distinctOptions = question.Options
            .Select(o => o.Trim().ToLowerInvariant())
            .Distinct()
            .Count();

        if (distinctOptions != 4)
            return new QuizValidationResult(false, "Seçenekler birbirinden farklı olmalıdır; aynı seçenek tekrarlanamaz.");

        // 5. Doğru cevap indeksi kontrolü
        if (question.CorrectOptionIndex < 0 || question.CorrectOptionIndex >= 4)
            return new QuizValidationResult(false, "Doğru seçenek indeksi 0 ile 3 arasında olmalıdır.");

        // 6. Metne dayanak kontrolü (Grounding Check)
        if (!string.IsNullOrWhiteSpace(sourceText))
        {
            var correctOption = question.Options[question.CorrectOptionIndex];
            var correctWords = Regex.Split(correctOption.ToLowerInvariant(), @"[^\p{L}0-9]+")
                .Where(w => w.Length >= 3)
                .ToList();

            var questionWords = Regex.Split(question.QuestionText.ToLowerInvariant(), @"[^\p{L}0-9]+")
                .Where(w => w.Length >= 3)
                .ToList();

            var combinedWords = correctWords.Concat(questionWords).ToList();

            if (combinedWords.Count > 0)
            {
                var sourceWords = Regex.Split(sourceText.ToLowerInvariant(), @"[^\p{L}0-9]+");
                var sourceStems = new HashSet<string>(
                    sourceWords.Where(w => w.Length >= 3).Select(KeyTermExtractor.GetF5Stem),
                    StringComparer.OrdinalIgnoreCase);

                bool hasGroundedTerm = combinedWords.Any(w => sourceStems.Contains(KeyTermExtractor.GetF5Stem(w)));

                if (!hasGroundedTerm)
                {
                    return new QuizValidationResult(false, "Soru veya doğru cevap kaynak metinde yer almamaktadır (dayanak kontrolü başarısız).");
                }
            }
        }

        return new QuizValidationResult(true);
    }
}

public interface IQuizGenerator
{
    Task<IReadOnlyList<QuizQuestionGeneratedDto>> GenerateAsync(string sectionText, int count, CancellationToken ct = default);
}

public class MockQuizGenerator : IQuizGenerator
{
    public Task<IReadOnlyList<QuizQuestionGeneratedDto>> GenerateAsync(string sectionText, int count, CancellationToken ct = default)
    {
        var sentences = TextChunker.SplitIntoSentences(sectionText);
        var questions = new List<QuizQuestionGeneratedDto>();

        int targetCount = Math.Min(count, Math.Max(1, sentences.Count));

        for (int i = 0; i < targetCount; i++)
        {
            var sentence = sentences[i % sentences.Count];
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var keyWord = words.Length > 0 ? words[0] : "Konu";

            var q = new QuizQuestionGeneratedDto(
                QuestionText: $"{keyWord} ile ilgili aşağıdaki ifadelerden hangisi doğrudur?",
                Options: new List<string>
                {
                    sentence.Trim(),
                    $"Bu konunun {keyWord} kavramıyla hiçbir ilişkisi bulunmamaktadır.",
                    $"{keyWord} yalnızca teorik bir varsayımdan ibarettir.",
                    $"Söz konusu {keyWord} işlemi sistem tarafından desteklenmez."
                },
                CorrectOptionIndex: 0,
                Explanation: $"Kaynak metinde açıkça belirtilmiştir: '{sentence.Trim()}'"
            );

            questions.Add(q);
        }

        return Task.FromResult<IReadOnlyList<QuizQuestionGeneratedDto>>(questions);
    }
}

public class GroqQuizGenerator : IQuizGenerator
{
    private readonly HttpClient _http;
    private readonly IQuizValidator _validator;
    private readonly ILogger<GroqQuizGenerator> _logger;

    public GroqQuizGenerator(HttpClient http, IQuizValidator validator, ILogger<GroqQuizGenerator> logger)
    {
        _http = http;
        _validator = validator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuizQuestionGeneratedDto>> GenerateAsync(string sectionText, int count, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sectionText))
            return Array.Empty<QuizQuestionGeneratedDto>();

        // Eğer API anahtarı ayarlanmamışsa Mock'a düş
        if (_http.DefaultRequestHeaders.Authorization is null)
        {
            _logger.LogWarning("Groq API anahtarı bulunamadı; yerel sahte üreticiye (mock) başvuruluyor.");
            return await new MockQuizGenerator().GenerateAsync(sectionText, count, ct);
        }

        try
        {
            var systemPrompt = @"Sen Türkçe ders notlarını analiz eden bir eğitim yapay zekasısın.
Sana verilen metne dayanarak çoktan seçmeli test soruları üret.
KURALLAR:
1. YALNIZCA verilen metindeki bilgilere dayan. Metinde olmayan hiçbir bilgi ekleme.
2. Her soru için tam 4 farklı seçenek oluştur.
3. Tek bir doğru cevap olsun ve seçenekler arasında 0-3 indeksini belirt.
4. Doğru cevabın metindeki dayanağını 'explanation' alanında açıkla.
5. Çıktıyı JSON formatında ver:
{
  ""questions"": [
    {
      ""questionText"": ""..."",
      ""options"": [""A"", ""B"", ""C"", ""D""],
      ""correctOptionIndex"": 0,
      ""explanation"": ""...""
    }
  ]
}";

            var userPrompt = $"Bölüm Metni:\n{sectionText}\n\nLütfen yukarıdaki metinden {count} adet çoktan seçmeli soru üret.";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.3
            };

            var response = await _http.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Groq API hatası: {StatusCode}", response.StatusCode);
                return await new MockQuizGenerator().GenerateAsync(sectionText, count, ct);
            }

            var jsonString = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(jsonString);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
                return await new MockQuizGenerator().GenerateAsync(sectionText, count, ct);

            using var contentDoc = JsonDocument.Parse(content);
            var questionsList = new List<QuizQuestionGeneratedDto>();

            if (contentDoc.RootElement.TryGetProperty("questions", out var questionsArray))
            {
                foreach (var qEl in questionsArray.EnumerateArray())
                {
                    var qText = qEl.GetProperty("questionText").GetString() ?? "";
                    var opts = qEl.GetProperty("options").EnumerateArray().Select(o => o.GetString() ?? "").ToList();
                    var corrIdx = qEl.GetProperty("correctOptionIndex").GetInt32();
                    var exp = qEl.TryGetProperty("explanation", out var expEl) ? expEl.GetString() : null;

                    var qDto = new QuizQuestionGeneratedDto(qText, opts, corrIdx, exp);
                    var val = _validator.ValidateQuestion(qDto, sectionText);
                    if (val.IsValid)
                    {
                        questionsList.Add(qDto);
                    }
                }
            }

            return questionsList.Count > 0 ? questionsList : await new MockQuizGenerator().GenerateAsync(sectionText, count, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq soru üretimi sırasında istisna oluştu; sahte üreticiye geçiliyor.");
            return await new MockQuizGenerator().GenerateAsync(sectionText, count, ct);
        }
    }
}
