namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34256, "Grid with SafeAreaEdges=Container has incorrect size when tab bar appears", PlatformAffected.Android)]
public class Issue34256 : TestShell
{
	protected override void Init()
	{
		Routing.RegisterRoute(nameof(Issue34256HiddenTabBarPage), typeof(Issue34256HiddenTabBarPage));

		var tabBar = new TabBar();

		var tab1 = new Tab { Title = "Tab 1" };
		tab1.Items.Add(new ShellContent
		{
			Title = "Tab 1",
			ContentTemplate = new DataTemplate(() => new Issue34256TabPage()),
			Route = "tab1"
		});

		var tab2 = new Tab { Title = "Tab 2" };
		tab2.Items.Add(new ShellContent
		{
			Title = "Tab 2",
			ContentTemplate = new DataTemplate(() => new ContentPage
			{
				Content = new Label { Text = "Tab 2 Content" }
			}),
			Route = "tab2"
		});

		tabBar.Items.Add(tab1);
		tabBar.Items.Add(tab2);
		Items.Add(tabBar);
	}
}

public class Issue34256TabPage : ContentPage
{
	public Issue34256TabPage()
	{
		// BoxView anchored to the bottom: if InnerGrid gets extra bottom padding
		// after navigation back, this view shifts up — detectable via GetRect().
		var bottomMarker = new BoxView
		{
			AutomationId = "BottomMarker",
			BackgroundColor = Colors.Red,
			HeightRequest = 20,
			VerticalOptions = LayoutOptions.End,
			HorizontalOptions = LayoutOptions.Fill
		};

		var navigateButton = new Button
		{
			Text = "Navigate to Hidden TabBar Page",
			AutomationId = "NavigateButton",
			VerticalOptions = LayoutOptions.Start
		};
		navigateButton.Clicked += async (s, e) =>
			await Shell.Current.GoToAsync(nameof(Issue34256HiddenTabBarPage));

		// Inner Grid with default SafeAreaEdges (Container) — subject of the bug
		var innerGrid = new Grid
		{
			AutomationId = "InnerGrid",
			Children = { navigateButton, bottomMarker }
		};

		// Root Grid with SafeAreaEdges=None, as described in the issue
		Content = new Grid
		{
			SafeAreaEdges = SafeAreaEdges.None,
			Children = { innerGrid }
		};
	}
}

public class Issue34256HiddenTabBarPage : ContentPage
{
	public Issue34256HiddenTabBarPage()
	{
		Shell.SetTabBarIsVisible(this, false);

		var backButton = new Button
		{
			Text = "Go Back",
			AutomationId = "GoBackButton",
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center
		};
		backButton.Clicked += async (s, e) => await Shell.Current.GoToAsync("..");

		Content = new VerticalStackLayout
		{
			VerticalOptions = LayoutOptions.Center,
			Children = { backButton }
		};
	}
}
