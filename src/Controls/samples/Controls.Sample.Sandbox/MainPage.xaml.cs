namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	readonly Label _status = new() { AutomationId = "NavStatus", Text = "READY", FontSize = 16 };
	readonly Label _state = new() { AutomationId = "NavState", FontSize = 12 };

	public MainPage()
	{
		InitializeComponent();
		Content = new Label { Text = "Launch SandboxShell to use the navigation harness." };
	}

	public MainPage(SandboxShell shell, string identity) : this()
	{
		Title = identity;
		AutomationId = "NavPage_" + identity;
		var actions = new VerticalStackLayout { Spacing = 4 };
		actions.Add(new Label
		{
			AutomationId = "NavInstructions",
			Text = "Source: AndroidNavigationArchitectureImprovements.md. Setup resets lazy roots; " +
				"self-tests assert managed/native presentation and each operation's completion. " +
				"Rebuild with SandboxUseLegacyShell=true for legacy parity. " +
				"Root gesture preview, saved-state recreation and resource retention require manual checks.",
			FontSize = 12
		});
		foreach (var (id, title) in SandboxShell.Actions)
		{
			var button = new Button { AutomationId = id, Text = title, FontSize = 13 };
			button.Clicked += async (_, _) => await shell.RunAsync(id);
			actions.Add(button);
		}

		var body = new Grid
		{
			Padding = 10,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		body.Add(new Label { Text = identity, AutomationId = "NavPageIdentity", FontSize = 22 }, 0, 0);
		body.Add(_status, 0, 1);
		body.Add(_state, 0, 2);
		body.Add(new ScrollView { Content = actions }, 0, 3);
		Content = body;
		Appearing += (_, _) => shell.Record($"APPEARING {identity}");
		Disappearing += (_, _) => shell.Record($"DISAPPEARING {identity}");
		HandlerChanged += (_, _) => shell.Record($"HANDLER {identity}: {Handler?.GetType().Name ?? "null"}");
	}

	internal void Update(string status, string state, bool failed)
	{
		_status.Text = status;
		_status.TextColor = failed ? Colors.Red : Colors.DarkGreen;
		_state.Text = state;
	}
}