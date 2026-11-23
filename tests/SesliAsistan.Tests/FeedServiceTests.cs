using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Services;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;
using Xunit;

namespace SesliAsistan.Tests;

public class FeedServiceTests
{
    private static FeedService CreateFeedService(AppDbContext db) =>
        new(new SharedContentRepository(db), new FriendshipRepository(db), new StudyGroupRepository(db));

    private static ShareService CreateShareService(AppDbContext db) =>
        new(
            new SharedContentRepository(db),
            new ShareAccessHandler(new UserRepository(db), new FriendshipRepository(db), new StudyGroupRepository(db)),
            new SummaryRepository(db),
            new FlashcardDeckRepository(db),
            new QuizRepository(db),
            new StudyGroupRepository(db),
            new UserRepository(db),
            new NotificationService(new NotificationRepository(db), new UnitOfWork(db), NullLogger<NotificationService>.Instance),
            new UnitOfWork(db),
            NullLogger<ShareService>.Instance);

    [Fact]
    public async Task GetFeedAsync_YalnizcaErisilebilirPaylasimlar_Listelenir()
    {
        using var db = TestHelpers.CreateDb();
        var me = new User { Email = "me@uni.edu.tr", FullName = "Ben", PasswordHash = "h" };
        var friend = new User { Email = "friend@uni.edu.tr", FullName = "Dost", PasswordHash = "h" };
        var stranger = new User { Email = "stranger@uni.edu.tr", FullName = "Yabancı", PasswordHash = "h" };
        await db.Users.AddRangeAsync(me, friend, stranger);
        await db.Friendships.AddAsync(new Friendship { Requester = me, Addressee = friend, Status = FriendshipStatus.Accepted });

        // 3 paylaşım: Public yabancıdan, Friends dosttan, Private yabancıdan
        var pubShare = new SharedContent { Owner = stranger, Title = "Genel Paylaşım", Privacy = PrivacyLevel.Public, CreatedAt = DateTime.UtcNow };
        var friendShare = new SharedContent { Owner = friend, Title = "Arkadaş Paylaşımı", Privacy = PrivacyLevel.Friends, CreatedAt = DateTime.UtcNow.AddMinutes(1) };
        var privShare = new SharedContent { Owner = stranger, Title = "Özel Yabancı Not", Privacy = PrivacyLevel.Private, CreatedAt = DateTime.UtcNow.AddMinutes(2) };

        await db.SharedContents.AddRangeAsync(pubShare, friendShare, privShare);
        await db.SaveChangesAsync();

        var feedService = CreateFeedService(db);
        var result = await feedService.GetFeedAsync(me.Id, null, pageSize: 10);

        result.Items.Should().Contain(i => i.ShareId == pubShare.Id);
        result.Items.Should().Contain(i => i.ShareId == friendShare.Id);
        result.Items.Should().NotContain(i => i.ShareId == privShare.Id);
    }

    [Fact]
    public async Task GetFeedAsync_CursorPagination_SonrakiSayfaImleciniUretir()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "page@uni.edu.tr", FullName = "Sayfalayıcı", PasswordHash = "h" };
        await db.Users.AddAsync(user);

        for (int i = 0; i < 5; i++)
        {
            db.SharedContents.Add(new SharedContent
            {
                Owner = user,
                Title = $"Paylaşım {i}",
                Privacy = PrivacyLevel.Public,
                CreatedAt = DateTime.UtcNow.AddSeconds(i)
            });
        }
        await db.SaveChangesAsync();

        var feedService = CreateFeedService(db);
        var page1 = await feedService.GetFeedAsync(user.Id, null, pageSize: 2);

        page1.Items.Should().HaveCount(2);
        page1.NextCursor.Should().NotBeNullOrEmpty();

        var page2 = await feedService.GetFeedAsync(user.Id, page1.NextCursor, pageSize: 2);
        page2.Items.Should().HaveCount(2);
        page2.Items.Select(i => i.ShareId).Should().NotIntersectWith(page1.Items.Select(i => i.ShareId));
    }

    [Fact]
    public async Task CopyShareAsync_KutuphanemeEkle_BagimsizIcerikKopyalar()
    {
        using var db = TestHelpers.CreateDb();
        var creator = new User { Email = "creator@uni.edu.tr", FullName = "Yazar", PasswordHash = "h" };
        var copier = new User { Email = "copier@uni.edu.tr", FullName = "Kopyalayan", PasswordHash = "h" };
        var doc = new Document { Title = "Doc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = creator };
        var deck = new FlashcardDeck { Document = doc, Owner = creator, Title = "Orijinal Deste" };
        deck.Cards.Add(new Flashcard { Front = "Ön", Back = "Arka" });

        await db.Users.AddRangeAsync(creator, copier);
        await db.Documents.AddAsync(doc);
        await db.FlashcardDecks.AddAsync(deck);

        var share = new SharedContent
        {
            Owner = creator,
            ContentType = SharedContentType.FlashcardDeck,
            ContentId = deck.Id,
            Title = "Orijinal Deste",
            Privacy = PrivacyLevel.Public
        };
        await db.SharedContents.AddAsync(share);
        await db.SaveChangesAsync();

        var shareService = CreateShareService(db);
        var copyResult = await shareService.CopyShareAsync(copier.Id, share.Id);

        copyResult.Should().NotBeNull();
        var copiedDeck = await db.FlashcardDecks.FindAsync(copyResult.NewContentId);
        copiedDeck.Should().NotBeNull();
        copiedDeck!.OwnerId.Should().Be(copier.Id);
        copiedDeck.Title.Should().Contain("Kopya");
    }

    [Fact]
    public async Task CopyShareAsync_OrijinalSilindiginde_KopyaEtkilenmez()
    {
        using var db = TestHelpers.CreateDb();
        var creator = new User { Email = "c1@uni.edu.tr", FullName = "C1", PasswordHash = "h" };
        var copier = new User { Email = "c2@uni.edu.tr", FullName = "C2", PasswordHash = "h" };
        var doc = new Document { Title = "Doc", OriginalFileName = "d.pdf", StoragePath = "p", Owner = creator };
        var deck = new FlashcardDeck { Document = doc, Owner = creator, Title = "Deste" };
        deck.Cards.Add(new Flashcard { Front = "F1", Back = "B1" });

        await db.Users.AddRangeAsync(creator, copier);
        await db.Documents.AddAsync(doc);
        await db.FlashcardDecks.AddAsync(deck);

        var share = new SharedContent
        {
            Owner = creator,
            ContentType = SharedContentType.FlashcardDeck,
            ContentId = deck.Id,
            Title = "Deste",
            Privacy = PrivacyLevel.Public
        };
        await db.SharedContents.AddAsync(share);
        await db.SaveChangesAsync();

        var shareService = CreateShareService(db);
        var copyResult = await shareService.CopyShareAsync(copier.Id, share.Id);

        // Orijinal deste siliniyor
        db.FlashcardDecks.Remove(deck);
        await db.SaveChangesAsync();

        // Kopyalanan deste hala erişilebilir olmalıdır
        var copiedDeck = await db.FlashcardDecks.FindAsync(copyResult.NewContentId);
        copiedDeck.Should().NotBeNull();
        copiedDeck!.OwnerId.Should().Be(copier.Id);
    }
}
