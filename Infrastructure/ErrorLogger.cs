namespace AI_PW.Infrastructure;

internal sealed class ErrorLogger
{
	private readonly string logDirectory;

	public ErrorLogger(string logDirectory)
	{
		this.logDirectory = logDirectory;
	}

	public void Log(string message, string details)
	{
		Directory.CreateDirectory(logDirectory);
		var logEntry = $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}{details}{Environment.NewLine}{Environment.NewLine}";
		File.AppendAllText(Path.Combine(logDirectory, "errors.log"), logEntry);
	}
}