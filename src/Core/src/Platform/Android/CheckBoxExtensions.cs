using System.Runtime.CompilerServices;
using Android.Content.Res;
using Android.Graphics;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Widget;
using Microsoft.Maui.Graphics;
using AColor = Android.Graphics.Color;

namespace Microsoft.Maui.Platform
{
	public static class CheckBoxExtensions
	{
		static readonly ConditionalWeakTable<AppCompatCheckBox, ColorStateList> _fallbackButtonTintCache = new();

		public static void UpdateBackground(this AppCompatCheckBox platformCheckBox, ICheckBox check)
		{
			var paint = check.Background;

			if (paint.IsNullOrEmpty())
				platformCheckBox.SetBackgroundColor(AColor.Transparent);
			else
				platformCheckBox.UpdateBackground((IView)check);
		}

		public static void UpdateIsChecked(this AppCompatCheckBox platformCheckBox, ICheckBox check)
		{
			platformCheckBox.Checked = check.IsChecked;
		}

		public static void UpdateForeground(this AppCompatCheckBox platformCheckBox, ICheckBox check)
		{
			if (!_fallbackButtonTintCache.TryGetValue(platformCheckBox, out var defaultButtonTintList))
			{
				defaultButtonTintList = platformCheckBox.ButtonTintList;
				if (defaultButtonTintList is not null)
					_fallbackButtonTintCache.Add(platformCheckBox, defaultButtonTintList);
			}

			platformCheckBox.UpdateForeground(check, defaultButtonTintList);
		}

		internal static void UpdateForeground(this AppCompatCheckBox platformCheckBox, ICheckBox check, ColorStateList? defaultButtonTintList)
		{
			var mode = PorterDuff.Mode.SrcIn;

			CompoundButtonCompat.SetButtonTintList(platformCheckBox, platformCheckBox.GetColorStateList(check, defaultButtonTintList));
			CompoundButtonCompat.SetButtonTintMode(platformCheckBox, mode);
		}

		internal static ColorStateList GetColorStateList(this AppCompatCheckBox platformCheckBox, ICheckBox check, ColorStateList? defaultButtonTintList = null)
		{
			// For the moment, we're only supporting solid color Paint for the Android Checkbox
			if (check.Foreground is SolidPaint solid)
			{
				var color = solid.Color;
				AColor tintColor = color.ToPlatform();
				return ColorStateListExtensions.CreateCheckBox(tintColor);
			}

			if (Material3Configuration.Enabled)
			{
				// Material 3: Use the original theme's buttonTint
				if (defaultButtonTintList is not null)
				{
					return defaultButtonTintList;
				}
			}

			// Material 2: Use accent color
			Graphics.Color accent = platformCheckBox.Context?.GetAccentColor() ?? Graphics.Color.FromArgb("#ff33b5e5");
			AColor tintColor2 = accent.ToPlatform();
			return ColorStateListExtensions.CreateCheckBox(tintColor2);
		}
	}
}
