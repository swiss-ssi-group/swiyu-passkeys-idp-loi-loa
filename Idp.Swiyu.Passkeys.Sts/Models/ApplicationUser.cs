using Microsoft.AspNetCore.Identity;

namespace Idp.Swiyu.Passkeys.Sts.Models;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public int? SwiyuIdentityId { get; set; }
}
