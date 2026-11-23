using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Services;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;
using Xunit;

namespace SesliAsistan.Tests;

public class ModerationServiceTests
{
    private static ModerationService CreateService(AppDbContext db) =>
        new(
            new ContentReportRepository(db),
            new SharedContentRepository(db),
            new UserRepository(db),
            new RefreshTokenRepository(db),
            new AdminActionRepository(db),
            new DocumentRepository(db),
            new AudioAssetRepository(db),
            new QuizRepository(db),
            new UnitOfWork(db),
            NullLogger<ModerationService>.Instance);

    [Fact]
    public async Task ReportContentAsync_AyniIcerigeTekrarSikayet_ConflictExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "rep@uni.edu.tr", FullName = "R", PasswordHash = "h" };
        var share = new SharedContent { Owner = user, Title = "Paylaşım", Privacy = PrivacyLevel.Public };
        await db.Users.AddAsync(user);
        await db.SharedContents.AddAsync(share);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.ReportContentAsync(user.Id, new CreateReportRequest(share.Id, "Spam içerik"));

        var act = () => service.ReportContentAsync(user.Id, new CreateReportRequest(share.Id, "Tekrar şikayet"));
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ReportContentAsync_GunlukSinirAsimi_BusinessValidationExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "limit@uni.edu.tr", FullName = "Limit", PasswordHash = "h" };
        await db.Users.AddAsync(user);

        for (int i = 0; i < ModerationService.DailyReportLimit; i++)
        {
            var s = new SharedContent { Owner = user, Title = $"P {i}", Privacy = PrivacyLevel.Public };
            db.SharedContents.Add(s);
            db.ContentReports.Add(new ContentReport { Reporter = user, SharedContent = s, Reason = "Neden", CreatedAt = DateTime.UtcNow });
        }
        var extraShare = new SharedContent { Owner = user, Title = "Extra", Privacy = PrivacyLevel.Public };
        db.SharedContents.Add(extraShare);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.ReportContentAsync(user.Id, new CreateReportRequest(extraShare.Id, "6. Şikayet"));

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task ReportContentAsync_UcFarkliKullaniciSikayetEdince_OtomatikUnderReviewOlur()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "o@uni.edu.tr", FullName = "O", PasswordHash = "h" };
        var u1 = new User { Email = "u1@uni.edu.tr", FullName = "U1", PasswordHash = "h" };
        var u2 = new User { Email = "u2@uni.edu.tr", FullName = "U2", PasswordHash = "h" };
        var u3 = new User { Email = "u3@uni.edu.tr", FullName = "U3", PasswordHash = "h" };
        var share = new SharedContent { Owner = owner, Title = "İhlal Eden Not", Privacy = PrivacyLevel.Public };

        await db.Users.AddRangeAsync(owner, u1, u2, u3);
        await db.SharedContents.AddAsync(share);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.ReportContentAsync(u1.Id, new CreateReportRequest(share.Id, "Telif ihlali"));
        await service.ReportContentAsync(u2.Id, new CreateReportRequest(share.Id, "Uygunsuz"));
        await service.ReportContentAsync(u3.Id, new CreateReportRequest(share.Id, "Yanıltıcı"));

        var updatedShare = await db.SharedContents.FindAsync(share.Id);
        updatedShare!.IsUnderReview.Should().BeTrue();
    }

    [Fact]
    public async Task ReviewReportAsync_KaldirmaVeReddetme_IcerikDurumunuGunceller()
    {
        using var db = TestHelpers.CreateDb();
        var admin = new User { Email = "adm@uni.edu.tr", FullName = "Admin", PasswordHash = "h", Role = UserRole.Admin };
        var owner = new User { Email = "own@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var share = new SharedContent { Owner = owner, Title = "Paylaşım", Privacy = PrivacyLevel.Public, IsUnderReview = true };
        var report = new ContentReport { Reporter = owner, SharedContent = share, Reason = "Sebep", Status = ReportStatus.UnderReview };

        await db.Users.AddRangeAsync(admin, owner);
        await db.SharedContents.AddAsync(share);
        await db.ContentReports.AddAsync(report);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        // Yönetici içeriği kaldırıyor
        await service.ReviewReportAsync(admin.Id, report.Id, new ReviewReportRequest(ReportStatus.ContentRemoved, "Kural ihlali"));

        var removedShare = await db.SharedContents.FindAsync(share.Id);
        removedShare!.IsRemoved.Should().BeTrue();
        removedShare.IsUnderReview.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserStatusAsync_AskiyaAlinanKullanici_OturumlariIptalEdilir()
    {
        using var db = TestHelpers.CreateDb();
        var admin = new User { Email = "admin_s@uni.edu.tr", FullName = "Admin", PasswordHash = "h", Role = UserRole.Admin };
        var student = new User { Email = "student_s@uni.edu.tr", FullName = "Öğrenci", PasswordHash = "h" };
        var token = new RefreshToken { User = student, TokenHash = "tok123", ExpiresAt = DateTime.UtcNow.AddDays(7) };

        await db.Users.AddRangeAsync(admin, student);
        await db.RefreshTokens.AddAsync(token);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateUserStatusAsync(admin.Id, student.Id, new UpdateUserStatusRequest(true, SuspendDays: 7, Reason: "Kural ihlali"));

        var updatedStudent = await db.Users.FindAsync(student.Id);
        updatedStudent!.IsSuspended.Should().BeTrue();
        updatedStudent.SuspensionExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var updatedToken = await db.RefreshTokens.FindAsync(token.Id);
        updatedToken!.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Moderation_AdminAction_DenetimKaydiOlusturur()
    {
        using var db = TestHelpers.CreateDb();
        var admin = new User { Email = "audit_admin@uni.edu.tr", FullName = "Admin", PasswordHash = "h", Role = UserRole.Admin };
        var targetUser = new User { Email = "target@uni.edu.tr", FullName = "Hedef", PasswordHash = "h" };

        await db.Users.AddRangeAsync(admin, targetUser);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateUserStatusAsync(admin.Id, targetUser.Id, new UpdateUserStatusRequest(true, 3, "Spam davranışı"));

        var action = db.AdminActions.FirstOrDefault(a => a.AdminId == admin.Id && a.TargetId == targetUser.Id);
        action.Should().NotBeNull();
        action!.ActionType.Should().Be(AdminActionType.SuspendUser);
        action.Reason.Should().Be("Spam davranışı");
    }
}
