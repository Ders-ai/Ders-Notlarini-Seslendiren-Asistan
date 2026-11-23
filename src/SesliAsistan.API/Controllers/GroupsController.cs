using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-7: Çalışma grubu oluşturma, davet koduyla katılma ve grup bilgisi.</summary>
[ApiController]
[Authorize(Policy = "StudentOrAdmin")]
[Produces("application/json")]
public class GroupsController : ControllerBase
{
    private readonly IStudyGroupService _groups;

    public GroupsController(IStudyGroupService groups) => _groups = groups;

    /// <summary>Çalışma grubu oluşturma.</summary>
    [HttpPost("api/v1/groups")]
    [ProducesResponseType<StudyGroupDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        var group = await _groups.CreateGroupAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
    }

    /// <summary>Davet koduyla gruba katılma.</summary>
    [HttpPost("api/v1/groups/join")]
    [ProducesResponseType<StudyGroupDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudyGroupDetailDto>> Join([FromBody] JoinGroupRequest request, CancellationToken ct) =>
        Ok(await _groups.JoinGroupAsync(User.GetUserId(), request, ct));

    /// <summary>Grup bilgisi, üyeler ve paylaşımlar.</summary>
    [HttpGet("api/v1/groups/{id:guid}")]
    [ProducesResponseType<StudyGroupDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyGroupDetailDto>> GetGroup(Guid id, CancellationToken ct) =>
        Ok(await _groups.GetGroupAsync(User.GetUserId(), id, ct));

    /// <summary>Üyeyi gruptan çıkarma / ayrılma.</summary>
    [HttpDelete("api/v1/groups/{id:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId, CancellationToken ct)
    {
        await _groups.RemoveMemberAsync(User.GetUserId(), id, memberId, ct);
        return NoContent();
    }
}
