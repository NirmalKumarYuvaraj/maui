using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Windows.Graphics;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace Microsoft.Maui.DeviceTests
{
	public partial class WindowHandlerTests : CoreHandlerTestBase
	{
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task NativeTitleBarDoesNotReserveCustomTitleBarSpace(bool setInCreated)
		{
			var window = new Window(new ContentPage());
			if (setInCreated)
			{
				window.Created += (_, _) =>
				{
					var platformWindow = (Microsoft.UI.Xaml.Window)window.Handler.PlatformView;
					platformWindow.GetAppWindow().TitleBar.ExtendsContentIntoTitleBar = false;
				};
			}

			await RunWindowTest(window, async handler =>
			{
				var titleBar = handler.PlatformView.GetAppWindow().TitleBar;
				var root = Assert.IsType<WindowRootView>(handler.MauiContext.GetNavigationRootManager().RootView);

				if (!setInCreated)
					titleBar.ExtendsContentIntoTitleBar = false;

				await AssertEventually(() => root.WindowTitleBarContentControlVisibility == WVisibility.Collapsed);
				Assert.Equal(0d, root.WindowTitleBarContentControlMinHeight);
				Assert.Null(titleBar.ButtonBackgroundColor);
				Assert.Null(titleBar.ButtonInactiveBackgroundColor);

				titleBar.ExtendsContentIntoTitleBar = true;

				await AssertEventually(() => root.WindowTitleBarContentControlVisibility == WVisibility.Visible);
				Assert.True(root.WindowTitleBarContentControlMinHeight > 0);
				Assert.Equal(Microsoft.UI.Colors.Transparent, titleBar.ButtonBackgroundColor);
				Assert.Equal(Microsoft.UI.Colors.Transparent, titleBar.ButtonInactiveBackgroundColor);
			});
		}

		[Fact]
		public async Task NativeTitleBarPreservesExplicitCaptionButtonColors()
		{
			var window = new Window(new ContentPage());
			window.Created += (_, _) =>
			{
				var platformWindow = (Microsoft.UI.Xaml.Window)window.Handler.PlatformView;
				var titleBar = platformWindow.GetAppWindow().TitleBar;
				titleBar.ExtendsContentIntoTitleBar = false;
				titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Red;
				titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Blue;
			};

			await RunWindowTest(window, async handler =>
			{
				var titleBar = handler.PlatformView.GetAppWindow().TitleBar;
				var root = Assert.IsType<WindowRootView>(handler.MauiContext.GetNavigationRootManager().RootView);

				await AssertEventually(() => root.WindowTitleBarContentControlVisibility == WVisibility.Collapsed);
				Assert.Equal(Microsoft.UI.Colors.Red, titleBar.ButtonBackgroundColor);
				Assert.Equal(Microsoft.UI.Colors.Blue, titleBar.ButtonInactiveBackgroundColor);
			});
		}

		[Fact(DisplayName = "Back Button Not Visible With No Navigation Page")]
		public async Task BackButtonNotVisibleWithBasicView()
		{
			var window = new WindowStub()
			{
				Content = new ButtonStub()
			};

			await RunWindowStubTest(window, handler =>
			{
				var navView = GetRootNavigationView(handler);
				Assert.Equal(UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed, navView.IsBackButtonVisible);
			});
		}

		[Fact(DisplayName = "MauiToolbar titleIcon Visibility Toggle")]
		public async Task MauiToolbarTitleIconVisibilityToggle()
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				MauiToolbar mauiToolbar = new MauiToolbar();
				var toolbarContent = (UI.Xaml.DependencyObject)mauiToolbar.Content;
				var control = toolbarContent.GetDescendantByName<UI.Xaml.UIElement>("titleIcon");

				Assert.Equal(WVisibility.Collapsed, control.Visibility);

				var tcs = new TaskCompletionSource<bool>();
				var fileImageSource = new FileImageSource() { File = "black.png" };
				fileImageSource.LoadImage(MauiContext, (result) =>
				{
					mauiToolbar.TitleIconImageSource = result.Value;
					tcs.SetResult(true);
				});
				await tcs.Task;

				Assert.Equal(WVisibility.Visible, control.Visibility);

				mauiToolbar.TitleIconImageSource = null;

				Assert.Equal(WVisibility.Collapsed, control.Visibility);
			});
		}

		[Fact(DisplayName = "MauiToolbar textBlockBorder Visibility Toggle")]
		public async Task MauiToolbarTextBlockBorderVisibilityToggle()
		{
			await InvokeOnMainThreadAsync(() =>
			{
				MauiToolbar mauiToolbar = new MauiToolbar();
				var toolbarContent = (UI.Xaml.DependencyObject)mauiToolbar.Content;
				var control = toolbarContent.GetDescendantByName<UI.Xaml.UIElement>("textBlockBorder");

				Assert.Equal(WVisibility.Collapsed, control.Visibility);

				mauiToolbar.Title = "text";

				Assert.Equal(WVisibility.Visible, control.Visibility);

				mauiToolbar.Title = "";

				Assert.Equal(WVisibility.Collapsed, control.Visibility);
			});
		}

		[Fact(DisplayName = "MauiToolbar menuContent Visibility Toggle")]
		public async Task MauiToolbarMenuContentVisibilityToggle()
		{
			await InvokeOnMainThreadAsync(() =>
			{
				MauiToolbar mauiToolbar = new MauiToolbar();
				var toolbarContent = (UI.Xaml.DependencyObject)mauiToolbar.Content;
				var control = toolbarContent.GetDescendantByName<UI.Xaml.UIElement>("menuContent");

				Assert.Equal(WVisibility.Collapsed, control.Visibility);

				mauiToolbar.SetMenuBar(new UI.Xaml.Controls.MenuBar() { Items = { new UI.Xaml.Controls.MenuBarItem() } });

				Assert.Equal(WVisibility.Visible, control.Visibility);

				mauiToolbar.SetMenuBar(new UI.Xaml.Controls.MenuBar());

				Assert.Equal(WVisibility.Collapsed, control.Visibility);

				mauiToolbar.SetMenuBar(new UI.Xaml.Controls.MenuBar() { Items = { new UI.Xaml.Controls.MenuBarItem() } });

				Assert.Equal(WVisibility.Visible, control.Visibility);

				mauiToolbar.SetMenuBar(null);

				Assert.Equal(WVisibility.Collapsed, control.Visibility);
			});
		}

		[Fact(DisplayName = "MauiToolbar titleView Visibility Toggle")]
		public async Task MauiToolbarTitleViewVisibilityToggle()
		{
			await InvokeOnMainThreadAsync(() =>
			{
				MauiToolbar mauiToolbar = new MauiToolbar();
				var toolbarContent = (UI.Xaml.DependencyObject)mauiToolbar.Content;
				var control = toolbarContent.GetDescendantByName<UI.Xaml.UIElement>("titleView");

				Assert.Equal(WVisibility.Collapsed, control.Visibility);

				mauiToolbar.TitleView = "text";

				Assert.Equal(WVisibility.Visible, control.Visibility);

				mauiToolbar.TitleView = null;

				Assert.Equal(WVisibility.Collapsed, control.Visibility);
			});
		}

		[Fact]
		public async Task ContentIsSetInitially()
		{
			var window = new Window
			{
				Page = new ContentPage
				{
					Content = new Label { Text = "Yay!" }
				}
			};

			await RunWindowTest(window, handler =>
			{
				var navigation = GetRootNavigationView(handler);
				var page = Assert.IsType<ContentPanel>(navigation.Content);

				var btn = Assert.IsAssignableFrom<UI.Xaml.Controls.TextBlock>(page.Children[0]);

				Assert.Equal("Yay!", btn.Text);
			});
		}

		[Fact]
		public async Task WindowSupportsEmptyPage_Platform()
		{
			var window = new Window(new ContentPage());

			await RunWindowTest(window, handler =>
			{
				var navigation = GetRootNavigationView(handler);
				var page = Assert.IsType<ContentPanel>(navigation.Content);

				Assert.Null(page.Content);
				Assert.Empty(page.Children);
			});
		}

		void MovePlatformWindow(UI.Xaml.Window window, Rect rect)
		{
			var density = window.GetDisplayDensity();
			window.GetAppWindow().MoveAndResize(new RectInt32(
				(int)(rect.X * density),
				(int)(rect.Y * density),
				(int)(rect.Width * density),
				(int)(rect.Height * density)));
		}

		RootNavigationView GetRootNavigationView(IWindowHandler handler)
		{
			var rootContainer = handler.PlatformView.Content;

			Assert.NotNull(rootContainer);

			var container = Assert.IsType<WindowRootViewContainer>(rootContainer);

			Assert.NotEmpty(container.Children);

			var root = Assert.IsType<WindowRootView>(container.Children[0]);
			var navigation = Assert.IsType<RootNavigationView>(root.Content);

			return navigation;
		}

		RootNavigationView GetRootNavigationView(NavigationRootManager navigationRootManager)
		{
			return (navigationRootManager.RootView as WindowRootView).NavigationViewControl;
		}

		Task RunWindowStubTest(IWindow window, Func<NavigationRootManager, Task> action)
		{
			return InvokeOnMainThreadAsync(async () =>
			{
				var scopedContext = new MauiContext(MauiContext.Services);
				scopedContext.AddWeakSpecific(window);

				var mauiContext = scopedContext.MakeScoped(true);
				var windowManager = mauiContext.GetNavigationRootManager();

				windowManager.Connect(window.Content.ToPlatform(mauiContext));

				var frameworkElement = windowManager.RootView;

				await AssertionExtensions.AttachAndRun(frameworkElement, async () =>
				{
					frameworkElement.Unloaded += (_, _) =>
					{
						windowManager?.Disconnect();
						windowManager = null;
					};

					await action.Invoke(windowManager);
				}, MauiContext);

				return;
			});
		}

		Task RunWindowStubTest(IWindow window, Action<NavigationRootManager> action) =>
			RunWindowStubTest(window, handler =>
			{
				action(handler);
				return Task.CompletedTask;
			});
	}
}