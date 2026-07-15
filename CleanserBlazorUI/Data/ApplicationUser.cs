using Microsoft.AspNetCore.Identity;

namespace CleanserBlazorUI.Data
{
    public class ApplicationUser : IdentityUser
    {
        public bool MustChangePassword { get; set; } = false;
    }

}
