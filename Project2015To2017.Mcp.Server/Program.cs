using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Project2015To2017.Mcp.Server.Logging;

var builder = Host.CreateApplicationBuilder(args);

// CRITICAL for stdio transport: stdout carries the JSON-RPC protocol, so no logging may go
// there. Remove all default providers and route migration log output to an in-memory scope
// that each tool call drains into its result instead.
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// The same instance is used both as a logging provider (so MigrationFacility's ILogger is
// captured) and as an injectable singleton (so the tool can open/drain a capture scope).
var capturing = new CapturingLoggerProvider();
builder.Logging.AddProvider(capturing);
builder.Services.AddSingleton(capturing);

builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithToolsFromAssembly();

await builder.Build().RunAsync();
