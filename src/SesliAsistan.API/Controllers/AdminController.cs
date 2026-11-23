using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-8: Yönetim paneli ve moderasyon uç noktaları (yalnızca Yönetici rolü erişebilir).</summary>
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IModerationService _moderation;

    public AdminController(IModerationService moderation) => _moderation = moderation;

    /// <summary>Şikâyet kuyruğu (AdminOnly).</summary>
    [HttpGet("api/v1/admin/reports")]
    [ProducesResponseType<IReadOnlyList<ContentReportDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ContentReportDto>>> GetReports(CancellationToken ct) =>
        Ok(await _moderation.GetReportsAsync(User.GetUserId(), ct));

    /// <summary>Karar: kaldır / reddet.</summary>
    [HttpPut("api/v1/admin/reports/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewReport(
        Guid id, [FromBody] ReviewReportRequest request, CancellationToken ct)
    {
        await _moderation.ReviewReportAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    /// <summary>Kullanıcıyı askıya alma / açma.</summary>
    [HttpPut("api/v1/admin/users/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        await _moderation.UpdateUserStatusAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    /// <summary>Yönetim paneli genel istatistikleri.</summary>
    [HttpGet("api/v1/admin/stats")]
    [ProducesResponseType<AdminStatsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminStatsDto>> GetStats(CancellationToken ct) =>
        Ok(await _moderation.GetStatsAsync(User.GetUserId(), ct));
}
