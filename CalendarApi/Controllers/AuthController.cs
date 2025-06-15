using CalendarApi.Dtos;
using CalendarApi.Models;
using CalendarApi.Services;
using CalendarApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CalendarApi.Controllers
{
    /// <summary>
    /// Controller for authentication and user management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AuthController(AuthService authService, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _authService = authService;
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="dto">The registration data.</param>
        /// <returns>Status of the registration.</returns>
        /// <response code="200">User registered successfully.</response>
        /// <response code="400">User already exists.</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("User already exists");

            _authService.CreatePasswordHash(dto.Password, out var hash, out var salt);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered");
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="dto">The login data.</param>
        /// <returns>JWT token if successful.</returns>
        /// <response code="200">Returns the JWT token.</response>
        /// <response code="401">Invalid credentials.</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!_authService.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                return Unauthorized("Invalid credentials");

            var token = _authService.GenerateJwtToken(user);
            return Ok(new { token });
        }

        /// <summary>
        /// Updates the current user's information. Requires authentication.
        /// </summary>
        /// <param name="dto">The updated user data.</param>
        /// <returns>Status of the update.</returns>
        /// <response code="200">User updated successfully.</response>
        /// <response code="400">Username or email already taken.</response>
        /// <response code="404">User not found.</response>
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateUser(UpdateUserDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            // Check for username or email conflicts (if desired)
            if (user.Username != dto.Username && _context.Users.Any(u => u.Username == dto.Username))
                return BadRequest("Username already taken.");
            if (user.Email != dto.Email && _context.Users.Any(u => u.Email == dto.Email))
                return BadRequest("Email already taken.");

            user.Username = dto.Username;
            user.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                using var hmac = new HMACSHA512();
                user.PasswordSalt = hmac.Key;
                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
            }

            await _context.SaveChangesAsync();
            return Ok("User updated successfully.");
        }

        /// <summary>
        /// Gets all users. Only available in development environment.
        /// </summary>
        /// <returns>List of users.</returns>
        /// <response code="200">Returns the list of users.</response>
        /// <response code="401">If not in development environment.</response>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            if (!_env.IsDevelopment())
                return Unauthorized("This endpoint is only available in development.");

            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }
    }
}
