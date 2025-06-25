using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
    public partial class BoxViewTests
    {
        [Fact]
        [Description("BoxView should render correctly when parent becomes visible after being initially hidden")]
        public async Task BoxViewRendersWhenParentBecomesVisible()
        {
            // Arrange - Create a BoxView inside a Grid that starts invisible
            var boxView = new BoxView
            {
                Color = Colors.Red,
                HeightRequest = 100,
                WidthRequest = 100
            };

            var parentGrid = new Grid
            {
                IsVisible = false, // Initially hidden
                HeightRequest = 100,
                WidthRequest = 100
            };

            parentGrid.Children.Add(boxView);

            // Act - Create the handler while parent is invisible
            var handler = await CreateHandlerAsync<BoxViewHandler>(boxView);
            
            await InvokeOnMainThreadAsync(async () =>
            {
                // Verify the BoxView handler is created
                Assert.NotNull(handler);
                Assert.NotNull(handler.PlatformView);
                
                // Make parent visible
                parentGrid.IsVisible = true;
                
                // Verify the BoxView is visible
                var isVisible = await GetPlatformIsVisible(handler);
                Assert.True(isVisible);
            });
        }

        [Fact]
        [Description("BoxView should render correctly when toggling parent visibility multiple times")]
        public async Task BoxViewRendersAfterMultipleVisibilityToggles()
        {
            // Arrange
            var boxView = new BoxView
            {
                Color = Colors.Blue,
                HeightRequest = 100,
                WidthRequest = 100
            };

            var parentGrid = new Grid
            {
                IsVisible = false, // Initially hidden
                HeightRequest = 100,
                WidthRequest = 100
            };

            parentGrid.Children.Add(boxView);

            // Act
            var handler = await CreateHandlerAsync<BoxViewHandler>(boxView);
            
            await InvokeOnMainThreadAsync(async () =>
            {
                // Initially invisible
                var isVisible = await GetPlatformIsVisible(handler);
                Assert.False(isVisible);
                
                // Make visible
                parentGrid.IsVisible = true;
                isVisible = await GetPlatformIsVisible(handler);
                Assert.True(isVisible);
                
                // Hide again
                parentGrid.IsVisible = false;
                isVisible = await GetPlatformIsVisible(handler);
                Assert.False(isVisible);
                
                // Make visible again
                parentGrid.IsVisible = true;
                isVisible = await GetPlatformIsVisible(handler);
                Assert.True(isVisible);
            });
        }
    }
}