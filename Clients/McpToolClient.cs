using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace AI_PW.Clients;

internal sealed class McpToolClient : IAsyncDisposable
{
	private readonly McpClient client;

	private McpToolClient(McpClient client)
	{
		this.client = client;
	}

	public static async Task<McpToolClient> CreateAsync(string serverProjectPath)
	{
		var transport = new StdioClientTransport(new StdioClientTransportOptions
		{
			Name = "AI PW Reports MCP Server",
			Command = "dotnet",
			Arguments = ["run", "--project", serverProjectPath],
			InheritEnvironmentVariables = true
		});

		var client = await McpClient.CreateAsync(transport);
		return new McpToolClient(client);
	}

	public async Task<string> CallToolAsync(string name, IReadOnlyDictionary<string, object?> arguments)
	{
		var result = await client.CallToolAsync(name, arguments);
		return string.Join(
			Environment.NewLine,
			result.Content.OfType<TextContentBlock>().Select(content => content.Text));
	}

	public async ValueTask DisposeAsync()
	{
		await client.DisposeAsync();
	}
}