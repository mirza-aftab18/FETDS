using Microsoft.AspNetCore.Identity;
namespace FETDS.Data;
// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public string Address { get; set; } = string.Empty;
}