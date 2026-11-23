using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-8: İçerik şikâyet etme.</summary>
[ApiController]
[Authorize(Policy = "StudentOrAdmin")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IModerationService _moderation;

    public ReportsController(IModerationService moderation) => _moderation = moderation;

    /// <summary>İçerik şikâyet etme.</summary>
    [HttpPost("api/v1/reports")]
    [ProducesResponseType<ContentReportDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Report([FromBody] CreateReportRequest request, CancellationToken ct)
    {
        var dto = await _moderation.ReportContentAsync(User.GetUserId(), request, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }
}
