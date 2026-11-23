using System.Text;
using Microsoft.EntityFrameworkCore;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;

namespace SesliAsistan.Business.Services;

public class FeedService : IFeedService
{
    private readonly ISharedContentRepository _shares;
    private readonly IFriendshipRepository _friendships;
    private readonly IStudyGroupRepository _groups;

    public FeedService(
        ISharedContentRepository shares,
        IFriendshipRepository friendships,
        IStudyGroupRepository groups)
    {
        _shares = shares;
        _friendships = friendships;
        _groups = groups;
    }

    public async Task<CursorPagedResult<FeedItemDto>> GetFeedAsync(Guid userId, string? cursor, int pageSize = 20, CancellationToken ct = default)
    {
        DateTime? cursorDate = null;
        Guid? cursorId = null;

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                var parts = decoded.Split('|');
                if (parts.Length == 2 && DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) && Guid.TryParse(parts[1], out var id))
                {
                    cursorDate = d;
                    cursorId = id;
                }
            }
            catch
            {
                // Geçersiz cursor durumunda ilk sayfadan başla
            }
        }

        var friendIds = await _friendships.GetFriendIdsAsync(userId, ct);
        var groupIds = await _groups.GetUserGroupIdsAsync(userId, ct);

        int limit = Math.Clamp(pageSize, 1, 50);
        var rawItems = await _shares.GetFeedAsync(userId, friendIds, groupIds, cursorDate, cursorId, limit + 1, ct);

        bool hasMore = rawItems.Count > limit;
        var itemsToReturn = hasMore ? rawItems.Take(limit).ToList() : rawItems.ToList();

        string? nextCursor = null;
        if (hasMore && itemsToReturn.Count > 0)
        {
            var last = itemsToReturn[^1];
            nextCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{last.CreatedAt:O}|{last.Id}"));
        }

        var dtos = itemsToReturn.Select(s => new FeedItemDto(
            s.Id,
            s.ContentType,
            s.ContentId,
            s.Title,
            new UserSummaryDto(s.OwnerId, s.Owner?.FullName ?? "Öğrenci"),
            s.Privacy,
            1,
            s.CreatedAt)).ToList();

        return new CursorPagedResult<FeedItemDto>(dtos, nextCursor);
    }
}
