namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	// Navigation Page Scenarios
	private async void OnDefaultNavigationPageClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Default Navigation Page");
		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPageBarBackgroundColorClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + BarBackgroundColor");
		var navPage = new NavigationPage(testPage)
		{
			BarBackgroundColor = Colors.Blue,
		};
		await Navigation.PushAsync(navPage);
	}

	private async void OnNavigationPageBarBackgroundClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + BarBackground");
		var navPage = new NavigationPage(testPage)
		{
			BarBackground = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 0),
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Colors.Red, Offset = 0.0f },
					new GradientStop { Color = Colors.Orange, Offset = 1.0f }
				}
			}
		};
		await Navigation.PushAsync(navPage);
	}

	private async void OnNavigationPageToolBarItemsClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + ToolBar Items");
		testPage.ToolbarItems.Add(new ToolbarItem("Save", "save.png", () => { }));
		testPage.ToolbarItems.Add(new ToolbarItem("Edit", "edit.png", () => { }));
		testPage.ToolbarItems.Add(new ToolbarItem("Delete", "delete.png", () => { }));
		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPageSecondaryToolBarItemsClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + Secondary ToolBar Items");
		testPage.ToolbarItems.Add(new ToolbarItem
		{
			Text = "Settings",
			IconImageSource = "settings.png",
			Order = ToolbarItemOrder.Secondary,
			Command = new Command(async () =>
			{
				await DisplayAlertAsync("Toolbar Action", "Settings clicked", "OK");
			})
		});
		testPage.ToolbarItems.Add(new ToolbarItem()
		{
			Text = "Share",
			IconImageSource = "share.png",
			Order = ToolbarItemOrder.Secondary,
			Command = new Command(async () =>
			{
				await DisplayAlertAsync("Toolbar Action", "Share clicked", "OK");
			})
		});

		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPagePrimaryAndSecondaryToolBarItemsClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + Secondary ToolBar Items");
		testPage.ToolbarItems.Add(new ToolbarItem("Save", "save.png", () => { }));
		testPage.ToolbarItems.Add(new ToolbarItem("Edit", "edit.png", () => { }));
		testPage.ToolbarItems.Add(new ToolbarItem("Delete", "delete.png", () => { }));
		testPage.ToolbarItems.Add(new ToolbarItem
		{
			Text = "Settings",
			IconImageSource = "settings.png",
			Order = ToolbarItemOrder.Secondary,
			Command = new Command(async () =>
			{
				await DisplayAlertAsync("Toolbar Action", "Settings clicked", "OK");
			})
		});
		testPage.ToolbarItems.Add(new ToolbarItem()
		{
			Text = "Share",
			IconImageSource = "share.png",
			Order = ToolbarItemOrder.Secondary,
			Command = new Command(async () =>
			{
				await DisplayAlertAsync("Toolbar Action", "Share clicked", "OK");
			})
		});

		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPageNoBarClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + HasNavigationBar=False");
		NavigationPage.SetHasNavigationBar(testPage, false);
		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPageNoBackButtonClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + HasBackButton=False");
		NavigationPage.SetHasBackButton(testPage, false);
		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPageTitleViewClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + TitleView");
		NavigationPage.SetTitleView(testPage, new Label { Text = "Custom Title", VerticalOptions = LayoutOptions.Center });
		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPageIconImageSourceClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + IconImageSource");
		NavigationPage.SetTitleIconImageSource(testPage, new FileImageSource { File = "dotnet_bot.png" });
		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPageIconImageSourceAndTitleViewClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Navigation Page + IconImageSource + TitleView");
		NavigationPage.SetTitleView(testPage, new Label { Text = "Custom Title", VerticalOptions = LayoutOptions.Center });
		NavigationPage.SetTitleIconImageSource(testPage, new FileImageSource { File = "dotnet_bot.png" });

		await Navigation.PushAsync(testPage);
	}

	private async void OnNavigationPageBackButtonTitleClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPageDynamicChanges("Navigation Page + Dynamic Changes");
		await Navigation.PushAsync(testPage);

		var firstPage = new EdgeToEdgeTestPage("Navigation Page + Back Button Title");
		var navPage = new NavigationPage(firstPage);
		await Navigation.PushModalAsync(navPage);
		var secondPage = new EdgeToEdgeTestPage("Page with Custom Back Button Title");

		NavigationPage.SetBackButtonTitle(secondPage, "Go Back");
		await navPage.PushAsync(secondPage);
	}

	private async void OnNavigationPageDynamicChangesClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPageDynamicChanges("Navigation Page + Dynamic Changes");
		await Navigation.PushAsync(testPage);
	}


	// Modal Navigation Page Scenarios
	private async void OnDefaultModalNavigationPageClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Default Modal Navigation Page");
		var navPage = new NavigationPage(testPage);
		await Navigation.PushModalAsync(navPage);
	}

	private async void OnModalNavigationPageBarBackgroundColorClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Modal Navigation Page + BarBackgroundColor");
		var navPage = new NavigationPage(testPage)
		{
			BarBackgroundColor = Colors.Green
		};
		await Navigation.PushModalAsync(navPage);
	}

	private async void OnModalNavigationPageBarBackgroundClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Modal Navigation Page + BarBackground");
		var navPage = new NavigationPage(testPage)
		{
			BarBackground = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(0, 1),
				GradientStops = new GradientStopCollection
				{
					new GradientStop { Color = Colors.Purple, Offset = 0.0f },
					new GradientStop { Color = Colors.Pink, Offset = 1.0f }
				}
			}
		};
		await Navigation.PushModalAsync(navPage);
	}

	private async void OnModalNavigationPageToolBarItemsClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Modal Navigation Page + ToolBar Items");
		testPage.ToolbarItems.Add(new ToolbarItem("Share", "share.png", () => { }));
		testPage.ToolbarItems.Add(new ToolbarItem("Print", "print.png", () => { }));
		var navPage = new NavigationPage(testPage);
		await Navigation.PushModalAsync(navPage);
	}

	private async void OnModalNavigationPageNoBarClicked(object sender, EventArgs e)
	{
		var testPage = new EdgeToEdgeTestPage("Modal Navigation Page + HasNavigationBar=False");
		NavigationPage.SetHasNavigationBar(testPage, false);
		var navPage = new NavigationPage(testPage);
		await Navigation.PushModalAsync(navPage);
	}

	// Page Type Scenarios
	private async void OnDefaultTabbedPageClicked(object sender, EventArgs e)
	{
		var tabbedPage = new EdgeToEdgeTabbedPage();
		await Navigation.PushModalAsync(tabbedPage);
	}

	private async void OnDefaultFlyoutPageClicked(object sender, EventArgs e)
	{
		var flyoutPage = new EdgeToEdgeFlyoutPage();
		await Navigation.PushModalAsync(flyoutPage);
	}

	// Shell Scenarios
	private void OnShellFlyoutClicked(object sender, EventArgs e)
	{
		var window = Application.Current?.Windows?.FirstOrDefault();
		if (window != null)
		{
			window.Page = new EdgeToEdgeShellFlyout();
		}
	}

	private void OnShellTabBarClicked(object sender, EventArgs e)
	{
		var window = Application.Current?.Windows?.FirstOrDefault();
		if (window != null)
		{
			window.Page = new EdgeToEdgeShellTabBar();
		}
	}

	private void OnShellBottomTabsClicked(object sender, EventArgs e)
	{
		var window = Application.Current?.Windows?.FirstOrDefault();
		if (window != null)
		{
			window.Page = new EdgeToEdgeShellBottomTabs();
		}
	}

	private void OnShellTopTabsClicked(object sender, EventArgs e)
	{
		var window = Application.Current?.Windows?.FirstOrDefault();
		if (window != null)
		{
			window.Page = new EdgeToEdgeShellTopTabs();
		}
	}

	private void OnShellNoNavBarClicked(object sender, EventArgs e)
	{
		var window = Application.Current?.Windows?.FirstOrDefault();
		if (window != null)
		{
			window.Page = new EdgeToEdgeShellNoNavBar();
		}
	}

	private void OnShellNavBarShadowClicked(object sender, EventArgs e)
	{
		var window = Application.Current?.Windows?.FirstOrDefault();
		if (window != null)
		{
			window.Page = new EdgeToEdgeShellNavBarShadow();
		}
	}

	private void OnShellBackgroundColorClicked(object sender, EventArgs e)
	{
		var window = Application.Current?.Windows?.FirstOrDefault();
		if (window != null)
		{
			window.Page = new EdgeToEdgeShellBackgroundColor();
		}
	}

	// Test Controls
	private void OnResetClicked(object sender, EventArgs e)
	{
		var window = Application.Current?.Windows?.FirstOrDefault();
		if (window != null)
		{
			window.Page = new NavigationPage(new MainPage());
		}
	}
}