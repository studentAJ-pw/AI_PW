using System.ComponentModel;
using AI_PW.Models;
using AI_PW.Services;
using ModelContextProtocol.Server;

namespace AI_PW.McpServer;

[McpServerToolType]
public sealed class ReportTools(MissingItemsReportService reportService)
{
	[McpServerTool(Name = "save_missing_items"), Description("Zapisuje informacje o brakujacych elementach gry planszowej do pliku JSON.")]
	public string SaveMissingItems(
		[Description("Pelna nazwa gry planszowej.")] string game,
		[Description("Lista elementow, ktorych brakuje.")] string[] missingItems)
	{
		if (string.IsNullOrWhiteSpace(game) || missingItems.Length == 0)
		{
			throw new ArgumentException("Gra i lista brakujacych elementow sa wymagane.");
		}

		var filePath = reportService.Save(new MissingItemsRequest
		{
			Game = game,
			MissingItems = missingItems.Where(item => !string.IsNullOrWhiteSpace(item)).ToList()
		});

		return $"Zapisano raport w pliku {filePath}.";
	}

	[McpServerTool(Name = "list_missing_items"), Description("Odczytuje wszystkie zapisane raporty brakujacych elementow gier.")]
	public string ListMissingItems()
	{
		return reportService.List();
	}

	[McpServerTool(Name = "get_missing_items_summary"), Description("Tworzy zestawienie wszystkich brakujacych elementow, laczac powtarzajace sie pozycje.")]
	public string GetMissingItemsSummary()
	{
		return reportService.GetSummary();
	}
}