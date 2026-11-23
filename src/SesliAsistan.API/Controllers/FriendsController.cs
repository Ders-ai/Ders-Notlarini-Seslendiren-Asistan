using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-7: Kullanıcı arama, arkadaşlık isteği gönderme, yanıtlama ve listeleme.</summary>
[ApiController]
[Authorize(Policy = "StudentOrAdmin")]
[Produces("application/json")]
public class FriendsController : ControllerBase
{
    private readonly IFriendshipService _friends;

    public FriendsController(IFriendshipService friends) => _friends = friends;

    /// <summary>Kullanıcı arama (e-posta gösterilmez).</summary>
    [HttpGet("api/v1/users/search")]
    [ProducesResponseType<IReadOnlyList<UserSearchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSearchDto>>> Search(
        [FromQuery] string q, CancellationToken ct) =>
        Ok(await _friends.SearchUsersAsync(User.GetUserId(), q, ct));

    /// <summary>Arkadaşlık isteği gönderme.</summary>
    [HttpPost("api/v1/friends/requests")]
    [ProducesResponseType<FriendDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequest request, CancellationToken ct)
    {
        var dto = await _friends.SendRequestAsync(User.GetUserId(), request.AddresseeId, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>İsteği kabul etme / reddetme.</summary>
    [HttpPut("api/v1/friends/requests/{id:guid}")]
    [ProducesResponseType<FriendDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FriendDto>> RespondRequest(
        Guid id,
        [FromBody] RespondFriendRequest request,
        CancellationToken ct) =>
        Ok(await _friends.RespondRequestAsync(User.GetUserId(), id, request.Accept, ct));

    /// <summary>Arkadaş listesi ve bekleyen istekler.</summary>
    [HttpGet("api/v1/friends")]
    [ProducesResponseType<FriendListDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FriendListDto>> GetFriends(CancellationToken ct) =>
        Ok(await _friends.GetFriendsAsync(User.GetUserId(), ct));

    /// <summary>Arkadaşlıktan çıkarma / engelleme.</summary>
    [HttpDelete("api/v1/friends/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(
        Guid userId,
        [FromQuery] bool block = false,
        CancellationToken ct = default)
    {
        await _friends.RemoveOrBlockFriendAsync(User.GetUserId(), userId, block);
        return NoContent();
    }
}
