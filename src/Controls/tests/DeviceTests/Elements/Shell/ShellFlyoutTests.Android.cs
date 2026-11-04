using System;
using System.Threading.Tasks;
using AndroidX.DrawerLayout.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shell)]
	public partial class ShellTests
	{
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task FlyoutSelectionCreatesDestinationAfterDrawerCloses(bool customFlyout)
		{
			bool drawerClosed = false;
			int creationCount = 0;
			var destination = new ShellContent
			{
				Route = "Destination",
				ContentTemplate = new DataTemplate(() =>
				{
					Assert.True(drawerClosed, "Destination construction must not interrupt the drawer close animation.");
					creationCount++;
					return new ContentPage { Title = "Destination" };
				})
			};

			await RunShellTest(shell =>
			{
				shell.Items.Add(new ContentPage());
				shell.Items.Add(destination);
				if (customFlyout)
					shell.FlyoutContent = new VerticalStackLayout { new Label { Text = "Custom flyout" } };
			},
			async shell =>
			{
				var drawer = GetDrawerLayout((IShellContext)shell.Handler);
				await AssertionExtensions.AssertEventually(() => drawer.GetChildAt(1)?.Width > 0);
				var flyout = drawer.GetChildAt(1);
				drawer.OpenDrawer(flyout, false);
				drawer.DrawerClosed += OnDrawerClosed;
				try
				{
					await ((IShellController)shell).OnFlyoutItemSelectedAsync(destination).WaitAsync(TimeSpan.FromSeconds(5));

					Assert.True(drawerClosed);
					Assert.Equal(1, creationCount);
					Assert.False(drawer.IsDrawerVisible(flyout));
					Assert.Equal("Destination", shell.CurrentPage.Title);
				}
				finally
				{
					drawer.DrawerClosed -= OnDrawerClosed;
				}

				void OnDrawerClosed(object sender, DrawerLayout.DrawerClosedEventArgs args) => drawerClosed = true;
			});
		}

		[Theory]
		[InlineData(FlyoutBehavior.Flyout)]
		[InlineData(FlyoutBehavior.Locked)]
		[InlineData(FlyoutBehavior.Disabled)]
		public async Task FlyoutSelectionDoesNotWaitWhenDrawerCannotClose(FlyoutBehavior behavior)
		{
			var destination = new ShellContent
			{
				ContentTemplate = new DataTemplate(() => new ContentPage { Title = "Destination" })
			};
			await RunShellTest(shell =>
			{
				shell.FlyoutBehavior = behavior;
				shell.Items.Add(new ContentPage());
				shell.Items.Add(destination);
			},
			async shell =>
			{
				await ((IShellController)shell).OnFlyoutItemSelectedAsync(destination).WaitAsync(TimeSpan.FromSeconds(5));
				Assert.Equal("Destination", shell.CurrentPage.Title);
				Assert.Equal(behavior == FlyoutBehavior.Locked, shell.FlyoutIsPresented);
			});
		}

		[Fact]
		public async Task FlyoutSelectionDuringClosingUsesLatestDestination()
		{
			int supersededCreations = 0;
			var superseded = new ShellContent
			{
				ContentTemplate = new DataTemplate(() =>
				{
					supersededCreations++;
					return new ContentPage { Title = "Superseded" };
				})
			};
			var destination = new ShellContent
			{
				ContentTemplate = new DataTemplate(() => new ContentPage { Title = "Destination" })
			};

			await RunShellTest(shell =>
			{
				shell.Items.Add(new ContentPage());
				shell.Items.Add(superseded);
				shell.Items.Add(destination);
			},
			async shell =>
			{
				var drawer = GetDrawerLayout((IShellContext)shell.Handler);
				await AssertionExtensions.AssertEventually(() => drawer.GetChildAt(1)?.Width > 0);
				drawer.OpenDrawer(drawer.GetChildAt(1), false);

				var controller = (IShellController)shell;
				var firstSelection = controller.OnFlyoutItemSelectedAsync(superseded);
				Assert.False(firstSelection.IsCompleted);
				var latestSelection = controller.OnFlyoutItemSelectedAsync(destination);
				await Task.WhenAll(firstSelection, latestSelection).WaitAsync(TimeSpan.FromSeconds(5));

				Assert.Equal(0, supersededCreations);
				Assert.Equal("Destination", shell.CurrentPage.Title);
			});
		}

		[Fact]
		public async Task FlyoutSelectionIsAbandonedWhenDrawerReopens()
		{
			int destinationCreations = 0;
			var destination = new ShellContent
			{
				ContentTemplate = new DataTemplate(() =>
				{
					destinationCreations++;
					return new ContentPage();
				})
			};
			await RunShellTest(shell =>
			{
				shell.Items.Add(new ContentPage { Title = "Original" });
				shell.Items.Add(destination);
			},
			async shell =>
			{
				var drawer = GetDrawerLayout((IShellContext)shell.Handler);
				await AssertionExtensions.AssertEventually(() => drawer.GetChildAt(1)?.Width > 0);
				var flyout = drawer.GetChildAt(1);
				drawer.OpenDrawer(flyout, false);

				var selection = ((IShellController)shell).OnFlyoutItemSelectedAsync(destination);
				Assert.False(selection.IsCompleted);
				shell.FlyoutIsPresented = true;
				await selection.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.Equal(0, destinationCreations);
				Assert.Equal("Original", shell.CurrentPage.Title);
				await AssertionExtensions.AssertEventually(() => drawer.IsDrawerOpen(flyout));
			});
		}

		[Fact]
		public async Task FlyoutSelectionCanStillBeCancelledAfterDrawerCloses()
		{
			int destinationCreations = 0;
			var destination = new ShellContent
			{
				ContentTemplate = new DataTemplate(() =>
				{
					destinationCreations++;
					return new ContentPage();
				})
			};
			await RunShellTest(shell =>
			{
				shell.Items.Add(new ContentPage { Title = "Original" });
				shell.Items.Add(destination);
			},
			async shell =>
			{
				var drawer = GetDrawerLayout((IShellContext)shell.Handler);
				await AssertionExtensions.AssertEventually(() => drawer.GetChildAt(1)?.Width > 0);
				var flyout = drawer.GetChildAt(1);
				drawer.OpenDrawer(flyout, false);
				shell.Navigating += CancelNavigation;
				try
				{
					await ((IShellController)shell).OnFlyoutItemSelectedAsync(destination).WaitAsync(TimeSpan.FromSeconds(5));
					Assert.False(drawer.IsDrawerVisible(flyout));
					Assert.Equal(0, destinationCreations);
					Assert.Equal("Original", shell.CurrentPage.Title);
				}
				finally
				{
					shell.Navigating -= CancelNavigation;
				}

				void CancelNavigation(object sender, ShellNavigatingEventArgs args) => args.Cancel();
			});
		}

		[Fact(DisplayName = "FlyoutIsPresented=true sets the visible status of the Shell Flyout.")]
		public async Task FlyoutIsPresentedOpenDrawer()
		{
			await RunShellTest(shell =>
			{
				shell.FlyoutContent = new VerticalStackLayout() { new Label() { Text = "Flyout Content" } };
			},
			async shell =>
			{
#if ANDROID || IOS || MACCATALYST
				var shellContext = (IShellContext)shell.Handler;
#endif
				// 1. Set FlyoutIsPresented=true to make the Shell Flyout visible.
				shell.FlyoutIsPresented = true;

				var dl = GetDrawerLayout(shellContext) as DrawerLayout;
				Assert.NotNull(dl);

				await AssertionExtensions.AssertEventually(() =>
				{
					// 2. Check that the Flyout has size.
					var flyoutFrame = GetFlyoutFrame(shellContext);
					return flyoutFrame.Width > 0 && flyoutFrame.Height > 0 && dl.IsOpen;
				});
			});
		}
	}
}
