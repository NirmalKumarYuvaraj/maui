#nullable disable
using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Carousel;
using Google.Android.Material.Shape;

namespace Microsoft.Maui.Controls.Handlers.Items2
{
	/// <summary>
	/// An adapter for <see cref="MauiCarouselRecyclerView2"/> that wraps each item view in a
	/// <see cref="MaskableFrameLayout"/>, satisfying the Material <see cref="CarouselLayoutManager"/>
	/// requirement that every direct RecyclerView child must be a <see cref="MaskableFrameLayout"/>.
	/// </summary>
	internal sealed class CarouselViewAdapter2
		: Items.CarouselViewAdapter<CarouselView, Items.IItemsViewSource>
	{
		// Default corner radius (dp) applied to the MaskableFrameLayout when no shape style
		// is provided. Matches Material's m3_carousel_*_corner_size.
		const float DefaultCornerRadiusDp = 16f;

		readonly Func<Context, Items.ItemContentView> _createItemContentView;
		readonly Func<bool> _isHorizontal;

		// Local template cache for DataTemplateSelector scenarios.
		// Mirrors the private _viewTypeDataTemplates in ItemsViewAdapter so we can
		// look up the correct template when creating a new ViewHolder.
		readonly Dictionary<int, DataTemplate> _templatesByViewType = new();

		internal CarouselViewAdapter2(
			CarouselView carouselView,
			Func<Context, Items.ItemContentView> createItemContentView,
			Func<bool> isHorizontal)
			: base(carouselView)
		{
			_createItemContentView = createItemContentView;
			_isHorizontal = isHorizontal;
		}

		/// <summary>
		/// Override the base <see cref="Items.CarouselViewAdapter{TItemsView,TItemsViewSource}.ItemCount"/>
		/// to ignore <see cref="CarouselView.Loop"/>.
		///
		/// The base adapter returns <c>CarouselViewLoopManager.LoopScale</c> (≈16384) when
		/// <c>Loop=true</c>, which works with <c>LinearLayoutManager</c> + MAUI's
		/// <c>SnapManager</c>. Material's <see cref="CarouselLayoutManager"/> was not designed
		/// for that scale: every measure pass that triggers <c>MeasureInvalidated</c> →
		/// <c>RequestLayout</c> re-enters layout from a different anchor in the 16384-item
		/// virtual range, never converges, and inflates view holders without ever recycling.
		/// That produces the "stuck on splash" / endless GC symptom on Android.
		///
		/// CarouselLayoutManager has no native looping support, so for <c>Handler2</c> we
		/// expose the real item count. Callers should keep <see cref="CarouselView.Loop"/>
		/// set to <c>false</c>.
		/// </summary>
		public override int ItemCount => ItemsSource?.Count ?? 0;

		// -----------------------------------------------------------------------
		// Track template per view-type so OnCreateViewHolder can look it up
		// -----------------------------------------------------------------------

		public override int GetItemViewType(int position)
		{
			var viewType = base.GetItemViewType(position);

			if (viewType == Items.ItemViewType.TextItem)
				return viewType;

			// Populate our local cache so OnCreateViewHolder can find the template.
			if (!_templatesByViewType.ContainsKey(viewType))
			{
				if (ItemsView.ItemTemplate is DataTemplateSelector selector)
				{
					if (ItemsSource is not null && position >= 0 && position < ItemsSource.Count)
					{
						var item = ItemsSource.GetItem(position);
						var template = selector.SelectTemplate(item, ItemsView);
						if (template is not null)
							_templatesByViewType.TryAdd(viewType, template);
					}
				}
				else if (ItemsView.ItemTemplate is not null)
				{
					_templatesByViewType.TryAdd(viewType, ItemsView.ItemTemplate);
				}
			}

			return viewType;
		}

		// -----------------------------------------------------------------------
		// ViewHolder creation — wrap ItemContentView in MaskableFrameLayout
		// -----------------------------------------------------------------------

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
		{
			var context = parent.Context;

			if (viewType == Items.ItemViewType.TextItem)
			{
				// Text items don't need MaskableFrameLayout; delegate to base.
				return base.OnCreateViewHolder(parent, viewType);
			}

			var itemContentView = _createItemContentView(context);
			itemContentView.LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent);

			// CarouselLayoutManager's strategy reads the *measured* width (horizontal) or
			// height (vertical) of the first child to build its KeylineState. Use
			// WRAP_CONTENT on the carousel axis so SizedItemContentView can push the
			// desired pixel size up through measurement. A fixed pixel value sampled
			// here from GetItemWidth/Height would be 0 before the RecyclerView is laid
			// out — Math.Max(1, 0) then yields 1px items, which combined with looping
			// (LoopScale ≈ 16384 items) causes an infinite measure / GC loop and a
			// stuck UI.
			bool horizontal = _isHorizontal?.Invoke() ?? true;
			var maskable = new MaskableFrameLayout(context)
			{
				LayoutParameters = new RecyclerView.LayoutParams(
					horizontal ? ViewGroup.LayoutParams.WrapContent : ViewGroup.LayoutParams.MatchParent,
					horizontal ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent),
			};

			// MaskableFrameLayout built in code has no shapeAppearance attribute, leaving the
			// shape model empty. Set a sensible default so Material's mask clipping behaves
			// like the XML-inflated samples.
			float radiusPx = context.Resources.DisplayMetrics.Density * DefaultCornerRadiusDp;
			maskable.ShapeAppearanceModel = new ShapeAppearanceModel()
				.ToBuilder()
				.SetAllCornerSizes(radiusPx)
				.Build();

			maskable.AddView(itemContentView);

			var template = _templatesByViewType.TryGetValue(viewType, out var dt)
				? dt
				: ItemsView.ItemTemplate;

			return new MaskableCarouselItemViewHolder(maskable, itemContentView, template);
		}

		// -----------------------------------------------------------------------
		// Bind / recycle
		// -----------------------------------------------------------------------

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			if (holder is MaskableCarouselItemViewHolder maskableHolder)
			{
				if (CarouselView is null || ItemsSource is null || position < 0 || position >= ItemsSource.Count)
					return;

				var item = ItemsSource.GetItem(position);
				maskableHolder.Bind(item, CarouselView);
				return;
			}

			base.OnBindViewHolder(holder, position);
		}

		public override void OnViewRecycled(Java.Lang.Object holder)
		{
			if (holder is MaskableCarouselItemViewHolder maskableHolder)
			{
				maskableHolder.Recycle(CarouselView);
				return;
			}

			base.OnViewRecycled(holder);
		}

		// -----------------------------------------------------------------------
		// Lifecycle — invalidate the template cache when the adapter is disposed
		// -----------------------------------------------------------------------

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_templatesByViewType.Clear();

			base.Dispose(disposing);
		}
	}
}
