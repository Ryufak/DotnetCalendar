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
    public class UserManagementTools
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string _baseUrl;

        public UserManagementTools(ApplicationDbContext context, IHttpContextAccessor httpContext, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _httpContext = httpContext;
            _baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5015/api/v2";
        }

        [McpServerTool, Description("Register a new user via the API controller.")]
        public async Task<string> RegisterUserViaApi(RegisterUserDto dto)
        {
            using var httpClient = new HttpClient();
            var apiUrl = $"{_baseUrl}/auth/register";
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new CalendarApi.Converters.DateTimeWithZConverter());
            var json = System.Text.Json.JsonSerializer.Serialize(dto, options);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return "User registered via API";
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }

        [McpServerTool, Description("Login a user via the API controller.")]
        public async Task<string> LoginUserViaApi(LoginUserDto dto)
        {
            using var httpClient = new HttpClient();
            var apiUrl = $"{_baseUrl}/auth/login";
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new CalendarApi.Converters.DateTimeWithZConverter());
            var json = System.Text.Json.JsonSerializer.Serialize(dto, options);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return $"User logged in via API. Token: {responseBody}";
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }

        [McpServerTool, Description("Update user information via the API controller using a JWT token.")]
        public async Task<string> UpdateUserViaApi(UpdateUserDto dto, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/auth/update";
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new CalendarApi.Converters.DateTimeWithZConverter());
            var json = System.Text.Json.JsonSerializer.Serialize(dto, options);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return "User updated via API";
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }

        [McpServerTool, Description("Deletes the authenticated user via the API controller using a JWT token.")]
        public async Task<string> DeleteUserViaApi(string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = $"{_baseUrl}/auth/delete";
            var response = await httpClient.DeleteAsync(apiUrl);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return "User deleted via API";
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }

        [McpServerTool, Description("Fetches users via the API controller. Can fetch all users or a specific user by ID. Requires JWT token.")]
        public async Task<string> FetchUsersViaApi(string jwtToken, bool GetAllUsers = true, int? id = null)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            if (GetAllUsers && id.HasValue)
                throw new ArgumentException("Cannot fetch all users and a specific user at the same time.");
            if (!GetAllUsers && (!id.HasValue || id <= 0))
                throw new ArgumentException("Must provide a valid user ID when not fetching all users.");

            var apiUrl = GetAllUsers ? $"{_baseUrl}/auth/users" : $"{_baseUrl}/auth/{id}";
            var response = await httpClient.GetAsync(apiUrl);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return $"Fetched users via API: {responseBody}";
            }
            else
            {
                var message = $"API error: {(int)response.StatusCode} {response.ReasonPhrase} - {responseBody}";
                throw new InvalidOperationException(message);
            }
        }
    }
}