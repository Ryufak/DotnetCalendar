using System.ComponentModel.DataAnnotations;

namespace CalendarApi.Dtos
{
    /// <summary>
    /// DTO for registering a new user.
    /// </summary>
    public class RegisterUserDto
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    /// <summary>
    /// DTO for user login.
    /// </summary>
    public class LoginUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = "";
    }

    /// <summary>
    /// DTO for updating user information.
    /// </summary>
    public class UpdateUserDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    /// <summary>
    /// DTO for returning user information (ID, username, email).
    /// </summary>
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    /// <summary>
    /// DTO for listing a user's events (for ParticipantsController).
    /// </summary>
    public class UserEventListDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsOngoing { get; set; }
        public bool IsPast { get; set; }
    }
}