[UNDER CONSTRUCTION - TO BE UPDATED]

Build instructions:

CalendarMcpServer's build path is different than the one for CalendarApi, so we need to build both. 
Sometimes, when you try to build from the root dir, it only builds CalendarApi, so you'll need to cd 
into CalendarMcpServer and build there too.

When running, be sure to run the server from the mcp.json file first and once it's initialized, start the
CalendarApi by using dotnet run/watch.

You need to update your appsettings file with the database credentials.







Additional comments:

Most endpoints require authorization, so using a JWC token is a must, be it in swagger or through the MCP server.
I've updated the token's lifespan to 24h, but copilot sometimes just loses the token and needs to reauthenticate. I can't really control that, but I added a line that the token is required in the McpServerTool Descriptions and it works, so that's resolved.
Worst case, you just have to remind it. 

I've implemented the basic MCP tools - CRUD operations for users, events and participants and finding timeslots.
I tried to add other tools acting as validations when providing vague information in the prompt (e.g. if we have two
users called John and the prompt is "Add John to that event"), but I don't think I can make it work in time. That's 
also true for a lot of other use cases that come to mind.

I tried using a http mcp server but to no avail (and lost quite some time), so I went with stdio and just split the api in two.

Also tried to integrate OpenAI API but had some problems with the keys and their endpoint, so I gave up on that. It was the right
call, because having to connect the OpenAI LLM to postman/curl (and who knows what else) are extra steps that I'm glad I avoided.

Overall, given the fact that I didn't know anything about C# and dotnet 3 days ago, I think I did fine.
I can brag all I want, most people don't read these :^)
