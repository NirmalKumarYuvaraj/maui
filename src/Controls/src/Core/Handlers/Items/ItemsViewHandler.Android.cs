#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using MeasureSpecMode = Android.Views.MeasureSpecMode;

namespace Microsoft.Maui.Controls.Handlers.Items
{
	public abstract partial class ItemsViewHandler<TItemsView> : ViewHandler<TItemsView, RecyclerView> where TItemsView : ItemsView
	{
		protected ItemsViewHandler(PropertyMapper mapper, CommandMapper commandMapper = null) : base(mapper, commandMapper)
		{
		}

		protected abstract IItemsLayout GetItemsLayout();

		protected virtual ItemsViewAdapter<TItemsView, IItemsViewSource> CreateAdapter() => new(VirtualView);

		protected override void ConnectHandler(RecyclerView platformView)
		{
			base.ConnectHandler(platformView);
			(platformView as IMauiRecyclerView<TItemsView>)?.SetUpNewElement(VirtualView);
		}

		protected override void DisconnectHandler(RecyclerView platformView)
		{
			base.DisconnectHandler(platformView);
			(platformView as IMauiRecyclerView<TItemsView>)?.TearDownOldElement(VirtualView);
		}

		protected override RecyclerView CreatePlatformView() =>
			new MauiRecyclerView<TItemsView, ItemsViewAdapter<TItemsView, IItemsViewSource>, IItemsViewSource>(Context, GetItemsLayout, CreateAdapter);

		public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			if (PlatformView.GetAdapter() is EmptyViewAdapter)
			{
				var view = (IView)VirtualView;
				var widthSpec = Context.CreateMeasureSpec(widthConstraint, view.Width, view.MinimumWidth, view.MaximumWidth);
				var heightSpec = Context.CreateMeasureSpec(heightConstraint, view.Height, view.MinimumHeight, view.MaximumHeight);

				UpdateEmptyViewSize(GetMeasureSpecSize(widthSpec), GetMeasureSpecSize(heightSpec));
			}

			return base.GetDesiredSize(widthConstraint, heightConstraint);
		}

		static double GetMeasureSpecSize(int measureSpec) =>
			AView.MeasureSpec.GetMode(measureSpec) == MeasureSpecMode.Unspecified
				? double.PositiveInfinity
				: AView.MeasureSpec.GetSize(measureSpec);

		public override void PlatformArrange(Rect frame)
		{
			var width = (int)Context.ToPixels(frame.Width);
			var height = (int)Context.ToPixels(frame.Height);

			UpdateEmptyViewSize(width, height);

			base.PlatformArrange(frame);
		}

		public static void MapItemsSource(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
			(handler.PlatformView as IMauiRecyclerView<TItemsView>)?.UpdateItemsSource();
		}

		public static void MapHorizontalScrollBarVisibility(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
			(handler.PlatformView as IMauiRecyclerView<TItemsView>)?.UpdateHorizontalScrollBarVisibility();
		}

		public static void MapVerticalScrollBarVisibility(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
			(handler.PlatformView as IMauiRecyclerView<TItemsView>)?.UpdateVerticalScrollBarVisibility();
		}

		public static void MapItemTemplate(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
			(handler.PlatformView as IMauiRecyclerView<TItemsView>)?.UpdateItemTemplate();
		}

		public static void MapEmptyView(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
			(handler.PlatformView as IMauiRecyclerView<TItemsView>)?.UpdateEmptyView();
		}

		public static void MapEmptyViewTemplate(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
			(handler.PlatformView as IMauiRecyclerView<TItemsView>)?.UpdateEmptyView();
		}

		public static void MapFlowDirection(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
			(handler.PlatformView as IMauiRecyclerView<TItemsView>)?.UpdateFlowDirection();
		}

		public static void MapIsVisible(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
		}

		public static void MapItemsUpdatingScrollMode(ItemsViewHandler<TItemsView> handler, ItemsView itemsView)
		{
			(handler.PlatformView as IMauiRecyclerView<TItemsView>)?.UpdateScrollingMode();
		}

		void UpdateEmptyViewSize(double width, double height)
		{
			var platformView = PlatformView;
			var adapter = platformView.GetAdapter();

			if (adapter is EmptyViewAdapter emptyViewAdapter)
			{
				emptyViewAdapter.RecyclerViewWidth = width;
				emptyViewAdapter.RecyclerViewHeight = height;

				// Unbounded constraints fall back to the RecyclerView's measured size.
				var effectiveWidth = emptyViewAdapter.GetWidth(platformView);
				var effectiveHeight = emptyViewAdapter.GetHeight(platformView);
				var widthChanged = emptyViewAdapter.LastEmptyViewWidth != effectiveWidth;
				var heightChanged = emptyViewAdapter.LastEmptyViewHeight != effectiveHeight;
				emptyViewAdapter.LastEmptyViewWidth = effectiveWidth;
				emptyViewAdapter.LastEmptyViewHeight = effectiveHeight;

				if (widthChanged || heightChanged)
				{
					// EmptyView position depends on whether the CollectionView has a header
					var structuredItemsView = VirtualView as StructuredItemsView;
					var hasHeader = (structuredItemsView?.Header ?? structuredItemsView?.HeaderTemplate) is not null;
					var emptyViewPosition = hasHeader ? 1 : 0;

					// Check if ViewHolder exists and is ready for immediate layout request
					var viewHolder = platformView.FindViewHolderForAdapterPosition(emptyViewPosition);
					if (viewHolder is not null)
					{
						PlatformInterop.RequestLayoutIfNeeded(viewHolder.ItemView);
					}
					else
					{
						// ViewHolder not created yet, defer layout request to next UI loop iteration
						platformView.Post(() =>
						{
							if (!ReferenceEquals(((IElementHandler)this).PlatformView, platformView))
							{
								return;
							}
							var vh = platformView.FindViewHolderForAdapterPosition(emptyViewPosition);
							vh?.ItemView.RequestLayout();
						});
					}
				}
			}
		}
	}
}