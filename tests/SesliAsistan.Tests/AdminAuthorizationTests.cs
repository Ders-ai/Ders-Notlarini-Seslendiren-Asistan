using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Services;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;
using Xunit;

namespace SesliAsistan.Tests;

public class AdminAuthorizationTests
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
    public async Task GetReportsAsync_OgrenciRoluyle_ForbiddenAppExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var student = new User { Email = "student_auth@uni.edu.tr", FullName = "Öğrenci", PasswordHash = "h", Role = UserRole.Student };
        await db.Users.AddAsync(student);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.GetReportsAsync(student.Id);

        await act.Should().ThrowAsync<ForbiddenAppException>();
    }

    [Fact]
    public async Task GetReportsAsync_YoneticiRoluyle_BasariylaListeler()
    {
        using var db = TestHelpers.CreateDb();
        var admin = new User { Email = "admin_auth@uni.edu.tr", FullName = "Yönetici", PasswordHash = "h", Role = UserRole.Admin };
        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var reports = await service.GetReportsAsync(admin.Id);

        reports.Should().NotBeNull();
    }
}
