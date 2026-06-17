using Microsoft.Maui.Graphics;

namespace Microsoft.Maui
{
	/// <summary>
	/// Represents a View which allows the user to select a binary or three-state choice.
	/// </summary>
	public interface ICheckBox : IView
	{
		/// <summary>
		/// Gets or sets a value that indicates whether the CheckBox is checked.
		/// When <see cref="IsThreeState"/> is <see langword="false"/>, only <see cref="CheckState.Checked"/>
		/// and <see cref="CheckState.Unchecked"/> are used and this maps to <c>true</c>/<c>false</c>.
		/// </summary>
		bool IsChecked { get; set; }

		/// <summary>
		/// Gets the current check state of the CheckBox (<see cref="CheckState.Unchecked"/>,
		/// <see cref="CheckState.Indeterminate"/>, or <see cref="CheckState.Checked"/>).
		/// </summary>
		CheckState CheckState { get; set; }

		/// <summary>
		/// Gets a value indicating whether the CheckBox supports three states
		/// (unchecked, indeterminate, checked). When <see langword="false"/>, only
		/// checked and unchecked are available.
		/// </summary>
		bool IsThreeState { get; }

		/// <summary>
		/// Gets the CheckBox Foreground Paint.
		/// </summary>
		Paint? Foreground { get; }
	}
}