namespace dt191g_project.Models
{
    public class TimeSlotGenerator
    {
        public int CompanyId { get; set; }
        public int ServiceId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
        public int IntervalMinutes { get; set; }
    }
}
