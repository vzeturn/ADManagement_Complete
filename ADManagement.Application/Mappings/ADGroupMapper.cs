using ADManagement.Application.DTOs;
using ADManagement.Domain.Entities;

namespace ADManagement.Application.Mappings;

/// <summary>
/// Mapper for converting between ADGroup domain entity and ADGroupDto
/// Complete version with all properties
/// </summary>
public static class ADGroupMapper
{
    /// <summary>
    /// Maps ADGroup entity to ADGroupDto
    /// </summary>
    public static ADGroupDto ToDto(ADGroup group)
    {
        if (group == null)
            throw new ArgumentNullException(nameof(group));

        return new ADGroupDto
        {
            // Core Identity
            Name = group.Name,
            SamAccountName = group.SamAccountName,
            DisplayName = group.DisplayName,
            DistinguishedName = group.DistinguishedName,
            Description = group.Description,

            // Group Type
            GroupScope = group.GroupScope,
            GroupType = group.GroupType,
            GroupCategory = group.GroupCategory,

            // Contact Information
            Email = group.Email,
            ProxyAddresses = group.ProxyAddresses?.ToList() ?? new List<string>(),

            // Management
            ManagedBy = group.ManagedBy,
            ManagerDisplayName = group.ManagerDisplayName,
            Info = group.Info,

            // Membership
            Members = group.Members?.ToList() ?? new List<string>(),
            MemberCount = group.Members?.Count ?? 0,
            MemberOf = group.MemberOf?.ToList() ?? new List<string>(),

            // Timestamps
            WhenCreated = group.WhenCreated,
            WhenChanged = group.WhenChanged,

            // Additional
            ObjectGuid = group.ObjectGuid,
            ObjectSid = group.ObjectSid,
            OrganizationalUnit = group.OrganizationalUnit,
            Path = group.Path,
            IsSystemCritical = group.IsSystemCritical
        };
    }

    /// <summary>
    /// Maps ADGroupDto to ADGroup entity
    /// </summary>
    public static ADGroup ToEntity(ADGroupDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new ADGroup
        {
            // Core Identity
            Name = dto.Name,
            SamAccountName = dto.SamAccountName,
            DisplayName = dto.DisplayName,
            DistinguishedName = dto.DistinguishedName,
            Description = dto.Description,

            // Group Type
            GroupScope = dto.GroupScope,
            GroupType = dto.GroupType,
            GroupCategory = dto.GroupCategory,

            // Contact Information
            Email = dto.Email,
            ProxyAddresses = dto.ProxyAddresses?.ToList() ?? new List<string>(),

            // Management
            ManagedBy = dto.ManagedBy,
            ManagerDisplayName = dto.ManagerDisplayName,
            Info = dto.Info,

            // Membership
            Members = dto.Members?.ToList() ?? new List<string>(),
            MemberOf = dto.MemberOf?.ToList() ?? new List<string>(),

            // Timestamps
            WhenCreated = dto.WhenCreated,
            WhenChanged = dto.WhenChanged,

            // Additional
            ObjectGuid = dto.ObjectGuid,
            ObjectSid = dto.ObjectSid,
            OrganizationalUnit = dto.OrganizationalUnit,
            Path = dto.Path,
            IsSystemCritical = dto.IsSystemCritical
        };
    }

    /// <summary>
    /// Maps a collection of ADGroup entities to ADGroupDto collection
    /// </summary>
    public static IEnumerable<ADGroupDto> ToDtoList(IEnumerable<ADGroup> groups)
    {
        if (groups == null)
            return Enumerable.Empty<ADGroupDto>();

        return groups.Select(ToDto);
    }

    /// <summary>
    /// Maps a collection of ADGroupDto to ADGroup entity collection
    /// </summary>
    public static IEnumerable<ADGroup> ToEntityList(IEnumerable<ADGroupDto> dtos)
    {
        if (dtos == null)
            return Enumerable.Empty<ADGroup>();

        return dtos.Select(ToEntity);
    }

    /// <summary>
    /// Updates an existing ADGroup entity with data from ADGroupDto
    /// </summary>
    public static void UpdateEntity(ADGroup entity, ADGroupDto dto)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Update mutable properties only
        entity.DisplayName = dto.DisplayName;
        entity.Description = dto.Description;
        entity.Email = dto.Email;
        entity.ManagedBy = dto.ManagedBy;
        entity.ManagerDisplayName = dto.ManagerDisplayName;
        entity.Info = dto.Info;

        // Note: Don't update Name, SamAccountName, DistinguishedName as they are identity properties
        // Don't update Members/MemberOf as they are managed separately
    }

    /// <summary>
    /// Creates a shallow copy of ADGroupDto
    /// </summary>
    public static ADGroupDto Clone(ADGroupDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new ADGroupDto
        {
            Name = dto.Name,
            SamAccountName = dto.SamAccountName,
            DisplayName = dto.DisplayName,
            DistinguishedName = dto.DistinguishedName,
            Description = dto.Description,
            GroupScope = dto.GroupScope,
            GroupType = dto.GroupType,
            GroupCategory = dto.GroupCategory,
            Email = dto.Email,
            ProxyAddresses = new List<string>(dto.ProxyAddresses),
            ManagedBy = dto.ManagedBy,
            ManagerDisplayName = dto.ManagerDisplayName,
            Info = dto.Info,
            Members = new List<string>(dto.Members),
            MemberCount = dto.MemberCount,
            MemberOf = new List<string>(dto.MemberOf),
            WhenCreated = dto.WhenCreated,
            WhenChanged = dto.WhenChanged,
            ObjectGuid = dto.ObjectGuid,
            ObjectSid = dto.ObjectSid,
            OrganizationalUnit = dto.OrganizationalUnit,
            Path = dto.Path,
            IsSystemCritical = dto.IsSystemCritical
        };
    }
}