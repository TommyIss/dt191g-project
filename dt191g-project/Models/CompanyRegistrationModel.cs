namespace dt191g_project.Models
{
    public class CompanyRegistrationModel
    {
        public string CompanyName { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }

        public string Category { get; set; }
        public string OpeningsHours { get; set; }
    }
}
