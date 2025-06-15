using CalendarApi.Data;
using CalendarApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Controllers
{
    [ApiController]
    [Route("api/dev")]
    public class DevDbController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DevDbController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            if (!_env.IsDevelopment())
                return Unauthorized("This endpoint is only available in development.");

            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents()
        {
            if (!_env.IsDevelopment())
                return Unauthorized("This endpoint is only available in development.");


            var events = await _context.Events
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Select(e => new CalendarEventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Participants = e.Participants.Select(p => new ParticipantDto
                    {
                        Id = p.UserId,
                        Username = p.User != null ? p.User.Username : "Unknown"
                    }).ToList()
                })
                .ToListAsync();

            return Ok(events);
        }
    }
}
