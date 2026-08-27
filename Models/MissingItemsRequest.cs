namespace AI_PW.Models;

public sealed class MissingItemsRequest
{
	public string Game { get; set; } = string.Empty;
	public List<string> MissingItems { get; set; } = new();
}