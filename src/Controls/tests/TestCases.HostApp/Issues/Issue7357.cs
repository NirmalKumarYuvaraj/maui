namespace Maui.Controls.Sample.Issues;

// The main test entry point - wrap in NavigationPage to show toolbar
[Issue(IssueTracker.Github, 7357, "[Android] FontImageSource.Size property not working in ToolbarItem", PlatformAffected.Android)]
public class Issue7357 : NavigationPage
{
	public Issue7357() : base(new Issue7357ContentPage())
	{
	}
}

// The actual content page with the ToolbarItem
public class Issue7357ContentPage : ContentPage
{
	public Issue7357ContentPage()
	{
		Title = "Issue 7357";

		// Create a ToolbarItem with FontImageSource and Size=40
		// On Android, the Size property is ignored and the icon renders at default 24x24 dp
		// On iOS/MacCatalyst, the Size property works correctly
		var toolbarItem = new ToolbarItem
		{
			Text = "Test",
			IconImageSource = new FontImageSource
			{
				FontFamily = "FA",
				Glyph = "\xf133", // Calendar icon
				Size = 40,
				Color = Colors.Black
			}
		};

		ToolbarItems.Add(toolbarItem);

		// Create reference images at different sizes to compare
		// This helps visualize the expected vs actual sizes
		var expectedSizeImage = new Image
		{
			AutomationId = "ExpectedSizeImage",
			Source = new FontImageSource
			{
				FontFamily = "FA",
				Glyph = "\xf133",
				Size = 40,
				Color = Colors.Black
			},
			WidthRequest = 40,
			HeightRequest = 40,
			HorizontalOptions = LayoutOptions.Start
		};

		var defaultSizeImage = new Image
		{
			AutomationId = "DefaultSizeImage",
			Source = new FontImageSource
			{
				FontFamily = "FA",
				Glyph = "\xf133",
				Size = 24, // Android default toolbar icon size
				Color = Colors.Black
			},
			WidthRequest = 24,
			HeightRequest = 24,
			HorizontalOptions = LayoutOptions.Start
		};

		Content = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 20,
			Children =
			{
				new Label
				{
					AutomationId = "InstructionLabel",
					Text = "The ToolbarItem icon should be 40x40 pixels.\n" +
						   "On Android, it incorrectly renders at 24x24 (default size).\n\n" +
						   "Compare the toolbar icon size with the reference images below:",
					FontSize = 14
				},
				new Label
				{
					Text = "Expected size (40x40):",
					FontSize = 12
				},
				expectedSizeImage,
				new Label
				{
					Text = "Default Android size (24x24 - this is the bug):",
					FontSize = 12
				},
				defaultSizeImage,
				new Label
				{
					AutomationId = "StatusLabel",
					Text = "If the toolbar icon matches the 24x24 image instead of 40x40, the bug is present.",
					FontSize = 12,
					TextColor = Colors.Red
				}
			}
		};
	}
}
