namespace AI_PW.Infrastructure;

public sealed class ErrorLogger
{
	private readonly string logDirectory;
	private readonly object sync = new();

	public ErrorLogger(string logDirectory)
	{
		this.logDirectory = logDirectory;
	}

	public void Log(string message, string details)
	{
		lock (sync)
		{
			Directory.CreateDirectory(logDirectory);
			var logEntry = $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}{details}{Environment.NewLine}{Environment.NewLine}";
			File.AppendAllText(Path.Combine(logDirectory, "errors.log"), logEntry);
		}
	}

	public void Info(string message, string details = "")
	{
		Log(message, details);
	}

	public string ReadAll()
	{
		var logPath = Path.Combine(logDirectory, "errors.log");
		return File.Exists(logPath) ? File.ReadAllText(logPath) : "Brak logów.";
	}
}