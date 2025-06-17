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
    /// Controller for managing participants of calendar events.
    /// </summary>
    [ApiController]
    [Route("api/v2/events/{eventId}/[controller]")]
    [Authorize]
    public class ParticipantsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantsController"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public ParticipantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the list of participants for a specific event.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <returns>A list of participants.</returns>
        /// <response code="200">Returns the list of participants.</response>
        /// <response code="403">If the user is not allowed to view participants.</response>
        /// <response code="404">If the event is not found.</response>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetParticipants(int eventId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var calendarEvent = await _context.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (calendarEvent == null) return NotFound("Event not found.");

            // Check if user is creator or participant
            var isParticipant = calendarEvent.CreatedById == userId ||
                calendarEvent.Participants.Any(p => p.UserId == userId);

            if (!isParticipant)
                return StatusCode(403, "You are not allowed to view this event's participants.");

            var participants = await _context.EventParticipants
                .Where(p => p.EventId == eventId)
                .Include(p => p.User)
                .Select(p => new ParticipantDto
                {
                    Id = p.UserId,
                    Username = p.User!.Username
                })
                .ToListAsync();

            return Ok(participants);
        }

        /// <summary>
        /// Adds participants to a specific event. Only the creator can add participants.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="dto">The DTO containing the list of user IDs to add as participants.</param>
        /// <returns>Status of the operation.</returns>
        /// <response code="200">Participants added successfully.</response>
        /// <response code="403">If the user is not the creator.</response>
        /// <response code="404">If the event is not found.</response>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddParticipants(int eventId, [FromBody] CalendarApi.Dtos.AddParticipantsDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var calendarEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (calendarEvent == null)
                return NotFound("Event not found.");

            if (calendarEvent.CreatedById != userId)
                return StatusCode(403, "Only the creator can add participants.");

            var allUserIds = dto.ParticipantIds.Distinct().ToList();
            var existingUserIds = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            var nonExistentUserIds = allUserIds.Except(existingUserIds).ToList();
            var existingParticipantIds = await _context.EventParticipants
                .Where(p => p.EventId == eventId)
                .Select(p => p.UserId)
                .ToListAsync();

            var alreadyParticipants = existingUserIds.Intersect(existingParticipantIds).ToList();
            var toAdd = existingUserIds.Except(existingParticipantIds).ToList();

            var newParticipants = toAdd.Select(id => new EventParticipant { EventId = eventId, UserId = id });
            _context.EventParticipants.AddRange(newParticipants);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                added = toAdd,
                alreadyInEvent = alreadyParticipants,
                notFound = nonExistentUserIds
            });
        }

        /// <summary>
        /// Removes a participant from a specific event. Only the creator can remove participants.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="dto">The DTO containing the list of user IDs to remove.</param>
        /// <returns>Status of the operation.</returns>
        /// <response code="200">Participant removed successfully.</response>
        /// <response code="400">If the creator tries to remove themselves.</response>
        /// <response code="403">If the user is not the creator.</response>
        /// <response code="404">If the event or participant is not found.</response>
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> RemoveParticipants(int eventId, [FromBody] CalendarApi.Dtos.RemoveParticipantsDto dto)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var calendarEvent = await _context.Events.FindAsync(eventId);

            if (calendarEvent == null) return NotFound();
            if (calendarEvent.CreatedById != requesterId)
                return StatusCode(403, "Only the creator can remove participants.");

            var userIdsToRemove = dto.UserIds?.Distinct().ToList() ?? new List<int>();
            var result = new
            {
                removed = new List<int>(),
                notRemoved = new List<int>(),
                notFound = new List<int>()
            };

            // Remove creator from removal list and track as notRemoved
            if (userIdsToRemove.Contains(requesterId))
            {
                result.notRemoved.Add(requesterId);
                userIdsToRemove.Remove(requesterId);
            }

            // Find which userIds exist
            var existingUserIds = await _context.Users
                .Where(u => userIdsToRemove.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();
            var nonExistentUserIds = userIdsToRemove.Except(existingUserIds).ToList();
            result.notFound.AddRange(nonExistentUserIds);
            userIdsToRemove = userIdsToRemove.Intersect(existingUserIds).ToList();

            // Find which userIds are participants
            var eventParticipantIds = await _context.EventParticipants
                .Where(p => p.EventId == eventId)
                .Select(p => p.UserId)
                .ToListAsync();
            var toRemove = userIdsToRemove.Intersect(eventParticipantIds).ToList();
            var notParticipants = userIdsToRemove.Except(eventParticipantIds).ToList();
            result.notRemoved.AddRange(notParticipants);

            // Remove participants
            var participants = await _context.EventParticipants
                .Where(p => p.EventId == eventId && toRemove.Contains(p.UserId))
                .ToListAsync();
            if (participants.Count > 0)
            {
                _context.EventParticipants.RemoveRange(participants);
                await _context.SaveChangesAsync();
                result.removed.AddRange(toRemove);
            }

            return Ok(result);
        }

        /// <summary>
        /// Gets all events for the logged-in user (future, ongoing, and optionally past).
        /// </summary>
        /// <param name="includePast">If true, include past events as well.</param>
        /// <returns>List of events for the user.</returns>
        /// <response code="200">Returns the list of events.</response>
        [Authorize]
        [HttpGet("/api/v2/events/my-events")]
        public async Task<IActionResult> GetUserEvents([FromQuery] bool includePast = true)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var now = DateTime.UtcNow;
            var events = await _context.Events
                .Include(e => e.Participants)
                .Where(e => e.Participants.Any(p => p.UserId == userId))
                .Select(e => new CalendarApi.Dtos.UserEventListDto
                {
                    EventId = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    IsOngoing = e.StartTime <= now && e.EndTime >= now,
                    IsPast = e.EndTime < now
                })
                .ToListAsync();

            if (!includePast)
                events = events.Where(ev => !ev.IsPast).ToList();

            return Ok(events);
        }

    }
}
