using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.Services;

public class ModerationService : IModerationService
{
    private readonly IContentReportRepository _reports;
    private readonly ISharedContentRepository _shares;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IAdminActionRepository _adminActions;
    private readonly IDocumentRepository _documents;
    private readonly IAudioAssetRepository _audioAssets;
    private readonly IQuizRepository _quizzes;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ModerationService> _logger;

    public const int DailyReportLimit = 5;
    public const int AutoHideThreshold = 3;

    public ModerationService(
        IContentReportRepository reports,
        ISharedContentRepository shares,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IAdminActionRepository adminActions,
        IDocumentRepository documents,
        IAudioAssetRepository audioAssets,
        IQuizRepository quizzes,
        IUnitOfWork uow,
        ILogger<ModerationService> logger)
    {
        _reports = reports;
        _shares = shares;
        _users = users;
        _refreshTokens = refreshTokens;
        _adminActions = adminActions;
        _documents = documents;
        _audioAssets = audioAssets;
        _quizzes = quizzes;
        _uow = uow;
        _logger = logger;
    }

    public async Task<ContentReportDto> ReportContentAsync(Guid userId, CreateReportRequest request, CancellationToken ct = default)
    {
        var share = await _shares.GetByIdAsync(request.SharedContentId, ct);
        if (share is null || share.IsRemoved)
            throw new NotFoundException("Şikâyet edilecek içerik bulunamadı.");

        // Kural 1: Kullanıcı aynı içeriği yalnızca 1 kez şikâyet edebilir
        if (await _reports.HasUserReportedAsync(userId, request.SharedContentId, ct))
            throw new ConflictException("Bu içeriği daha önce şikâyet ettiniz.");

        // Kural 2: Günlük en fazla 5 şikâyet sınırı
        var todayReports = await _reports.CountRecentReportsByUserAsync(userId, DateTime.UtcNow.Date, ct);
        if (todayReports >= DailyReportLimit)
            throw new BusinessValidationException("Reports", $"Günlük şikâyet sınırına ({DailyReportLimit} adet) ulaştınız.");

        var report = new ContentReport
        {
            ReporterId = userId,
            SharedContentId = request.SharedContentId,
            Reason = request.Reason.Trim(),
            Status = ReportStatus.Open
        };

        await _reports.AddAsync(report, ct);

        // Kural 3: 3 farklı kullanıcı tarafından şikâyet edilirse otomatik olarak akıştan gizlenir
        var totalReports = await _reports.GetReportCountForContentAsync(request.SharedContentId, ct) + 1;
        share.ReportCount = totalReports;

        if (totalReports >= AutoHideThreshold)
        {
            share.IsUnderReview = true;
            report.Status = ReportStatus.UnderReview;
            _shares.Update(share);
            _logger.LogWarning("İçerik {ContentId} {Count} şikâyete ulaştığı için otomatik incelemeye alındı.", share.Id, totalReports);
        }

        await _uow.SaveChangesAsync(ct);

        var reporter = await _users.GetByIdAsync(userId, ct);
        return new ContentReportDto(
            report.Id,
            report.SharedContentId,
            share.Title,
            share.ContentType,
            report.ReporterId,
            reporter?.FullName ?? "Kullanıcı",
            report.Reason,
            report.Status,
            totalReports,
            report.CreatedAt);
    }

    public async Task<IReadOnlyList<ContentReportDto>> GetReportsAsync(Guid adminId, CancellationToken ct = default)
    {
        var admin = await _users.GetByIdAsync(adminId, ct);
        if (admin is null || admin.Role != UserRole.Admin)
            throw new ForbiddenAppException("Yalnızca yöneticiler şikâyet kuyruğuna erişebilir.");

        var reports = await _reports.GetOpenReportsAsync(ct);

        return reports.Select(r => new ContentReportDto(
            r.Id,
            r.SharedContentId,
            r.SharedContent?.Title ?? "İçerik",
            r.SharedContent?.ContentType ?? SharedContentType.Summary,
            r.ReporterId,
            r.Reporter?.FullName ?? "Kullanıcı",
            r.Reason,
            r.Status,
            r.SharedContent?.ReportCount ?? 1,
            r.CreatedAt)).ToList();
    }

    public async Task ReviewReportAsync(Guid adminId, Guid reportId, ReviewReportRequest request, CancellationToken ct = default)
    {
        var admin = await _users.GetByIdAsync(adminId, ct);
        if (admin is null || admin.Role != UserRole.Admin)
            throw new ForbiddenAppException("Yalnızca yöneticiler moderasyon işlemi yapabilir.");

        var report = await _reports.Query()
            .Include(r => r.SharedContent)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report is null)
            throw new NotFoundException("Şikâyet bulunamadı.");

        report.Status = request.Decision;
        report.ReviewedById = adminId;

        AdminActionType actionType;

        if (request.Decision == ReportStatus.ContentRemoved)
        {
            report.SharedContent.IsRemoved = true;
            report.SharedContent.IsUnderReview = false;
            actionType = AdminActionType.RemoveContent;
        }
        else
        {
            // Şikâyet reddedildi -> içerik tekrar yayına alınır
            report.SharedContent.IsUnderReview = false;
            actionType = AdminActionType.DismissReport;
        }

        _shares.Update(report.SharedContent);
        _reports.Update(report);

        // Denetim kaydı (Audit log) oluştur
        var audit = new AdminAction
        {
            AdminId = adminId,
            ActionType = actionType,
            TargetType = "SharedContent",
            TargetId = report.SharedContentId,
            Reason = request.Note
        };
        await _adminActions.AddAsync(audit, ct);

        await _uow.SaveChangesAsync(ct);
    }

    public async Task UpdateUserStatusAsync(Guid adminId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken ct = default)
    {
        var admin = await _users.GetByIdAsync(adminId, ct);
        if (admin is null || admin.Role != UserRole.Admin)
            throw new ForbiddenAppException("Yalnızca yöneticiler kullanıcı durumunu güncelleyebilir.");

        var user = await _users.GetByIdAsync(targetUserId, ct);
        if (user is null)
            throw new NotFoundException("Kullanıcı bulunamadı.");

        user.IsSuspended = request.IsSuspended;
        if (request.IsSuspended)
        {
            user.SuspensionExpiresAt = DateTime.UtcNow.AddDays(request.SuspendDays ?? 7);

            // Güvenlik: Askıya alınan kullanıcının tüm aktif oturumlarını (refresh token) iptal et
            var tokens = await _refreshTokens.Query().Where(t => t.UserId == targetUserId && t.RevokedAt == null).ToListAsync(ct);
            foreach (var t in tokens)
            {
                t.RevokedAt = DateTime.UtcNow;
            }
        }
        else
        {
            user.SuspensionExpiresAt = null;
        }

        _users.Update(user);

        // Denetim kaydı (Audit log)
        var audit = new AdminAction
        {
            AdminId = adminId,
            ActionType = request.IsSuspended ? AdminActionType.SuspendUser : AdminActionType.UnsuspendUser,
            TargetType = "User",
            TargetId = targetUserId,
            Reason = request.Reason
        };
        await _adminActions.AddAsync(audit, ct);

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<AdminStatsDto> GetStatsAsync(Guid adminId, CancellationToken ct = default)
    {
        var admin = await _users.GetByIdAsync(adminId, ct);
        if (admin is null || admin.Role != UserRole.Admin)
            throw new ForbiddenAppException("Yalnızca yöneticiler istatistikleri görüntüleyebilir.");

        var totalUsers = await _users.Query().CountAsync(ct);
        var active7Days = await _users.Query().CountAsync(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-7), ct);
        var totalDocs = await _documents.Query().CountAsync(ct);
        var totalShares = await _shares.Query().CountAsync(s => !s.IsRemoved, ct);
        var openReports = await _reports.Query().CountAsync(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.UnderReview, ct);
        var totalTts = await _audioAssets.Query().CountAsync(ct);
        var totalQuizzes = await _quizzes.Query().CountAsync(ct);

        return new AdminStatsDto(
            totalUsers,
            active7Days,
            totalDocs,
            totalShares,
            openReports,
            totalTts,
            totalQuizzes);
    }
}
