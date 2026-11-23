using FluentAssertions;
using SesliAsistan.Business.Services;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;
using Xunit;

namespace SesliAsistan.Tests;

public class ShareAccessHandlerTests
{
    private static ShareAccessHandler CreateHandler(AppDbContext db) =>
        new(new UserRepository(db), new FriendshipRepository(db), new StudyGroupRepository(db));

    [Fact]
    public async Task CanAccessAsync_Sahip_HerZamanErisir()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "owner@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        await db.Users.AddAsync(owner);
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Özel Not", Privacy = PrivacyLevel.Private };
        var handler = CreateHandler(db);

        var canAccess = await handler.CanAccessAsync(owner.Id, share);
        canAccess.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessAsync_Admin_HerZamanErisir()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "stu@uni.edu.tr", FullName = "Öğrenci", PasswordHash = "h" };
        var admin = new User { Email = "admin@uni.edu.tr", FullName = "Yönetici", PasswordHash = "h", Role = UserRole.Admin };
        await db.Users.AddRangeAsync(owner, admin);
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Not", Privacy = PrivacyLevel.Private };
        var handler = CreateHandler(db);

        var canAccess = await handler.CanAccessAsync(admin.Id, share);
        canAccess.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessAsync_PrivateIcerik_YabanciErisemez()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "o@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var stranger = new User { Email = "s@uni.edu.tr", FullName = "Yabancı", PasswordHash = "h" };
        await db.Users.AddRangeAsync(owner, stranger);
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Özel", Privacy = PrivacyLevel.Private };
        var handler = CreateHandler(db);

        var canAccess = await handler.CanAccessAsync(stranger.Id, share);
        canAccess.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessAsync_PublicIcerik_GirisYapmisKullaniciErisir()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "pub_o@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var user = new User { Email = "pub_u@uni.edu.tr", FullName = "Kullanıcı", PasswordHash = "h" };
        await db.Users.AddRangeAsync(owner, user);
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Genel", Privacy = PrivacyLevel.Public };
        var handler = CreateHandler(db);

        var canAccess = await handler.CanAccessAsync(user.Id, share);
        canAccess.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessAsync_FriendsIcerik_ArkadasErisir()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "fo@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var friend = new User { Email = "ff@uni.edu.tr", FullName = "Arkadaş", PasswordHash = "h" };
        await db.Users.AddRangeAsync(owner, friend);
        await db.Friendships.AddAsync(new Friendship { Requester = owner, Addressee = friend, Status = FriendshipStatus.Accepted });
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Arkadaşlara Özel", Privacy = PrivacyLevel.Friends };
        var handler = CreateHandler(db);

        var canAccess = await handler.CanAccessAsync(friend.Id, share);
        canAccess.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessAsync_FriendsIcerik_ArkadasOlmayanErisemez()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "nfo@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var notFriend = new User { Email = "nff@uni.edu.tr", FullName = "Arkadaş Değil", PasswordHash = "h" };
        await db.Users.AddRangeAsync(owner, notFriend);
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Arkadaşlara Özel", Privacy = PrivacyLevel.Friends };
        var handler = CreateHandler(db);

        var canAccess = await handler.CanAccessAsync(notFriend.Id, share);
        canAccess.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessAsync_GroupIcerik_GrupUyesiErisir()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "go@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var member = new User { Email = "gm@uni.edu.tr", FullName = "Üye", PasswordHash = "h" };
        var group = new StudyGroup { Name = "Çalışma Grubu", InviteCode = "GRP12345", Owner = owner };
        group.Members.Add(new GroupMember { User = member, Role = GroupRole.Member });

        await db.Users.AddRangeAsync(owner, member);
        await db.StudyGroups.AddAsync(group);
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Grup Notu", Privacy = PrivacyLevel.Group, GroupId = group.Id };
        var handler = CreateHandler(db);

        var canAccess = await handler.CanAccessAsync(member.Id, share);
        canAccess.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessAsync_GroupIcerik_GrupUyesiOlmayanErisemez()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "ngo@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var outsider = new User { Email = "ngout@uni.edu.tr", FullName = "Dışarıdaki", PasswordHash = "h" };
        var group = new StudyGroup { Name = "Grup", InviteCode = "GRP67890", Owner = owner };

        await db.Users.AddRangeAsync(owner, outsider);
        await db.StudyGroups.AddAsync(group);
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Grup", Privacy = PrivacyLevel.Group, GroupId = group.Id };
        var handler = CreateHandler(db);

        var canAccess = await handler.CanAccessAsync(outsider.Id, share);
        canAccess.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessAsync_KaldirilmisIcerik_HicKimseErisemez()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "ro@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var admin = new User { Email = "ra@uni.edu.tr", FullName = "Admin", PasswordHash = "h", Role = UserRole.Admin };
        await db.Users.AddRangeAsync(owner, admin);
        await db.SaveChangesAsync();

        var share = new SharedContent { OwnerId = owner.Id, Title = "Silinmiş İçerik", Privacy = PrivacyLevel.Public, IsRemoved = true };
        var handler = CreateHandler(db);

        var ownerAccess = await handler.CanAccessAsync(owner.Id, share);
        var adminAccess = await handler.CanAccessAsync(admin.Id, share);

        ownerAccess.Should().BeFalse();
        adminAccess.Should().BeFalse();
    }
}
