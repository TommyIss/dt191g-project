namespace dt191g_project.Models
{
    public class CompanyDashboardModel
    {
        public Company Company { get; set; }
        public int ServicesCount { get; set; }
        public int TimeSlotsCount { get; set; }
        public int BookingsCount { get; set; }
        public string Role { get; set; }
    }
}
