using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SesliAsistan.Business.DTOs.Learning;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.Business.Speech;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.Services;

public class FlashcardService : IFlashcardService
{
    private readonly IFlashcardDeckRepository _decks;
    private readonly IGenericRepository<Flashcard> _cards;
    private readonly IDocumentRepository _documents;
    private readonly IGenericRepository<DocumentSection> _sections;
    private readonly IKeyTermExtractor _termExtractor;
    private readonly IDefinitionMatcher _definitionMatcher;
    private readonly ITtsService _ttsService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FlashcardService> _logger;

    public FlashcardService(
        IFlashcardDeckRepository decks,
        IGenericRepository<Flashcard> cards,
        IDocumentRepository documents,
        IGenericRepository<DocumentSection> sections,
        IKeyTermExtractor termExtractor,
        IDefinitionMatcher definitionMatcher,
        ITtsService ttsService,
        IUnitOfWork uow,
        ILogger<FlashcardService> logger)
    {
        _decks = decks;
        _cards = cards;
        _documents = documents;
        _sections = sections;
        _termExtractor = termExtractor;
        _definitionMatcher = definitionMatcher;
        _ttsService = ttsService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<FlashcardDeckDto> CreateFromSectionAsync(
        Guid userId,
        Guid documentId,
        Guid sectionId,
        CreateFlashcardsRequest? request,
        CancellationToken ct = default)
    {
        var doc = await _documents.GetByIdAsync(documentId, ct);
        if (doc is null || doc.OwnerId != userId)
            throw new NotFoundException("Belge bulunamadı.");

        var section = await _sections.Query().FirstOrDefaultAsync(s => s.Id == sectionId && s.DocumentId == documentId, ct);
        if (section is null)
            throw new NotFoundException("Bölüm bulunamadı.");

        var text = section.Content ?? string.Empty;
        var terms = _termExtractor.ExtractKeyTerms(text, section.Title);

        var deckTitle = !string.IsNullOrWhiteSpace(request?.Title)
            ? request.Title.Trim()
            : $"{section.Title} - Bilgi Kartları";

        var deck = new FlashcardDeck
        {
            DocumentId = documentId,
            OwnerId = userId,
            Title = deckTitle,
            Privacy = PrivacyLevel.Private
        };

        int order = 0;
        foreach (var term in terms)
        {
            var def = _definitionMatcher.FindDefinition(term.Term, text) ?? $"{term.Term} kavramı bu bölümde açıklanmıştır.";
            deck.Cards.Add(new Flashcard
            {
                DeckId = deck.Id,
                Front = term.Term,
                Back = def,
                OrderIndex = order++
            });
        }

        // Eğer hiç terim çıkmadıysa en azından başlık için bir kart oluştur
        if (deck.Cards.Count == 0)
        {
            deck.Cards.Add(new Flashcard
            {
                DeckId = deck.Id,
                Front = section.Title,
                Back = text.Length > 200 ? text[..200] + "..." : text,
                OrderIndex = 0
            });
        }

        await _decks.AddAsync(deck, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Kullanıcı {UserId} için {Count} kartlı deste oluşturuldu.", userId, deck.Cards.Count);

        return new FlashcardDeckDto(
            deck.Id,
            deck.DocumentId,
            sectionId,
            deck.Title,
            deck.Privacy,
            deck.CreatedAt,
            deck.Cards.Select(c => new FlashcardDto(c.Id, c.Front, c.Back, c.OrderIndex)).ToList());
    }

    public async Task<IReadOnlyList<FlashcardDeckListItemDto>> GetUserDecksAsync(Guid userId, CancellationToken ct = default)
    {
        var decks = await _decks.GetByOwnerAsync(userId, ct);
        return decks.Select(d => new FlashcardDeckListItemDto(
            d.Id,
            d.DocumentId,
            d.Title,
            d.Cards.Count,
            d.Privacy,
            d.CreatedAt)).ToList();
    }

    public async Task<FlashcardDeckDto> GetDeckAsync(Guid userId, Guid deckId, CancellationToken ct = default)
    {
        var deck = await _decks.GetWithCardsAsync(deckId, ct);
        if (deck is null || deck.OwnerId != userId)
            throw new NotFoundException("Deste bulunamadı.");

        return new FlashcardDeckDto(
            deck.Id,
            deck.DocumentId,
            null,
            deck.Title,
            deck.Privacy,
            deck.CreatedAt,
            deck.Cards.Select(c => new FlashcardDto(c.Id, c.Front, c.Back, c.OrderIndex)).ToList());
    }

    public async Task<FlashcardDto> UpdateCardAsync(Guid userId, Guid cardId, UpdateFlashcardRequest request, CancellationToken ct = default)
    {
        var card = await _cards.Query().Include(c => c.Deck).FirstOrDefaultAsync(c => c.Id == cardId, ct);
        if (card is null || card.Deck.OwnerId != userId)
            throw new NotFoundException("Kart bulunamadı.");

        if (string.IsNullOrWhiteSpace(request.Front) || string.IsNullOrWhiteSpace(request.Back))
            throw new BusinessValidationException("Card", "Kart ön ve arka yüzü boş olamaz.");

        card.Front = request.Front.Trim();
        card.Back = request.Back.Trim();

        _cards.Update(card);
        await _uow.SaveChangesAsync(ct);

        return new FlashcardDto(card.Id, card.Front, card.Back, card.OrderIndex);
    }

    public async Task DeleteDeckAsync(Guid userId, Guid deckId, CancellationToken ct = default)
    {
        var deck = await _decks.GetByIdAsync(deckId, ct);
        if (deck is null || deck.OwnerId != userId)
            throw new NotFoundException("Deste bulunamadı.");

        _decks.Remove(deck);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<AudioResultDto> SynthesizeCardAudioAsync(
        Guid userId,
        Guid cardId,
        string side,
        string? voice,
        decimal? speed,
        CancellationToken ct = default)
    {
        var card = await _cards.Query().Include(c => c.Deck).FirstOrDefaultAsync(c => c.Id == cardId, ct);
        if (card is null || card.Deck.OwnerId != userId)
            throw new NotFoundException("Kart bulunamadı.");

        var textToRead = side.Equals("back", StringComparison.OrdinalIgnoreCase) ? card.Back : card.Front;

        // Kart metnini bölüm gibi sentezle
        return await _ttsService.SynthesizeSectionAudioAsync(
            userId, card.Deck.DocumentId, cardId, voice, speed, ct);
    }
}
