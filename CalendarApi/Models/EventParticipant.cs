namespace CalendarApi.Models
{
    public class EventParticipant
    {
        public int UserId { get; set; }
        public User? User { get; set; }

        public int EventId { get; set; }
        public CalendarEvent? Event { get; set; }
    }
}
