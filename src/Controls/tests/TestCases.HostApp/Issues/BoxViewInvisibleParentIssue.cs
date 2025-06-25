using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls.TestCases.HostApp.Issues
{
	[Issue(IssueTracker.Github, 3, "BoxView will not render on iOS if it's parent is not visible when the page is built, but is made visible afterwards", PlatformAffected.iOS)]
	public class BoxViewInvisibleParentIssue : ContentPage
	{
		private readonly Grid _visibleGrid;
		private readonly Grid _hiddenGrid;
		private readonly Button _toggleButton;
		private int _clickCount = 0;

		public BoxViewInvisibleParentIssue()
		{
			Title = "BoxView Invisible Parent Issue";

			_toggleButton = new Button
			{
				Text = "Toggle Visibility",
				AutomationId = "ToggleButton"
			};
			_toggleButton.Clicked += OnToggleClicked;

			// Grid that is initially visible
			_visibleGrid = new Grid
			{
				HeightRequest = 100,
				BackgroundColor = Colors.LightBlue,
				AutomationId = "VisibleGrid"
			};

			var visibleBoxView = new BoxView
			{
				Color = Colors.Orange,
				HeightRequest = 100,
				AutomationId = "VisibleBoxView"
			};

			var visibleLabel = new Label
			{
				Text = "Initially Visible BoxView",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				AutomationId = "VisibleLabel"
			};

			_visibleGrid.Children.Add(visibleBoxView);
			_visibleGrid.Children.Add(visibleLabel);

			// Grid that is initially hidden
			_hiddenGrid = new Grid
			{
				HeightRequest = 100,
				BackgroundColor = Colors.LightGreen,
				IsVisible = false,
				AutomationId = "HiddenGrid"
			};

			var hiddenBoxView = new BoxView
			{
				Color = Colors.Red,
				HeightRequest = 100,
				AutomationId = "HiddenBoxView"
			};

			var hiddenLabel = new Label
			{
				Text = "Initially Hidden BoxView",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				AutomationId = "HiddenLabel"
			};

			_hiddenGrid.Children.Add(hiddenBoxView);
			_hiddenGrid.Children.Add(hiddenLabel);

			var instructionLabel = new Label
			{
				Text = "Tap the button to toggle visibility. The initially hidden BoxView should render properly when made visible.",
				HorizontalOptions = LayoutOptions.Center,
				Margin = new Thickness(20)
			};

			Content = new StackLayout
			{
				Padding = new Thickness(20),
				Children =
				{
					instructionLabel,
					_toggleButton,
					_visibleGrid,
					_hiddenGrid
				}
			};
		}

		private void OnToggleClicked(object sender, EventArgs e)
		{
			_clickCount++;
			_toggleButton.Text = $"Clicked {_clickCount} time{(_clickCount == 1 ? "" : "s")}";

			_visibleGrid.IsVisible = !_visibleGrid.IsVisible;
			_hiddenGrid.IsVisible = !_hiddenGrid.IsVisible;
		}
	}
}