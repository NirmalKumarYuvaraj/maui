using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Platform
{
	public static class CheckBoxExtensions
	{
		public static void UpdateIsChecked(this MauiCheckBox platformCheckBox, ICheckBox check)
		{
			platformCheckBox.IsChecked = check.IsChecked;
		}

		/// <summary>
		/// Updates the platform checkbox to reflect <see cref="ICheckBox.CheckState"/>,
		/// including the indeterminate state.
		/// </summary>
		public static void UpdateCheckState(this MauiCheckBox platformCheckBox, ICheckBox check)
		{
			platformCheckBox.CheckState = check.CheckState;
		}

		/// <summary>
		/// Updates whether the platform checkbox cycles through three states on tap.
		/// </summary>
		public static void UpdateIsThreeState(this MauiCheckBox platformCheckBox, ICheckBox check)
		{
			platformCheckBox.IsThreeState = check.IsThreeState;
		}

		public static void UpdateForeground(this MauiCheckBox platformCheckBox, ICheckBox check)
		{
			// For the moment, we're only supporting solid color Paint for the iOS Checkbox
			if (check.Foreground is SolidPaint solid)
			{
				platformCheckBox.CheckBoxTintColor = solid.Color;
			}
			else if (check.Foreground is null)
			{
				// Color was cleared; reset to null so the view inherits the default tint color
				platformCheckBox.CheckBoxTintColor = null;
			}
		}
	}
}