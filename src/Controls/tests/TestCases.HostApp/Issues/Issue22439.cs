namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 22439, "Button out of sight not clickable when translated", PlatformAffected.iOS | PlatformAffected.Android)]
	public class Issue22439 : ContentPage
	{
		public Issue22439()
		{
			// The issue: When a button starts outside the visible screen and is translated into view,
			// it's not clickable on Android and iOS because the WrapperView (ContainerView) doesn't 
			// receive the translation, so touch events outside its original bounds are ignored.

			var resultLabel = new Label
			{
				Text = "Tap the translated button",
				AutomationId = "ResultLabel",
				HorizontalOptions = LayoutOptions.Center
			};

			// Create a button that will be translated into view
			// Adding Shadow ensures a WrapperView is created, which exercises the fix path
			var translatedButton = new Button
			{
				Text = "Translated Button",
				AutomationId = "TranslatedButton",
				BackgroundColor = Colors.Red,
				TextColor = Colors.White,
				Shadow = new Shadow { Radius = 5, Opacity = 0.5f }, // Forces WrapperView creation
				// Start at the right edge, outside the visible area
				TranslationX = 400,
				Command = new Command(() => resultLabel.Text = "Button Clicked!")
			};

			// Create a button to trigger the translation
			var translateButton = new Button
			{
				Text = "Translate Button Into View",
				AutomationId = "TranslateButton",
				Command = new Command(() =>
				{
					// Translate the button into the visible area
					translatedButton.TranslationX = 0;
				})
			};

			// Create an initially offscreen button inside a wider layout
			// This simulates the flyout-like scenario described in the issue
			var offscreenLayout = new Grid
			{
				ColumnDefinitions = new ColumnDefinitionCollection
				{
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength(300) }
				},
				WidthRequest = 600,
				HeightRequest = 100,
				HorizontalOptions = LayoutOptions.Start
			};

			var mainContent = new Label
			{
				Text = "Main Content",
				BackgroundColor = Colors.LightBlue,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center
			};

			var offscreenButton = new Button
			{
				Text = "Offscreen Button",
				AutomationId = "OffscreenButton",
				BackgroundColor = Colors.Green,
				TextColor = Colors.White,
				Shadow = new Shadow { Radius = 5, Opacity = 0.5f }, // Forces WrapperView creation
				Command = new Command(() => resultLabel.Text = "Offscreen Button Clicked!")
			};

			Grid.SetColumn(mainContent, 0);
			Grid.SetColumn(offscreenButton, 1);
			offscreenLayout.Children.Add(mainContent);
			offscreenLayout.Children.Add(offscreenButton);

			// Create a container that clips the overflow and can be translated
			var clippingContainer = new Grid
			{
				IsClippedToBounds = true,
				HeightRequest = 100,
				HorizontalOptions = LayoutOptions.Fill,
				Children = { offscreenLayout }
			};

			// Button to slide the layout to reveal the offscreen button
			var slideButton = new Button
			{
				Text = "Slide Layout to Reveal Offscreen Button",
				AutomationId = "SlideButton",
				Command = new Command(() =>
				{
					// Translate the layout to the left to reveal the button in the second column
					offscreenLayout.TranslationX = -300;
				})
			};

			Content = new VerticalStackLayout
			{
				Spacing = 20,
				Padding = new Thickness(20),
				Children =
				{
					resultLabel,
					new Label { Text = "Test 1: Button with TranslationX" },
					translatedButton,
					translateButton,
					new BoxView { HeightRequest = 20 },
					new Label { Text = "Test 2: Layout with offscreen button" },
					clippingContainer,
					slideButton
				}
			};
		}
	}
}
