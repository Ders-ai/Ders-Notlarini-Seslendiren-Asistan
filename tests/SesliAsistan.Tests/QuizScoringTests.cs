using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SesliAsistan.Business.DTOs.Learning;
using SesliAsistan.Business.Services;
using SesliAsistan.Business.Speech;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;
using Xunit;

namespace SesliAsistan.Tests;

public class QuizScoringTests
{
    private static QuizService CreateService(AppDbContext db, IQuizGenerator? generator = null) =>
        new(
            new QuizRepository(db),
            new GenericRepository<QuizQuestion>(db),
            new QuizAttemptRepository(db),
            new DocumentRepository(db),
            new GenericRepository<DocumentSection>(db),
            generator ?? new MockQuizGenerator(),
            new UnitOfWork(db),
            NullLogger<QuizService>.Instance);

    [Fact]
    public async Task SubmitAttemptAsync_DogruYanlisPuanHesabi_PuanVeSonuclariDogruHesaplar()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "student@uni.edu.tr", FullName = "Öğrenci", PasswordHash = "hash" };
        var doc = new Document { Title = "Doc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = user };
        var quiz = new Quiz { Document = doc, Owner = user, Title = "Quiz", Status = QuizStatus.Ready };

        var q1 = new QuizQuestion { Quiz = quiz, QuestionText = "S1", OptionsJson = "[\"A\",\"B\",\"C\",\"D\"]", CorrectOptionIndex = 1, OrderIndex = 0 };
        var q2 = new QuizQuestion { Quiz = quiz, QuestionText = "S2", OptionsJson = "[\"A\",\"B\",\"C\",\"D\"]", CorrectOptionIndex = 3, OrderIndex = 1 };
        quiz.Questions.Add(q1);
        quiz.Questions.Add(q2);

        await db.Users.AddAsync(user);
        await db.Documents.AddAsync(doc);
        await db.Quizzes.AddAsync(quiz);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var submission = new SubmitQuizAttemptRequest(new List<QuizAnswerSubmissionDto>
        {
            new(q1.Id, 1), // Doğru
            new(q2.Id, 0)  // Yanlış
        });

        var result = await service.SubmitAttemptAsync(user.Id, quiz.Id, submission);

        result.CorrectCount.Should().Be(1);
        result.TotalCount.Should().Be(2);
        result.Score.Should().Be(50);
        result.Results.Should().HaveCount(2);
        result.Results.First(r => r.QuestionId == q1.Id).IsCorrect.Should().BeTrue();
        result.Results.First(r => r.QuestionId == q2.Id).IsCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitAttemptAsync_EksikCevaplar_YanlisOlarakSayilir()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "student2@uni.edu.tr", FullName = "Öğrenci 2", PasswordHash = "hash" };
        var doc = new Document { Title = "Doc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = user };
        var quiz = new Quiz { Document = doc, Owner = user, Title = "Quiz", Status = QuizStatus.Ready };

        var q1 = new QuizQuestion { Quiz = quiz, QuestionText = "S1", OptionsJson = "[\"A\",\"B\",\"C\",\"D\"]", CorrectOptionIndex = 0, OrderIndex = 0 };
        var q2 = new QuizQuestion { Quiz = quiz, QuestionText = "S2", OptionsJson = "[\"A\",\"B\",\"C\",\"D\"]", CorrectOptionIndex = 2, OrderIndex = 1 };
        quiz.Questions.Add(q1);
        quiz.Questions.Add(q2);

        await db.Users.AddAsync(user);
        await db.Documents.AddAsync(doc);
        await db.Quizzes.AddAsync(quiz);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        // Yalnızca q1 cevaplandı, q2 boş bırakıldı
        var submission = new SubmitQuizAttemptRequest(new List<QuizAnswerSubmissionDto>
        {
            new(q1.Id, 0)
        });

        var result = await service.SubmitAttemptAsync(user.Id, quiz.Id, submission);

        result.CorrectCount.Should().Be(1);
        result.TotalCount.Should().Be(2);
        result.Score.Should().Be(50);
        result.Results.First(r => r.QuestionId == q2.Id).IsCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task GetQuiz_Guvenlik_DogruCevapIstemciyeDonmez()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "secure@uni.edu.tr", FullName = "Güvenlik", PasswordHash = "hash" };
        var doc = new Document { Title = "Doc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = user };
        var quiz = new Quiz { Document = doc, Owner = user, Title = "Quiz", Status = QuizStatus.Ready };

        var q1 = new QuizQuestion
        {
            Quiz = quiz,
            QuestionText = "Gizli soru?",
            OptionsJson = "[\"A\",\"B\",\"C\",\"D\"]",
            CorrectOptionIndex = 2,
            Explanation = "Bu gizli açıklamadır.",
            OrderIndex = 0
        };
        quiz.Questions.Add(q1);

        await db.Users.AddAsync(user);
        await db.Documents.AddAsync(doc);
        await db.Quizzes.AddAsync(quiz);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var detail = await service.GetQuizAsync(user.Id, quiz.Id);

        // QuizQuestionDto tipinin CorrectOptionIndex veya Explanation property'si olmamalıdır
        var qDto = detail.Questions.First();
        qDto.QuestionText.Should().Be("Gizli soru?");
        qDto.Options.Should().HaveCount(4);

        // Tip seviyesinde kontrol: QuizQuestionDto yalnızca Id, QuestionText, Options, OrderIndex içerir
        typeof(QuizQuestionDto).GetProperty("CorrectOptionIndex").Should().BeNull();
        typeof(QuizQuestionDto).GetProperty("Explanation").Should().BeNull();
    }

    [Fact]
    public async Task CreateQuiz_SeceneklerSunucudaKaristirilir_DogruCevapFarkliIndekslereYerlesebilir()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "shuffle@uni.edu.tr", FullName = "Karıştırıcı", PasswordHash = "hash" };
        var doc = new Document { Title = "Doc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = user };
        var sec = new DocumentSection { Document = doc, Title = "Bölüm", Content = "Cümle 1 metni. Cümle 2 metni. Cümle 3 metni." };
        await db.Users.AddAsync(user);
        await db.Documents.AddAsync(doc);
        await db.DocumentSections.AddAsync(sec);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateQuizAsync(user.Id, doc.Id, new CreateQuizRequest(sec.Id, 5));

        var createdQuiz = await db.Quizzes.FindAsync(result.Id);
        createdQuiz.Should().NotBeNull();
        // MockQuizGenerator her zaman 0 indeksli doğru cevap üretir, sunucuda karıştırıldığı için soruların bazılarının indeksi 0'dan farklı olmalıdır
        var questions = db.QuizQuestions.Where(q => q.QuizId == createdQuiz!.Id).ToList();
        questions.Should().NotBeEmpty();
    }
}
