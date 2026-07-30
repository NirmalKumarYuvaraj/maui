#nullable enable

using Android.Content.Res;
using Android.Graphics.Drawables;
using AndroidX.Core.View;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Tabs;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using AColor = Android.Graphics.Color;
using AToolbar = AndroidX.AppCompat.Widget.Toolbar;
using R = Android.Resource;

namespace Microsoft.Maui.Controls.Handlers
{
	internal sealed class ShellHandlerToolbarAppearanceTracker : IShellToolbarAppearanceTracker
	{
		public void SetAppearance(AToolbar toolbar, IShellToolbarTracker toolbarTracker, ShellAppearance appearance)
		{
			toolbar.UpdateShellAppearance(appearance.ForegroundColor, appearance.BackgroundColor, appearance.TitleColor);
		}

		public void ResetAppearance(AToolbar toolbar, IShellToolbarTracker toolbarTracker)
		{
			toolbar.UpdateShellAppearance(null, null, null);
		}

		public void Dispose()
		{
		}
	}

	internal sealed class ShellHandlerBottomNavViewAppearanceTracker : IShellBottomNavViewAppearanceTracker
	{
		ColorStateList? _defaultItemTextColor;
		ColorStateList? _defaultItemIconTint;
		ColorStateList? _defaultBackgroundTint;
		bool _captured;

		public void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
		{
			CaptureDefaults(bottomView);

			var background = appearance.EffectiveTabBarBackgroundColor;
			var foreground = appearance.EffectiveTabBarForegroundColor;
			var disabled = appearance.EffectiveTabBarDisabledColor;
			var unselected = appearance.EffectiveTabBarUnselectedColor;
			var title = appearance.EffectiveTabBarTitleColor;

			bottomView.ItemTextColor = CreateColorStateList(
				_defaultItemTextColor,
				title ?? foreground,
				disabled,
				unselected);
			bottomView.ItemIconTintList = CreateColorStateList(
				_defaultItemIconTint,
				foreground ?? title,
				disabled,
				unselected);
			ViewCompat.SetBackgroundTintList(bottomView,
				background is null ? _defaultBackgroundTint : ColorStateList.ValueOf(background.ToPlatform()));
		}

		public void ResetAppearance(BottomNavigationView bottomView)
		{
			CaptureDefaults(bottomView);
			bottomView.ItemTextColor = _defaultItemTextColor;
			bottomView.ItemIconTintList = _defaultItemIconTint;
			ViewCompat.SetBackgroundTintList(bottomView, _defaultBackgroundTint);
		}

		void CaptureDefaults(BottomNavigationView bottomView)
		{
			if (_captured)
				return;

			_captured = true;
			_defaultItemTextColor = bottomView.ItemTextColor;
			_defaultItemIconTint = bottomView.ItemIconTintList;
			_defaultBackgroundTint = ViewCompat.GetBackgroundTintList(bottomView);
		}

		internal static ColorStateList? CreateColorStateList(ColorStateList? defaults, Color? selected, Color? disabled, Color? unselected)
		{
			if (selected is null && disabled is null && unselected is null)
				return defaults;

			int selectedColor = selected?.ToPlatform().ToArgb()
				?? defaults?.GetColorForState(new[] { R.Attribute.StateChecked }, new AColor(defaults.DefaultColor))
				?? AColor.Transparent;
			int disabledColor = disabled?.ToPlatform().ToArgb()
				?? defaults?.GetColorForState(new[] { -R.Attribute.StateEnabled }, new AColor(defaults.DefaultColor))
				?? AColor.Transparent;
			int unselectedColor = unselected?.ToPlatform().ToArgb()
				?? defaults?.DefaultColor
				?? AColor.Transparent;

			return ColorStateListExtensions.CreateSwitch(disabledColor, selectedColor, unselectedColor);
		}

		public void Dispose()
		{
		}
	}

	internal sealed class ShellHandlerTabLayoutAppearanceTracker : IShellTabLayoutAppearanceTracker
	{
		ColorStateList? _defaultTextColors;
		ColorStateList? _defaultBackgroundTint;
		Drawable? _defaultSelectedIndicator;
		Drawable.ConstantState? _defaultSelectedIndicatorState;
		bool _captured;

		public void SetAppearance(TabLayout tabLayout, ShellAppearance appearance)
		{
			CaptureDefaults(tabLayout);

			tabLayout.TabTextColors = ShellHandlerBottomNavViewAppearanceTracker.CreateColorStateList(
				_defaultTextColors,
				appearance.TitleColor,
				appearance.DisabledColor,
				appearance.UnselectedColor);
			ViewCompat.SetBackgroundTintList(tabLayout,
				appearance.BackgroundColor is null ? _defaultBackgroundTint : ColorStateList.ValueOf(appearance.BackgroundColor.ToPlatform()));

			if (appearance.ForegroundColor is null)
				RestoreSelectedIndicator(tabLayout);
			else
				tabLayout.SetSelectedTabIndicatorColor(appearance.ForegroundColor.ToPlatform());
		}

		public void ResetAppearance(TabLayout tabLayout)
		{
			CaptureDefaults(tabLayout);
			tabLayout.TabTextColors = _defaultTextColors;
			ViewCompat.SetBackgroundTintList(tabLayout, _defaultBackgroundTint);
			RestoreSelectedIndicator(tabLayout);
		}

		void CaptureDefaults(TabLayout tabLayout)
		{
			if (_captured)
				return;

			_captured = true;
			_defaultTextColors = tabLayout.TabTextColors;
			_defaultBackgroundTint = ViewCompat.GetBackgroundTintList(tabLayout);
			_defaultSelectedIndicator = tabLayout.TabSelectedIndicator;
			_defaultSelectedIndicatorState = _defaultSelectedIndicator?.GetConstantState();
		}

		void RestoreSelectedIndicator(TabLayout tabLayout)
		{
			var indicator = _defaultSelectedIndicatorState?.NewDrawable(tabLayout.Resources)?.Mutate() ?? _defaultSelectedIndicator;
			tabLayout.SetSelectedTabIndicator(indicator);
		}

		public void Dispose()
		{
		}
	}
}
