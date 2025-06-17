using CalendarApi.Converters;
using System.Text.Json;
using System.Text;
using System.ComponentModel;
using System.Security.Claims;
using CalendarApi.Dtos;
using CalendarApi.Models;
using CalendarApi.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System.Security.Cryptography;




namespace CalendarApi.Tools
{

    [McpServerToolType]
    public class EventManagementTools
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string _baseUrl;

        private string PatchDateTimeMilliseconds(string json)
        {

            json = json.Replace(":00\"", ":00.000Z\"");
            json = json.Replace(":30\"", ":30.000Z\"");
            return json;
        }

        public EventManagementTools(ApplicationDbContext context, IHttpContextAccessor httpContext, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _httpContext = httpContext;
            _baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5015/api/v2";
        }

        [McpServerTool, Description("Create a new calendar event via the API controller.")]
        public async Task<string> CreateEventViaApi(CreateEventDto dto, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/create";
            var options = new JsonSerializerOptions();
            options.Converters.Add(new DateTimeWithZConverter());

            var json = JsonSerializer.Serialize(dto, options);
            json = PatchDateTimeMilliseconds(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return responseBody;
            }
            else
            {
                // Always include status code, reason, and response body for LLM context
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                // Optionally, you could return this as a string instead of throwing, or wrap in a result object
                throw new InvalidOperationException(message);
            }
        }

        [McpServerTool, Description("Fetch events via the API controller. You can specify whether to get all events, a date range or a specific event by ID. Requires JWT token.")]
        public async Task<string> FetchEventsViaApi(string jwtToken, bool GetAllEvents = true, int? eventId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            string apiUrl;

            if (GetAllEvents)
                apiUrl = $"{_baseUrl}/events/get-all";
            else if (eventId.HasValue)
                apiUrl = $"{_baseUrl}/events/{eventId.Value}";
            else if (startDate.HasValue && endDate.HasValue)
            {
                // Format using DateTimeWithZConverter for query params
                var options = new JsonSerializerOptions();
                options.Converters.Add(new DateTimeWithZConverter());
                var fromStr = JsonSerializer.Serialize(startDate.Value, options).Trim('"');
                var toStr = JsonSerializer.Serialize(endDate.Value, options).Trim('"');
                apiUrl = $"{_baseUrl}/events?from={fromStr}&to={toStr}";
            }
            else
                throw new ArgumentException("Start and end dates must be provided for fetching events in a date range.");

            var response = await httpClient.GetAsync(apiUrl);
            var json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                // Try to deserialize as a list first, then as a single event (for eventId)
                var options = new JsonSerializerOptions();
                options.Converters.Add(new DateTimeWithZConverter());
                try
                {
                    var events = JsonSerializer.Deserialize<List<CalendarApi.Dtos.CalendarEventDto>>(json, options);
                    if (events != null)
                        return JsonSerializer.Serialize(events, options);
                }
                catch { /* Not a list, try single event */ }
                try
                {
                    var singleEvent = JsonSerializer.Deserialize<CalendarApi.Dtos.CalendarEventDto>(json, options);
                    if (singleEvent != null)
                        return JsonSerializer.Serialize(new List<CalendarApi.Dtos.CalendarEventDto> { singleEvent }, options);
                }
                catch { }
                // If neither, just return the raw JSON
                return json;
            }
            else
            {
                var error = json;
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
            var apiUrl = $"{_baseUrl}/events/update/{eventId}";
            var options = new JsonSerializerOptions();
            options.Converters.Add(new DateTimeWithZConverter());
            var json = JsonSerializer.Serialize(dto, options);
            json = PatchDateTimeMilliseconds(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return responseBody;
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }

        [McpServerTool, Description("Delete an existing calendar event via the API controller.")]
        public async Task<string> DeleteEventViaApi(int eventId, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/delete/{eventId}";
            var response = await httpClient.DeleteAsync(apiUrl);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return responseBody;
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }

        [McpServerTool, Description("Find free time slots for a group of users via the API controller. Requires JWT token.")]
        public async Task<string> FindFreeSlotsViaApi(FreeSlotRequestDto dto, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/free-slots";
            var options = new JsonSerializerOptions();
            options.Converters.Add(new DateTimeWithZConverter());
            var json = JsonSerializer.Serialize(dto, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var slots = JsonSerializer.Deserialize<List<CalendarApi.Dtos.TimeSlotDto>>(responseBody, options) ?? new();
                return JsonSerializer.Serialize(slots, options);
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }
        

    }
}