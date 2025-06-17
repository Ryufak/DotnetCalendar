
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

    /// <summary>
    /// DTO for removing participants from an event.
    /// </summary>
    public class RemoveParticipantsDto
    {
        public List<int> UserIds { get; set; } = new();
    }

    /// <summary>
    /// DTO for adding participants to an event.
    /// </summary>
    public class AddParticipantsDto
    {
        public List<int> ParticipantIds { get; set; } = new();
    }
    
    /// <summary>
    /// DTO for fetching user events (my-events endpoint).
    /// </summary>
    public class FetchUserEventsDto
    {
        public bool IncludePast { get; set; } = false;
    }
}
