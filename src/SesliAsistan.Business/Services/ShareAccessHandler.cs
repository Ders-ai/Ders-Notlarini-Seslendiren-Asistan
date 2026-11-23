using Microsoft.EntityFrameworkCore;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.Services;

public class ShareAccessHandler : IShareAccessHandler
{
    private readonly IUserRepository _users;
    private readonly IFriendshipRepository _friendships;
    private readonly IStudyGroupRepository _groups;

    public ShareAccessHandler(
        IUserRepository users,
        IFriendshipRepository friendships,
        IStudyGroupRepository groups)
    {
        _users = users;
        _friendships = friendships;
        _groups = groups;
    }

    public async Task<bool> CanAccessAsync(Guid userId, SharedContent share, CancellationToken ct = default)
    {
        if (share is null) return false;

        // Kaldırılmış içeriklere hiç kimse erişemez
        if (share.IsRemoved) return false;

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null || !user.IsActive || user.IsSuspended) return false;

        bool isOwner = share.OwnerId == userId;
        bool isAdmin = user.Role == UserRole.Admin;

        // İncelemedeki içeriklere yalnızca sahibi ve yönetici erişebilir
        if (share.IsUnderReview && !isOwner && !isAdmin)
            return false;

        // Sahip veya yönetici her zaman erişebilir
        if (isOwner || isAdmin)
            return true;

        return share.Privacy switch
        {
            PrivacyLevel.Public => true,
            PrivacyLevel.Friends => await _friendships.AreFriendsAsync(userId, share.OwnerId, ct),
            PrivacyLevel.Group => share.GroupId.HasValue && await _groups.IsMemberAsync(share.GroupId.Value, userId, ct),
            PrivacyLevel.Private => false,
            _ => false
        };
    }
}
