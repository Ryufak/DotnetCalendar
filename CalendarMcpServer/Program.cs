using CalendarApi.Data;
using CalendarApi.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Configuration;


var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        var connectionString = config.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(opt =>
            opt.UseNpgsql(connectionString));
        services.AddHttpContextAccessor();
        services.AddScoped<UserManagementTools>();
        services.AddScoped<EventManagementTools>();
        services.AddScoped<ParticipantManagementTools>();
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(UserManagementTools).Assembly);
    });

var app = builder.Build();
app.Run();


