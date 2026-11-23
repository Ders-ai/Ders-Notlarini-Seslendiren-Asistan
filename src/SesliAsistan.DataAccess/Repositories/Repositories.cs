using Microsoft.EntityFrameworkCore;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.Entities.Common;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.DataAccess.Repositories;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    IQueryable<T> Query();
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> Set;

    public GenericRepository(AppDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(x => x.Id == id, ct);

    public IQueryable<T> Query() => Set.AsQueryable();

    public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
}

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        Set.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct);
}

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
}

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context) { }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        Set.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
}

public interface IDocumentRepository : IGenericRepository<Document>
{
    Task<(IReadOnlyList<Document> Items, int Total)> GetPagedByOwnerAsync(
        Guid ownerId, int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Document?> GetWithSectionsAsync(Guid id, CancellationToken ct = default);
}

public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Document> Items, int Total)> GetPagedByOwnerAsync(
        Guid ownerId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Where(d => d.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Document?> GetWithSectionsAsync(Guid id, CancellationToken ct = default) =>
        Set.AsNoTracking()
           .Include(d => d.Sections.OrderBy(s => s.OrderIndex))
           .FirstOrDefaultAsync(d => d.Id == id, ct);
}

public interface IPasswordResetCodeRepository : IGenericRepository<PasswordResetCode>
{
    Task<PasswordResetCode?> GetLatestValidCodeAsync(Guid userId, CancellationToken ct = default);
    Task InvalidateUserCodesAsync(Guid userId, CancellationToken ct = default);
}

public class PasswordResetCodeRepository : GenericRepository<PasswordResetCode>, IPasswordResetCodeRepository
{
    public PasswordResetCodeRepository(AppDbContext context) : base(context) { }

    public Task<PasswordResetCode?> GetLatestValidCodeAsync(Guid userId, CancellationToken ct = default) =>
        Set.Where(c => c.UserId == userId && !c.IsUsed && c.FailedAttempts < 5 && c.ExpiresAt > DateTime.UtcNow)
           .OrderByDescending(c => c.CreatedAt)
           .FirstOrDefaultAsync(ct);

    public async Task InvalidateUserCodesAsync(Guid userId, CancellationToken ct = default)
    {
        var activeCodes = await Set.Where(c => c.UserId == userId && !c.IsUsed).ToListAsync(ct);
        foreach (var code in activeCodes)
        {
            code.IsUsed = true;
        }
    }
}

public interface IAudioAssetRepository : IGenericRepository<AudioAsset>
{
    Task<AudioAsset?> FindByHashAsync(string contentHash, string voice, decimal speed, CancellationToken ct = default);
}

public class AudioAssetRepository : GenericRepository<AudioAsset>, IAudioAssetRepository
{
    public AudioAssetRepository(AppDbContext context) : base(context) { }

    public Task<AudioAsset?> FindByHashAsync(string contentHash, string voice, decimal speed, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(a => a.ContentHash == contentHash && a.Voice == voice && a.Speed == speed, ct);
}

public interface IListeningProgressRepository : IGenericRepository<ListeningProgress>
{
    Task<ListeningProgress?> GetByDocumentAsync(Guid userId, Guid documentId, CancellationToken ct = default);
}

public class ListeningProgressRepository : GenericRepository<ListeningProgress>, IListeningProgressRepository
{
    public ListeningProgressRepository(AppDbContext context) : base(context) { }

    public Task<ListeningProgress?> GetByDocumentAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(p => p.UserId == userId && p.DocumentId == documentId, ct);
}

public interface ISummaryRepository : IGenericRepository<Summary>
{
    Task<Summary?> GetBySectionAsync(Guid documentId, Guid sectionId, SummaryLength length, CancellationToken ct = default);
}

public class SummaryRepository : GenericRepository<Summary>, ISummaryRepository
{
    public SummaryRepository(AppDbContext context) : base(context) { }

    public Task<Summary?> GetBySectionAsync(Guid documentId, Guid sectionId, SummaryLength length, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(s => s.DocumentId == documentId && s.Length == length, ct);
}

// ---- FG-6: Bilgi Kartı ve Test Repositories ----

public interface IFlashcardDeckRepository : IGenericRepository<FlashcardDeck>
{
    Task<FlashcardDeck?> GetWithCardsAsync(Guid deckId, CancellationToken ct = default);
    Task<IReadOnlyList<FlashcardDeck>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
}

public class FlashcardDeckRepository : GenericRepository<FlashcardDeck>, IFlashcardDeckRepository
{
    public FlashcardDeckRepository(AppDbContext context) : base(context) { }

    public Task<FlashcardDeck?> GetWithCardsAsync(Guid deckId, CancellationToken ct = default) =>
        Set.Include(d => d.Cards.OrderBy(c => c.OrderIndex))
           .FirstOrDefaultAsync(d => d.Id == deckId, ct);

    public async Task<IReadOnlyList<FlashcardDeck>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Where(d => d.OwnerId == ownerId)
                 .Include(d => d.Cards)
                 .OrderByDescending(d => d.CreatedAt)
                 .ToListAsync(ct);
}

public interface IQuizRepository : IGenericRepository<Quiz>
{
    Task<Quiz?> GetWithQuestionsAsync(Guid quizId, CancellationToken ct = default);
    Task<IReadOnlyList<Quiz>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<int> GetDailyCountAsync(Guid userId, DateTime dateUtc, CancellationToken ct = default);
}

public class QuizRepository : GenericRepository<Quiz>, IQuizRepository
{
    public QuizRepository(AppDbContext context) : base(context) { }

    public Task<Quiz?> GetWithQuestionsAsync(Guid quizId, CancellationToken ct = default) =>
        Set.Include(q => q.Questions.OrderBy(x => x.OrderIndex))
           .FirstOrDefaultAsync(q => q.Id == quizId, ct);

    public async Task<IReadOnlyList<Quiz>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Where(q => q.OwnerId == ownerId)
                 .Include(q => q.Questions)
                 .OrderByDescending(q => q.CreatedAt)
                 .ToListAsync(ct);

    public Task<int> GetDailyCountAsync(Guid userId, DateTime dateUtc, CancellationToken ct = default)
    {
        var startOfDay = dateUtc.Date;
        var endOfDay = startOfDay.AddDays(1);
        return Set.CountAsync(q => q.OwnerId == userId && q.CreatedAt >= startOfDay && q.CreatedAt < endOfDay, ct);
    }
}

public interface IQuizAttemptRepository : IGenericRepository<QuizAttempt>
{
    Task<IReadOnlyList<QuizAttempt>> GetHistoryByQuizAsync(Guid quizId, Guid userId, CancellationToken ct = default);
}

public class QuizAttemptRepository : GenericRepository<QuizAttempt>, IQuizAttemptRepository
{
    public QuizAttemptRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<QuizAttempt>> GetHistoryByQuizAsync(Guid quizId, Guid userId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Where(a => a.QuizId == quizId && a.UserId == userId)
                 .OrderByDescending(a => a.CompletedAt)
                 .ToListAsync(ct);
}

// ---- FG-7: İşbirlikçi Ağ Repositories ----

public interface IFriendshipRepository : IGenericRepository<Friendship>
{
    Task<Friendship?> FindRelationshipAsync(Guid userA, Guid userB, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetFriendIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Friendship>> GetFriendsAndPendingAsync(Guid userId, CancellationToken ct = default);
    Task<bool> AreFriendsAsync(Guid userA, Guid userB, CancellationToken ct = default);
}

public class FriendshipRepository : GenericRepository<Friendship>, IFriendshipRepository
{
    public FriendshipRepository(AppDbContext context) : base(context) { }

    public Task<Friendship?> FindRelationshipAsync(Guid userA, Guid userB, CancellationToken ct = default) =>
        Set.Include(f => f.Requester)
           .Include(f => f.Addressee)
           .FirstOrDefaultAsync(f =>
                (f.RequesterId == userA && f.AddresseeId == userB) ||
                (f.RequesterId == userB && f.AddresseeId == userA), ct);

    public async Task<IReadOnlyList<Guid>> GetFriendIdsAsync(Guid userId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Where(f => f.Status == FriendshipStatus.Accepted && (f.RequesterId == userId || f.AddresseeId == userId))
                 .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                 .ToListAsync(ct);

    public async Task<IReadOnlyList<Friendship>> GetFriendsAndPendingAsync(Guid userId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Include(f => f.Requester)
                 .Include(f => f.Addressee)
                 .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                 .ToListAsync(ct);

    public Task<bool> AreFriendsAsync(Guid userA, Guid userB, CancellationToken ct = default) =>
        Set.AnyAsync(f => f.Status == FriendshipStatus.Accepted &&
            ((f.RequesterId == userA && f.AddresseeId == userB) || (f.RequesterId == userB && f.AddresseeId == userA)), ct);
}

public interface IStudyGroupRepository : IGenericRepository<StudyGroup>
{
    Task<StudyGroup?> GetByInviteCodeAsync(string code, CancellationToken ct = default);
    Task<StudyGroup?> GetWithMembersAsync(Guid groupId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetUserGroupIdsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyGroup>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
}

public class StudyGroupRepository : GenericRepository<StudyGroup>, IStudyGroupRepository
{
    public StudyGroupRepository(AppDbContext context) : base(context) { }

    public Task<StudyGroup?> GetByInviteCodeAsync(string code, CancellationToken ct = default) =>
        Set.Include(g => g.Members)
           .FirstOrDefaultAsync(g => g.InviteCode == code, ct);

    public Task<StudyGroup?> GetWithMembersAsync(Guid groupId, CancellationToken ct = default) =>
        Set.Include(g => g.Members).ThenInclude(m => m.User)
           .FirstOrDefaultAsync(g => g.Id == groupId, ct);

    public async Task<IReadOnlyList<Guid>> GetUserGroupIdsAsync(Guid userId, CancellationToken ct = default) =>
        await Context.GroupMembers.AsNoTracking()
                     .Where(m => m.UserId == userId)
                     .Select(m => m.GroupId)
                     .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyGroup>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Where(g => g.Members.Any(m => m.UserId == userId))
                 .Include(g => g.Members)
                 .ToListAsync(ct);

    public Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) =>
        Context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, ct);
}

public interface ISharedContentRepository : IGenericRepository<SharedContent>
{
    Task<SharedContent?> GetByContentAsync(SharedContentType type, Guid contentId, CancellationToken ct = default);
    Task<IReadOnlyList<SharedContent>> GetFeedAsync(
        Guid userId,
        IReadOnlyList<Guid> friendIds,
        IReadOnlyList<Guid> groupIds,
        DateTime? cursorDate,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default);
}

public class SharedContentRepository : GenericRepository<SharedContent>, ISharedContentRepository
{
    public SharedContentRepository(AppDbContext context) : base(context) { }

    public Task<SharedContent?> GetByContentAsync(SharedContentType type, Guid contentId, CancellationToken ct = default) =>
        Set.Include(s => s.Owner)
           .Include(s => s.Group)
           .FirstOrDefaultAsync(s => s.ContentType == type && s.ContentId == contentId && !s.IsRemoved, ct);

    public async Task<IReadOnlyList<SharedContent>> GetFeedAsync(
        Guid userId,
        IReadOnlyList<Guid> friendIds,
        IReadOnlyList<Guid> groupIds,
        DateTime? cursorDate,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default)
    {
        // Yetki filtresi: Public HERKESE, Friends ARKADAŞLARA, Group GRUP ÜYELERİNE, Private SADECE SAHİBİNE
        var query = Set.AsNoTracking()
            .Include(s => s.Owner)
            .Where(s => !s.IsRemoved && !s.IsUnderReview)
            .Where(s =>
                s.OwnerId == userId ||
                s.Privacy == PrivacyLevel.Public ||
                (s.Privacy == PrivacyLevel.Friends && friendIds.Contains(s.OwnerId)) ||
                (s.Privacy == PrivacyLevel.Group && s.GroupId.HasValue && groupIds.Contains(s.GroupId.Value)));

        // İmleç tabanlı sayfalama (cursor-based pagination)
        if (cursorDate.HasValue && cursorId.HasValue)
        {
            query = query.Where(s => s.CreatedAt < cursorDate.Value ||
                                    (s.CreatedAt == cursorDate.Value && s.Id.CompareTo(cursorId.Value) < 0));
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Take(limit)
            .ToListAsync(ct);
    }
}

// ---- FG-8: Şikâyet ve Moderasyon Repositories ----

public interface IContentReportRepository : IGenericRepository<ContentReport>
{
    Task<IReadOnlyList<ContentReport>> GetOpenReportsAsync(CancellationToken ct = default);
    Task<int> CountRecentReportsByUserAsync(Guid userId, DateTime since, CancellationToken ct = default);
    Task<int> GetReportCountForContentAsync(Guid sharedContentId, CancellationToken ct = default);
    Task<bool> HasUserReportedAsync(Guid userId, Guid sharedContentId, CancellationToken ct = default);
}

public class ContentReportRepository : GenericRepository<ContentReport>, IContentReportRepository
{
    public ContentReportRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ContentReport>> GetOpenReportsAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Where(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.UnderReview)
                 .Include(r => r.Reporter)
                 .Include(r => r.SharedContent)
                 .OrderByDescending(r => r.CreatedAt)
                 .ToListAsync(ct);

    public Task<int> CountRecentReportsByUserAsync(Guid userId, DateTime since, CancellationToken ct = default) =>
        Set.CountAsync(r => r.ReporterId == userId && r.CreatedAt >= since, ct);

    public Task<int> GetReportCountForContentAsync(Guid sharedContentId, CancellationToken ct = default) =>
        Set.CountAsync(r => r.SharedContentId == sharedContentId, ct);

    public Task<bool> HasUserReportedAsync(Guid userId, Guid sharedContentId, CancellationToken ct = default) =>
        Set.AnyAsync(r => r.ReporterId == userId && r.SharedContentId == sharedContentId, ct);
}

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId, int limit = 50, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId, int limit = 50, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Where(n => n.UserId == userId)
                 .OrderByDescending(n => n.CreatedAt)
                 .Take(limit)
                 .ToListAsync(ct);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await Set.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var n in unread)
        {
            n.IsRead = true;
        }
    }
}

public interface IAdminActionRepository : IGenericRepository<AdminAction>
{
}

public class AdminActionRepository : GenericRepository<AdminAction>, IAdminActionRepository
{
    public AdminActionRepository(AppDbContext context) : base(context) { }
}


/// <summary>Birden fazla repository üzerindeki değişiklikleri tek işlemde kaydeder.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public UnitOfWork(AppDbContext context) => _context = context;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
