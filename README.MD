### Configuration and setup

1. We're using PostgreSQL, so you need to update the appsettings.Development.json/appsettings.json file with the appropriate database credentials.

2. Install the dependencies.
```
dotnet restore
```

### Building and running the api

1. Clean and then build the api.
```
dotnet clean
dotnet build
```

Sometimes this does not build the MCP api, so you'll need to use:
```
dotnet build CalendarMcpServer/CalendarMcpServer.csproj
```

2. Apply database migrations.
```
dotnet ef database update --project CalendarApi/CalendarApi.csproj
```

3. Run the MCP Server using the .vscode/mcp.json file or by using (and wait for it to fully initialize):
```
dotnet run --project CalendarMcpServer/CalendarMcpServer.csproj
```

4. Run the main api.
```
dotnet run --project CalendarApi/CalendarApi.csproj
```

5. You should now be able to test both the endpoints and the MCP integration.


### Additional notes

- I've set up some initial database entries - both users and events that can be used in some testing cases.

- I've added Swagger to the project. Most endpoints require authorization, so you'll initially need to create an account and log in tp get the JWT. I've set the validity to 24h, so you can save it in a file when testing for easier access.

- Copilot also needs to authenticate with a JWT, so you can decide if it's going to use your credentials or if you'll ask him to create an account automatically. Keep in mind that copilot sometimes forgets the token, so you might need to prompt it to log in again (or not, as it could automatically remember due to how the McpServerTool Descriptions are written.)

- I've implemented the basic MCP tools - CRUD operations for users, events and participants and finding timeslots and some other lookups. I tried to add other tools acting as validations when providing vague information in the prompt (e.g. if we have two users called John and the prompt is "Add John to that event"), but I don't think I can make it work in time. That's 
also true for a lot of other use cases that come to mind, but I think the current solution checks the homework requirements.

- I tried using a http mcp server but to no avail (and lost quite some time), so I went with stdio and just split the api in two.

- Also tried to integrate OpenAI API but had some problems with the keys and their endpoint, so I gave up on that. It was the right call, because having to connect the OpenAI LLM to postman/curl (and who knows what else) are extra steps that I'm glad I avoided.

