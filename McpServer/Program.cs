using AI_PW.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var reportsDirectory = Path.GetFullPath(
	Path.Combine(AppContext.BaseDirectory, "../../../../reports"));

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(new MissingItemsReportService(reportsDirectory));
builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithTools<AI_PW.McpServer.ReportTools>();

await builder.Build().RunAsync();
