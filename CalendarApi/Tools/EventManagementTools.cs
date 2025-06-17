using System.ComponentModel;
using System.Security.Claims;
using CalendarApi.Dtos;
using CalendarApi.Models;
using CalendarApi.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System.Security.Cryptography;
using System.Text;



namespace CalendarApi.Tools
{

    [McpServerToolType]
    public class EventManagementTools
    {

        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string _baseUrl;

        public EventManagementTools(ApplicationDbContext context, IHttpContextAccessor httpContext, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _httpContext = httpContext;
            _baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5015/api/v2";
        }

        [McpServerTool, Description("Find an event by title. If ambiguous, returns possible matches for clarification.")]
        public async Task<string> FindEventByTitle(string title, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events";
            var response = await httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(error)) error = "<empty response body>";
                throw new InvalidOperationException($"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
            }
            var json = await response.Content.ReadAsStringAsync();
            var events = System.Text.Json.JsonSerializer.Deserialize<List<CalendarApi.Dtos.CalendarEventDto>>(json) ?? new();
            var matches = events.Where(e => e.Title != null && e.Title.ToLower().Contains(title.ToLower())).ToList();
            if (matches.Count == 0)
                return $"No events found matching '{title}'. Would you like to create a new event? Please provide: title, description, start time, end time, participants.";
            if (matches.Count == 1)
                return $"EventId: {matches[0].Id}";
            var options = string.Join("\n", matches.Select(e => $"ID: {e.Id}, Title: {e.Title}, Start: {e.StartTime:yyyy-MM-dd HH:mm}, End: {e.EndTime:yyyy-MM-dd HH:mm}"));
            return $"Multiple events found matching '{title}':\n{options}\nPlease specify the event ID.";
        }


        [McpServerTool, Description("Create a new calendar event via the API controller.")]
        public async Task<string> CreateEventViaApi(CreateEventDto dto, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events";
            var response = await httpClient.PostAsJsonAsync(apiUrl, dto);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return json;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(error))
                    error = "<empty response body>";
                throw new InvalidOperationException($"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
            }
        }

        [McpServerTool, Description("Fetch events via the API controller. You can specify whether to get all events or a date range. Requires JWT token.")]
        public async Task FetchEventsViaApi(string jwtToken, bool GetAllEvents = true, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            string apiUrl;

            if (GetAllEvents)
                apiUrl = $"{_baseUrl}/events";
            else
            {
                if (!startDate.HasValue || !endDate.HasValue)
                    throw new ArgumentException("Start and end dates must be provided for fetching events in a date range.");

                apiUrl = $"{_baseUrl}/events?from={startDate.Value:yyyy-MM-dd}&to={endDate.Value:yyyy-MM-dd}";
            }

            var response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var events = System.Text.Json.JsonSerializer.Deserialize<List<CalendarApi.Dtos.CalendarEventDto>>(json) ?? new();
                Console.WriteLine($"Fetched events: {System.Text.Json.JsonSerializer.Serialize(events)}");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(error))
                    error = "<empty response body>";
                throw new InvalidOperationException($"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
            }
        }

        [McpServerTool, Description("Update an existing calendar event via the API controller.")]
        public async Task<string> UpdateEventViaApi(int eventId, UpdateEventDto dto, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/{eventId}";
            var response = await httpClient.PutAsJsonAsync(apiUrl, dto);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return json;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(error))
                    error = "<empty response body>";
                throw new InvalidOperationException($"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
            }
        }

        [McpServerTool, Description("Delete an existing calendar event via the API controller.")]
        public async Task<string> DeleteEventViaApi(int eventId, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/{eventId}";
            var response = await httpClient.DeleteAsync(apiUrl);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(error))
                    error = "<empty response body>";
                throw new InvalidOperationException($"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
            }
        }


        [McpServerTool, Description("Find free time slots for a group of users via the API controller. Requires JWT token.")]
        public async Task<string> FindFreeSlotsViaApi(FreeSlotRequestDto dto, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/free-slots";
            var response = await httpClient.PostAsJsonAsync(apiUrl, dto);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var slots = System.Text.Json.JsonSerializer.Deserialize<List<CalendarApi.Dtos.TimeSlotDto>>(json) ?? new();
                return System.Text.Json.JsonSerializer.Serialize(slots);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"API error: {error}");
            }
        }

        

    }
}