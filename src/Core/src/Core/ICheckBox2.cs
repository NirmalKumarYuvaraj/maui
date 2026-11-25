namespace Microsoft.Maui
{
	/// <summary>
	/// Represents a <see cref="ICheckBox"/> that can also represent an indeterminate state.
	/// </summary>
	/// <remarks>
	/// <see cref="ICheckBox.IsChecked"/> remains a Boolean projection of <see cref="CheckState"/>:
	/// it is <see langword="true"/> only for <see cref="CheckBoxState.Checked"/>.
	/// </remarks>
	public interface ICheckBox2 : ICheckBox
	{
		/// <summary>
		/// Gets or sets the complete state of the checkbox.
		/// </summary>
		CheckBoxState CheckState { get; set; }

		/// <summary>
		/// Gets a value that indicates whether the checkbox supports the indeterminate state,
		/// and whether user interaction cycles through all three states.
		/// </summary>
		bool IsThreeState { get; }
	}
}
