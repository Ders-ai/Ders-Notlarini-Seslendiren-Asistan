using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.DTOs.Social;

// ---- FG-7: Kullanıcı Arama ve Arkadaşlık ----
public record UserSearchDto(Guid Id, string FullName);

public record SendFriendRequest(Guid AddresseeId);

public record RespondFriendRequest(bool Accept);

public record FriendDto(
    Guid FriendshipId,
    Guid UserId,
    string FullName,
    FriendshipStatus Status,
    bool IsOutgoing,
    DateTime CreatedAt);

public record FriendListDto(
    IReadOnlyList<FriendDto> Friends,
    IReadOnlyList<FriendDto> PendingIncoming,
    IReadOnlyList<FriendDto> PendingOutgoing);

// ---- FG-7: Çalışma Grupları ----
public record CreateGroupRequest(string Name, string? Description = null);

public record JoinGroupRequest(string InviteCode);

public record GroupMemberDto(
    Guid UserId,
    string FullName,
    GroupRole Role,
    DateTime JoinedAt);

public record StudyGroupDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string InviteCode,
    Guid OwnerId,
    bool IsOwner,
    IReadOnlyList<GroupMemberDto> Members);

public record StudyGroupListItemDto(
    Guid Id,
    string Name,
    string? Description,
    int MemberCount,
    GroupRole MyRole);

// ---- FG-7: İçerik Paylaşımı ve Akış ----
public record ShareContentRequest(
    SharedContentType ContentType,
    Guid ContentId,
    PrivacyLevel Privacy,
    Guid? GroupId = null);

public record UpdateShareRequest(
    PrivacyLevel Privacy,
    Guid? GroupId = null);

public record UserSummaryDto(Guid Id, string DisplayName);

public record FeedItemDto(
    Guid ShareId,
    SharedContentType ContentType,
    Guid ContentId,
    string Title,
    UserSummaryDto Owner,
    PrivacyLevel Visibility,
    int ItemCount,
    DateTime CreatedAt);

public record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor);

public record CopyShareResultDto(
    Guid NewContentId,
    SharedContentType ContentType,
    string Message);

// ---- FG-7: Bildirimler ----
public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt);

// ---- FG-8: Şikâyet ve Moderasyon (Yönetim Paneli) ----
public record CreateReportRequest(Guid SharedContentId, string Reason);

public record ContentReportDto(
    Guid Id,
    Guid SharedContentId,
    string ContentTitle,
    SharedContentType ContentType,
    Guid ReporterId,
    string ReporterName,
    string Reason,
    ReportStatus Status,
    int ReportCount,
    DateTime CreatedAt);

public record ReviewReportRequest(ReportStatus Decision, string? Note = null);

public record UpdateUserStatusRequest(bool IsSuspended, int? SuspendDays = null, string? Reason = null);

public record AdminStatsDto(
    int TotalUsers,
    int ActiveUsers7Days,
    int TotalDocuments,
    int TotalSharedContents,
    int OpenReportsCount,
    int TotalTtsGenerated,
    int TotalQuizzesGenerated);
