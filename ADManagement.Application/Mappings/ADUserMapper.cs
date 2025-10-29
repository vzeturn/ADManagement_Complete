using ADManagement.Application.DTOs;
using ADManagement.Domain.Entities;

namespace ADManagement.Application.Mappings;

/// <summary>
/// Mapper for ADUser entity to ADUserDto
/// </summary>
public static class ADUserMapper
{
    /// <summary>
    /// Maps ADUser entity to ADUserDto
    /// </summary>
    public static ADUserDto ToDto(this ADUser user)
    {
        return new ADUserDto
        {
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Department = user.Department,
            Title = user.Title,
            Company = user.Company,
            Office = user.Office,
            Manager = user.Manager,
            PhoneNumber = user.PhoneNumber,
            MobileNumber = user.MobileNumber,
            FaxNumber = user.FaxNumber,
            StreetAddress = user.StreetAddress,
            City = user.City,
            State = user.State,
            PostalCode = user.PostalCode,
            Country = user.Country,
            DistinguishedName = user.DistinguishedName,
            IsEnabled = user.IsEnabled,
            IsLockedOut = user.IsLockedOut,
            AccountStatus = user.AccountStatus,
            LastLogon = user.LastLogon,
            LastPasswordSet = user.LastPasswordSet,
            AccountExpires = user.AccountExpires,
            Description = user.Description,
            MemberOf = new List<string>(user.MemberOf)
        };
    }
    
    /// <summary>
    /// Maps collection of ADUser entities to collection of ADUserDto
    /// </summary>
    public static IEnumerable<ADUserDto> ToDto(this IEnumerable<ADUser> users)
    {
        return users.Select(ToDto);
    }
}