namespace Microsoft.Maui
{
	/// <summary>
	/// Defines the visual check state of a <see cref="ICheckBox"/>.
	/// </summary>
	public enum CheckState
	{
		/// <summary>The checkbox is not checked.</summary>
		Unchecked = 0,

		/// <summary>The checkbox is in an indeterminate state (neither fully checked nor unchecked).</summary>
		Indeterminate = 1,

		/// <summary>The checkbox is checked.</summary>
		Checked = 2,
	}
}
