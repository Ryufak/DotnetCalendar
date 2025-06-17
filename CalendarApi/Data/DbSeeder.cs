using CalendarApi.Data;
using CalendarApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            // Ensure database is created
            context.Database.Migrate();

            // Seed users
            if (!context.Users.Any())
            {
                var users = new List<User>
                {
                    new User { Username = "user1", Email = "user1@example.com", FirstName = "John", LastName = "Doe", PasswordHash = System.Text.Encoding.UTF8.GetBytes("password") },
                    new User { Username = "user2", Email = "user2@example.com", FirstName = "Jane", LastName = "Smith", PasswordHash = System.Text.Encoding.UTF8.GetBytes("password") },
                    new User { Username = "user3", Email = "user3@example.com", FirstName = "John", LastName = "Steamers", PasswordHash = System.Text.Encoding.UTF8.GetBytes("password") },
                    new User { Username = "user4", Email = "user4@example.com", FirstName = "Peret", LastName = "Piper", PasswordHash = System.Text.Encoding.UTF8.GetBytes("password") },
                    new User { Username = "user5", Email = "user5@example.com", FirstName = "Cassidy", LastName = "Cassidy", PasswordHash = System.Text.Encoding.UTF8.GetBytes("password") },
                    new User { Username = "user6", Email = "user6@example.com", FirstName = "Joshua", LastName = "Petelski", PasswordHash = System.Text.Encoding.UTF8.GetBytes("password") },
                    new User { Username = "user12345", Email = "user12345@example.com", FirstName = "Jon", LastName = "Stalinski", PasswordHash = System.Text.Encoding.UTF8.GetBytes("password") }
                };
                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // Seed events
            if (!context.Events.Any())
            {
                var allUsers = context.Users.ToList();
                if (allUsers.Count < 3)
                    return;

                var user1 = allUsers.FirstOrDefault(u => u.Username == "user1");
                var user2 = allUsers.FirstOrDefault(u => u.Username == "user2");
                var user3 = allUsers.FirstOrDefault(u => u.Username == "user3");
                if (user1 != null && user2 != null && user3 != null)
                {
                    var events = new List<CalendarEvent>
                    {
                        new CalendarEvent
                        {
                            Title = "Picnic",
                            Description = $"A fun picnic organized by {user1.FirstName} {user1.LastName} at Central Park. Bring your own snacks!",
                            StartTime = new DateTime(2025, 6, 17, 5, 0, 0, DateTimeKind.Utc),
                            EndTime = new DateTime(2025, 6, 17, 7, 30, 0, DateTimeKind.Utc),
                            CreatedById = user1.Id,
                        },
                        new CalendarEvent
                        {
                            Title = "Easter Event",
                            Description = $"Easter celebration hosted by {user2.FirstName} {user2.LastName}. Egg hunt and games for everyone.",
                            StartTime = new DateTime(2025, 7, 12, 12, 0, 0, DateTimeKind.Utc),
                            EndTime = new DateTime(2025, 7, 12, 13, 30, 0, DateTimeKind.Utc),
                            CreatedById = user2.Id
                        },
                        new CalendarEvent
                        {
                            Title = "Easter Event",
                            Description = $"Throwback Easter event from 2012, organized by {user3.FirstName} {user3.LastName}.",
                            StartTime = new DateTime(2012, 7, 12, 12, 0, 0, DateTimeKind.Utc),
                            EndTime = new DateTime(2012, 7, 12, 13, 30, 0, DateTimeKind.Utc),
                            CreatedById = user3.Id
                        },
                        new CalendarEvent
                        {
                            Title = "Board Games Night",
                            Description = $"{user1.FirstName} and {user2.FirstName} invite you to a night of classic board games and pizza.",
                            StartTime = new DateTime(2025, 8, 20, 18, 0, 0, DateTimeKind.Utc),
                            EndTime = new DateTime(2025, 8, 20, 21, 0, 0, DateTimeKind.Utc),
                            CreatedById = user1.Id
                        },
                        new CalendarEvent
                        {
                            Title = "Tech Meetup",
                            Description = $"Join {user3.FirstName} for a discussion on the latest in AI and technology trends.",
                            StartTime = new DateTime(2025, 9, 15, 17, 0, 0, DateTimeKind.Utc),
                            EndTime = new DateTime(2025, 9, 15, 19, 0, 0, DateTimeKind.Utc),
                            CreatedById = user3.Id
                        },
                        new CalendarEvent
                        {
                            Title = "Coffee Catchup",
                            Description = $"Casual coffee meetup with {user2.FirstName} at the downtown cafe.",
                            StartTime = new DateTime(2025, 10, 5, 10, 0, 0, DateTimeKind.Utc),
                            EndTime = new DateTime(2025, 10, 5, 11, 30, 0, DateTimeKind.Utc),
                            CreatedById = user2.Id
                        }
                    };
                    context.Events.AddRange(events);
                    context.SaveChanges();

                    // Seed participants: add 3-5 random users to each event
                    var rnd = new System.Random();
                    var allEvents = context.Events.ToList();
                    foreach (var ev in allEvents)
                    {
                        var participantCount = rnd.Next(3, Math.Min(6, allUsers.Count + 1));
                        var shuffledUsers = allUsers.OrderBy(u => rnd.Next()).Take(participantCount).ToList();
                        foreach (var user in shuffledUsers)
                        {
                            context.EventParticipants.Add(new EventParticipant { EventId = ev.Id, UserId = user.Id });
                        }
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
