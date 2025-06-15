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
    [Route("api/events/{eventId}/[controller]")]
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
                return Forbid("You are not allowed to view this event's participants.");

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
        /// <param name="participantIds">The list of user IDs to add as participants.</param>
        /// <returns>Status of the operation.</returns>
        /// <response code="200">Participants added successfully.</response>
        /// <response code="403">If the user is not the creator.</response>
        /// <response code="404">If the event is not found.</response>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddParticipants(int eventId, [FromBody] List<int> participantIds)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var calendarEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (calendarEvent == null)
                return NotFound("Event not found.");

            if (calendarEvent.CreatedById != userId)
                return Forbid("Only the creator can add participants.");

            var existingUserIds = await _context.Users
                .Where(u => participantIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            var existingParticipantIds = await _context.EventParticipants
                .Where(p => p.EventId == eventId)
                .Select(p => p.UserId)
                .ToListAsync();

            var newParticipants = existingUserIds
                .Except(existingParticipantIds)
                .Select(id => new EventParticipant { EventId = eventId, UserId = id });

            _context.EventParticipants.AddRange(newParticipants);
            await _context.SaveChangesAsync();

            return Ok("Participants added.");
        }

        /// <summary>
        /// Removes a participant from a specific event. Only the creator can remove participants.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="userId">The ID of the user to remove.</param>
        /// <returns>Status of the operation.</returns>
        /// <response code="200">Participant removed successfully.</response>
        /// <response code="400">If the creator tries to remove themselves.</response>
        /// <response code="403">If the user is not the creator.</response>
        /// <response code="404">If the event or participant is not found.</response>
        [Authorize]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveParticipant(int eventId, int userId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var calendarEvent = await _context.Events.FindAsync(eventId);

            if (calendarEvent == null) return NotFound();
            if (calendarEvent.CreatedById != requesterId)
                return Forbid("Only the creator can remove participants.");

            if (calendarEvent.CreatedById == userId)
                return BadRequest("The creator cannot remove themselves from the event.");

            var participant = await _context.EventParticipants
                .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId);

            if (participant == null)
                return NotFound("Participant not found.");

            _context.EventParticipants.Remove(participant);
            await _context.SaveChangesAsync();

            return Ok("Participant removed.");
        }
    }
}
