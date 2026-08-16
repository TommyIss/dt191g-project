namespace dt191g_project.Models
{
    public class ServiceFilterModel
    {
        public string City { get; set; }
        public string Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? DurationMinutes { get; set; }
        public string CompanyName { get; set; }
    }
}
