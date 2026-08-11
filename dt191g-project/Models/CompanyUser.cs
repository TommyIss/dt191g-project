namespace dt191g_project.Models
{
    public class CompanyUser
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CompanyId { get; set; }

        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }

        public ApplicationUser User { get; set; }
        public Company Company { get; set; }
    }
}
