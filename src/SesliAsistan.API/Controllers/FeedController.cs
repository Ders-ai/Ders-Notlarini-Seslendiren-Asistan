using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-7: Sosyal paylaşım akışı (imleç tabanlı sayfalama).</summary>
[ApiController]
[Authorize(Policy = "StudentOrAdmin")]
[Produces("application/json")]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feed;

    public FeedController(IFeedService feed) => _feed = feed;

    /// <summary>Paylaşım akışı (imleç tabanlı sayfalama).</summary>
    [HttpGet("api/v1/feed")]
    [ProducesResponseType<CursorPagedResult<FeedItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPagedResult<FeedItemDto>>> GetFeed(
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _feed.GetFeedAsync(User.GetUserId(), cursor, pageSize, ct));
}
