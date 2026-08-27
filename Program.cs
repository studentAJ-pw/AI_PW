using AI_PW.Clients;
using AI_PW.Infrastructure;

namespace AI_PW;

internal static class Program
{
	private static async Task Main()
	{
		Console.WriteLine("Agent raportow brakow gier planszowych");
		Console.WriteLine("Wpisz 'exit' lub 'quit', aby zakończyć.\n");

		var apiKey = Environment.GetEnvironmentVariable("API_KEY");

		if (string.IsNullOrWhiteSpace(apiKey))
		{
			Console.Error.WriteLine("Nie znaleziono zmiennej środowiskowej API_KEY.");
			return;
		}

		var errorLogger = new ErrorLogger("logs");
		var mcpServerProject = Path.GetFullPath(
			Path.Combine(AppContext.BaseDirectory, "../../../McpServer/McpServer.csproj"));
		await using var mcpClient = await McpToolClient.CreateAsync(mcpServerProject);
		using var openRouterClient = new OpenRouterClient(apiKey, errorLogger, mcpClient);

		while (true)
		{
			Console.Write("Napisz wiadomość: ");
			var userInput = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(userInput))
			{
				continue;
			}

			if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
				userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
			{
				break;
			}

			try
			{
				var answer = await openRouterClient.SendMessageAsync(userInput);
				if (answer is not null)
				{
					Console.WriteLine(answer);
				}
			}
			catch (Exception exception)
			{
				errorLogger.Log("Nieobsluzony wyjatek aplikacji", exception.ToString());
				Console.Error.WriteLine("Wystapil nieoczekiwany blad. Szczegoly zapisano w logs/errors.log.");
			}
		}
	}
}