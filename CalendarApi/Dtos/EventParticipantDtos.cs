namespace CalendarApi.Dtos
{
    /// <summary>
    /// DTO representing a participant in an event.
    /// </summary>
    public class ParticipantDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for adding a participant to an event.
    /// </summary>
    public class AddParticipantDto
    {
        public int EventId { get; set; }
        public int UserId { get; set; }
    }
}
