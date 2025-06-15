using System.ComponentModel.DataAnnotations;

namespace CalendarApi.Models
{
    public class CalendarEvent
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public int CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();

    }
}
