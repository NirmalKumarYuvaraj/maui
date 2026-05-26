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
	/// <see cref="CarouselLayoutManager"/> (with <see cref="FullScreenCarouselStrategy"/>) instead of
	/// <see cref="LinearLayoutManager"/>.
	///
	/// All MAUI CarouselView API surface (Position, CurrentItem, IsSwipeEnabled, IsBounceEnabled,
	/// PeekAreaInsets, ItemsLayout) is preserved by inheriting the existing scroll and visual-state
	/// machinery from <see cref="Items.MauiCarouselRecyclerView"/>.
	///
	/// <para>
	/// <b>Looping is not supported on this handler.</b> Material's <see cref="CarouselLayoutManager"/>
	/// has no concept of a virtual range, so the LoopScale (≈16384) trick used by MAUI's
	/// LinearLayoutManager-based implementation does not work. Callers must keep
	/// <see cref="CarouselView.Loop"/> set to <c>false</c>; <see cref="CarouselViewAdapter2.ItemCount"/>
	/// is locked to <c>ItemsSource.Count</c> to guard the adapter side.
	/// </para>
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
		///
		/// Currently locked to <see cref="FullScreenCarouselStrategy"/>: the other Material
		/// strategies (MultiBrowse, Hero, Uncontained) require items to be smaller than the
		/// viewport, which conflicts with how Handler2 sizes items (full RecyclerView width/
		/// height via <see cref="Items.SizedItemContentView"/>). If a future change wires up
		/// strategy-aware sizing, this can become user-selectable via an attached property.
		/// </summary>
		protected virtual CarouselStrategy CreateCarouselStrategy() => new FullScreenCarouselStrategy();

		// -----------------------------------------------------------------------
		// Snap — replace MAUI snap manager with CarouselSnapHelper
		// -----------------------------------------------------------------------

		protected override void UpdateSnapBehavior()
		{
			// Detach any previous snap helper to avoid duplicate fling listeners.
			_carouselSnapHelper?.AttachToRecyclerView(null);

			// CarouselLayoutManager ships its own snap helper; attach it directly.
			// Deliberately do NOT call base.UpdateSnapBehavior() so MAUI's SnapManager
			// does not attach a conflicting snap helper.
			_carouselSnapHelper = new CarouselSnapHelper();
			_carouselSnapHelper.AttachToRecyclerView(this);
		}

		public override void UpdateLayoutManager()
		{
			base.UpdateLayoutManager();

			// The base swaps the LayoutManager; the previously attached CarouselSnapHelper
			// still references the old LayoutManager internally. Re-attach so snapping
			// continues to track the current Material CarouselLayoutManager.
			UpdateSnapBehavior();
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

		protected override RecyclerView.ItemDecoration CreateSpacingDecoration(IItemsLayout itemsLayout)
			=> new NoOpItemDecoration();

		sealed class NoOpItemDecoration : RecyclerView.ItemDecoration { }

		// -----------------------------------------------------------------------
		// Scroll listener — override to use CarouselLayoutManager-aware listener
		// -----------------------------------------------------------------------

		protected override Items.RecyclerViewScrollListener<CarouselView, Items.IItemsViewSource> CreateScrollListener()
			=> new CarouselViewOnScrollListener2(Carousel, ItemsViewAdapter, () => _carouselSnapHelper);

		// -----------------------------------------------------------------------
		// Initial visual state trigger — post once after layout is ready
		// -----------------------------------------------------------------------

		public override void SetUpNewElement(CarouselView newElement)
		{
			// NOTE: CarouselView.Loop must be set to false by the caller when using
			// CarouselViewHandler2. The base class's loop machinery (LoopScale ≈ 16384
			// virtual items, LoopedPosition jump to ~8192 in UpdateInitialPosition) is
			// incompatible with Material's CarouselLayoutManager, which has no concept
			// of a virtual range. The adapter's ItemCount override (see
			// CarouselViewAdapter2) ensures the RecyclerView never sees a virtual range,
			// but Loop=true on the CarouselView will still cause the base class to seek
			// to an out-of-bounds position on initial layout.

			base.SetUpNewElement(newElement);
			Post(UpdateCarouselVisualStates);
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			// SetUpNewElement already posts an initial visual-state update. Only post
			// again here if we attached without SetUpNewElement having run yet (re-attach
			// of an existing view), which is signalled by an empty tracked-views list.
			if (_trackedVisibleViews.Count == 0)
			{
				Post(UpdateCarouselVisualStates);
			}
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

			// Use CarouselSnapHelper's notion of the "snap target" as the current item —
			// this matches what the user sees as the focal item after a fling.
			int carouselPosition = -1;
			if (_carouselSnapHelper is not null)
			{
				var snapView = _carouselSnapHelper.FindSnapView(carouselLayoutManager);
				if (snapView is not null)
					carouselPosition = GetChildAdapterPosition(snapView);
			}

			if (carouselPosition == RecyclerView.NoPosition || carouselPosition < 0)
				carouselPosition = (first + last) / 2;

			var previousPosition = carouselPosition - 1;
			var nextPosition = carouselPosition + 1;

			var newViews = new List<Controls.View>();

			for (int i = first; i <= last; i++)
			{
				var androidCell = carouselLayoutManager.FindViewByPosition(i);

				// Each item is a MaskableFrameLayout wrapping an ItemContentView
				// (see CarouselViewAdapter2.OnCreateViewHolder).
				Items.ItemContentView contentCell = androidCell as Items.ItemContentView;
				if (contentCell is null && androidCell is global::Android.Views.ViewGroup vg && vg.ChildCount > 0)
					contentCell = vg.GetChildAt(0) as Items.ItemContentView;

				if (contentCell?.View is not Controls.View mauiView)
					continue;

				string targetState;
				if (i == carouselPosition)
					targetState = CarouselView.CurrentItemVisualState;
				else if (i == previousPosition)
					targetState = CarouselView.PreviousItemVisualState;
				else if (i == nextPosition)
					targetState = CarouselView.NextItemVisualState;
				else
					targetState = CarouselView.DefaultItemVisualState;

				VisualStateManager.GoToState(mauiView, targetState);

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
				if (pos < first)
					first = pos;
				if (pos > last)
					last = pos;
			}

			if (first == int.MaxValue)
				return (RecyclerView.NoPosition, RecyclerView.NoPosition);

			return (first, last);
		}

		// Mirrors MauiCarouselRecyclerView._oldViews for the CarouselLayoutManager path.
		List<Controls.View> _trackedVisibleViews = new();

		// -----------------------------------------------------------------------
		// Dispose / teardown
		// -----------------------------------------------------------------------

		public override void TearDownOldElement(CarouselView oldElement)
		{
			ClearTrackedVisibleViews(oldElement);
			base.TearDownOldElement(oldElement);
		}

		void ClearTrackedVisibleViews(CarouselView carouselView)
		{
			if (_trackedVisibleViews.Count == 0)
				return;

			if (carouselView is not null)
			{
				foreach (var view in _trackedVisibleViews)
					carouselView.VisibleViews.Remove(view);
			}

			_trackedVisibleViews.Clear();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;
				_carouselSnapHelper?.AttachToRecyclerView(null);
				_carouselSnapHelper = null;
				_trackedVisibleViews.Clear();
			}

			base.Dispose(disposing);
		}

		// -----------------------------------------------------------------------
		// IMauiCarouselRecyclerView2 — forward to base IMauiCarouselRecyclerView impl
		// -----------------------------------------------------------------------

		void IMauiCarouselRecyclerView2.UpdateFromCurrentItem()
			=> ((Items.IMauiCarouselRecyclerView)this).UpdateFromCurrentItem();

		void IMauiCarouselRecyclerView2.UpdateFromPosition()
			=> ((Items.IMauiCarouselRecyclerView)this).UpdateFromPosition();

		bool IMauiCarouselRecyclerView2.IsSwipeEnabled
		{
			get => IsSwipeEnabled;
			set => IsSwipeEnabled = value;
		}
	}
}
