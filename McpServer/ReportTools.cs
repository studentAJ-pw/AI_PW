using System.ComponentModel;
using AI_PW.Infrastructure;
using AI_PW.Models;
using AI_PW.Services;
using ModelContextProtocol.Server;

namespace AI_PW.McpServer;

[McpServerToolType]
public sealed class ReportTools(MissingItemsReportService reportService, ErrorLogger logger)
{
	[McpServerTool(Name = "save_missing_items"), Description("Zapisuje informacje o brakujacych elementach gry planszowej do pliku JSON.")]
	public string SaveMissingItems(
		[Description("Pelna nazwa gry planszowej.")] string game,
		[Description("Lista elementow, ktorych brakuje.")] string[] missingItems)
	{
		logger.Info("MCP: odebrano wywolanie save_missing_items", $"Gra: {game}{Environment.NewLine}Elementy: {string.Join(", ", missingItems)}");

		if (string.IsNullOrWhiteSpace(game) || missingItems.Length == 0)
		{
			throw new ArgumentException("Gra i lista brakujacych elementow sa wymagane.");
		}

		var filePath = reportService.Save(new MissingItemsRequest
		{
			Game = game,
			MissingItems = missingItems.Where(item => !string.IsNullOrWhiteSpace(item)).ToList()
		});
		logger.Info("MCP: zapisano raport", $"Gra: {game}{Environment.NewLine}Elementy: {string.Join(", ", missingItems)}");

		return $"Zapisano raport w pliku {filePath}.";
	}

	[McpServerTool(Name = "list_missing_items"), Description("Odczytuje wszystkie zapisane raporty brakujacych elementow gier.")]
	public string ListMissingItems()
	{
		logger.Info("MCP: odebrano wywolanie list_missing_items");
		var result = reportService.List();
		logger.Info("MCP: zakonczono list_missing_items", $"Dlugosc wyniku: {result.Length} znakow");
		return result;
	}

	[McpServerTool(Name = "get_missing_items_summary"), Description("Tworzy zestawienie wszystkich brakujacych elementow, laczac powtarzajace sie pozycje.")]
	public string GetMissingItemsSummary()
	{
		logger.Info("MCP: odebrano wywolanie get_missing_items_summary");
		var result = reportService.GetSummary();
		logger.Info("MCP: zakonczono get_missing_items_summary", $"Dlugosc wyniku: {result.Length} znakow");
		return result;
	}
}