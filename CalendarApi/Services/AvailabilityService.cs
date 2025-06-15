using CalendarApi.Data;
using Microsoft.EntityFrameworkCore;



public class AvailabilityService
{
    private readonly ApplicationDbContext _context;

    public AvailabilityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<(DateTime start, DateTime end)>> FindFreeSlotsAsync(
        List<int> userIds,
        DateTime from,
        DateTime to,
        TimeSpan slotDuration)
    {
        // Fetch events for all users in the date range
        var events = await _context.EventParticipants
            .Where(p => userIds.Contains(p.UserId) &&
                        p.Event != null &&
                        p.Event.StartTime < to &&
                        p.Event.EndTime > from)
            .Select(p => new
            {
                p.Event!.StartTime,
                p.Event!.EndTime
            })
            .ToListAsync();

        // Normalize busy intervals
        var busyTimes = events
            .Select(e => (Start: e.StartTime, End: e.EndTime))
            .OrderBy(e => e.Start)
            .ToList();

        // Merge overlapping intervals
        var merged = new List<(DateTime start, DateTime end)>();
        foreach (var interval in busyTimes)
        {
            if (!merged.Any() || merged.Last().end < interval.Start)
            {
                merged.Add(interval);
            }
            else
            {
                var last = merged.Last();
                merged[^1] = (last.start, new DateTime(Math.Max(last.end.Ticks, interval.End.Ticks)));
            }
        }

        // Look for gaps
        var freeSlots = new List<(DateTime start, DateTime end)>();
        var cursor = from;

        foreach (var (busyStart, busyEnd) in merged)
        {
            if (cursor < busyStart)
            {
                var gap = busyStart - cursor;
                if (gap >= slotDuration)
                {
                    var slotStart = cursor;
                    while (slotStart + slotDuration <= busyStart)
                    {
                        freeSlots.Add((slotStart, slotStart + slotDuration));
                        slotStart += slotDuration;
                    }
                }
            }

            cursor = new DateTime(Math.Max(cursor.Ticks, busyEnd.Ticks));
        }

        // Handle time after last event
        if (cursor < to)
        {
            var slotStart = cursor;
            while (slotStart + slotDuration <= to)
            {
                freeSlots.Add((slotStart, slotStart + slotDuration));
                slotStart += slotDuration;
            }
        }

        return freeSlots;
    }
}



