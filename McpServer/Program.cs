using AI_PW.Infrastructure;
using AI_PW.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var reportsDirectory = Path.GetFullPath(
	Path.Combine(AppContext.BaseDirectory, "../../../../reports"));
var logger = new ErrorLogger(Path.Combine(reportsDirectory, "..", "logs"));
logger.Info("MCP: serwer uruchomiony", "Transport: stdio");

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(new MissingItemsReportService(reportsDirectory));
builder.Services.AddSingleton(logger);
builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithTools<AI_PW.McpServer.ReportTools>();

await builder.Build().RunAsync();
