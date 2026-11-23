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

public class StudyGroupServiceTests
{
    private static StudyGroupService CreateService(AppDbContext db) =>
        new(
            new StudyGroupRepository(db),
            new GenericRepository<GroupMember>(db),
            new UserRepository(db),
            new UnitOfWork(db),
            NullLogger<StudyGroupService>.Instance);

    [Fact]
    public async Task CreateGroupAsync_GecerliIstek_GrupOlustururVe8KarakterDavetKoduUretir()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "lead@uni.edu.tr", FullName = "Grup Lideri", PasswordHash = "h" };
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateGroupAsync(user.Id, new CreateGroupRequest("Algoritma Çalışma Grubu", "Açıklama"));

        result.Should().NotBeNull();
        result.Name.Should().Be("Algoritma Çalışma Grubu");
        result.InviteCode.Should().HaveLength(8);
        result.Members.Should().ContainSingle(m => m.UserId == user.Id && m.Role == GroupRole.Owner);
    }

    [Fact]
    public async Task JoinGroupAsync_GecerliKod_GrubaUyeOlur()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "owner@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var joiner = new User { Email = "joiner@uni.edu.tr", FullName = "Katılımcı", PasswordHash = "h" };
        await db.Users.AddRangeAsync(owner, joiner);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var group = await service.CreateGroupAsync(owner.Id, new CreateGroupRequest("Veri Yapıları Grubu"));

        var joined = await service.JoinGroupAsync(joiner.Id, new JoinGroupRequest(group.InviteCode));
        joined.Members.Should().Contain(m => m.UserId == joiner.Id && m.Role == GroupRole.Member);
    }

    [Fact]
    public async Task JoinGroupAsync_GecersizKod_NotFoundExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var user = new User { Email = "u@uni.edu.tr", FullName = "U", PasswordHash = "h" };
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.JoinGroupAsync(user.Id, new JoinGroupRequest("HATALIKOD"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task JoinGroupAsync_KapasiteDolu_BusinessValidationExceptionFirlatir()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "o@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var group = new StudyGroup { Name = "Dolu Grup", InviteCode = "FULLGRP1", Owner = owner };
        group.Members.Add(new GroupMember { User = owner, Role = GroupRole.Owner });

        // 29 ilave üye ekleyerek 30'a tamamla
        for (int i = 0; i < 29; i++)
        {
            var m = new User { Email = $"m{i}@uni.edu.tr", FullName = $"M {i}", PasswordHash = "h" };
            db.Users.Add(m);
            group.Members.Add(new GroupMember { User = m, Role = GroupRole.Member });
        }

        var candidate = new User { Email = "cand@uni.edu.tr", FullName = "31. Aday", PasswordHash = "h" };
        db.Users.Add(candidate);
        await db.StudyGroups.AddAsync(group);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var act = () => service.JoinGroupAsync(candidate.Id, new JoinGroupRequest("FULLGRP1"));

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task RemoveMemberAsync_SahipUyeCikarabilir()
    {
        using var db = TestHelpers.CreateDb();
        var owner = new User { Email = "own@uni.edu.tr", FullName = "Sahip", PasswordHash = "h" };
        var member = new User { Email = "mem@uni.edu.tr", FullName = "Üye", PasswordHash = "h" };
        var group = new StudyGroup { Name = "Grup", InviteCode = "GRP11223", Owner = owner };
        group.Members.Add(new GroupMember { User = owner, Role = GroupRole.Owner });
        var memRef = new GroupMember { User = member, Role = GroupRole.Member };
        group.Members.Add(memRef);

        await db.Users.AddRangeAsync(owner, member);
        await db.StudyGroups.AddAsync(group);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.RemoveMemberAsync(owner.Id, group.Id, member.Id);

        var updated = await service.GetGroupAsync(owner.Id, group.Id);
        updated.Members.Should().NotContain(m => m.UserId == member.Id);
    }
}
