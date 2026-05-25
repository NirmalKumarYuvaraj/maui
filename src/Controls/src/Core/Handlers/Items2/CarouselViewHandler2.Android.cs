#nullable disable
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls.Handlers.Items2
{
	/// <summary>
	/// Android-specific partial for <see cref="CarouselViewHandler2"/>.
	/// Uses <see cref="MauiCarouselRecyclerView2"/> as the platform view, which replaces
	/// <see cref="AndroidX.RecyclerView.Widget.LinearLayoutManager"/> with the Material Design
	/// <see cref="Google.Android.Material.Carousel.CarouselLayoutManager"/>.
	/// All MAUI <see cref="CarouselView"/> properties are preserved.
	/// </summary>
	public partial class CarouselViewHandler2 : Items.ItemsViewHandler<CarouselView>
	{
		double _widthConstraint;
		double _heightConstraint;

		protected override IItemsLayout GetItemsLayout() => VirtualView.ItemsLayout;

		protected override Items.ItemsViewAdapter<CarouselView, Items.IItemsViewSource> CreateAdapter()
		{
			// CarouselViewAdapter2 wraps each item in MaskableFrameLayout, which is required
			// by CarouselLayoutManager. SizedItemContentView is not used here because
			// CarouselLayoutManager controls item sizing via its strategy.
			return new CarouselViewAdapter2(VirtualView, context =>
				new Items.ItemContentView(context));
		}

		protected override RecyclerView CreatePlatformView() =>
			new MauiCarouselRecyclerView2(Context, GetItemsLayout, CreateAdapter);

		// -----------------------------------------------------------------------
		// Property mappers
		// -----------------------------------------------------------------------

		public static PropertyMapper<CarouselView, CarouselViewHandler2> Mapper =
			new(Items.ItemsViewHandler<CarouselView>.ItemsViewMapper)
			{
				[Controls.CarouselView.ItemsLayoutProperty.PropertyName] = MapItemsLayout,
				[Controls.CarouselView.IsSwipeEnabledProperty.PropertyName] = MapIsSwipeEnabled,
				[Controls.CarouselView.PeekAreaInsetsProperty.PropertyName] = MapPeekAreaInsets,
				[Controls.CarouselView.IsBounceEnabledProperty.PropertyName] = MapIsBounceEnabled,
				[Controls.CarouselView.PositionProperty.PropertyName] = MapPosition,
				[Controls.CarouselView.CurrentItemProperty.PropertyName] = MapCurrentItem,
			};

		public CarouselViewHandler2() : base(Mapper) { }

		public CarouselViewHandler2(PropertyMapper mapper = null) : base(mapper ?? Mapper) { }

		// -----------------------------------------------------------------------
		// Map methods — same surface as CarouselViewHandler for MAUI API compatibility
		// -----------------------------------------------------------------------

		public static void MapIsSwipeEnabled(CarouselViewHandler2 handler, CarouselView carouselView)
		{
			if (handler.PlatformView is IMauiCarouselRecyclerView2 carousel2)
				carousel2.IsSwipeEnabled = carouselView.IsSwipeEnabled;
			else if (handler.PlatformView is Items.IMauiCarouselRecyclerView carousel)
				carousel.IsSwipeEnabled = carouselView.IsSwipeEnabled;
		}

		public static void MapIsBounceEnabled(CarouselViewHandler2 handler, CarouselView carouselView)
		{
			handler.PlatformView.OverScrollMode =
				carouselView?.IsBounceEnabled == true ? OverScrollMode.Always : OverScrollMode.Never;
		}

		public static void MapPeekAreaInsets(CarouselViewHandler2 handler, CarouselView carouselView)
		{
			// CarouselLayoutManager manages item sizing via its strategy; applying PeekAreaInsets
			// as RecyclerView padding with clipToPadding=false achieves the peek effect.
			var ctx = handler.Context;
			int leftPx = (int)ctx.ToPixels(carouselView.PeekAreaInsets.Left);
			int topPx = (int)ctx.ToPixels(carouselView.PeekAreaInsets.Top);
			int rightPx = (int)ctx.ToPixels(carouselView.PeekAreaInsets.Right);
			int bottomPx = (int)ctx.ToPixels(carouselView.PeekAreaInsets.Bottom);

			handler.PlatformView.SetPadding(leftPx, topPx, rightPx, bottomPx);
			handler.PlatformView.SetClipToPadding(false);
		}

		public static void MapPosition(CarouselViewHandler2 handler, CarouselView carouselView)
		{
			if (carouselView.Position < 0)
				return;

			if (handler.PlatformView is IMauiCarouselRecyclerView2 carousel2)
				carousel2.UpdateFromPosition();
			else if (handler.PlatformView is Items.IMauiCarouselRecyclerView carousel)
				carousel.UpdateFromPosition();
		}

		public static void MapCurrentItem(CarouselViewHandler2 handler, CarouselView carouselView)
		{
			if (handler.PlatformView is IMauiCarouselRecyclerView2 carousel2)
				carousel2.UpdateFromCurrentItem();
			else if (handler.PlatformView is Items.IMauiCarouselRecyclerView carousel)
				carousel.UpdateFromCurrentItem();
		}

		internal static void MapItemsLayout(CarouselViewHandler2 handler, CarouselView carouselView)
		{
			if (handler.PlatformView is Items.IMauiRecyclerView<CarouselView> recyclerView)
				recyclerView.UpdateLayoutManager();
		}

		// -----------------------------------------------------------------------
		// Size / arrange — mirror CarouselViewHandler for correct item sizing
		// -----------------------------------------------------------------------

		public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			_widthConstraint = widthConstraint;
			_heightConstraint = heightConstraint;

			if (!double.IsInfinity(_widthConstraint))
				_widthConstraint = Context.ToPixels(_widthConstraint);

			if (!double.IsInfinity(_heightConstraint))
				_heightConstraint = Context.ToPixels(_heightConstraint);

			return base.GetDesiredSize(widthConstraint, heightConstraint);
		}

		public override void PlatformArrange(Rect frame)
		{
			_widthConstraint = Context.ToPixels(frame.Width);
			_heightConstraint = Context.ToPixels(frame.Height);

			base.PlatformArrange(frame);
		}

		// -----------------------------------------------------------------------
		// Item size helpers (used by SizedItemContentView callbacks)
		// -----------------------------------------------------------------------

		double GetItemWidth()
		{
			var itemWidth = _widthConstraint;

			if ((PlatformView as Items.IMauiRecyclerView<CarouselView>)?.ItemsLayout
				is LinearItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal })
			{
				var width = PlatformView.MeasuredWidth == 0 ? _widthConstraint : PlatformView.MeasuredWidth;

				if (double.IsInfinity(width))
					return width;

				itemWidth = (int)(width
					- Context?.ToPixels(VirtualView.PeekAreaInsets.Left)
					- Context?.ToPixels(VirtualView.PeekAreaInsets.Right));
			}

			return itemWidth;
		}

		double GetItemHeight()
		{
			var itemHeight = _heightConstraint;

			if ((PlatformView as Items.IMauiRecyclerView<CarouselView>)?.ItemsLayout
				is LinearItemsLayout { Orientation: ItemsLayoutOrientation.Vertical })
			{
				var height = PlatformView.MeasuredHeight == 0 ? _heightConstraint : PlatformView.MeasuredHeight;

				if (double.IsInfinity(height))
					return height;

				itemHeight = (int)(height
					- Context?.ToPixels(VirtualView.PeekAreaInsets.Top)
					- Context?.ToPixels(VirtualView.PeekAreaInsets.Bottom));
			}

			return itemHeight;
		}
	}
}
