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

        [McpServerTool, Description("Fetch participants for an event via the API controller.")]
        public async Task<List<ParticipantDto>> FetchParticipantsViaApi(int eventId, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/{eventId}/participants";
            var response = await httpClient.GetAsync(apiUrl);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<ParticipantDto>>(responseBody) ?? new();
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }

        [McpServerTool, Description("Add participants to an event via the API controller.")]
        public async Task<string> AddParticipantsViaApi(int eventId, List<int> participantIds, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/{eventId}/participants";
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new CalendarApi.Converters.DateTimeWithZConverter());
            var json = System.Text.Json.JsonSerializer.Serialize(participantIds, options);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(apiUrl, content);
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

        [McpServerTool, Description("Remove a participant from an event via the API controller.")]
        public async Task<string> RemoveParticipantViaApi(int eventId, List<int> userIds, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/{eventId}/participants";
            var dto = new RemoveParticipantsDto { UserIds = userIds };
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new CalendarApi.Converters.DateTimeWithZConverter());
            var json = System.Text.Json.JsonSerializer.Serialize(dto, options);
            var request = new System.Net.Http.HttpRequestMessage(HttpMethod.Delete, apiUrl)
            {
                Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            var response = await httpClient.SendAsync(request);
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

        [McpServerTool, Description("Fetch all events for the logged-in user (future, ongoing, and optionally past) via the API controller. Requires JWT token.")]
        public async Task<List<CalendarApi.Dtos.CalendarEventDto>> FetchUserEventsViaApi(FetchUserEventsDto dto, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/events/my-events?includePast={dto.IncludePast.ToString().ToLower()}";
            var response = await httpClient.GetAsync(apiUrl);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions();
                options.Converters.Add(new CalendarApi.Converters.DateTimeWithZConverter());
                var events = System.Text.Json.JsonSerializer.Deserialize<List<CalendarApi.Dtos.CalendarEventDto>>(responseBody, options) ?? new();
                return events.OrderBy(e => e.StartTime).ToList();
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }
        


    }
    
}