using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SesliAsistan.Business.DTOs.Learning;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.Business.Speech;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.Services;

public class QuizService : IQuizService
{
    private readonly IQuizRepository _quizzes;
    private readonly IGenericRepository<QuizQuestion> _questions;
    private readonly IQuizAttemptRepository _attempts;
    private readonly IDocumentRepository _documents;
    private readonly IGenericRepository<DocumentSection> _sections;
    private readonly IQuizGenerator _quizGenerator;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<QuizService> _logger;

    public const int MaxDailyQuizzesPerUser = 10;

    public QuizService(
        IQuizRepository quizzes,
        IGenericRepository<QuizQuestion> questions,
        IQuizAttemptRepository attempts,
        IDocumentRepository documents,
        IGenericRepository<DocumentSection> sections,
        IQuizGenerator quizGenerator,
        IUnitOfWork uow,
        ILogger<QuizService> logger)
    {
        _quizzes = quizzes;
        _questions = questions;
        _attempts = attempts;
        _documents = documents;
        _sections = sections;
        _quizGenerator = quizGenerator;
        _uow = uow;
        _logger = logger;
    }

    public async Task<QuizDetailDto> CreateQuizAsync(Guid userId, Guid documentId, CreateQuizRequest request, CancellationToken ct = default)
    {
        var doc = await _documents.GetByIdAsync(documentId, ct);
        if (doc is null || doc.OwnerId != userId)
            throw new NotFoundException("Belge bulunamadı.");

        // Kota kontrolü: Günlük test üretim sınırı (10 test)
        var todayCount = await _quizzes.GetDailyCountAsync(userId, DateTime.UtcNow, ct);
        if (todayCount >= MaxDailyQuizzesPerUser)
            throw new BusinessValidationException("Quota", $"Günlük test üretim sınırına ({MaxDailyQuizzesPerUser} test) ulaştınız. Lütfen yarın tekrar deneyin.");

        string sectionText = string.Empty;
        string defaultTitle = "Bölüm Testi";

        if (request.SectionId.HasValue)
        {
            var section = await _sections.Query()
                .FirstOrDefaultAsync(s => s.Id == request.SectionId.Value && s.DocumentId == documentId, ct);
            if (section is null)
                throw new NotFoundException("Bölüm bulunamadı.");

            sectionText = section.Content ?? string.Empty;
            defaultTitle = $"{section.Title} Testi";
        }
        else
        {
            var sections = await _sections.Query()
                .Where(s => s.DocumentId == documentId)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync(ct);

            sectionText = string.Join("\n\n", sections.Select(s => s.Content).Where(c => !string.IsNullOrEmpty(c)));
            defaultTitle = $"{doc.Title} Genel Testi";
        }

        var quiz = new Quiz
        {
            DocumentId = documentId,
            SectionId = request.SectionId,
            OwnerId = userId,
            Title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : defaultTitle,
            Status = QuizStatus.Pending,
            Privacy = PrivacyLevel.Private
        };

        try
        {
            int requestedCount = Math.Clamp(request.QuestionCount, 1, 15);
            var generated = await _quizGenerator.GenerateAsync(sectionText, requestedCount, ct);

            int order = 0;
            var random = new Random();

            foreach (var g in generated)
            {
                // Seçenekleri rastgele karıştır (shuffle) ve yeni doğru indeksi hesapla
                var originalCorrect = g.Options[g.CorrectOptionIndex];
                var shuffledOptions = g.Options.OrderBy(_ => random.Next()).ToList();
                var newCorrectIndex = shuffledOptions.IndexOf(originalCorrect);

                var question = new QuizQuestion
                {
                    QuizId = quiz.Id,
                    QuestionText = g.QuestionText,
                    OptionsJson = JsonSerializer.Serialize(shuffledOptions),
                    CorrectOptionIndex = newCorrectIndex,
                    Explanation = g.Explanation,
                    OrderIndex = order++
                };

                quiz.Questions.Add(question);
            }

            quiz.Status = QuizStatus.Ready;
            await _quizzes.AddAsync(quiz, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test oluşturulurken hata meydana geldi: {QuizId}", quiz.Id);
            quiz.Status = QuizStatus.Failed;
            await _quizzes.AddAsync(quiz, ct);
            await _uow.SaveChangesAsync(ct);
        }

        return MapToDetailDto(quiz);
    }

    public async Task<QuizDetailDto> GetQuizAsync(Guid userId, Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _quizzes.GetWithQuestionsAsync(quizId, ct);
        if (quiz is null || quiz.OwnerId != userId)
            throw new NotFoundException("Test bulunamadı.");

        return MapToDetailDto(quiz);
    }

    public async Task<QuizAttemptResultDto> SubmitAttemptAsync(
        Guid userId,
        Guid quizId,
        SubmitQuizAttemptRequest request,
        CancellationToken ct = default)
    {
        var quiz = await _quizzes.GetWithQuestionsAsync(quizId, ct);
        if (quiz is null || quiz.OwnerId != userId)
            throw new NotFoundException("Test bulunamadı.");

        var submissionDict = request.Answers?.ToDictionary(a => a.QuestionId, a => a.SelectedOptionIndex)
                             ?? new Dictionary<Guid, int>();

        int correctCount = 0;
        int totalCount = quiz.Questions.Count;
        var results = new List<QuestionResultDto>();

        foreach (var q in quiz.Questions.OrderBy(x => x.OrderIndex))
        {
            bool isAnswered = submissionDict.TryGetValue(q.Id, out var selectedIndex);
            bool isCorrect = isAnswered && selectedIndex == q.CorrectOptionIndex;

            if (isCorrect) correctCount++;

            results.Add(new QuestionResultDto(
                q.Id,
                isCorrect,
                q.CorrectOptionIndex,
                q.Explanation));
        }

        int score = totalCount > 0 ? (int)Math.Round((double)correctCount * 100 / totalCount) : 0;

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            CorrectCount = correctCount,
            TotalQuestions = totalCount,
            CompletedAt = DateTime.UtcNow
        };

        await _attempts.AddAsync(attempt, ct);
        await _uow.SaveChangesAsync(ct);

        return new QuizAttemptResultDto(
            attempt.Id,
            correctCount,
            totalCount,
            score,
            results);
    }

    public async Task<IReadOnlyList<QuizAttemptHistoryDto>> GetAttemptsHistoryAsync(Guid userId, Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _quizzes.GetByIdAsync(quizId, ct);
        if (quiz is null || quiz.OwnerId != userId)
            throw new NotFoundException("Test bulunamadı.");

        var history = await _attempts.GetHistoryByQuizAsync(quizId, userId, ct);

        return history.Select(h => new QuizAttemptHistoryDto(
            h.Id,
            h.QuizId,
            quiz.Title,
            h.CorrectCount,
            h.TotalQuestions,
            h.TotalQuestions > 0 ? (int)Math.Round((double)h.CorrectCount * 100 / h.TotalQuestions) : 0,
            h.CompletedAt)).ToList();
    }

    private static QuizDetailDto MapToDetailDto(Quiz quiz)
    {
        // GÜVENLİK: İstemciye dönen soru listesinde CorrectOptionIndex ve Explanation ASLA yer almaz!
        var questions = quiz.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q =>
            {
                var options = JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new List<string>();
                return new QuizQuestionDto(q.Id, q.QuestionText, options, q.OrderIndex);
            })
            .ToList();

        return new QuizDetailDto(
            quiz.Id,
            quiz.DocumentId,
            quiz.SectionId,
            quiz.Title,
            quiz.Status,
            quiz.Privacy,
            quiz.CreatedAt,
            questions);
    }
}
