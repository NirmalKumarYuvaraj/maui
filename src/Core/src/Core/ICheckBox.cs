using Microsoft.Maui.Graphics;

namespace Microsoft.Maui
{
	/// <summary>
	/// Represents a View with checked, unchecked, and optional indeterminate states.
	/// </summary>
	public interface ICheckBox : IView
	{
		/// <summary>
		/// Gets or sets whether the CheckBox is checked, unchecked, or indeterminate.
		/// </summary>
		bool? IsChecked { get; set; }

		/// <summary>
		/// Gets or sets a value that indicates whether the CheckBox supports three states (checked, unchecked, and indeterminate).
		/// </summary>
		bool IsThreeState { get; set; }

		/// <summary>
		/// Gets the CheckBox Foreground Paint.
		/// </summary>
		Paint? Foreground { get; }
	}
}