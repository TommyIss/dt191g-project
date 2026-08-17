namespace dt191g_project.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public int TimeSlotId { get; set; }
        public string? CustomerId { get; set; }

        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? UserId { get; set; }
        public int CompanyId { get; set; }

        public Service? Service { get; set; }
        public TimeSlot? TimeSlot { get; set; }
        public ApplicationUser? Customer { get; set; }
    }
}