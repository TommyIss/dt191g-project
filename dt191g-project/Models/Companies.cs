namespace dt191g_project.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public string Category { get; set; }
        public string OpeningsHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Service> Services { get; set; }
        public ICollection<TimeSlot> TimeSlots { get; set; }
        public ICollection<CompanyUser> CompanyUsers { get; set; }
    }
}
