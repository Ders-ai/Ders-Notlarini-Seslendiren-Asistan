using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Exceptions;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.Services;

public class StudyGroupService : IStudyGroupService
{
    private readonly IStudyGroupRepository _groups;
    private readonly IGenericRepository<GroupMember> _members;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<StudyGroupService> _logger;

    public const int MaxMembersPerGroup = 30;

    public StudyGroupService(
        IStudyGroupRepository groups,
        IGenericRepository<GroupMember> members,
        IUserRepository users,
        IUnitOfWork uow,
        ILogger<StudyGroupService> logger)
    {
        _groups = groups;
        _members = members;
        _users = users;
        _uow = uow;
        _logger = logger;
    }

    public async Task<StudyGroupDetailDto> CreateGroupAsync(Guid userId, CreateGroupRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessValidationException("Name", "Grup adı boş olamaz.");

        var inviteCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var group = new StudyGroup
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            InviteCode = inviteCode,
            OwnerId = userId
        };

        group.Members.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            Role = GroupRole.Owner
        });

        await _groups.AddAsync(group, ct);
        await _uow.SaveChangesAsync(ct);

        var owner = await _users.GetByIdAsync(userId, ct);
        var memberDtos = new List<GroupMemberDto>
        {
            new(userId, owner?.FullName ?? "Grup Yöneticisi", GroupRole.Owner, group.CreatedAt)
        };

        return new StudyGroupDetailDto(
            group.Id,
            group.Name,
            group.Description,
            group.InviteCode,
            group.OwnerId,
            true,
            memberDtos);
    }

    public async Task<StudyGroupDetailDto> JoinGroupAsync(Guid userId, JoinGroupRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.InviteCode))
            throw new BusinessValidationException("InviteCode", "Davet kodu boş olamaz.");

        var group = await _groups.GetByInviteCodeAsync(request.InviteCode.Trim().ToUpperInvariant(), ct);
        if (group is null)
            throw new NotFoundException("Geçersiz davet kodu veya grup bulunamadı.");

        // Maksimum üye kontrolü (en fazla 30 üye)
        if (group.Members.Count >= MaxMembersPerGroup)
            throw new BusinessValidationException(new Dictionary<string, string[]>
            {
                ["Members"] = [$"Grup maksimum üye kapasitesine ({MaxMembersPerGroup} üye) ulaşmıştır."]
            });

        if (group.Members.Any(m => m.UserId == userId))
            throw new ConflictException("Bu gruba zaten üyesiniz.");

        var newMember = new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            Role = GroupRole.Member
        };

        await _members.AddAsync(newMember, ct);
        await _uow.SaveChangesAsync(ct);

        return await GetGroupAsync(userId, group.Id, ct);
    }

    public async Task<StudyGroupDetailDto> GetGroupAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        var group = await _groups.GetWithMembersAsync(groupId, ct);
        if (group is null || !group.Members.Any(m => m.UserId == userId))
            throw new NotFoundException("Çalışma grubu bulunamadı veya bu gruba üye değilsiniz.");

        var memberDtos = group.Members.Select(m => new GroupMemberDto(
            m.UserId,
            m.User?.FullName ?? "Öğrenci",
            m.Role,
            m.CreatedAt)).ToList();

        return new StudyGroupDetailDto(
            group.Id,
            group.Name,
            group.Description,
            group.InviteCode,
            group.OwnerId,
            group.OwnerId == userId,
            memberDtos);
    }

    public async Task RemoveMemberAsync(Guid currentUserId, Guid groupId, Guid memberId, CancellationToken ct = default)
    {
        var group = await _groups.GetWithMembersAsync(groupId, ct);
        if (group is null)
            throw new NotFoundException("Grup bulunamadı.");

        // Yalnızca grup sahibi başkasını çıkarabilir veya üye kendisi gruptan ayrılabilir
        if (group.OwnerId != currentUserId && currentUserId != memberId)
            throw new ForbiddenAppException("Gruptan üye çıkarma yetkiniz bulunmamaktadır.");

        var member = group.Members.FirstOrDefault(m => m.UserId == memberId);
        if (member is null)
            throw new NotFoundException("Üye grupta bulunamadı.");

        if (member.Role == GroupRole.Owner && currentUserId == memberId)
            throw new BusinessValidationException("Group", "Grup sahibi gruptan doğrudan ayrılamaz; önce grubu devretmeli veya silmelidir.");

        _members.Remove(member);
        await _uow.SaveChangesAsync(ct);
    }
}
