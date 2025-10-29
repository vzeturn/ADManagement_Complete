using ADManagement.Application.DTOs;
using ADManagement.Domain.Entities;

namespace ADManagement.Application.Mappings;

/// <summary>
/// Mapper for OrganizationalUnit entity to OrganizationalUnitDto
/// </summary>
public static class OrganizationalUnitMapper
{
    /// <summary>
    /// Maps OrganizationalUnit entity to OrganizationalUnitDto
    /// </summary>
    public static OrganizationalUnitDto ToDto(this OrganizationalUnit ou)
    {
        return new OrganizationalUnitDto
        {
            Name = ou.Name,
            DistinguishedName = ou.DistinguishedName,
            Description = ou.Description,
            Path = ou.Path,
            WhenCreated = ou.WhenCreated,
            WhenChanged = ou.WhenChanged
        };
    }
    
    /// <summary>
    /// Maps collection of OrganizationalUnit entities to collection of OrganizationalUnitDto
    /// </summary>
    public static IEnumerable<OrganizationalUnitDto> ToDto(this IEnumerable<OrganizationalUnit> ous)
    {
        return ous.Select(ToDto);
    }
}