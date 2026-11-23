using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-7: İçerik paylaşımı, gizlilik güncelleme ve kütüphaneye kopyalama.</summary>
[ApiController]
[Authorize(Policy = "StudentOrAdmin")]
[Produces("application/json")]
public class SharesController : ControllerBase
{
    private readonly IShareService _shares;

    public SharesController(IShareService shares) => _shares = shares;

    /// <summary>İçeriği gizlilik düzeyiyle paylaşma.</summary>
    [HttpPost("api/v1/shares")]
    [ProducesResponseType<FeedItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Share([FromBody] ShareContentRequest request, CancellationToken ct)
    {
        var dto = await _shares.CreateShareAsync(User.GetUserId(), request, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>Gizlilik düzeyini değiştirme / geri alma.</summary>
    [HttpPut("api/v1/shares/{id:guid}")]
    [ProducesResponseType<FeedItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedItemDto>> UpdateShare(
        Guid id, [FromBody] UpdateShareRequest request, CancellationToken ct) =>
        Ok(await _shares.UpdateShareAsync(User.GetUserId(), id, request, ct));

    /// <summary>"Kütüphaneme ekle" ile kopyalama.</summary>
    [HttpPost("api/v1/shares/{id:guid}/copy")]
    [ProducesResponseType<CopyShareResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CopyShareResultDto>> Copy(Guid id, CancellationToken ct) =>
        Ok(await _shares.CopyShareAsync(User.GetUserId(), id, ct));
}
