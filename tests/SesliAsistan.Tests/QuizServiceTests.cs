using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SesliAsistan.Business.DTOs.Learning;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Services;
using SesliAsistan.Business.Speech;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;
using Xunit;

namespace SesliAsistan.Tests;

public class QuizServiceTests
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
    public async Task CreateQuizAsync_GecerliIstek_QuizBasariylaOlusturulurVeReadyDurumunaGecer()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "test@uni.edu.tr", FullName = "Ali Veli", PasswordHash = "hash" };
        var doc = new Document { Title = "Veritabanı", OriginalFileName = "db.pdf", StoragePath = "p", Owner = user };
        var sec = new DocumentSection { Document = doc, Title = "Normalizasyon", Content = "Normalizasyon veri tekrarını önler." };
        await db.Users.AddAsync(user);
        await db.Documents.AddAsync(doc);
        await db.DocumentSections.AddAsync(sec);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateQuizAsync(user.Id, doc.Id, new CreateQuizRequest(sec.Id, 3));

        result.Should().NotBeNull();
        result.Status.Should().Be(QuizStatus.Ready);
        result.Questions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateQuizAsync_GeneratorHataVerdiginde_QuizStatusFailedOlur()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "fail@uni.edu.tr", FullName = "Hasan", PasswordHash = "hash" };
        var doc = new Document { Title = "TestDoc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = user };
        await db.Users.AddAsync(user);
        await db.Documents.AddAsync(doc);
        await db.SaveChangesAsync();

        var failingGen = new FailingQuizGenerator();
        var service = CreateService(db, failingGen);

        var result = await service.CreateQuizAsync(user.Id, doc.Id, new CreateQuizRequest());
        result.Status.Should().Be(QuizStatus.Failed);
    }

    [Fact]
    public async Task CreateQuizAsync_GunlukKotaAsildiginda_BusinessValidationExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "quota@uni.edu.tr", FullName = "Zeynep", PasswordHash = "hash" };
        var doc = new Document { Title = "Ders", OriginalFileName = "d.pdf", StoragePath = "p", Owner = user };
        await db.Users.AddAsync(user);
        await db.Documents.AddAsync(doc);

        for (int i = 0; i < QuizService.MaxDailyQuizzesPerUser; i++)
        {
            db.Quizzes.Add(new Quiz
            {
                Document = doc,
                Owner = user,
                Title = $"Test {i}",
                Status = QuizStatus.Ready,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.CreateQuizAsync(user.Id, doc.Id, new CreateQuizRequest());

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task GetQuizAsync_BaskaKullanicininTestineErisim_NotFoundExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "owner@uni.edu.tr", FullName = "Sahip", PasswordHash = "hash" };
        var other = new User { Email = "other@uni.edu.tr", FullName = "Yabancı", PasswordHash = "hash" };
        var doc = new Document { Title = "Doc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = owner };
        var quiz = new Quiz { Document = doc, Owner = owner, Title = "Özel Test", Status = QuizStatus.Ready };
        await db.Users.AddRangeAsync(owner, other);
        await db.Documents.AddAsync(doc);
        await db.Quizzes.AddAsync(quiz);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.GetQuizAsync(other.Id, quiz.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetQuizAsync_KendiTestiIcin_SoruDetaylariBasariylaDoner()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "user@uni.edu.tr", FullName = "Ayşe", PasswordHash = "hash" };
        var doc = new Document { Title = "Doc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = user };
        var quiz = new Quiz { Document = doc, Owner = user, Title = "Quiz", Status = QuizStatus.Ready };
        quiz.Questions.Add(new QuizQuestion { QuestionText = "Soru 1?", OptionsJson = "[\"A\",\"B\",\"C\",\"D\"]", CorrectOptionIndex = 0 });
        await db.Users.AddAsync(user);
        await db.Documents.AddAsync(doc);
        await db.Quizzes.AddAsync(quiz);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetQuizAsync(user.Id, quiz.Id);

        result.Should().NotBeNull();
        result.Title.Should().Be("Quiz");
        result.Questions.Should().HaveCount(1);
    }

    private class FailingQuizGenerator : IQuizGenerator
    {
        public Task<IReadOnlyList<QuizQuestionGeneratedDto>> GenerateAsync(string sectionText, int count, CancellationToken ct = default) =>
            throw new InvalidOperationException("API Bağlantı Hatası");
    }
}
