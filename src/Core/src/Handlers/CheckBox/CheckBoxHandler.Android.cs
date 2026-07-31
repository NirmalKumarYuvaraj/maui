using System;
using Android.Content.Res;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.CheckBox;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Handlers
{
	public partial class CheckBoxHandler : ViewHandler<ICheckBox, AppCompatCheckBox>
	{
		ColorStateList? _defaultButtonTintList;

		protected override AppCompatCheckBox CreatePlatformView()
		{
			var platformCheckBox = new MaterialCheckBox(MauiMaterialContextThemeWrapper.Create(Context))
			{
				SoundEffectsEnabled = false
			};

			platformCheckBox.SetClipToOutline(true);
			return platformCheckBox;
		}

		protected override void ConnectHandler(AppCompatCheckBox platformView)
		{
			_defaultButtonTintList = platformView.ButtonTintList;

			base.ConnectHandler(platformView);
			Material3ThemeManager.ThemeChanged += OnMaterial3ThemeChanged;

			platformView.CheckedChange += OnCheckedChange;
		}

		protected override void DisconnectHandler(AppCompatCheckBox platformView)
		{
			platformView.CheckedChange -= OnCheckedChange;
			Material3ThemeManager.ThemeChanged -= OnMaterial3ThemeChanged;
			_defaultButtonTintList = null;

			base.DisconnectHandler(platformView);
		}

		// This is an Android-specific mapping
		public static partial void MapBackground(ICheckBoxHandler handler, ICheckBox check)
		{
			handler.PlatformView?.UpdateBackground(check);
		}

		public static partial void MapIsChecked(ICheckBoxHandler handler, ICheckBox check)
		{
			handler.PlatformView?.UpdateIsChecked(check);
		}

		public static partial void MapForeground(ICheckBoxHandler handler, ICheckBox check)
		{
			if (handler is CheckBoxHandler platformHandler)
			{
				if (Material3Configuration.Enabled && check.Foreground.IsNullOrEmpty())
					platformHandler._defaultButtonTintList = Material3ThemeDefaults.GetCheckBoxTint(platformHandler.PlatformView.Context);

				handler.PlatformView?.UpdateForeground(check, platformHandler._defaultButtonTintList);
			}
			else
				handler.PlatformView?.UpdateForeground(check);
		}

		void OnMaterial3ThemeChanged(object? sender, EventArgs e)
		{
			if (VirtualView?.Foreground.IsNullOrEmpty() == true)
				UpdateValue(nameof(ICheckBox.Foreground));
		}

		void OnCheckedChange(object? sender, CompoundButton.CheckedChangeEventArgs e)
		{
			if (VirtualView != null)
				VirtualView.IsChecked = e.IsChecked;
		}
	}
}