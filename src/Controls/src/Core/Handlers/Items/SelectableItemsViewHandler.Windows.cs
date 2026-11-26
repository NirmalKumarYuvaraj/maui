#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using WASDKListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using WASDKSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs;

namespace Microsoft.Maui.Controls.Handlers.Items
{
	public partial class SelectableItemsViewHandler<TItemsView> : StructuredItemsViewHandler<TItemsView> where TItemsView : SelectableItemsView
	{
		bool _ignorePlatformSelectionChange;
		bool _ignoreVirtualSelectionChange;
		long? _selectionModeChangedToken;
		SelectableItemsView _selectionModeView;

		protected override void ConnectHandler(ListViewBase platformView)
		{
			base.ConnectHandler(platformView);

			if (ItemsView != null)
			{
				ItemsView.SelectionChanged += VirtualSelectionChanged;
				ObserveSelectionMode(ItemsView);
			}

			var newListViewBase = ListViewBase;

			if (newListViewBase != null)
			{
				newListViewBase.SelectionMode = ToPlatformSelectionMode(ItemsView.SelectionMode);
				_selectionModeChangedToken = newListViewBase.RegisterPropertyChangedCallback(
					ListViewBase.SelectionModeProperty, OnPlatformSelectionModeChanged);
				newListViewBase.SelectionChanged += PlatformSelectionChanged;
			}

			UpdatePlatformSelection();
		}

		protected override void DisconnectHandler(ListViewBase platformView)
		{
			var oldListViewBase = platformView;

			if (oldListViewBase != null)
			{
				oldListViewBase.SelectionChanged -= PlatformSelectionChanged;
				if (_selectionModeChangedToken is long token)
				{
					oldListViewBase.UnregisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, token);
					_selectionModeChangedToken = null;
				}
				oldListViewBase.ClearValue(ListViewBase.SelectionModeProperty);
			}

			if (ItemsView != null)
			{
				ItemsView.SelectionChanged -= VirtualSelectionChanged;
			}

			if (_selectionModeView != null)
			{
				_selectionModeView.PropertyChanged -= OnVirtualViewPropertyChanged;
				_selectionModeView = null;
			}

			base.DisconnectHandler(platformView);
		}

		public static void MapSelectedItem(SelectableItemsViewHandler<TItemsView> handler, SelectableItemsView itemsView)
		{
		}

		public static void MapSelectedItems(SelectableItemsViewHandler<TItemsView> handler, SelectableItemsView itemsView)
		{
		}

		public static void MapSelectionMode(SelectableItemsViewHandler<TItemsView> handler, SelectableItemsView itemsView)
		{
			handler.ObserveSelectionMode(itemsView);
			if (((IElementHandler)handler).PlatformView is ListViewBase platformView)
			{
				platformView.SelectionMode = ToPlatformSelectionMode(itemsView.SelectionMode);
				handler.UpdatePlatformSelection();
			}
		}

		static WASDKListViewSelectionMode ToPlatformSelectionMode(SelectionMode selectionMode) =>
			selectionMode switch
			{
				SelectionMode.Single => WASDKListViewSelectionMode.Single,
				SelectionMode.Multiple => WASDKListViewSelectionMode.Multiple,
				_ => WASDKListViewSelectionMode.None
			};

		void ObserveSelectionMode(SelectableItemsView itemsView)
		{
			// SelectionChanged can fire before the property mapper runs.
			if (ReferenceEquals(_selectionModeView, itemsView))
				return;

			if (_selectionModeView != null)
				_selectionModeView.PropertyChanged -= OnVirtualViewPropertyChanged;

			_selectionModeView = itemsView;
			_selectionModeView.PropertyChanged += OnVirtualViewPropertyChanged;
		}

		void OnVirtualViewPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(SelectableItemsView.SelectionMode) &&
				ReferenceEquals(sender, ((IElementHandler)this).VirtualView) &&
				((IElementHandler)this).PlatformView is ListViewBase platformView &&
				sender is SelectableItemsView itemsView)
			{
				platformView.SelectionMode = ToPlatformSelectionMode(itemsView.SelectionMode);
			}
		}

		void OnPlatformSelectionModeChanged(Microsoft.UI.Xaml.DependencyObject sender, Microsoft.UI.Xaml.DependencyProperty property)
		{
			if (sender is not ListViewBase platformView ||
				!ReferenceEquals(((IElementHandler)this).PlatformView, platformView) ||
				((IElementHandler)this).VirtualView is not SelectableItemsView itemsView)
				return;

			var selectionMode = platformView.SelectionMode switch
			{
				WASDKListViewSelectionMode.Single => SelectionMode.Single,
				WASDKListViewSelectionMode.Multiple => SelectionMode.Multiple,
				_ => SelectionMode.None
			};

			if (itemsView.SelectionMode != selectionMode)
				itemsView.SelectionMode = selectionMode;
		}

		void UpdatePlatformSelection()
		{
			_ignorePlatformSelectionChange = true;

			switch (ListViewBase.SelectionMode)
			{
				case WASDKListViewSelectionMode.None:
					break;
				case WASDKListViewSelectionMode.Single:
					if (ItemsView != null)
					{
						if (ItemsView.SelectedItem == null)
						{
							ListViewBase.SelectedItem = null;
						}
						else
						{
							ListViewBase.SelectedItem =
								ListViewBase.Items.FirstOrDefault(item =>
								{
									if (item is ItemTemplateContext itemPair)
									{
										return object.Equals(itemPair.Item, ItemsView.SelectedItem);
									}
									else
									{
										return object.Equals(item, ItemsView.SelectedItem);
									}
								});
						}
					}

					break;
				case WASDKListViewSelectionMode.Multiple:
					ListViewBase.SelectedItems.Clear();
					foreach (var nativeItem in ListViewBase.Items)
					{
						if (nativeItem is ItemTemplateContext itemPair && ItemsView.SelectedItems.Contains(itemPair.Item))
						{
							ListViewBase.SelectedItems.Add(nativeItem);
						}
						else if (ItemsView.SelectedItems.Contains(nativeItem))
						{
							ListViewBase.SelectedItems.Add(nativeItem);
						}
					}
					break;
				case WASDKListViewSelectionMode.Extended:
					break;
				default:
					break;
			}

			UpdateItemContentControlSelection(ListViewBase);
			_ignorePlatformSelectionChange = false;
		}

		void VirtualSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// When the selection changes within the SelectionChanged event, the new selection isn't immediately reflected in the view.
			// After the virtual selection is correctly updated, the flag is reset to enable future updates
			if (_ignoreVirtualSelectionChange)
			{
				_ignoreVirtualSelectionChange = false;
				return;
			}
			UpdatePlatformSelection();
		}

		void PlatformSelectionChanged(object sender, WASDKSelectionChangedEventArgs args)
		{
			if (sender is not ListViewBase platformView)
			{
				return;
			}

			UpdateVirtualSelection(platformView);
		}

		void UpdateVirtualSelection(ListViewBase platformView)
		{
			if (_ignorePlatformSelectionChange ||
				!ReferenceEquals(((IElementHandler)this).PlatformView, platformView) ||
				((IElementHandler)this).VirtualView is null)
			{
				return;
			}

			switch (platformView.SelectionMode)
			{
				case WASDKListViewSelectionMode.None:
					break;
				case WASDKListViewSelectionMode.Single:
					UpdateVirtualSingleSelection(platformView);
					break;
				case WASDKListViewSelectionMode.Multiple:
					UpdateVirtualMultipleSelection(platformView);
					break;
				default:
					break;
			}

			if (!ReferenceEquals(((IElementHandler)this).PlatformView, platformView))
			{
				return;
			}

			UpdateItemContentControlSelection(platformView);
		}

		void UpdateVirtualSingleSelection(ListViewBase platformView)
		{
			var selectedItem = platformView.SelectedItem is ItemTemplateContext itemPair
				? itemPair.Item
				: platformView.SelectedItem;

			if (ItemsView != null)
			{
				_ignoreVirtualSelectionChange = true;
				ItemsView.SelectedItem = selectedItem;

				_ignoreVirtualSelectionChange = false;
			}
		}

		void UpdateVirtualMultipleSelection(ListViewBase platformView)
		{
			ItemsView.SelectionChanged -= VirtualSelectionChanged;

			var selection = new List<object>();
			for (int n = 0; n < platformView.SelectedItems.Count; n++)
			{
				var item = platformView.SelectedItems[n];
				selection.Add(item is ItemTemplateContext itc ? itc.Item : item);
			}

			ItemsView.UpdateSelectedItems(selection);

			ItemsView.SelectionChanged += VirtualSelectionChanged;
		}

		void UpdateItemContentControlSelection(ListViewBase platformView)
		{
			var formsItemContentControls = platformView.GetChildren<ItemContentControl>();

			foreach (var formsItemContentControl in formsItemContentControls)
			{
				bool isSelected = object.Equals(ItemsView.SelectedItem, formsItemContentControl.FormsDataContext) || ItemsView.SelectedItems.Contains(formsItemContentControl.FormsDataContext);
				formsItemContentControl.UpdateIsSelected(isSelected);
			}
		}

		protected override void UpdateItemsLayout()
		{
			_ignorePlatformSelectionChange = true;

			base.UpdateItemsLayout();
			_ignorePlatformSelectionChange = false;
		}

		protected override void UpdateItemsSource()
		{
			_ignorePlatformSelectionChange = true;

			base.UpdateItemsSource();
			UpdatePlatformSelection();

			_ignorePlatformSelectionChange = false;
		}
	}
}
