using CalendarApi.Data;
using CalendarApi.Dtos;
using CalendarApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CalendarApi.Controllers
{
    /// <summary>
    /// Controller for managing calendar events and related operations.
    /// </summary>
    [ApiController]
    [Route("api/v2/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AvailabilityService _availabilityService;

        public EventsController(ApplicationDbContext context, IWebHostEnvironment env, AvailabilityService availabilityService)
        {
            _context = context;
            _env = env;
            _availabilityService = availabilityService;
        }

        private async Task<bool> HasOverlappingEvents(DateTime start, DateTime end, List<int> userIds, int? excludeEventId = null)
        {
            return await _context.EventParticipants
                .Include(ep => ep.Event)
                .Where(ep =>
                    userIds.Contains(ep.UserId) &&
                    ep.EventId != excludeEventId && 
                    (ep.Event != null && ep.Event.StartTime < end && ep.Event.EndTime > start)
                )
                .AnyAsync();
        }

        /// <summary>
        /// Creates a new calendar event with specified participants.
        /// The authenticated user is automatically added as a participant and event creator.
        /// </summary>
        /// <param name="dto">Event creation details including title, description, time, and participant IDs.</param>
        /// <returns>The created event with participant details.</returns>
        /// <response code="200">Event created successfully.</response>
        /// <response code="400">Invalid input, e.g., start time is after end time.</response>
        /// <response code="401">Unauthorized - user not authenticated.</response>
        /// <response code="409">Conflict - participant has overlapping events.</response>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateEvent(CreateEventDto dto)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
                return Unauthorized("User ID not found or invalid in token");

            if (dto.StartTime >= dto.EndTime)
                return BadRequest("Start time must be before end time.");

            var participantIds = dto.ParticipantIds?.Distinct().ToList() ?? new List<int>();
            if (!participantIds.Contains(userId))
                participantIds.Add(userId);

            if (await HasOverlappingEvents(dto.StartTime, dto.EndTime, participantIds))
                return Conflict("One or more participants already have an event at this time.");

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

            foreach (var pid in participantIds.Distinct())
            {
                _context.EventParticipants.Add(new EventParticipant
                {
                    EventId = calendarEvent.Id,
                    UserId = pid
                });
            }

            await _context.SaveChangesAsync();

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

        /// <summary>
        /// Gets all events (development only).
        /// </summary>
        /// <returns>List of all events with participants.</returns>
        /// <response code="200">Returns the list of events.</response>
        /// <response code="401">If not in development environment.</response>
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

        /// <summary>
        /// Gets a specific event by its ID.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <returns>The event details.</returns>
        /// <response code="200">Returns the event.</response>
        /// <response code="404">Event not found.</response>
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

        /// <summary>
        /// Gets events for the authenticated user, optionally filtered by date range.
        /// </summary>
        /// <param name="from">Start date (optional).</param>
        /// <param name="to">End date (optional).</param>
        /// <returns>List of events.</returns>
        /// <response code="200">Returns the list of events.</response>
        /// <response code="400">Start date is after end date.</response>
        [Authorize]
        [HttpGet]
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

        /// <summary>
        /// Updates an existing event. Only the creator can update.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <param name="dto">The updated event data.</param>
        /// <returns>Status of the update.</returns>
        /// <response code="200">Event updated successfully.</response>
        /// <response code="400">Invalid input.</response>
        /// <response code="401">Unauthorized or not the creator.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="409">Overlapping event for a participant.</response>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, UpdateEventDto dto)
        {
            // var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
                return Unauthorized("User ID not found or invalid.");

            var calendarEvent = await _context.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (calendarEvent == null)
                return NotFound();

            if (calendarEvent.CreatedById != userId)
                return Unauthorized("You can only edit your own events.");

            var newStart = dto.StartTime ?? calendarEvent.StartTime;
            var newEnd = dto.EndTime ?? calendarEvent.EndTime;

            if (newStart >= newEnd)
                return BadRequest("Start time must be before end time.");

            var participants = await _context.EventParticipants
                .Where(p => p.EventId == id)
                .Select(p => p.UserId)
                .ToListAsync();

            if (!participants.Contains(userId))
                participants.Add(userId);

            participants.Add(userId);

            if (dto.StartTime.HasValue && dto.EndTime.HasValue) // Already true due to previous check, but ;eaving this because of warnings
                if (await HasOverlappingEvents(dto.StartTime.Value, dto.EndTime.Value, participants, excludeEventId: id))
                    return Conflict("One or more participants already have an overlapping event.");

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.Title))
                calendarEvent.Title = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                calendarEvent.Description = dto.Description;

            calendarEvent.StartTime = newStart;
            calendarEvent.EndTime = newEnd;

            await _context.SaveChangesAsync();
            return Ok("Updated");
        }

        /// <summary>
        /// Deletes an event. Only the creator can delete.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <returns>Status of the deletion.</returns>
        /// <response code="200">Event deleted successfully.</response>
        /// <response code="401">Unauthorized or not the creator.</response>
        /// <response code="404">Event not found.</response>
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

        /// <summary>
        /// Finds free time slots for a group of users within a specified range.
        /// </summary>
        /// <param name="dto">Request details including user IDs, time range, and slot duration.</param>
        /// <returns>List of available time slots.</returns>
        /// <response code="200">Returns the list of free slots.</response>
        /// <response code="400">Invalid input.</response>
        [HttpPost("free-slots")]
        public async Task<IActionResult> GetFreeSlots([FromBody] FreeSlotRequestDto dto)
        {
            if (dto.From >= dto.To)
                return BadRequest("Invalid time range.");

            if (dto.SlotDurationMinutes <= 0)
                return BadRequest("Slot duration must be greater than 0.");

            var slots = await _availabilityService.FindFreeSlotsAsync(
                dto.UserIds,
                dto.From,
                dto.To,
                TimeSpan.FromMinutes(dto.SlotDurationMinutes));

            var result = slots.Select(s => new TimeSlotDto
            {
                Start = s.start,
                End = s.end
            });

            return Ok(result);
        }



    }
}
