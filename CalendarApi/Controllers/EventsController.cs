using CalendarApi.Data;
using CalendarApi.Dtos;
using CalendarApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CalendarApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateEvent(CreateEventDto dto)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
                return Unauthorized("User ID not found or invalid in token");

            if (dto.StartTime >= dto.EndTime)
                return BadRequest("Start time must be before end time.");

            var calendarEvent = new CalendarEvent
            {
                Title = dto.Title,
                Description = dto.Description,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CreatedById = userId
            };

            _context.Events.Add(calendarEvent);
            await _context.SaveChangesAsync();

            if (dto.ParticipantIds != null)
            {
                foreach (var pid in dto.ParticipantIds.Distinct())
                {
                    _context.EventParticipants.Add(new EventParticipant
                    {
                        EventId = calendarEvent.Id,
                        UserId = pid
                    });
                }

                await _context.SaveChangesAsync();
            }

            var resultDto = new CalendarEventDto
            {
                Id = calendarEvent.Id,
                Title = calendarEvent.Title,
                Description = calendarEvent.Description,
                StartTime = calendarEvent.StartTime,
                EndTime = calendarEvent.EndTime,
                Participants = await _context.EventParticipants
                    .Where(p => p.EventId == calendarEvent.Id)
                    .Include(p => p.User)
                    .Select(p => new ParticipantDto
                    {
                        Id = p.UserId,
                        Username = p.User != null ? p.User.Username : "Unknown"
                    })
                    .ToListAsync()
            };

            return Ok(resultDto);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var calendarEvent = await _context.Events
                .Include(e => e.Participants)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (calendarEvent == null)
                return NotFound();

            var resultDto = new CalendarEventDto
            {
                Id = calendarEvent.Id,
                Title = calendarEvent.Title,
                Description = calendarEvent.Description,
                StartTime = calendarEvent.StartTime,
                EndTime = calendarEvent.EndTime,
                Participants = calendarEvent.Participants.Select(p => new ParticipantDto
                {
                    Id = p.UserId,
                    Username = p.User != null ? p.User.Username : "Unknown"
                }).ToList()
            };

            return Ok(resultDto);
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetEvents([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (from.HasValue && to.HasValue && from > to)
                return BadRequest("Start date must be earlier than end date.");

            var query = _context.Events
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(e => e.StartTime >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.EndTime <= to.Value);

            var events = await query
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

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, UpdateEventDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var calendarEvent = await _context.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (calendarEvent == null)
                return NotFound();

            if (calendarEvent.CreatedById != userId)
                return Unauthorized("You can only edit your own events.");

            // Optional: Validate date range
            if (dto.StartTime.HasValue && dto.EndTime.HasValue && dto.StartTime >= dto.EndTime)
                return BadRequest("Start time must be before end time.");

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.Title))
                calendarEvent.Title = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                calendarEvent.Description = dto.Description;

            if (dto.StartTime.HasValue)
                calendarEvent.StartTime = dto.StartTime.Value;

            if (dto.EndTime.HasValue)
                calendarEvent.EndTime = dto.EndTime.Value;

            // Update participants (clear and re-add)
            if (dto.ParticipantIds != null)
            {
                _context.EventParticipants.RemoveRange(calendarEvent.Participants);
                foreach (var pid in dto.ParticipantIds.Distinct())
                {
                    _context.EventParticipants.Add(new EventParticipant
                    {
                        EventId = calendarEvent.Id,
                        UserId = pid
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Updated");
        }




        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized("User ID not found or invalid in token");
            }
            var calendarEvent = await _context.Events.FindAsync(id);

            if (calendarEvent == null || calendarEvent.CreatedById != userId)
                return Unauthorized();

            _context.Events.Remove(calendarEvent);
            await _context.SaveChangesAsync();
            return Ok("Deleted");
        }
    }
}
