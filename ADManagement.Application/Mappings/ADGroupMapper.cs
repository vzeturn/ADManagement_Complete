using ADManagement.Application.DTOs;
using ADManagement.Domain.Entities;

namespace ADManagement.Application.Mappings;

/// <summary>
/// Mapper for ADGroup entity to ADGroupDto
/// </summary>
public static class ADGroupMapper
{
    /// <summary>
    /// Maps ADGroup entity to ADGroupDto
    /// </summary>
    public static ADGroupDto ToDto(this ADGroup group)
    {
        return new ADGroupDto
        {
            Name = group.Name,
            DisplayName = group.DisplayName,
            Description = group.Description,
            DistinguishedName = group.DistinguishedName,
            GroupScope = group.GroupScope,
            GroupType = group.GroupType,
            MemberCount = group.Members.Count,
            Members = new List<string>(group.Members),
            WhenCreated = group.WhenCreated,
            WhenChanged = group.WhenChanged
        };
    }
    
    /// <summary>
    /// Maps collection of ADGroup entities to collection of ADGroupDto
    /// </summary>
    public static IEnumerable<ADGroupDto> ToDto(this IEnumerable<ADGroup> groups)
    {
        return groups.Select(ToDto);
    }
}