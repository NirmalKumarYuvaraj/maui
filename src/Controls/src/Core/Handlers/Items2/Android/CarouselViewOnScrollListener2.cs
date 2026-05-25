#nullable disable
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Carousel;
using AView = Android.Views.View;

namespace Microsoft.Maui.Controls.Handlers.Items2
{
	/// <summary>
	/// Scroll listener for <see cref="MauiCarouselRecyclerView2"/>.
	/// Inherits directly from <see cref="Items.RecyclerViewScrollListener{TItemsView,TItemsViewSource}"/>
	/// (not from <see cref="Items.CarouselViewOnScrollListener"/>) so it has no dependency on
	/// <see cref="Items.CarouselViewLoopManager"/> and can use
	/// <see cref="Google.Android.Material.Carousel.CarouselLayoutManager"/> for item positions.
	/// </summary>
	internal class CarouselViewOnScrollListener2 : Items.RecyclerViewScrollListener<CarouselView, Items.IItemsViewSource>
	{
		readonly CarouselView _carouselView;

		public CarouselViewOnScrollListener2(
			CarouselView carouselView,
			Items.ItemsViewAdapter<CarouselView, Items.IItemsViewSource> itemsViewAdapter)
			: base(carouselView, itemsViewAdapter, true)
		{
			_carouselView = carouselView;
		}

		public override void OnScrollStateChanged(RecyclerView recyclerView, int state)
		{
			base.OnScrollStateChanged(recyclerView, state);

			if (_carouselView.IsSwipeEnabled)
				_carouselView.SetIsDragging(state == RecyclerView.ScrollStateDragging);

			_carouselView.IsScrolling = state != RecyclerView.ScrollStateIdle;

			// Update visual states (Current/Previous/Next) once the carousel settles.
			if (state == RecyclerView.ScrollStateIdle && recyclerView is MauiCarouselRecyclerView2 rv2)
				rv2.UpdateCarouselVisualStatesInternal();
		}

		protected override (int First, int Center, int Last) GetVisibleItemsIndex(RecyclerView recyclerView)
		{
			if (recyclerView.GetLayoutManager() is not CarouselLayoutManager carouselLayoutManager)
				return (-1, -1, -1);

			// CarouselLayoutManager doesn't extend LinearLayoutManager, so we walk children.
			var (first, last) = GetFirstAndLastVisiblePositions(carouselLayoutManager);

			if (first == RecyclerView.NoPosition)
				return (-1, -1, -1);

			// Find the view closest to the center of the RecyclerView as the "current" item.
			float centerX = recyclerView.Width / 2f;
			float centerY = recyclerView.Height / 2f;
			var centerChild = recyclerView.FindChildViewUnder(centerX, centerY);
			var centerPosition = centerChild != null
				? recyclerView.GetChildAdapterPosition(centerChild)
				: (first + last) / 2;

			return (
				GetDataIndexFromView(recyclerView.FindViewHolderForAdapterPosition(first)?.ItemView),
				GetDataIndexFromView(recyclerView.FindViewHolderForAdapterPosition(centerPosition)?.ItemView),
				GetDataIndexFromView(recyclerView.FindViewHolderForAdapterPosition(last)?.ItemView)
			);
		}

		static (int First, int Last) GetFirstAndLastVisiblePositions(CarouselLayoutManager layoutManager)
		{
			int first = int.MaxValue;
			int last = int.MinValue;

			for (int i = 0; i < layoutManager.ChildCount; i++)
			{
				var child = layoutManager.GetChildAt(i);
				if (child is null)
					continue;

				int pos = layoutManager.GetPosition(child);
				if (pos < first) first = pos;
				if (pos > last) last = pos;
			}

			if (first == int.MaxValue)
				return (RecyclerView.NoPosition, RecyclerView.NoPosition);

			return (first, last);
		}

		int GetDataIndexFromView(AView view)
		{
			// ItemView is now MaskableFrameLayout wrapping ItemContentView (see CarouselViewAdapter2).
			Items.ItemContentView cell = view as Items.ItemContentView;
			if (cell is null && view is global::Android.Views.ViewGroup vg && vg.ChildCount > 0)
				cell = vg.GetChildAt(0) as Items.ItemContentView;

			if (cell is not null && ItemsViewAdapter is not null)
			{
				var bindingContext = (cell.View as VisualElement)?.BindingContext;
				return ItemsViewAdapter.GetPositionForItem(bindingContext);
			}

			return -1;
		}
	}
}
