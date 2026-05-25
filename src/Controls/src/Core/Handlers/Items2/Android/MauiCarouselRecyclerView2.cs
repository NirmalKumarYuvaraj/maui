#nullable disable
using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Carousel;

namespace Microsoft.Maui.Controls.Handlers.Items2
{
	/// <summary>
	/// A <see cref="Items.MauiCarouselRecyclerView"/> variant that uses the Material Design
	/// <see cref="CarouselLayoutManager"/> (with <see cref="MultiBrowseCarouselStrategy"/>) instead of
	/// <see cref="LinearLayoutManager"/>.
	///
	/// All MAUI CarouselView API surface (Position, CurrentItem, IsSwipeEnabled, IsBounceEnabled,
	/// PeekAreaInsets, ItemsLayout, Loop) is preserved by inheriting the existing scroll, loop, and
	/// visual-state machinery from <see cref="Items.MauiCarouselRecyclerView"/>.
	/// </summary>
	public class MauiCarouselRecyclerView2 :
		Items.MauiCarouselRecyclerView,
		IMauiCarouselRecyclerView2
	{
		CarouselSnapHelper _carouselSnapHelper;
		bool _disposed;

		public MauiCarouselRecyclerView2(
			Context context,
			Func<IItemsLayout> getItemsLayout,
			Func<Items.ItemsViewAdapter<CarouselView, Items.IItemsViewSource>> getAdapter)
			: base(context, getItemsLayout, getAdapter)
		{
		}

		// -----------------------------------------------------------------------
		// Layout manager — swap LinearLayoutManager for CarouselLayoutManager
		// -----------------------------------------------------------------------

		protected override LayoutManager SelectLayoutManager(IItemsLayout layoutSpecification)
		{
			var orientation = RecyclerView.Horizontal;

			if (layoutSpecification is LinearItemsLayout linearItemsLayout)
			{
				orientation = linearItemsLayout.Orientation == ItemsLayoutOrientation.Vertical
					? RecyclerView.Vertical
					: RecyclerView.Horizontal;
			}

			return new CarouselLayoutManager(CreateCarouselStrategy(), orientation);
		}

		/// <summary>
		/// Creates the <see cref="CarouselStrategy"/> to use.
		/// Override in a subclass to supply <see cref="HeroCarouselStrategy"/>,
		/// <see cref="FullScreenCarouselStrategy"/>, or <see cref="UncontainedCarouselStrategy"/>.
		/// </summary>
		protected virtual CarouselStrategy CreateCarouselStrategy() =>
			new MultiBrowseCarouselStrategy();

		// -----------------------------------------------------------------------
		// Snap — replace MAUI snap manager with CarouselSnapHelper
		// -----------------------------------------------------------------------

		protected override void UpdateSnapBehavior()
		{
			// Detach any previous snap helper to avoid duplicate fling listeners.
			_carouselSnapHelper?.AttachToRecyclerView(null);
			_carouselSnapHelper = null;

			// CarouselLayoutManager ships its own snap helper; attach it directly.
			// Deliberately do NOT call base.UpdateSnapBehavior() so MAUI's SnapManager
			// does not attach a conflicting snap helper.
			_carouselSnapHelper = new CarouselSnapHelper();
			_carouselSnapHelper.AttachToRecyclerView(this);
		}

		protected override void ScrollToRequested(object sender, ScrollToRequestEventArgs args)
		{
			// Skip the MAUI snap-manager reset (no SingleSnapHelper attached) and go straight
			// to the underlying scroll so CarouselSnapHelper continues to control snapping.
			ScrollTo(args);
		}

		// -----------------------------------------------------------------------
		// Spacing decoration — CarouselLayoutManager manages item sizes via its
		// strategy, so we use a no-op decoration. PeekAreaInsets are applied as
		// RecyclerView padding by the handler instead.
		// -----------------------------------------------------------------------

		protected override RecyclerView.ItemDecoration CreateSpacingDecoration(IItemsLayout itemsLayout) =>
			new NoOpItemDecoration();

		sealed class NoOpItemDecoration : RecyclerView.ItemDecoration { }

		// -----------------------------------------------------------------------
		// Scroll listener — override to use CarouselLayoutManager-aware listener
		// -----------------------------------------------------------------------

		protected override Items.RecyclerViewScrollListener<CarouselView, Items.IItemsViewSource> CreateScrollListener() =>
			new CarouselViewOnScrollListener2(Carousel, ItemsViewAdapter);

		// -----------------------------------------------------------------------
		// Initial visual state trigger — post once after layout is ready
		// -----------------------------------------------------------------------

		public override void SetUpNewElement(CarouselView newElement)
		{
			base.SetUpNewElement(newElement);
			Post(UpdateCarouselVisualStates);
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();
			Post(UpdateCarouselVisualStates);
		}

		// -----------------------------------------------------------------------
		// Visual states — adapt FindFirst/Last to CarouselLayoutManager
		// -----------------------------------------------------------------------
		// NOTE: Do NOT override OnLayout here. Calling VisualStateManager.GoToState()
		// inside OnLayout triggers MeasureInvalidated → RequestLayout → OnLayout again,
		// creating an infinite GC loop. Visual states are updated from the scroll
		// listener (on SCROLL_STATE_IDLE) and once after initial layout via Post().

		internal void UpdateCarouselVisualStatesInternal() => UpdateCarouselVisualStates();

		void UpdateCarouselVisualStates()
		{
			if (GetLayoutManager() is not CarouselLayoutManager carouselLayoutManager)
				return;

			var (first, last) = GetFirstAndLastVisiblePositions(carouselLayoutManager);

			if (first == RecyclerView.NoPosition)
				return;

			// Approximate the "current" (center) item using the view under the mid-point.
			float centerX = Width / 2f;
			float centerY = Height / 2f;
			var centerChild = FindChildViewUnder(centerX, centerY);
			var carouselPosition = centerChild != null
				? GetChildAdapterPosition(centerChild)
				: (first + last) / 2;

			var previousPosition = carouselPosition - 1;
			var nextPosition = carouselPosition + 1;

			var newViews = new List<Controls.View>();

			for (int i = first; i <= last; i++)
			{
				var androidCell = carouselLayoutManager.FindViewByPosition(i);

				// Each item is a MaskableFrameLayout wrapping an ItemContentView (see CarouselViewAdapter2).
				Items.ItemContentView contentCell = androidCell as Items.ItemContentView;
				if (contentCell is null && androidCell is global::Android.Views.ViewGroup vg && vg.ChildCount > 0)
					contentCell = vg.GetChildAt(0) as Items.ItemContentView;

				if (contentCell is null)
					continue;

				if (contentCell.View is not Controls.View mauiView)
					continue;

				if (i == carouselPosition)
					VisualStateManager.GoToState(mauiView, CarouselView.CurrentItemVisualState);
				else if (i == previousPosition)
					VisualStateManager.GoToState(mauiView, CarouselView.PreviousItemVisualState);
				else if (i == nextPosition)
					VisualStateManager.GoToState(mauiView, CarouselView.NextItemVisualState);
				else
					VisualStateManager.GoToState(mauiView, CarouselView.DefaultItemVisualState);

				newViews.Add(mauiView);

				if (!Carousel.VisibleViews.Contains(mauiView))
					Carousel.VisibleViews.Add(mauiView);
			}

			// Remove items that have scrolled out of view.
			foreach (var oldView in _trackedVisibleViews)
			{
				if (!newViews.Contains(oldView))
				{
					VisualStateManager.GoToState(oldView, CarouselView.DefaultItemVisualState);
					Carousel.VisibleViews.Remove(oldView);
				}
			}

			_trackedVisibleViews = newViews;
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

		// Mirrors MauiCarouselRecyclerView._oldViews for the CarouselLayoutManager path.
		List<Controls.View> _trackedVisibleViews = new();

		// -----------------------------------------------------------------------
		// Dispose
		// -----------------------------------------------------------------------

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;
				_carouselSnapHelper?.AttachToRecyclerView(null);
				_carouselSnapHelper = null;
			}

			base.Dispose(disposing);
		}

		// -----------------------------------------------------------------------
		// IMauiCarouselRecyclerView2 — forward to base IMauiCarouselRecyclerView impl
		// -----------------------------------------------------------------------

		void IMauiCarouselRecyclerView2.UpdateFromCurrentItem() =>
			((Items.IMauiCarouselRecyclerView)this).UpdateFromCurrentItem();

		void IMauiCarouselRecyclerView2.UpdateFromPosition() =>
			((Items.IMauiCarouselRecyclerView)this).UpdateFromPosition();
	}
}
