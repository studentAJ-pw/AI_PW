using AI_PW.Clients;
using AI_PW.Infrastructure;
using System.Windows.Forms;

namespace AI_PW;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		var apiKey = Environment.GetEnvironmentVariable("API_KEY");

		if (string.IsNullOrWhiteSpace(apiKey))
		{
			MessageBox.Show("Nie znaleziono zmiennej środowiskowej API_KEY.", "Brak konfiguracji", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		ApplicationConfiguration.Initialize();
		Application.Run(new MainForm(apiKey));
	}
}