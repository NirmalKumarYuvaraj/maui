namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnOpenModalPageClicked(object sender, EventArgs e)
	{
		var modalPage = new ModalPage();
		await Navigation.PushModalAsync(modalPage);
	}
}

public class ModalPage : ContentPage
{
	public ModalPage()
	{
		// Colored background makes it immediately obvious whether the modal
		// extends behind the status bar and navigation bar (edge-to-edge).
		BackgroundColor = Colors.CornflowerBlue;

		Content = new StackLayout
		{
			Children =
			{
				new Label
				{
					Text = "Modal Page - Edge to Edge Check\n\nStatus bar and nav bar should be transparent with this background visible behind them.",
					TextColor = Colors.White,
					Margin = new Thickness(16)
				},
				new Button
				{
					Text = "Close",
					Command = new Command(async () => await Navigation.PopModalAsync())
				}
			}
		};
	}
}

public class CustomTabbedPage : TabbedPage
{
	public CustomTabbedPage()
	{
		Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);
		for (int i = 1; i < 5; i++)
		{
			var navPage = new NavigationPage { Title = $"Tab {i}", };
			var contentPage = new ContentPage
			{
				Title = $"Tab {i}",
				Content = new Label
				{
					Text = $"This is tab {i}.",
					VerticalOptions = LayoutOptions.Center,
					HorizontalOptions = LayoutOptions.Center
				},
			};
			if (i == 2 || i == 3)
			{
				NavigationPage.SetHasNavigationBar(contentPage, false);
			}
			navPage.PushAsync(contentPage);
			Children.Add(navPage);
		}
	}
}