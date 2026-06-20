namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		// Title = "Sandbox";
		// CollectionView collectionView = new CollectionView
		// {
		// 	ItemsSource = Enumerable.Range(1, 100).Select(i => $"Item {i}").ToList(),
		// 	ItemTemplate = new DataTemplate(() =>
		// 	{
		// 		Label label = new Label();
		// 		label.SetBinding(Label.TextProperty, ".");
		// 		return new StackLayout
		// 		{
		// 			Padding = new Thickness(10),
		// 			Children = { label }
		// 		};
		// 	})
		// };

		// Grid grid = new Grid();
		// grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
		// grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(75) });
		// grid.Add(collectionView, 0, 0);
		// Entry entry = new Entry { Placeholder = "Type something..." };
		// grid.Add(entry, 0, 1);
		// Content = grid;
	}

	void OnButtonClicked(object sender, EventArgs e)
	{
		this.SafeAreaEdges = SafeAreaEdges.None;
	}
}

public class CustomTabbedPage : TabbedPage
{

	public CustomTabbedPage()
	{
		Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);
		for (int i = 1; i < 5; i++)
		{
			var navPage = new NavigationPage
			{
				Title = $"Tab {i}",
			};

			var button1 = new Button() { Text = $"Click me to Disable Navigation Bar on Tab {i}" };
			button1.Clicked += (sender, args) =>
			{
				NavigationPage.SetHasNavigationBar(navPage.CurrentPage, false);
			};

			var button2 = new Button() { Text = $"Click me to Enable Navigation Bar on Tab {i}" };
			button2.Clicked += (sender, args) =>
			{
				NavigationPage.SetHasNavigationBar(navPage.CurrentPage, true);
			};

			var stackLayout = new StackLayout
			{
				Children = { button1, button2, new Label
				{
					Text = $"Content for Tab {i}",
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				} }
			};

			var page = new ContentPage
			{
				Title = $"Tab {i}",
				Content = stackLayout
			};

			if (i == 2 || i == 3)
			{
				NavigationPage.SetHasNavigationBar(page, false);
			}
			navPage.PushAsync(page);
			Children.Add(navPage);
		}
	}
}