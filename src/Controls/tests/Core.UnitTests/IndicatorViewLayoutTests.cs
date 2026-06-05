using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class IndicatorViewTests : BaseTestFixture
	{
		// Regression test for https://github.com/dotnet/maui/issues/35775
		// IndicatorView subscribed to a shared ObservableCollection via CollectionChanged
		// but never unsubscribed, keeping the view alive after page navigation.
		[Fact]
		public async Task IndicatorView_WithSharedObservableCollection_IsCollectedAfterUnlink()
		{
			// Arrange: a shared (rooted) collection, like a ViewModel property
			var sharedItems = new ObservableCollection<string> { "item1", "item2", "item3" };

			WeakReference weakCarousel = null;
			WeakReference weakIndicator = null;

			// Use a recursive helper to ensure the stack frame holding local references
			// is popped before GC runs (required for correct GC behavior on all runtimes).
			CreateLinkedViews(0, sharedItems, out weakCarousel, out weakIndicator);

			// Act: GC with the shared collection still rooted (the leak scenario)
			Assert.False(await weakCarousel.WaitForCollect(), "CarouselView should be collected after all strong references are dropped");
			Assert.False(await weakIndicator.WaitForCollect(), "IndicatorView leaked: shared ObservableCollection holds it alive via CollectionChanged subscription");
		}

		[Fact]
		public async Task IndicatorView_WhenItemsSourceCleared_IsCollected()
		{
			var sharedItems = new ObservableCollection<string> { "item1", "item2", "item3" };

			WeakReference weakCarousel = null;
			WeakReference weakIndicator = null;

			CreateLinkedViewsWithClearedSource(0, sharedItems, out weakCarousel, out weakIndicator);

			Assert.False(await weakCarousel.WaitForCollect(), "CarouselView should be collected after ItemsSource is cleared");
			Assert.False(await weakIndicator.WaitForCollect(), "IndicatorView should be collected after ItemsSource is cleared");
		}

		// Recursive method ensures the objects created inside are not held on the current stack frame,
		// allowing the GC to collect them after return (mirrors the pattern in BindingUnitTests).
		static void CreateLinkedViews(int depth, ObservableCollection<string> sharedItems, out WeakReference weakCarousel, out WeakReference weakIndicator)
		{
			if (depth < 1024)
			{
				CreateLinkedViews(depth + 1, sharedItems, out weakCarousel, out weakIndicator);
				return;
			}

			var carouselView = new CarouselView { ItemsSource = sharedItems };
			var indicatorView = new IndicatorView();
			carouselView.IndicatorView = indicatorView;

			weakCarousel = new WeakReference(carouselView);
			weakIndicator = new WeakReference(indicatorView);
			// Local variables go out of scope when this stack frame is popped
		}

		static void CreateLinkedViewsWithClearedSource(int depth, ObservableCollection<string> sharedItems, out WeakReference weakCarousel, out WeakReference weakIndicator)
		{
			if (depth < 1024)
			{
				CreateLinkedViewsWithClearedSource(depth + 1, sharedItems, out weakCarousel, out weakIndicator);
				return;
			}

			var carouselView = new CarouselView { ItemsSource = sharedItems };
			var indicatorView = new IndicatorView();
			carouselView.IndicatorView = indicatorView;

			weakCarousel = new WeakReference(carouselView);
			weakIndicator = new WeakReference(indicatorView);

			// Simulate explicit cleanup (e.g., OnDisappearing clearing ItemsSource)
			carouselView.ItemsSource = null;
		}


		[Fact]
		public void IndicatorStackLayoutNoItems_ResetIndicators_ShouldHaveNoChildren()
		{
			// Arrange
			var indicatorView = new IndicatorView();
			var indicatorStackLayout = new IndicatorStackLayout(indicatorView);

			// Act
			indicatorStackLayout.ResetIndicators();

			// Assert
			Assert.Empty(indicatorStackLayout.Children);
		}

		[Fact]
		public void IndicatorStackLayoutWithItems_ResetIndicators_ShouldBindChildren()
		{
			// Arrange
			var indicatorView = new IndicatorView() { ItemsSource = new List<string> { "item1", "item2" } };
			var indicatorStackLayout = new IndicatorStackLayout(indicatorView);

			// Act
			indicatorStackLayout.ResetIndicators();

			// Assert
			Assert.Equal(2, indicatorStackLayout.Children.Count);
		}

		[Theory]
		[InlineData(1, 2)]
		[InlineData(0, 2)]
		[InlineData(-2, 2)]
		public void IndicatorStackLayout_ResetIndicatorCount_ShouldBindChildren(int oldCount, int expected)
		{
			// Arrange
			var indicatorView = new IndicatorView() { ItemsSource = new List<string> { "item1", "item2" } };
			var indicatorStackLayout = new IndicatorStackLayout(indicatorView);
			Assert.Empty(indicatorStackLayout.Children);

			// Act
			indicatorStackLayout.ResetIndicatorCount(oldCount);

			// Assert
			Assert.Equal(expected, indicatorStackLayout.Children.Count);
		}

		[Fact]
		public void IndicatorLayout_ShouldBeRemovedWhenIndicatorTemplateIsNulled()
		{
			// Arrange
			var indicatorView = new IndicatorView() { ItemsSource = new List<string> { "item1", "item2" } };
			indicatorView.IndicatorTemplate = new DataTemplate();
			Assert.NotNull(indicatorView.IndicatorLayout);

			// Act
			indicatorView.IndicatorTemplate = null;

			//Assert
			Assert.Null(indicatorView.IndicatorLayout);
		}
	}
}
