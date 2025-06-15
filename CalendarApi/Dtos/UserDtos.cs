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
        [Required]
        public string Username { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        public string? Password { get; set; }
    }
}