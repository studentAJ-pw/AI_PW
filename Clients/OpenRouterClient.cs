using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AI_PW.Infrastructure;
using AI_PW.Models;
using AI_PW.Services;

namespace AI_PW.Clients;

internal sealed class OpenRouterClient : IDisposable
{
	private const string OpenRouterUrl = "https://openrouter.ai/api/v1/chat/completions";
	private const string Model = "openrouter/free";
	private readonly HttpClient httpClient;
	private readonly ErrorLogger errorLogger;
	private readonly McpToolClient mcpClient;

	public OpenRouterClient(string apiKey, ErrorLogger errorLogger, McpToolClient mcpClient)
	{
		this.errorLogger = errorLogger;
		this.mcpClient = mcpClient;
		httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", apiKey.Trim());
		httpClient.DefaultRequestHeaders.Add("X-Title", "AI_PW Console Chat");
	}

	public async Task<string?> SendMessageAsync(string userInput)
	{
		var messages = new List<object>
		{
			new { role = "user", content = userInput }
		};
		var tools = CreateTools();
		var requestBody = JsonSerializer.Serialize(new { model = Model, messages, tools, tool_choice = "auto" });
		var responseBody = await SendRequestAsync(requestBody);

		if (responseBody is null)
		{
			return null;
		}

		using var json = JsonDocument.Parse(responseBody);
		var message = json.RootElement.GetProperty("choices")[0].GetProperty("message");

		if (!message.TryGetProperty("tool_calls", out var toolCalls))
		{
			return message.GetProperty("content").GetString();
		}

		var assistantToolCalls = new List<object>();
		foreach (var toolCall in toolCalls.EnumerateArray())
		{
			var function = toolCall.GetProperty("function");
			var functionName = function.GetProperty("name").GetString();
			var argumentsJson = function.GetProperty("arguments").GetString() ?? "{}";

			string toolResult;
			if (functionName == "save_missing_items")
			{
				var arguments = ParseMissingItemsRequest(argumentsJson);

				if (arguments is null || string.IsNullOrWhiteSpace(arguments.Game) || arguments.MissingItems.Count == 0)
				{
					Console.Error.WriteLine("Model przekazal niepoprawne dane raportu.");
					Console.Error.WriteLine($"Argumenty funkcji: {argumentsJson}");
					errorLogger.Log("Niepoprawne argumenty funkcji save_missing_items", argumentsJson);
					return null;
				}

				toolResult = await mcpClient.CallToolAsync(
					functionName,
					new Dictionary<string, object?>
					{
						["game"] = arguments.Game,
						["missingItems"] = arguments.MissingItems
					});
			}
			else if (functionName == "list_missing_items")
			{
				toolResult = await mcpClient.CallToolAsync(functionName, new Dictionary<string, object?>());
			}
			else if (functionName == "get_missing_items_summary")
			{
				toolResult = await mcpClient.CallToolAsync(functionName, new Dictionary<string, object?>());
			}
			else
			{
				continue;
			}

			assistantToolCalls.Add(new
			{
				id = toolCall.GetProperty("id").GetString(),
				type = "function",
				function = new { name = functionName, arguments = argumentsJson }
			});
			messages.Add(new
			{
				role = "tool",
				tool_call_id = toolCall.GetProperty("id").GetString(),
				content = toolResult
			});
		}

		messages.Insert(1, new { role = "assistant", content = (string?)null, tool_calls = assistantToolCalls });
		var finalRequestBody = JsonSerializer.Serialize(new { model = Model, messages, tools });
		var finalResponseBody = await SendRequestAsync(finalRequestBody);

		if (finalResponseBody is null)
		{
			return null;
		}

		using var finalJson = JsonDocument.Parse(finalResponseBody);
		return finalJson.RootElement.GetProperty("choices")[0]
			.GetProperty("message").GetProperty("content").GetString();
	}

	public void Dispose()
	{
		httpClient.Dispose();
	}

	private async Task<string?> SendRequestAsync(string requestBody)
	{
		using var requestContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
		using var response = await httpClient.PostAsync(OpenRouterUrl, requestContent);
		var responseBody = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			Console.Error.WriteLine($"OpenRouter zwrócił błąd {(int)response.StatusCode}.");
			Console.Error.WriteLine(responseBody);
			errorLogger.Log($"OpenRouter zwrócił błąd {(int)response.StatusCode}", responseBody);
			return null;
		}

		return responseBody;
	}

	private static object[] CreateTools()
	{
		return new object[]
		{
			new
			{
				type = "function",
				function = new
				{
					name = "save_missing_items",
					description = "Zapisuje informacje o brakujacych elementach gry planszowej do pliku JSON.",
					parameters = new
					{
						type = "object",
						properties = new
						{
							game = new { type = "string", description = "Pelna nazwa gry planszowej." },
							missingItems = new
							{
								type = "array",
								items = new { type = "string" },
								description = "Lista elementow, ktorych brakuje."
							}
						},
						required = new[] { "game", "missingItems" }
					}
				}
			},
			new
			{
				type = "function",
				function = new
				{
					name = "list_missing_items",
					description = "Odczytuje wszystkie zapisane raporty brakujacych elementow gier.",
					parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
				}
			},
			new
			{
				type = "function",
				function = new
				{
					name = "get_missing_items_summary",
					description = "Tworzy zestawienie wszystkich brakujacych elementow, laczac powtarzajace sie pozycje.",
					parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
				}
			}
		};
	}

	private static MissingItemsRequest? ParseMissingItemsRequest(string argumentsJson)
	{
		try
		{
			using var json = JsonDocument.Parse(argumentsJson);
			var root = json.RootElement;
			var game = FindProperty(root, "game", "gameName", "game_name", "gra");
			var missingItems = FindProperty(root, "missingItems", "missing_items", "missingElements", "missing_elements", "items", "braki");

			if (game is null || missingItems is null)
			{
				return null;
			}

			var items = missingItems.Value.ValueKind == JsonValueKind.Array
				? missingItems.Value.EnumerateArray()
					.Where(item => item.ValueKind == JsonValueKind.String)
					.Select(item => item.GetString() ?? string.Empty)
					.Where(item => !string.IsNullOrWhiteSpace(item))
					.ToList()
				: missingItems.Value.ValueKind == JsonValueKind.String
					? new List<string> { missingItems.Value.GetString() ?? string.Empty }
					: new List<string>();

			return new MissingItemsRequest
			{
				Game = game.Value.GetString() ?? string.Empty,
				MissingItems = items
			};
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static JsonElement? FindProperty(JsonElement objectElement, params string[] names)
	{
		if (objectElement.ValueKind != JsonValueKind.Object)
		{
			return null;
		}

		foreach (var property in objectElement.EnumerateObject())
		{
			if (names.Any(name => NormalizePropertyName(name) == NormalizePropertyName(property.Name)))
			{
				return property.Value;
			}
		}

		return null;
	}

	private static string NormalizePropertyName(string value)
	{
		return value.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
	}
}