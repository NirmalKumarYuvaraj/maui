using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class ShellBackNavigationCompatibilityTests : BaseTestFixture
	{
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void RootCommandRegistersOnlyWhileEnabled(bool onShell)
		{
			var page = new ContentPage();
			var shell = new Shell { Items = { page } };
			var behavior = new BackButtonBehavior { Command = new Command(() => { }) };
			Shell.SetBackButtonBehavior(onShell ? shell : page, behavior);

			Assert.True(Window.CanConsumeBackNavigation(shell));
			behavior.IsEnabled = false;
			Assert.False(Window.CanConsumeBackNavigation(shell));
			behavior.IsEnabled = true;
			Assert.True(Window.CanConsumeBackNavigation(shell));
			behavior.Command = null;
			Assert.False(Window.CanConsumeBackNavigation(shell));
		}

		[Fact]
		public void RootCancellationRegistersUntilUnsubscribed()
		{
			var shell = new Shell { Items = { new ContentPage() } };
			EventHandler<ShellNavigatingEventArgs> cancel = (_, args) => args.Cancel();
			shell.Navigating += cancel;

			Assert.True(Window.CanConsumeBackNavigation(shell));
			Assert.True(shell.SendBackButtonPressed());

			shell.Navigating -= cancel;
			Assert.False(Window.CanConsumeBackNavigation(shell));
		}

		[Fact]
		public async Task CancelledBackPreservesSectionStack()
		{
			var shell = new Shell { Items = { new ContentPage() } };
			_ = new TestWindow(shell);
			var detail = new ContentPage();
			await shell.Navigation.PushAsync(detail);
			var cancelled = false;
			shell.Navigating += (_, args) =>
			{
				if (args.Source is ShellNavigationSource.Pop or ShellNavigationSource.PopToRoot)
				{
					args.Cancel();
					cancelled = true;
				}
			};

			Assert.True(Window.CanConsumeBackNavigation(shell));
			Assert.True(shell.SendBackButtonPressed());
			Assert.True(cancelled);
			Assert.Equal(2, shell.CurrentItem.CurrentItem.Stack.Count);
			Assert.Same(detail, shell.CurrentPage);
		}

		[Fact]
		public async Task BackClosesPresentedFlyoutBeforePopping()
		{
			var shell = new Shell { FlyoutBehavior = FlyoutBehavior.Flyout, Items = { new ContentPage() } };
			_ = new TestWindow(shell);
			var detail = new ContentPage();
			await shell.Navigation.PushAsync(detail);
			shell.FlyoutIsPresented = true;

			Assert.True(shell.SendBackButtonPressed());

			Assert.False(shell.FlyoutIsPresented);
			Assert.Equal(2, shell.CurrentItem.CurrentItem.Stack.Count);
			Assert.Same(detail, shell.CurrentPage);
		}

		[Fact]
		public async Task DisabledBackCommandIsNotExecuted()
		{
			var shell = new Shell { Items = { new ContentPage() } };
			_ = new TestWindow(shell);
			var detail = new ContentPage();
			var executed = false;
			Shell.SetBackButtonBehavior(detail, new BackButtonBehavior
			{
				Command = new Command(() => executed = true),
				IsEnabled = false
			});
			await shell.Navigation.PushAsync(detail);

			shell.SendBackButtonPressed();

			Assert.False(executed);
		}

		[Fact]
		public async Task ConcurrentPopsReturnEachPoppedPage()
		{
			var shell = new Shell { Items = { new ContentPage() } };
			_ = new TestWindow(shell);
			var first = new ContentPage();
			var second = new ContentPage();
			await shell.Navigation.PushAsync(first, false);
			await shell.Navigation.PushAsync(second, false);

			var firstPopStarted = new TaskCompletionSource<Action>(TaskCreationOptions.RunContinuationsAsynchronously);
			shell.Navigating += (_, args) =>
			{
				if (!firstPopStarted.Task.IsCompleted)
					firstPopStarted.TrySetResult(args.GetDeferral().Complete);
			};

			var firstPop = shell.Navigation.PopAsync(false);
			var completeFirstPop = await firstPopStarted.Task;
			var secondPop = shell.Navigation.PopAsync(false);
			completeFirstPop();

			Assert.Same(second, await firstPop);
			Assert.Same(first, await secondPop);
			Assert.Single(shell.CurrentItem.CurrentItem.Stack);
		}
	}
}
