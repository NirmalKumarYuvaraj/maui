using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
    public class ShellContentViewNavigationTests : ShellTestBase
    {
        [Fact]
        public async Task NavigatingToContentViewThrowsArgumentException()
        {
            // Register a ContentView route
            Routing.RegisterRoute("contentview", typeof(ContentView));

            var shell = new TestShell(
                CreateShellItem(shellItemRoute: "item")
            );

            // Attempting to navigate to a ContentView should throw an ArgumentException
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => shell.GoToAsync("//item/contentview")
            );

            Assert.Contains("contentview", exception.Message, StringComparison.InvariantCulture);
            Assert.Contains("ContentView", exception.Message, StringComparison.InvariantCulture);
            Assert.Contains("ContentPage", exception.Message, StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task NavigatingToContentPageSucceeds()
        {
            // Register a ContentPage route
            Routing.RegisterRoute("contentpage", typeof(ContentPage));

            var shell = new TestShell(
                CreateShellItem(shellItemRoute: "item")
            );

            // Navigating to a ContentPage should succeed
            await shell.GoToAsync("//item/contentpage");

            Assert.Equal("//item/contentpage", shell.CurrentState.Location.ToString());
        }

        [Fact]
        public void ShellContentWithContentViewTemplateThrowsException()
        {
            var shellContent = new ShellContent
            {
                Title = "Test",
                Route = "test",
                ContentTemplate = new DataTemplate(() => new ContentView())
            };

            // Attempting to get content from a template that creates ContentView should throw InvalidOperationException
            var exception = Assert.Throws<InvalidOperationException>(
                () => ((IShellContentController)shellContent).GetOrCreateContent()
            );

            Assert.Contains("ContentView", exception.Message, StringComparison.InvariantCulture);
            Assert.Contains("ContentPage", exception.Message, StringComparison.InvariantCulture);
            Assert.Contains("ShellContent", exception.Message, StringComparison.InvariantCulture);
        }

        [Fact]
        public void ApplyQueryAttributesToContentViewThrowsException()
        {
            var contentView = new ContentView();
            var queryParams = new ShellRouteParameters();

            // Attempting to apply query attributes to a ContentView as the last navigation item should throw InvalidOperationException
            var exception = Assert.Throws<InvalidOperationException>(
                () => ShellNavigationManager.ApplyQueryAttributes(contentView, queryParams, isLastItem: true, isPopping: false)
            );

            Assert.Contains("ContentView", exception.Message, StringComparison.InvariantCulture);
            Assert.Contains("ContentPage", exception.Message, StringComparison.InvariantCulture);
            Assert.Contains("Shell navigation requires", exception.Message, StringComparison.InvariantCulture);
        }

        [Fact]
        public void ApplyQueryAttributesToContentPageSucceeds()
        {
            var contentPage = new ContentPage { Title = "Test ContentPage" };
            var queryParams = new ShellRouteParameters();

            // Applying query attributes to a ContentPage should succeed without throwing
            ShellNavigationManager.ApplyQueryAttributes(contentPage, queryParams, isLastItem: true, isPopping: false);

            // If we reach here without exception, the test passes
            Assert.True(true);
        }
    }
}
