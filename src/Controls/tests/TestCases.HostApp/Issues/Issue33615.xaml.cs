namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33615, "[Android] Title of FlyOutPage is not updating anymore after showing a NonFlyOutPage", PlatformAffected.Android)]
public partial class Issue33615 : FlyoutPage
{
	private ContentPage _detailPage2;
	private ContentPage _temporaryPage;
	private FlyoutPage _originalFlyoutPage;

	public Issue33615()
	{
		InitializeComponent();

		_detailPage2 = new ContentPage
		{
			Title = "DetailPage2",
			AutomationId = "DetailPage2",
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 15,
				Children =
				{
					new Label
					{
						Text = "DetailPage2",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold,
						AutomationId = "DetailPage2Label"
					},
					new Label
					{
						Text = "You're now on DetailPage2. The title should have updated."
					},
					new Button
					{
						Text = "Go to DetailPage1",
						AutomationId = "GoToDetailPage1Button"
					}
				}
			}
		};

		// Wire up button click for DetailPage2
		var backButton = (Button)((VerticalStackLayout)_detailPage2.Content).Children[2];
		backButton.Clicked += OnGoToDetailPage1;

		_temporaryPage = new ContentPage
		{
			Title = "TemporaryPage",
			AutomationId = "TemporaryPage",
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Children =
				{
					new Label
					{
						Text = "Temporary Non-FlyoutPage",
						FontSize = 24,
						AutomationId = "TemporaryPageLabel"
					},
					new Label
					{
						Text = "This simulates temporarily replacing Window.Page. Will restore FlyoutPage in 2 seconds..."
					}
				}
			}
		};
	}

	private void OnGoToDetailPage2(object sender, EventArgs e)
	{
		UpdateStatus("Navigating to DetailPage2...");
		Detail = new NavigationPage(_detailPage2);
	}

	private void OnGoToDetailPage1(object sender, EventArgs e)
	{
		UpdateStatus("Navigating to DetailPage1...");

		var detailPage1 = new ContentPage
		{
			Title = "DetailPage1",
			AutomationId = "DetailPage1",
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 15,
				Children =
				{
					new Label
					{
						Text = "DetailPage1",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold,
						AutomationId = "DetailPage1Label"
					},
					new Label { Text = "Back on DetailPage1. The title should have updated." },
					new Button
					{
						Text = "Go to DetailPage2",
						AutomationId = "GoToDetailPage2Button"
					},
					new Button
					{
						Text = "Trigger Bug (Swap Window.Page)",
						AutomationId = "TriggerBugButton",
						BackgroundColor = Colors.Red,
						TextColor = Colors.White
					},
					new Label
					{
						Text = "Status: Ready",
						AutomationId = "StatusLabel",
						FontSize = 14,
						TextColor = Colors.Green
					}
				}
			}
		};

		// Wire up buttons for new DetailPage1
		var navButton = (Button)((VerticalStackLayout)detailPage1.Content).Children[2];
		navButton.Clicked += OnGoToDetailPage2;

		var bugButton = (Button)((VerticalStackLayout)detailPage1.Content).Children[3];
		bugButton.Clicked += OnTriggerBug;

		Detail = new NavigationPage(detailPage1);
	}

	private async void OnTriggerBug(object sender, EventArgs e)
	{
		UpdateStatus("Triggering bug: Swapping Window.Page...");

		// Save reference to current FlyoutPage
		_originalFlyoutPage = this;

		// Get the window and temporarily replace its Page with a non-FlyoutPage
		var window = Application.Current?.Windows.FirstOrDefault();
		if (window != null)
		{
			window.Page = _temporaryPage;

			// Wait 2 seconds to simulate showing a different page
			await Task.Delay(TimeSpan.FromSeconds(2));

			window.Page = _originalFlyoutPage;

			UpdateStatus("Bug triggered! FlyoutPage restored. Now try navigating - title won't update.");
		}
	}

	private void UpdateStatus(string message)
	{
		// Try to find and update the StatusLabel on the current detail page
		if (Detail is NavigationPage navPage &&
			navPage.CurrentPage is ContentPage currentPage &&
			currentPage.Content is VerticalStackLayout stack)
		{
			var statusLabel = stack.Children.OfType<Label>()
				.FirstOrDefault(l => l.AutomationId == "StatusLabel");

			if (statusLabel != null)
			{
				statusLabel.Text = $"Status: {message}";
			}
		}
	}
}
