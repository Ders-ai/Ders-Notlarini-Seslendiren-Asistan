using FluentAssertions;
using SesliAsistan.Business.DTOs.Learning;
using SesliAsistan.Business.Speech;
using Xunit;

namespace SesliAsistan.Tests;

public class QuizValidatorTests
{
    private readonly QuizValidator _validator = new();
    private const string SourceText = "İlişkisel veritabanlarında birincil anahtar her satırı tekil olarak tanımlar. Yabancı anahtar ise tablolar arası ilişki kurar.";

    [Fact]
    public void ValidateQuestion_GecersizVeyaBosSoruMetni_HataDoner()
    {
        var questionNull = new QuizQuestionGeneratedDto("", new List<string> { "A", "B", "C", "D" }, 0, "Açıklama");
        var result = _validator.ValidateQuestion(questionNull, SourceText);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Soru metni");
    }

    [Fact]
    public void ValidateQuestion_DorttenAzSecenek_HataDoner()
    {
        var question = new QuizQuestionGeneratedDto(
            "Birincil anahtar nedir?",
            new List<string> { "A", "B", "C" },
            0,
            "Açıklama");

        var result = _validator.ValidateQuestion(question, SourceText);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("4 seçenek");
    }

    [Fact]
    public void ValidateQuestion_TekrarEdenSecenekler_HataDoner()
    {
        var question = new QuizQuestionGeneratedDto(
            "Birincil anahtar nedir?",
            new List<string> { "Tekil tanımlayıcı", "Yabancı anahtar", "Tekil tanımlayıcı", "Tablo" },
            0,
            "Açıklama");

        var result = _validator.ValidateQuestion(question, SourceText);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("farklı olmalıdır");
    }

    [Fact]
    public void ValidateQuestion_GecersizDogruSecenekIndeksi_HataDoner()
    {
        var questionNegative = new QuizQuestionGeneratedDto(
            "Birincil anahtar nedir?",
            new List<string> { "A", "B", "C", "D" },
            -1,
            "Açıklama");

        var questionOverflow = new QuizQuestionGeneratedDto(
            "Birincil anahtar nedir?",
            new List<string> { "A", "B", "C", "D" },
            4,
            "Açıklama");

        _validator.ValidateQuestion(questionNegative, SourceText).IsValid.Should().BeFalse();
        _validator.ValidateQuestion(questionOverflow, SourceText).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateQuestion_MetindeDayanagiOlmayanSoru_Elenir()
    {
        var hallucinatedQuestion = new QuizQuestionGeneratedDto(
            "Kuantum dolanıklığı hangi deneyle kanıtlanmıştır?",
            new List<string> { "Bell deneyi", "Young deneyi", "Michelson deneyi", "Cavendish deneyi" },
            0,
            "Bell eşitsizlikleri kuantum fiziğinde test edilmiştir.");

        var result = _validator.ValidateQuestion(hallucinatedQuestion, SourceText);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("dayanak");
    }

    [Fact]
    public void ValidateQuestion_GecerliVeDayanakliSoru_BasariylaGecer()
    {
        var validQuestion = new QuizQuestionGeneratedDto(
            "İlişkisel veritabanlarında birincil anahtarın temel görevi nedir?",
            new List<string>
            {
                "Her satırı tekil olarak tanımlamak",
                "Tabloları silmek",
                "Veri boyutunu sınırlandırmak",
                "Kullanıcı şifrelerini saklamak"
            },
            0,
            "Metinde birincil anahtarın her satırı tekil olarak tanımladığı belirtilmiştir.");

        var result = _validator.ValidateQuestion(validQuestion, SourceText);
        result.IsValid.Should().BeTrue();
    }
}
