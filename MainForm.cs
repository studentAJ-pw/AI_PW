using AI_PW.Clients;
using AI_PW.Infrastructure;
using System.Drawing;
using System.Windows.Forms;

namespace AI_PW;

internal sealed class MainForm : Form
{
	private readonly TextBox messageTextBox = new();
	private readonly TextBox conversationTextBox = new();
	private readonly TextBox logsTextBox = new();
	private readonly Button sendButton = new();
	private readonly Button refreshLogsButton = new();
	private readonly Label statusLabel = new();
	private readonly ErrorLogger errorLogger = new("logs");
	private McpToolClient? mcpClient;
	private OpenRouterClient? openRouterClient;
	private bool isBusy;

	public MainForm(string apiKey)
	{
		Text = "Agent raportów gier planszowych";
		StartPosition = FormStartPosition.CenterScreen;
		MinimumSize = new Size(620, 420);
		Size = new Size(760, 520);

		var titleLabel = new Label
		{
			Dock = DockStyle.Top,
			Text = "Agent brakujących elementów",
			Font = new Font("Segoe UI", 16, FontStyle.Bold),
			Padding = new Padding(16, 14, 16, 8),
			Height = 52
		};

		conversationTextBox.Multiline = true;
		conversationTextBox.ReadOnly = true;
		conversationTextBox.ScrollBars = ScrollBars.Vertical;
		conversationTextBox.Dock = DockStyle.Fill;
		conversationTextBox.Font = new Font("Segoe UI", 10);
		conversationTextBox.BackColor = Color.White;

		logsTextBox.Multiline = true;
		logsTextBox.ReadOnly = true;
		logsTextBox.ScrollBars = ScrollBars.Both;
		logsTextBox.WordWrap = false;
		logsTextBox.Dock = DockStyle.Fill;
		logsTextBox.Font = new Font("Consolas", 9);
		logsTextBox.BackColor = Color.FromArgb(248, 248, 248);

		refreshLogsButton.Text = "Odśwież logi";
		refreshLogsButton.Dock = DockStyle.Bottom;
		refreshLogsButton.Height = 36;
		refreshLogsButton.Click += (_, _) => RefreshLogs();
		var logsPanel = new Panel { Dock = DockStyle.Fill };
		logsPanel.Controls.Add(logsTextBox);
		logsPanel.Controls.Add(refreshLogsButton);

		var inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 92, Padding = new Padding(16, 10, 16, 12) };
		messageTextBox.Multiline = true;
		messageTextBox.ScrollBars = ScrollBars.Vertical;
		messageTextBox.Dock = DockStyle.Fill;
		messageTextBox.Font = new Font("Segoe UI", 10);
		messageTextBox.KeyDown += MessageTextBox_KeyDown;

		sendButton.Text = "Wyślij";
		sendButton.Dock = DockStyle.Right;
		sendButton.Width = 100;
		sendButton.Margin = new Padding(10, 0, 0, 0);
		sendButton.Click += async (_, _) => await SendMessageAsync();

		statusLabel.Text = "Gotowy";
		statusLabel.Dock = DockStyle.Bottom;
		statusLabel.Height = 24;
		statusLabel.Padding = new Padding(16, 2, 16, 2);
		statusLabel.ForeColor = Color.DimGray;
		messageTextBox.Enabled = false;
		sendButton.Enabled = false;

		inputPanel.Controls.Add(messageTextBox);
		inputPanel.Controls.Add(sendButton);
		var tabs = new TabControl { Dock = DockStyle.Fill };
		tabs.TabPages.Add(new TabPage("Rozmowa") { Controls = { conversationTextBox, inputPanel } });
		tabs.TabPages.Add(new TabPage("Logi") { Controls = { logsPanel } });
		Controls.Add(tabs);
		Controls.Add(statusLabel);
		Controls.Add(titleLabel);
		Load += async (_, _) => await InitializeAsync(apiKey);
		RefreshLogs();
		FormClosed += async (_, _) => await DisposeClientsAsync();
	}

	private async Task InitializeAsync(string apiKey)
	{
		try
		{
			statusLabel.Text = "Łączenie z MCP...";
			var mcpServerProject = Path.GetFullPath(
				Path.Combine(AppContext.BaseDirectory, "../../../McpServer/McpServer.csproj"));
			mcpClient = await McpToolClient.CreateAsync(mcpServerProject);
			openRouterClient = new OpenRouterClient(apiKey, errorLogger, mcpClient);
			messageTextBox.Enabled = true;
			sendButton.Enabled = true;
			statusLabel.Text = "Gotowy";
			RefreshLogs();
			messageTextBox.Focus();
		}
		catch (Exception exception)
		{
			errorLogger.Log("Nie mozna uruchomic klienta MCP", exception.ToString());
			statusLabel.Text = "Nie udało się połączyć z MCP. Szczegóły są w logs/errors.log";
		}
	}

	private async void MessageTextBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Enter && e.Control)
		{
			e.SuppressKeyPress = true;
			await SendMessageAsync();
		}
	}

	private async Task SendMessageAsync()
	{
		if (isBusy || string.IsNullOrWhiteSpace(messageTextBox.Text))
		{
			return;
		}

		var userInput = messageTextBox.Text.Trim();
		messageTextBox.Clear();
		conversationTextBox.AppendText($"Ty: {userInput}{Environment.NewLine}{Environment.NewLine}");
		SetBusy(true);

		try
		{
			if (openRouterClient is null)
			{
				return;
			}

			var answer = await openRouterClient.SendMessageAsync(userInput);
			conversationTextBox.AppendText($"Agent: {answer ?? "Brak odpowiedzi."}{Environment.NewLine}{Environment.NewLine}");
			statusLabel.Text = "Gotowy";
			RefreshLogs();
		}
		catch (Exception exception)
		{
			errorLogger.Log("Nieobsluzony wyjatek aplikacji", exception.ToString());
			conversationTextBox.AppendText("Agent: Wystąpił błąd. Szczegóły zapisano w logs/errors.log.\r\n\r\n");
			statusLabel.Text = "Błąd - szczegóły zapisano w logs/errors.log";
			RefreshLogs();
		}
		finally
		{
			SetBusy(false);
		}
	}

	private void RefreshLogs()
	{
		logsTextBox.Text = errorLogger.ReadAll();
		logsTextBox.SelectionStart = logsTextBox.TextLength;
		logsTextBox.ScrollToCaret();
	}

	private async Task DisposeClientsAsync()
	{
		openRouterClient?.Dispose();
		if (mcpClient is not null)
		{
			await mcpClient.DisposeAsync();
		}
	}

	private void SetBusy(bool busy)
	{
		isBusy = busy;
		sendButton.Enabled = !busy;
		messageTextBox.Enabled = !busy;
		statusLabel.Text = busy ? "Agent pracuje..." : "Gotowy";
	}
}