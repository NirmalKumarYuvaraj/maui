using Microsoft.Maui.Graphics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.Maui.Platform
{
	public static class CheckBoxExtensions
	{
		public static void UpdateIsChecked(this CheckBox platformCheckBox, ICheckBox check)
		{
			platformCheckBox.IsChecked = check.IsChecked;
		}

		/// <summary>
		/// Updates the platform checkbox to reflect <see cref="ICheckBox.CheckState"/>,
		/// using WinUI's native nullable <c>IsChecked</c> (<see langword="null"/> = indeterminate).
		/// </summary>
		public static void UpdateCheckState(this CheckBox platformCheckBox, ICheckBox check)
		{
			platformCheckBox.IsChecked = check.CheckState switch
			{
				CheckState.Checked => true,
				CheckState.Indeterminate => null,
				_ => false,
			};
		}

		/// <summary>
		/// Enables or disables WinUI's native three-state cycling behaviour
		/// (<see cref="CheckBox.IsThreeState"/>).
		/// </summary>
		public static void UpdateIsThreeState(this CheckBox platformCheckBox, ICheckBox check)
		{
			platformCheckBox.IsThreeState = check.IsThreeState;
		}

		public static void UpdateForeground(this CheckBox platformCheckBox, ICheckBox check)
		{
			var tintBrush = check.Foreground?.ToPlatform();

			if (tintBrush == null)
			{
				platformCheckBox.Resources.RemoveKeys(_tintColorResourceKeys);
				platformCheckBox.Foreground = null;
			}
			else
			{
				platformCheckBox.Resources.SetValueForAllKey(_tintColorResourceKeys, tintBrush);
				platformCheckBox.Foreground = tintBrush;
			}

			platformCheckBox.RefreshThemeResources();
		}

		// ResourceKeys controlling the stroke and the checked fill color of the CheckBox.
		// https://docs.microsoft.com/en-us/windows/winui/api/microsoft.ui.xaml.controls.checkbox?view=winui-3.0#control-style-and-template
		static readonly string[] _tintColorResourceKeys =
		{
			"CheckBoxCheckBackgroundFillChecked",
			"CheckBoxCheckBackgroundFillCheckedPointerOver",
			"CheckBoxCheckBackgroundFillCheckedPressed",
			"CheckBoxCheckBackgroundFillCheckedDisabled",
			"CheckBoxCheckBackgroundStrokeUnchecked",
			"CheckBoxCheckBackgroundStrokeUncheckedPointerOver",
			"CheckBoxCheckBackgroundStrokeUncheckedPressed",
			"CheckBoxCheckBackgroundStrokeUncheckedDisabled",
			"CheckBoxCheckBackgroundStrokeChecked",
			"CheckBoxCheckBackgroundStrokeCheckedPointerOver",
			"CheckBoxCheckBackgroundStrokeCheckedPressed",
			"CheckBoxCheckBackgroundStrokeCheckedDisabled",
			"CheckBoxCheckBackgroundStrokeIndeterminate",
			"CheckBoxCheckBackgroundStrokeIndeterminatePointerOver",
			"CheckBoxCheckBackgroundStrokeIndeterminatePressed",
			"CheckBoxCheckBackgroundStrokeIndeterminateDisabled",
		};
	}
}