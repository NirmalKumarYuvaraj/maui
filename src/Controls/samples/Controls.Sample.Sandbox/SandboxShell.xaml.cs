namespace Maui.Controls.Sample;

public partial class SandboxShell : Shell
{
	public SandboxShell()
	{
		InitializeComponent();
	}
}

public abstract class BlackTabPage : ContentPage
{
	protected BlackTabPage(string title)
	{
		Title = title;
		AutomationId = $"{title}Page";
		BackgroundColor = Colors.Black;

		// Keep native container backgrounds and tab transitions at their defaults.
		var layout = new Grid
		{
			BackgroundColor = Colors.Black,
			Padding = new Thickness(24),
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				new Label
				{
					Text = "Tab-switch flash reproduction\nTap different bottom tabs repeatedly, including tabs already visited.",
					TextColor = Colors.White,
					FontSize = 16,
					HorizontalTextAlignment = TextAlignment.Center
				}
			}
		};
		layout.Add(new Label
		{
			AutomationId = $"{title}Title",
			Text = title,
			TextColor = Colors.White,
			FontSize = 30,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		}, 0, 1);
		layout.Add(new Label
		{
			Text = "Expected: the page and tab bar stay black.\nWatch for a brief grey page flash while the tab bar stays black.",
			TextColor = Colors.White,
			FontSize = 16,
			HorizontalTextAlignment = TextAlignment.Center
		}, 0, 2);
		Content = layout;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		System.Console.WriteLine($"SANDBOX: {Title} appeared at {System.DateTimeOffset.Now:O}");
	}
}

public sealed class HomePage : BlackTabPage
{
	public HomePage() : base("Home") { }
}

public sealed class DrawsPage : BlackTabPage
{
	public DrawsPage() : base("Draws") { }
}

public sealed class InvestmentsPage : BlackTabPage
{
	public InvestmentsPage() : base("Investments") { }
}

public sealed class HoldersPage : BlackTabPage
{
	public HoldersPage() : base("Holders") { }
}