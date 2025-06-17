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
    public class ParticipantManagementTools

    {

        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string _baseUrl;

        public ParticipantManagementTools(ApplicationDbContext context, IHttpContextAccessor httpContext, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _httpContext = httpContext;
            _baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5015/api/v2";
        }

        [McpServerTool, Description("Add a participant to an event by name. If ambiguous, returns possible matches for clarification.")]
        public async Task<string> AddParticipantByName(int eventId, string name, string jwtToken)
        {
            // Search users by first or last name (case-insensitive, partial match)
            var matches = await _context.Users
                .Where(u => (!string.IsNullOrEmpty(u.FirstName) && u.FirstName.ToLower().Contains(name.ToLower())) || (!string.IsNullOrEmpty(u.LastName) && u.LastName.ToLower().Contains(name.ToLower())))
                .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName })
                .ToListAsync();

            if (matches.Count == 0)
                return $"No users found matching '{name}'. Would you like to create a new user? Please provide: username, email, first name, last name.";
            if (matches.Count == 1)
            {
                // Add the single match
                var userId = matches[0].Id;
                return await AddParticipantsViaApi(eventId, new List<int> { userId }, jwtToken);
            }
            // Multiple matches, return list for clarification
            var options = string.Join("\n", matches.Select(u => $"ID: {u.Id}, Username: {u.Username}, Name: {u.FirstName} {u.LastName}"));
            return $"Multiple users found matching '{name}':\n{options}\nPlease specify the user ID to add.";
        }

        [McpServerTool, Description("Fetch participants for an event via the API controller.")]
        public async Task<List<ParticipantDto>> FetchParticipantsViaApi(int eventId, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/{eventId}/participants";
            var response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<List<ParticipantDto>>(json) ?? new();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(error))
                    error = "<empty response body>";
                throw new InvalidOperationException($"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
            }
        }

        [McpServerTool, Description("Add participants to an event via the API controller.")]
        public async Task<string> AddParticipantsViaApi(int eventId, List<int> participantIds, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/{eventId}/participants";
            var response = await httpClient.PostAsJsonAsync(apiUrl, participantIds);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(error))
                    error = "<empty response body>";
                throw new InvalidOperationException($"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
            }
        }

        [McpServerTool, Description("Remove a participant from an event via the API controller.")]
        public async Task<string> RemoveParticipantViaApi(int eventId, int userId, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/{eventId}/participants/{userId}";
            var response = await httpClient.DeleteAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(error))
                    error = "<empty response body>";
                throw new InvalidOperationException($"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {error}");
            }
        }
        
    }
    
}