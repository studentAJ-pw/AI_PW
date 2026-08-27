using System.Text.Json;
using AI_PW.Models;

namespace AI_PW.Services;

public sealed class MissingItemsReportService
{
	private readonly string reportsDirectory;

	public MissingItemsReportService(string reportsDirectory)
	{
		this.reportsDirectory = reportsDirectory;
	}

	public string Save(MissingItemsRequest report)
	{
		Directory.CreateDirectory(reportsDirectory);
		var fileName = $"{SanitizeFileName(report.Game)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";
		var filePath = Path.Combine(reportsDirectory, fileName);
		var reportToSave = new
		{
			game = report.Game,
			missingItems = report.MissingItems,
			createdAt = DateTime.UtcNow,
			status = "new"
		};

		var json = JsonSerializer.Serialize(reportToSave, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(filePath, json);
		return filePath;
	}

	public string List()
	{
		if (!Directory.Exists(reportsDirectory))
		{
			return "[]";
		}

		var reports = Directory.GetFiles(reportsDirectory, "*.json")
			.Select(filePath => new
			{
				filePath,
				content = File.ReadAllText(filePath)
			})
			.ToList();

		return JsonSerializer.Serialize(reports);
	}

	public string GetSummary()
	{
		var itemCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var reportCount = 0;

		if (Directory.Exists(reportsDirectory))
		{
			foreach (var filePath in Directory.GetFiles(reportsDirectory, "*.json"))
			{
				using var json = JsonDocument.Parse(File.ReadAllText(filePath));
				var missingItems = json.RootElement.GetProperty("missingItems");
				reportCount++;

				foreach (var item in missingItems.EnumerateArray())
				{
					var itemName = item.GetString();
					if (!string.IsNullOrWhiteSpace(itemName))
					{
						itemCounts[itemName] = itemCounts.GetValueOrDefault(itemName) + 1;
					}
				}
			}
		}

		var summary = new
		{
			reportCount,
			missingItems = itemCounts
				.OrderByDescending(item => item.Value)
				.Select(item => new { item = item.Key, reports = item.Value })
				.ToList()
		};

		return JsonSerializer.Serialize(summary);
	}

	private static string SanitizeFileName(string value)
	{
		foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
		{
			value = value.Replace(invalidCharacter, '_');
		}

		return value.Trim().Replace(' ', '-');
	}
}