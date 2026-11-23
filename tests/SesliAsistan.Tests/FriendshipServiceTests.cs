using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Services;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;
using Xunit;

namespace SesliAsistan.Tests;

public class FriendshipServiceTests
{
    private static FriendshipService CreateService(AppDbContext db) =>
        new(
            new FriendshipRepository(db),
            new UserRepository(db),
            new NotificationService(new NotificationRepository(db), new UnitOfWork(db), NullLogger<NotificationService>.Instance),
            new UnitOfWork(db),
            NullLogger<FriendshipService>.Instance);

    [Fact]
    public async Task SendRequestAsync_GecerliIstek_PendingIstekOlusturur()
    {
        using var db = TestHelpers.CreateDb();
        var u1 = new User { Email = "u1@uni.edu.tr", FullName = "Kullanıcı 1", PasswordHash = "h" };
        var u2 = new User { Email = "u2@uni.edu.tr", FullName = "Kullanıcı 2", PasswordHash = "h" };
        await db.Users.AddRangeAsync(u1, u2);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.SendRequestAsync(u1.Id, u2.Id);

        result.Status.Should().Be(FriendshipStatus.Pending);
        result.IsOutgoing.Should().BeTrue();
    }

    [Fact]
    public async Task SendRequestAsync_KendineIstek_BusinessValidationExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var u1 = new User { Email = "self@uni.edu.tr", FullName = "Kendi", PasswordHash = "h" };
        await db.Users.AddAsync(u1);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.SendRequestAsync(u1.Id, u1.Id);

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task SendRequestAsync_CakisanBekleyenIstek_ConflictExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var u1 = new User { Email = "a@uni.edu.tr", FullName = "A", PasswordHash = "h" };
        var u2 = new User { Email = "b@uni.edu.tr", FullName = "B", PasswordHash = "h" };
        await db.Users.AddRangeAsync(u1, u2);
        await db.Friendships.AddAsync(new Friendship { Requester = u1, Addressee = u2, Status = FriendshipStatus.Pending });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.SendRequestAsync(u1.Id, u2.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SendRequestAsync_KarsilikliIstek_OtomatikKabulEdilir()
    {
        using var db = TestHelpers.CreateDb();
        var u1 = new User { Email = "x@uni.edu.tr", FullName = "X", PasswordHash = "h" };
        var u2 = new User { Email = "y@uni.edu.tr", FullName = "Y", PasswordHash = "h" };
        await db.Users.AddRangeAsync(u1, u2);
        // u2 daha önce u1'e istek göndermiş
        await db.Friendships.AddAsync(new Friendship { Requester = u2, Addressee = u1, Status = FriendshipStatus.Pending });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        // Şimdi u1 u2'ye istek gönderiyor
        var result = await service.SendRequestAsync(u1.Id, u2.Id);

        result.Status.Should().Be(FriendshipStatus.Accepted);
    }

    [Fact]
    public async Task SendRequestAsync_EngellenenKullanici_IstekGonderilemez()
    {
        using var db = TestHelpers.CreateDb();
        var u1 = new User { Email = "b1@uni.edu.tr", FullName = "B1", PasswordHash = "h" };
        var u2 = new User { Email = "b2@uni.edu.tr", FullName = "B2", PasswordHash = "h" };
        await db.Users.AddRangeAsync(u1, u2);
        await db.Friendships.AddAsync(new Friendship { Requester = u1, Addressee = u2, Status = FriendshipStatus.Blocked });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.SendRequestAsync(u1.Id, u2.Id);

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task RemoveOrBlockFriendAsync_ArkadasliktanCikarirVeEngeller()
    {
        using var db = TestHelpers.CreateDb();
        var u1 = new User { Email = "r1@uni.edu.tr", FullName = "R1", PasswordHash = "h" };
        var u2 = new User { Email = "r2@uni.edu.tr", FullName = "R2", PasswordHash = "h" };
        await db.Users.AddRangeAsync(u1, u2);
        var f = new Friendship { Requester = u1, Addressee = u2, Status = FriendshipStatus.Accepted };
        await db.Friendships.AddAsync(f);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.RemoveOrBlockFriendAsync(u1.Id, u2.Id, block: true);

        var updated = await db.Friendships.FindAsync(f.Id);
        updated!.Status.Should().Be(FriendshipStatus.Blocked);

        await service.RemoveOrBlockFriendAsync(u1.Id, u2.Id, block: false);
        var deleted = await db.Friendships.FindAsync(f.Id);
        deleted.Should().BeNull();
    }
}
