using Microsoft.Maui.Graphics;
using Tizen.UIExtensions.NUI.GraphicsView;

namespace Microsoft.Maui.Platform
{
	public static class CheckBoxExtensions
	{
		public static void UpdateIsChecked(this CheckBox platformCheck, ICheckBox check)
		{
			platformCheck.IsChecked = check.IsChecked;
		}

		/// <summary>
		/// Updates the platform checkbox. Tizen does not natively support indeterminate;
		/// <see cref="CheckState.Indeterminate"/> is rendered as unchecked.
		/// </summary>
		public static void UpdateCheckState(this CheckBox platformCheck, ICheckBox check)
		{
			// Tizen checkbox is two-state only; indeterminate falls back to unchecked.
			platformCheck.IsChecked = check.CheckState == CheckState.Checked;
		}

		public static void UpdateForeground(this CheckBox platformCheck, ICheckBox check)
		{
			// For the moment, we're only supporting solid color Paint
			if (check.Foreground is SolidPaint solid)
			{
				platformCheck.Color = solid.Color.ToPlatform();
			}
		}
	}
}