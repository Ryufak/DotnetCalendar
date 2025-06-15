namespace CalendarApi.Dtos
{
    public class CreateEventDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<int>? ParticipantIds { get; set; }
    }
}
