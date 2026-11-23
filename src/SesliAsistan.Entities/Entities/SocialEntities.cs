using SesliAsistan.Entities.Common;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Entities.Entities;

/// <summary>FG-7: Arkadaşlık ilişkisi (Kullanıcı ↔ Kullanıcı, çoka-çok).</summary>
public class Friendship : BaseEntity
{
    public Guid RequesterId { get; set; }
    public User Requester { get; set; } = null!;
    public Guid AddresseeId { get; set; }
    public User Addressee { get; set; } = null!;
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
}

/// <summary>FG-7: Çalışma grubu.</summary>
public class StudyGroup : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string InviteCode { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}

public class GroupMember : BaseEntity
{
    public Guid GroupId { get; set; }
    public StudyGroup Group { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public GroupRole Role { get; set; } = GroupRole.Member;
}

/// <summary>FG-7: Paylaşılan içerik (özet / kart destesi / test) — çok biçimli (polimorfik) referans.</summary>
public class SharedContent : BaseEntity
{
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public string Title { get; set; } = null!;
    public SharedContentType ContentType { get; set; }
    public Guid ContentId { get; set; }
    public PrivacyLevel Privacy { get; set; }
    public Guid? GroupId { get; set; }
    public StudyGroup? Group { get; set; }
    public bool IsUnderReview { get; set; }
    public bool IsRemoved { get; set; }
    public int ReportCount { get; set; }
}

/// <summary>FG-8: Şikâyet kaydı (yönetim paneli).</summary>
public class ContentReport : BaseEntity
{
    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;
    public Guid SharedContentId { get; set; }
    public SharedContent SharedContent { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public Guid? ReviewedById { get; set; }
}

/// <summary>FG-7: Bildirim.</summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
}

/// <summary>FG-8: Yönetici moderasyon denetim kaydı (audit log).</summary>
public class AdminAction : BaseEntity
{
    public Guid AdminId { get; set; }
    public User Admin { get; set; } = null!;
    public AdminActionType ActionType { get; set; }
    public string TargetType { get; set; } = null!; // "SharedContent", "User"
    public Guid TargetId { get; set; }
    public string? Reason { get; set; }
}
