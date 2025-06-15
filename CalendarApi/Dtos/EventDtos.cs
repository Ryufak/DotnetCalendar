namespace CalendarApi.Dtos
{
    /// <summary>
    /// DTO representing a calendar event with participants.
    /// </summary>
    public class CalendarEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<ParticipantDto> Participants { get; set; } = new();
    }

    /// <summary>
    /// DTO for updating an event.
    /// </summary>
    public class UpdateEventDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// DTO for creating a new event.
    /// </summary>
    public class CreateEventDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<int>? ParticipantIds { get; set; }
    }

    /// <summary>
    /// DTO for requesting free time slots for users.
    /// </summary>
    public class FreeSlotRequestDto
    {
        public List<int> UserIds { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int SlotDurationMinutes { get; set; }
    }

    /// <summary>
    /// DTO representing a time slot.
    /// </summary>
    public class TimeSlotDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
