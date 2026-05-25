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
	/// An adapter for <see cref="MauiCarouselRecyclerView2"/> that wraps each item view in a
	/// <see cref="MaskableFrameLayout"/>, satisfying the Material <see cref="CarouselLayoutManager"/>
	/// requirement that every direct RecyclerView child must be a <see cref="MaskableFrameLayout"/>.
	/// </summary>
	internal sealed class CarouselViewAdapter2
		: Items.CarouselViewAdapter<CarouselView, Items.IItemsViewSource>
	{
		readonly Func<Context, Items.ItemContentView> _createItemContentView;

		// Local template cache for DataTemplateSelector scenarios.
		// Mirrors the private _viewTypeDataTemplates in ItemsViewAdapter so we can
		// look up the correct template when creating a new ViewHolder.
		readonly Dictionary<int, DataTemplate> _templatesByViewType = new();

		internal CarouselViewAdapter2(
			CarouselView carouselView,
			Func<Context, Items.ItemContentView> createItemContentView)
			: base(carouselView)
		{
			_createItemContentView = createItemContentView;
		}

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
					int posInList = NormalizePosition(position);
					if (posInList >= 0 && ItemsSource != null)
					{
						var item = ItemsSource.GetItem(posInList);
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

			var maskable = new MaskableFrameLayout(context);
			maskable.LayoutParameters = new RecyclerView.LayoutParams(
				RecyclerView.LayoutParams.MatchParent,
				RecyclerView.LayoutParams.MatchParent);
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
				int posInList = NormalizePosition(position);
				if (posInList >= 0)
					maskableHolder.Bind(ItemsSource.GetItem(posInList), CarouselView);
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
		// Helpers
		// -----------------------------------------------------------------------

		// Replicates CarouselViewAdapter.GetPositionInList (private) for loop support.
		int NormalizePosition(int position)
		{
			if (CarouselView is null || ItemsSource is null)
				return -1;

			bool hasItems = ItemsSource.Count > 0;
			if (!hasItems)
				return -1;

			return (CarouselView.Loop && hasItems) ? position % ItemsSource.Count : position;
		}
	}
}
