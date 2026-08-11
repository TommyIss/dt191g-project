namespace dt191g_project.Models
{
    public class TimeSlot
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsBooked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } 
        
        public Company Company { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}
