using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Learning;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-6: Bilgi kartı destesi oluşturma, listeleme, güncelleme, silme ve seslendirme.</summary>
[ApiController]
[Authorize(Policy = "StudentOrAdmin")]
[Produces("application/json")]
public class FlashcardsController : ControllerBase
{
    private readonly IFlashcardService _flashcards;

    public FlashcardsController(IFlashcardService flashcards) => _flashcards = flashcards;

    /// <summary>Bölümden bilgi kartı destesi üretme.</summary>
    [HttpPost("api/v1/documents/{id:guid}/sections/{sid:guid}/flashcards")]
    [ProducesResponseType<FlashcardDeckDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDeck(
        Guid id, Guid sid,
        [FromBody] CreateFlashcardsRequest? request,
        CancellationToken ct)
    {
        var deck = await _flashcards.CreateFromSectionAsync(User.GetUserId(), id, sid, request, ct);
        return CreatedAtAction(nameof(GetDeck), new { deckId = deck.Id }, deck);
    }

    /// <summary>Kullanıcının destelerini listeleme.</summary>
    [HttpGet("api/v1/flashcard-decks")]
    [ProducesResponseType<IReadOnlyList<FlashcardDeckListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FlashcardDeckListItemDto>>> GetDecks(CancellationToken ct) =>
        Ok(await _flashcards.GetUserDecksAsync(User.GetUserId(), ct));

    /// <summary>Deste ve kartlarını görüntüleme.</summary>
    [HttpGet("api/v1/flashcard-decks/{deckId:guid}")]
    [ProducesResponseType<FlashcardDeckDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FlashcardDeckDto>> GetDeck(Guid deckId, CancellationToken ct) =>
        Ok(await _flashcards.GetDeckAsync(User.GetUserId(), deckId, ct));

    /// <summary>Kartın ön/arka yüzünü düzenleme.</summary>
    [HttpPut("api/v1/flashcards/{cardId:guid}")]
    [ProducesResponseType<FlashcardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FlashcardDto>> UpdateCard(
        Guid cardId, [FromBody] UpdateFlashcardRequest request, CancellationToken ct) =>
        Ok(await _flashcards.UpdateCardAsync(User.GetUserId(), cardId, request, ct));

    /// <summary>Desteyi silme.</summary>
    [HttpDelete("api/v1/flashcard-decks/{deckId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDeck(Guid deckId, CancellationToken ct)
    {
        await _flashcards.DeleteDeckAsync(User.GetUserId(), deckId, ct);
        return NoContent();
    }

    /// <summary>Kartın sesli hâli (ön / arka).</summary>
    [HttpGet("api/v1/flashcards/{cardId:guid}/audio")]
    [ProducesResponseType<AudioResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AudioResultDto>> GetCardAudio(
        Guid cardId,
        [FromQuery] string side = "front",
        [FromQuery] string? voice = null,
        [FromQuery] decimal? speed = null,
        CancellationToken ct = default) =>
        Ok(await _flashcards.SynthesizeCardAudioAsync(User.GetUserId(), cardId, side, voice, speed, ct));
}
