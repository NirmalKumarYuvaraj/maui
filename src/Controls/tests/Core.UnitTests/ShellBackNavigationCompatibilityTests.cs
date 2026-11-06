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
	}
}
