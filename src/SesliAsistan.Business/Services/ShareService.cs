using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.Services;

public class ShareService : IShareService
{
    private readonly ISharedContentRepository _shares;
    private readonly IShareAccessHandler _accessHandler;
    private readonly ISummaryRepository _summaries;
    private readonly IFlashcardDeckRepository _decks;
    private readonly IQuizRepository _quizzes;
    private readonly IStudyGroupRepository _groups;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ShareService> _logger;

    public ShareService(
        ISharedContentRepository shares,
        IShareAccessHandler accessHandler,
        ISummaryRepository summaries,
        IFlashcardDeckRepository decks,
        IQuizRepository quizzes,
        IStudyGroupRepository groups,
        IUserRepository users,
        INotificationService notifications,
        IUnitOfWork uow,
        ILogger<ShareService> logger)
    {
        _shares = shares;
        _accessHandler = accessHandler;
        _summaries = summaries;
        _decks = decks;
        _quizzes = quizzes;
        _groups = groups;
        _users = users;
        _notifications = notifications;
        _uow = uow;
        _logger = logger;
    }

    public async Task<FeedItemDto> CreateShareAsync(Guid userId, ShareContentRequest request, CancellationToken ct = default)
    {
        string title = "Paylaşılan İçerik";
        int itemCount = 0;

        switch (request.ContentType)
        {
            case SharedContentType.Summary:
                var summary = await _summaries.GetByIdAsync(request.ContentId, ct);
                if (summary is null || summary.OwnerId != userId)
                    throw new NotFoundException("Paylaşılacak özet bulunamadı.");
                title = $"Özet ({summary.Length})";
                itemCount = 1;
                break;

            case SharedContentType.FlashcardDeck:
                var deck = await _decks.GetWithCardsAsync(request.ContentId, ct);
                if (deck is null || deck.OwnerId != userId)
                    throw new NotFoundException("Paylaşılacak deste bulunamadı.");
                title = deck.Title;
                itemCount = deck.Cards.Count;
                break;

            case SharedContentType.Quiz:
                var quiz = await _quizzes.GetWithQuestionsAsync(request.ContentId, ct);
                if (quiz is null || quiz.OwnerId != userId)
                    throw new NotFoundException("Paylaşılacak test bulunamadı.");
                title = quiz.Title;
                itemCount = quiz.Questions.Count;
                break;

            default:
                throw new BusinessValidationException("ContentType", "Geçersiz içerik türü.");
        }

        if (request.Privacy == PrivacyLevel.Group)
        {
            if (!request.GroupId.HasValue || !await _groups.IsMemberAsync(request.GroupId.Value, userId, ct))
                throw new BusinessValidationException("GroupId", "Grup paylaşımı için üyesi olduğunuz geçerli bir grup seçilmelidir.");
        }

        var share = await _shares.GetByContentAsync(request.ContentType, request.ContentId, ct);
        if (share is null)
        {
            share = new SharedContent
            {
                OwnerId = userId,
                ContentType = request.ContentType,
                ContentId = request.ContentId,
                Title = title,
                Privacy = request.Privacy,
                GroupId = request.GroupId
            };
            await _shares.AddAsync(share, ct);
        }
        else
        {
            share.Privacy = request.Privacy;
            share.GroupId = request.GroupId;
            share.Title = title;
            _shares.Update(share);
        }

        await _uow.SaveChangesAsync(ct);

        var owner = await _users.GetByIdAsync(userId, ct);
        return new FeedItemDto(
            share.Id,
            share.ContentType,
            share.ContentId,
            share.Title,
            new UserSummaryDto(userId, owner?.FullName ?? "Öğrenci"),
            share.Privacy,
            itemCount,
            share.CreatedAt);
    }

    public async Task<FeedItemDto> UpdateShareAsync(Guid userId, Guid shareId, UpdateShareRequest request, CancellationToken ct = default)
    {
        var share = await _shares.GetByIdAsync(shareId, ct);
        if (share is null || share.OwnerId != userId || share.IsRemoved)
            throw new NotFoundException("Paylaşım bulunamadı.");

        if (request.Privacy == PrivacyLevel.Group)
        {
            if (!request.GroupId.HasValue || !await _groups.IsMemberAsync(request.GroupId.Value, userId, ct))
                throw new BusinessValidationException("GroupId", "Geçerli bir çalışma grubu seçiniz.");
        }

        share.Privacy = request.Privacy;
        share.GroupId = request.GroupId;

        _shares.Update(share);
        await _uow.SaveChangesAsync(ct);

        var owner = await _users.GetByIdAsync(userId, ct);
        return new FeedItemDto(
            share.Id,
            share.ContentType,
            share.ContentId,
            share.Title,
            new UserSummaryDto(userId, owner?.FullName ?? "Öğrenci"),
            share.Privacy,
            1,
            share.CreatedAt);
    }

    public async Task<CopyShareResultDto> CopyShareAsync(Guid userId, Guid shareId, CancellationToken ct = default)
    {
        var share = await _shares.GetByIdAsync(shareId, ct);
        if (share is null || !await _accessHandler.CanAccessAsync(userId, share, ct))
            throw new NotFoundException("İçerik bulunamadı veya erişim yetkiniz yok.");

        Guid newId = Guid.Empty;

        switch (share.ContentType)
        {
            case SharedContentType.Summary:
                var summary = await _summaries.GetByIdAsync(share.ContentId, ct);
                if (summary is null) throw new NotFoundException("Özet bulunamadı.");

                var newSummary = new Summary
                {
                    DocumentId = summary.DocumentId,
                    OwnerId = userId,
                    Length = summary.Length,
                    Content = summary.Content,
                    Privacy = PrivacyLevel.Private
                };
                await _summaries.AddAsync(newSummary, ct);
                newId = newSummary.Id;
                break;

            case SharedContentType.FlashcardDeck:
                var deck = await _decks.GetWithCardsAsync(share.ContentId, ct);
                if (deck is null) throw new NotFoundException("Deste bulunamadı.");

                var newDeck = new FlashcardDeck
                {
                    DocumentId = deck.DocumentId,
                    OwnerId = userId,
                    Title = $"{deck.Title} (Kopya)",
                    Privacy = PrivacyLevel.Private
                };
                foreach (var c in deck.Cards)
                {
                    newDeck.Cards.Add(new Flashcard
                    {
                        DeckId = newDeck.Id,
                        Front = c.Front,
                        Back = c.Back,
                        OrderIndex = c.OrderIndex
                    });
                }
                await _decks.AddAsync(newDeck, ct);
                newId = newDeck.Id;
                break;

            case SharedContentType.Quiz:
                var quiz = await _quizzes.GetWithQuestionsAsync(share.ContentId, ct);
                if (quiz is null) throw new NotFoundException("Test bulunamadı.");

                var newQuiz = new Quiz
                {
                    DocumentId = quiz.DocumentId,
                    SectionId = quiz.SectionId,
                    OwnerId = userId,
                    Title = $"{quiz.Title} (Kopya)",
                    Status = QuizStatus.Ready,
                    Privacy = PrivacyLevel.Private
                };
                foreach (var q in quiz.Questions)
                {
                    newQuiz.Questions.Add(new QuizQuestion
                    {
                        QuizId = newQuiz.Id,
                        QuestionText = q.QuestionText,
                        OptionsJson = q.OptionsJson,
                        CorrectOptionIndex = q.CorrectOptionIndex,
                        Explanation = q.Explanation,
                        OrderIndex = q.OrderIndex
                    });
                }
                await _quizzes.AddAsync(newQuiz, ct);
                newId = newQuiz.Id;
                break;
        }

        await _uow.SaveChangesAsync(ct);
        return new CopyShareResultDto(newId, share.ContentType, "İçerik başarıyla kütüphanenize kopyalandı.");
    }
}
