namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 10445, "Shell.Background does not support gradient brushes", PlatformAffected.All)]
public class Issue10445 : TestShell
{
	protected override void Init()
	{
		// Create a gradient brush for the Shell navigation bar background
		var gradientBrush = new LinearGradientBrush
		{
			StartPoint = new Point(0, 0),
			EndPoint = new Point(1, 0),
			GradientStops = new GradientStopCollection
			{
				new GradientStop { Color = Color.FromArgb("#FF6B6B"), Offset = 0.0f },
				new GradientStop { Color = Color.FromArgb("#4ECDC4"), Offset = 1.0f }
			}
		};

		// Create the content page
		var page = CreateContentPage("Gradient Test");
		
		// Set Shell.Background on the page (this should apply to the navigation bar)
		Shell.SetBackground(page, gradientBrush);

		page.Content = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 10,
			Children =
			{
				new Label
				{
					Text = "Shell Background Gradient Test",
					FontSize = 20,
					FontAttributes = FontAttributes.Bold,
					AutomationId = "TitleLabel"
				},
				new Label
				{
					Text = "The navigation bar at the top should show a horizontal gradient from red (#FF6B6B) on the left to cyan (#4ECDC4) on the right.",
					AutomationId = "DescriptionLabel"
				},
				new Label
				{
					Text = "Reference gradient (nav bar should match this):",
					AutomationId = "ReferenceLabel"
				},
				new BoxView
				{
					HeightRequest = 50,
					Background = gradientBrush,
					AutomationId = "ReferenceGradient"
				}
			}
		};
	}
}
