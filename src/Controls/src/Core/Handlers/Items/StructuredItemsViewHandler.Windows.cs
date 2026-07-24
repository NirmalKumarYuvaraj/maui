#nullable disable
using System;
using System.ComponentModel;
using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WASDKApp = Microsoft.UI.Xaml.Application;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode;
using WSetter = Microsoft.UI.Xaml.Setter;
using WStyle = Microsoft.UI.Xaml.Style;

namespace Microsoft.Maui.Controls.Handlers.Items
{
	public partial class StructuredItemsViewHandler<TItemsView> : ItemsViewHandler<TItemsView> where TItemsView : StructuredItemsView
	{
		View _currentHeader;
		View _currentFooter;
		WeakNotifyPropertyChangedProxy _layoutPropertyChangedProxy;
		PropertyChangedEventHandler _layoutPropertyChanged;
		const string ListViewItemStyleKey = "DefaultListViewItemStyle";
		const string GridViewItemStyleKey = "DefaultGridViewItemStyle";
		static WStyle _listViewItemStyle;
		static WStyle _gridViewItemStyle;

		~StructuredItemsViewHandler() => _layoutPropertyChangedProxy?.Unsubscribe();

		protected override IItemsLayout Layout { get => ItemsView?.ItemsLayout; }

		protected override void ConnectHandler(ListViewBase platformView)
		{
			base.ConnectHandler(platformView);

			if (Layout is not null)
			{
				_layoutPropertyChanged ??= LayoutPropertyChanged;
				_layoutPropertyChangedProxy = new WeakNotifyPropertyChangedProxy(Layout, _layoutPropertyChanged);
			}
			else
			{
				_layoutPropertyChangedProxy?.Unsubscribe();
				_layoutPropertyChangedProxy = null;
			}
		}

		protected override void DisconnectHandler(ListViewBase platformView)
		{
			base.DisconnectHandler(platformView);

			_layoutPropertyChangedProxy?.Unsubscribe();
			_layoutPropertyChangedProxy = null;
		}

		void LayoutPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == GridItemsLayout.SpanProperty.PropertyName)
				UpdateItemsLayoutSpan();
			else if (e.PropertyName == GridItemsLayout.HorizontalItemSpacingProperty.PropertyName || e.PropertyName == GridItemsLayout.VerticalItemSpacingProperty.PropertyName)
				UpdateItemsLayoutItemSpacing();
			else if (e.PropertyName == LinearItemsLayout.ItemSpacingProperty.PropertyName)
				UpdateItemsLayoutItemSpacing();
		}

		public static void MapHeaderTemplate(StructuredItemsViewHandler<TItemsView> handler, StructuredItemsView itemsView)
		{
			handler.UpdateHeader();
		}

		public static void MapFooterTemplate(StructuredItemsViewHandler<TItemsView> handler, StructuredItemsView itemsView)
		{
			handler.UpdateFooter();
		}

		public static void MapItemsLayout(StructuredItemsViewHandler<TItemsView> handler, StructuredItemsView itemsView)
		{
			handler.UpdateItemsLayout();
		}

		public static void MapItemSizingStrategy(StructuredItemsViewHandler<TItemsView> handler, StructuredItemsView itemsView)
		{

		}

		protected override ListViewBase SelectListViewBase()
		{
			_listViewItemStyle = GetDefaultStyle(ListViewItemStyleKey);
			_gridViewItemStyle = GetDefaultStyle(GridViewItemStyleKey);

			switch (VirtualView.ItemsLayout)
			{
				case GridItemsLayout gridItemsLayout:
					return CreateGridView(gridItemsLayout);
				case LinearItemsLayout listItemsLayout when listItemsLayout.Orientation == ItemsLayoutOrientation.Vertical:
					return CreateVerticalListView(listItemsLayout);
				case LinearItemsLayout listItemsLayout when listItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal:
					return CreateHorizontalListView(listItemsLayout);
			}

			throw new NotImplementedException("The layout is not implemented");
		}

		protected virtual void UpdateHeader()
		{
			if (ListViewBase == null)
			{
				return;
			}

			if (_currentHeader != null)
			{
				Element.RemoveLogicalChild(_currentHeader);
				_currentHeader.Cleanup();
				_currentHeader = null;
			}

			var header = ItemsView.Header ?? ItemsView.HeaderTemplate;

			switch (header)
			{
				case null:
					ListViewBase.Header = null;
					break;

				case string text:
					ListViewBase.HeaderTemplate = null;
					ListViewBase.Header = new TextBlock { Text = text };
					break;

				case View view:
					ListViewBase.HeaderTemplate = ViewTemplate;
					_currentHeader = view;
					Element.AddLogicalChild(_currentHeader);
					ListViewBase.Header = view;
					break;

				default:
					var headerTemplate = ItemsView.HeaderTemplate;
					if (headerTemplate != null)
					{
						ListViewBase.HeaderTemplate = ItemsViewTemplate;
						ListViewBase.Header = new ItemTemplateContext(headerTemplate, header, Element, mauiContext: MauiContext);
					}
					else
					{
						ListViewBase.HeaderTemplate = null;
						ListViewBase.Header = null;
					}
					break;
			}
		}

		protected virtual void UpdateFooter()
		{
			if (ListViewBase == null)
			{
				return;
			}

			if (_currentFooter != null)
			{
				Element.RemoveLogicalChild(_currentFooter);
				_currentFooter.Cleanup();
				_currentFooter = null;
			}

			var footer = ItemsView.Footer ?? ItemsView.FooterTemplate;

			switch (footer)
			{
				case null:
					ListViewBase.Footer = null;
					break;

				case string text:
					ListViewBase.FooterTemplate = null;
					ListViewBase.Footer = new TextBlock { Text = text };
					break;

				case View view:
					ListViewBase.FooterTemplate = ViewTemplate;
					_currentFooter = view;
					Element.AddLogicalChild(_currentFooter);
					ListViewBase.Footer = view;
					break;

				default:
					var footerTemplate = ItemsView.FooterTemplate;
					if (footerTemplate != null)
					{
						ListViewBase.FooterTemplate = ItemsViewTemplate;
						ListViewBase.Footer = new ItemTemplateContext(footerTemplate, footer, Element, mauiContext: MauiContext);
					}
					else
					{
						ListViewBase.FooterTemplate = null;
						ListViewBase.Footer = null;
					}
					break;
			}
		}

		static ListViewBase CreateGridView(GridItemsLayout gridItemsLayout)
		{
			var gridView = new FormsGridView
			{
				Orientation = gridItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal
					? Orientation.Horizontal
					: Orientation.Vertical,

				Span = gridItemsLayout.Span,
				HorizontalItemSpacing = gridItemsLayout.HorizontalItemSpacing,
				VerticalItemSpacing = gridItemsLayout.VerticalItemSpacing,
				ItemContainerStyle = GetItemContainerStyle()
			};

			if (gridView.Orientation == Orientation.Horizontal)
			{
				ScrollViewer.SetVerticalScrollMode(gridView, WScrollMode.Disabled);
				ScrollViewer.SetHorizontalScrollMode(gridView, WScrollMode.Enabled);
			}

			return gridView;
		}

		static ListViewBase CreateVerticalListView(LinearItemsLayout listItemsLayout)
		{
			return new FormsListView()
			{
				ItemSpacing = listItemsLayout?.ItemSpacing ?? 0,
				IsHorizontalOrientation = false,
				ItemContainerStyle = GetVerticalItemContainerStyle()
			};
		}

		static ListViewBase CreateHorizontalListView(LinearItemsLayout listItemsLayout)
		{
			var horizontalListView = new FormsListView()
			{
				ItemsPanel = (ItemsPanelTemplate)WASDKApp.Current.Resources["HorizontalListItemsPanel"],
				ItemSpacing = listItemsLayout?.ItemSpacing ?? 0,
				IsHorizontalOrientation = true,
				ItemContainerStyle = GetHorizontalItemContainerStyle()
			};
			ScrollViewer.SetVerticalScrollBarVisibility(horizontalListView, Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Hidden);
			ScrollViewer.SetVerticalScrollMode(horizontalListView, WScrollMode.Disabled);
			ScrollViewer.SetHorizontalScrollMode(horizontalListView, WScrollMode.Auto);
			ScrollViewer.SetHorizontalScrollBarVisibility(horizontalListView, Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto);

			return horizontalListView;
		}

		// Item spacing is no longer baked into these styles as a Margin/Padding value; it's applied per-container
		// (halved, with outer edges trimmed) in FormsGridView/FormsListView.PrepareContainerForItemOverride so that
		// adjacent items are separated by the exact configured spacing instead of double that amount.
		static WStyle GetItemContainerStyle()
		{
			var style = new WStyle(typeof(GridViewItem));

			if (_gridViewItemStyle is not null)
			{
				style.BasedOn = _gridViewItemStyle;
			}

			style.Setters.Add(new WSetter(Control.PaddingProperty, WinUIHelpers.CreateThickness(0)));
			style.Setters.Add(new WSetter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));

			return style;
		}

		static WStyle GetDefaultStyle(string resourceKey)
		{
			return Microsoft.UI.Xaml.Application.Current.Resources[resourceKey] as WStyle;
		}

		static WStyle GetVerticalItemContainerStyle()
		{
			var style = new WStyle(typeof(ListViewItem));

			if (_listViewItemStyle is not null)
			{
				style.BasedOn = _listViewItemStyle;
			}

			style.Setters.Add(new WSetter(FrameworkElement.MinHeightProperty, 0));
			style.Setters.Add(new WSetter(Control.PaddingProperty, WinUIHelpers.CreateThickness(0)));
			style.Setters.Add(new WSetter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));

			return style;
		}

		static WStyle GetHorizontalItemContainerStyle()
		{
			var style = new WStyle(typeof(ListViewItem));

			if (_listViewItemStyle is not null)
			{
				style.BasedOn = _listViewItemStyle;
			}

			style.Setters.Add(new WSetter(FrameworkElement.MinWidthProperty, 0));
			style.Setters.Add(new WSetter(Control.PaddingProperty, WinUIHelpers.CreateThickness(0)));
			style.Setters.Add(new WSetter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Stretch));

			return style;
		}

		void UpdateItemsLayoutSpan()
		{
			if (ListViewBase is FormsGridView formsGridView)
			{
				formsGridView.Span = ((GridItemsLayout)Layout).Span;
				formsGridView.RefreshItemMargins();
			}
		}

		void UpdateItemsLayoutItemSpacing()
		{
			if (ListViewBase is FormsGridView formsGridView && Layout is GridItemsLayout gridLayout)
			{
				formsGridView.HorizontalItemSpacing = gridLayout.HorizontalItemSpacing;
				formsGridView.VerticalItemSpacing = gridLayout.VerticalItemSpacing;
				formsGridView.RefreshItemMargins();
			}

			if (Layout is LinearItemsLayout linearItemsLayout)
			{
				switch (ListViewBase)
				{
					case FormsListView formsListView:
						formsListView.ItemSpacing = linearItemsLayout.ItemSpacing;
						formsListView.IsHorizontalOrientation = linearItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal;
						formsListView.RefreshItemMargins();
						break;
					case WListView listView:
						listView.ItemContainerStyle = GetHorizontalItemContainerStyle();
						break;
				}
			}
		}
	}
}