using Microsoft.AspNetCore.Identity;

namespace dt191g_project.Models
{
    public class ApplicationUser: IdentityUser
    {
        public CustomerProfile CustomerProfile { get; set; }
        public ICollection<CompanyUser> CompanyUsers { get; set; }
        public int CompanyId { get; internal set; }
    }
}
