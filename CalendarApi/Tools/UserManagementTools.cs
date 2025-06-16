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

        public UserManagementTools(ApplicationDbContext context, IHttpContextAccessor httpContext)
        {
            _context = context;
            _httpContext = httpContext;
        }

        [McpServerTool, Description("Register a new user via the API controller.")]
        public async Task<string> RegisterUserViaApi(RegisterUserDto dto)
        {
            using var httpClient = new HttpClient();
            var apiUrl = "http://localhost:5015/api/auth/register";
            var response = await httpClient.PostAsJsonAsync(apiUrl, dto);
            if (response.IsSuccessStatusCode)
            {
                return "User registered via API";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"API error: {error}");
            }
        }

        [McpServerTool, Description("Login a user via the API controller.")]
        public async Task<string> LoginUserViaApi(LoginUserDto dto)
        {
            using var httpClient = new HttpClient();
            var apiUrl = "http://localhost:5015/api/auth/login";
            var response = await httpClient.PostAsJsonAsync(apiUrl, dto);
            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadAsStringAsync();
                return $"User logged in via API. Token: {token}";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"API error: {error}");
            }
        }

        [McpServerTool, Description("Update user information via the API controller using a JWT token.")]
        public async Task<string> UpdateUserViaApi(UpdateUserDto dto, string jwtToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            var apiUrl = "http://localhost:5015/api/auth";
            var response = await httpClient.PutAsJsonAsync(apiUrl, dto);
            if (response.IsSuccessStatusCode)
            {
                return "User updated via API";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"API error: {error}");
            }
        }

        
    }
}