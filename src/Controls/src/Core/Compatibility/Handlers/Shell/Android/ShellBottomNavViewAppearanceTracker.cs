#nullable disable
using System;
using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Util;
using AndroidX.AppCompat.Content.Res;
using AndroidX.Core.View;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Shape;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Graphics;
using AColor = Android.Graphics.Color;
using R = Android.Resource;

namespace Microsoft.Maui.Controls.Platform.Compatibility
{
	public class ShellBottomNavViewAppearanceTracker : IShellBottomNavViewAppearanceTracker
	{
		IShellContext _shellContext;
		ShellItem _shellItem;
		static ColorStateList _defaultListLight;
		static ColorStateList _defaultListDark;

		bool _disposed;
		ColorStateList _itemTextColor;
		ColorStateList _itemIconTint;

		public ShellBottomNavViewAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
		{
			_shellItem = shellItem;
			_shellContext = shellContext;
		}

		static ColorStateList GetDefaultTabColorList(Context context) =>
			ShellRenderer.IsDarkTheme ?
			_defaultListDark ??= MakeDefaultColorStateList(context)
			: _defaultListLight ??= MakeDefaultColorStateList(context);

		public virtual void ResetAppearance(BottomNavigationView bottomView)
		{
			// When resetting, restore default theme-based colors
			// The default list uses theme attributes, allowing XML layout theme defaults to apply
			bottomView.ItemIconTintList = GetDefaultTabColorList(_shellContext.AndroidContext);
			bottomView.ItemTextColor = GetDefaultTabColorList(_shellContext.AndroidContext);
			// Don't set background - let XML layout theme background apply
			// This is the key difference from SetAppearance - we skip SetBackgroundColor entirely
		}

		public virtual void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
		{
			IShellAppearanceElement controller = appearance;
			var backgroundColor = controller.EffectiveTabBarBackgroundColor;
			var foregroundColor = controller.EffectiveTabBarForegroundColor;
			var disabledColor = controller.EffectiveTabBarDisabledColor;
			var unselectedColor = controller.EffectiveTabBarUnselectedColor;
			var titleColor = controller.EffectiveTabBarTitleColor;

			// Only set explicit colors if provided, otherwise use theme defaults
			if (titleColor is not null || foregroundColor is not null || disabledColor is not null || unselectedColor is not null)
			{
				_itemTextColor = MakeColorStateList(
					titleColor ?? foregroundColor,
					disabledColor,
					unselectedColor);

				_itemIconTint = MakeColorStateList(
					foregroundColor ?? titleColor,
					disabledColor,
					unselectedColor);

				if (_itemTextColor is not null)
					bottomView.ItemTextColor = _itemTextColor;
				if (_itemIconTint is not null)
					bottomView.ItemIconTintList = _itemIconTint;
			}

			// Only set background color if explicitly provided
			if (backgroundColor is not null)
				SetBackgroundColor(bottomView, backgroundColor);
		}

		protected virtual void SetBackgroundColor(BottomNavigationView bottomView, Color color)
		{
#pragma warning disable XAOBS001 // Obsolete
			var menuView = bottomView.GetChildAt(0) as BottomNavigationMenuView;
#pragma warning restore XAOBS001 // Obsolete
			var oldBackground = bottomView.Background;
			var colorDrawable = oldBackground as ColorDrawable;
			var colorChangeRevealDrawable = oldBackground as ColorChangeRevealDrawable;
			AColor lastColor = colorChangeRevealDrawable?.EndColor ?? colorDrawable?.Color ?? Colors.Transparent.ToPlatform();
			
			// Color should always be provided by caller; if null, don't change background
			// (lets XML layout theme handle it)
			if (color == null)
				return;
				
			AColor newColor = color.ToPlatform();

			if (menuView == null)
			{
				if (colorDrawable != null && lastColor == newColor)
					return;

				if (lastColor != newColor || colorDrawable == null)
				{
					// taken from android source code
					var backgroundColor = new MaterialShapeDrawable();
					backgroundColor.FillColor = ColorStateList.ValueOf(newColor);
					backgroundColor.InitializeElevationOverlay(bottomView.Context);

#pragma warning disable CS0618 // Obsolete
					ViewCompat.SetBackground(bottomView, backgroundColor);
#pragma warning restore CS0618 // Obsolete
				}
			}
			else
			{
				if (colorChangeRevealDrawable != null && lastColor == newColor)
					return;

				var index = ((IShellItemController)_shellItem).GetItems().IndexOf(_shellItem.CurrentItem);
				var menu = bottomView.Menu;
				index = Math.Min(index, menu.Size() - 1);

				var child = menuView.GetChildAt(index);
				if (child == null)
					return;

				var touchPoint = new Point(child.Left + (child.Right - child.Left) / 2, child.Top + (child.Bottom - child.Top) / 2);

#pragma warning disable CS0618 // Obsolete
				ViewCompat.SetBackground(bottomView, new ColorChangeRevealDrawable(lastColor, newColor, touchPoint));
#pragma warning restore CS0618 // Obsolete
			}
		}

		static ColorStateList MakeDefaultColorStateList(Context context)
		{
			TypedValue mTypedValue = new TypedValue();
			if (context.Theme?.ResolveAttribute(R.Attribute.TextColorSecondary, mTypedValue, true) == false)
				return null;

			var baseCSL = AppCompatResources.GetColorStateList(context, mTypedValue.ResourceId);
			
			// Resolve colorPrimary from theme instead of hardcoded values
			TypedValue colorPrimaryValue = new TypedValue();
			AColor colorPrimary;
			if (context.Theme?.ResolveAttribute(R.Attribute.ColorPrimary, colorPrimaryValue, true) == true)
			{
				colorPrimary = new AColor(colorPrimaryValue.Data);
			}
			else
			{
				// Fallback to white/gray if theme attribute not found
				colorPrimary = (ShellRenderer.IsDarkTheme) ? AColor.White : AColor.Gray;
			}
			
			int defaultColor = baseCSL.DefaultColor;
			var disabledcolor = baseCSL.GetColorForState(new[] { -R.Attribute.StateEnabled }, AColor.Gray);

			return MakeColorStateList(colorPrimary, disabledcolor, defaultColor);
		}

		ColorStateList MakeColorStateList(Color titleColor, Color disabledColor, Color unselectedColor)
		{
			var defaultList = GetDefaultTabColorList(_shellContext.AndroidContext);

			var disabledInt = disabledColor == null ?
				defaultList.GetColorForState(new[] { -R.Attribute.StateEnabled }, AColor.Gray) :
				disabledColor.ToPlatform().ToArgb();

			var checkedInt = titleColor == null ?
				defaultList.GetColorForState(new[] { R.Attribute.StateChecked }, AColor.Black) :
				titleColor.ToPlatform().ToArgb();

			var defaultColor = unselectedColor == null ?
				defaultList.DefaultColor :
				unselectedColor.ToPlatform().ToArgb();

			return MakeColorStateList(checkedInt, disabledInt, defaultColor);
		}

		static ColorStateList MakeColorStateList(int titleColorInt, int disabledColorInt, int defaultColor)
		{
			return ColorStateListExtensions.CreateSwitch(disabledColorInt, titleColorInt, defaultColor);
		}

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;

			if (disposing)
			{
				_itemTextColor?.Dispose();
				_itemIconTint?.Dispose();

				_itemIconTint = null;
				_shellItem = null;
				_shellContext = null;
				_itemTextColor = null;
			}
		}

		#endregion IDisposable
	}
}
