namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		Shell.SetSearchHandler(this, new SearchHandler
		{
			Placeholder = "Search",
			ClearPlaceholderEnabled = true,
		});

		Content = new Label
		{
			Text = "SearchHandler",
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
		};
	}
}