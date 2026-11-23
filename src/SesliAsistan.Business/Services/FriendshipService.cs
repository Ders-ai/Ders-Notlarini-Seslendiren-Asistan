using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.Services;

public class FriendshipService : IFriendshipService
{
    private readonly IFriendshipRepository _friendships;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FriendshipService> _logger;

    public FriendshipService(
        IFriendshipRepository friendships,
        IUserRepository users,
        INotificationService notifications,
        IUnitOfWork uow,
        ILogger<FriendshipService> logger)
    {
        _friendships = friendships;
        _users = users;
        _notifications = notifications;
        _uow = uow;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserSearchDto>> SearchUsersAsync(Guid currentUserId, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<UserSearchDto>();

        var clean = query.Trim().ToLowerInvariant();

        // GÜVENLİK: Kullanıcı arama sonuçlarında e-posta adresi döndürülmez (OWASP gizlilik kuralı)
        var users = await _users.Query()
            .AsNoTracking()
            .Where(u => u.Id != currentUserId && !u.IsDeleted && u.IsActive && !u.IsSuspended)
            .Where(u => u.FullName.ToLower().Contains(clean) || u.Email.ToLower().StartsWith(clean))
            .Take(20)
            .Select(u => new UserSearchDto(u.Id, u.FullName))
            .ToListAsync(ct);

        return users;
    }

    public async Task<FriendDto> SendRequestAsync(Guid currentUserId, Guid addresseeId, CancellationToken ct = default)
    {
        if (currentUserId == addresseeId)
            throw new BusinessValidationException("Friendship", "Kullanıcı kendisine arkadaşlık isteği gönderemez.");

        var addressee = await _users.GetByIdAsync(addresseeId, ct);
        if (addressee is null || addressee.IsDeleted || !addressee.IsActive)
            throw new NotFoundException("Kullanıcı bulunamadı.");

        var existing = await _friendships.FindRelationshipAsync(currentUserId, addresseeId, ct);

        if (existing is not null)
        {
            if (existing.Status == FriendshipStatus.Blocked)
                throw new BusinessValidationException("Friendship", "Bu kullanıcıya arkadaşlık isteği gönderilemez.");

            if (existing.Status == FriendshipStatus.Accepted)
                throw new ConflictException("Bu kullanıcıyla zaten arkadaşsınız.");

            // Karşılıklı istek gönderildiğinde ilişki otomatik olarak kabul edilir
            if (existing.RequesterId == addresseeId && existing.Status == FriendshipStatus.Pending)
            {
                existing.Status = FriendshipStatus.Accepted;
                await _uow.SaveChangesAsync(ct);

                var currentUser = await _users.GetByIdAsync(currentUserId, ct);
                await _notifications.SendNotificationAsync(
                    addresseeId,
                    "Arkadaşlık İsteği Kabul Edildi",
                    $"{currentUser?.FullName ?? "Bir kullanıcı"} ile artık arkadaşsınız.",
                    ct);

                return new FriendDto(existing.Id, addressee.Id, addressee.FullName, existing.Status, false, existing.CreatedAt);
            }

            if (existing.RequesterId == currentUserId && existing.Status == FriendshipStatus.Pending)
                throw new ConflictException("Zaten bekleyen bir arkadaşlık isteğiniz bulunmaktadır.");
        }

        var friendship = new Friendship
        {
            RequesterId = currentUserId,
            AddresseeId = addresseeId,
            Status = FriendshipStatus.Pending
        };

        await _friendships.AddAsync(friendship, ct);
        await _uow.SaveChangesAsync(ct);

        var sender = await _users.GetByIdAsync(currentUserId, ct);
        await _notifications.SendNotificationAsync(
            addresseeId,
            "Yeni Arkadaşlık İsteği",
            $"{sender?.FullName ?? "Bir kullanıcı"} size arkadaşlık isteği gönderdi.",
            ct);

        return new FriendDto(friendship.Id, addressee.Id, addressee.FullName, friendship.Status, true, friendship.CreatedAt);
    }

    public async Task<FriendDto> RespondRequestAsync(Guid currentUserId, Guid friendshipId, bool accept, CancellationToken ct = default)
    {
        var friendship = await _friendships.Query()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        if (friendship is null || friendship.AddresseeId != currentUserId)
            throw new NotFoundException("Arkadaşlık isteği bulunamadı.");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new ConflictException("Bu istek daha önce yanıtlanmış.");

        friendship.Status = accept ? FriendshipStatus.Accepted : FriendshipStatus.Rejected;
        await _uow.SaveChangesAsync(ct);

        if (accept)
        {
            await _notifications.SendNotificationAsync(
                friendship.RequesterId,
                "Arkadaşlık İsteği Kabul Edildi",
                $"{friendship.Addressee.FullName} arkadaşlık isteğinizi kabul etti.",
                ct);
        }

        return new FriendDto(
            friendship.Id,
            friendship.RequesterId,
            friendship.Requester.FullName,
            friendship.Status,
            false,
            friendship.CreatedAt);
    }

    public async Task<FriendListDto> GetFriendsAsync(Guid currentUserId, CancellationToken ct = default)
    {
        var list = await _friendships.GetFriendsAndPendingAsync(currentUserId, ct);

        var friends = new List<FriendDto>();
        var incoming = new List<FriendDto>();
        var outgoing = new List<FriendDto>();

        foreach (var f in list)
        {
            bool isMeRequester = f.RequesterId == currentUserId;
            var otherUser = isMeRequester ? f.Addressee : f.Requester;

            var dto = new FriendDto(
                f.Id,
                otherUser.Id,
                otherUser.FullName,
                f.Status,
                isMeRequester,
                f.CreatedAt);

            if (f.Status == FriendshipStatus.Accepted)
            {
                friends.Add(dto);
            }
            else if (f.Status == FriendshipStatus.Pending)
            {
                if (isMeRequester) outgoing.Add(dto);
                else incoming.Add(dto);
            }
        }

        return new FriendListDto(friends, incoming, outgoing);
    }

    public async Task RemoveOrBlockFriendAsync(Guid currentUserId, Guid targetUserId, bool block = false, CancellationToken ct = default)
    {
        var friendship = await _friendships.FindRelationshipAsync(currentUserId, targetUserId, ct);
        if (friendship is null)
            throw new NotFoundException("Arkadaşlık ilişkisi bulunamadı.");

        if (block)
        {
            friendship.Status = FriendshipStatus.Blocked;
            _friendships.Update(friendship);
        }
        else
        {
            _friendships.Remove(friendship);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
