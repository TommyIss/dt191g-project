namespace dt191g_project.Models
{
    public class CustomerProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public ApplicationUser User { get; set; }
    }
}
