#nullable disable
using System;

namespace Microsoft.Maui.Controls
{
	/// <summary>Event Args for <see cref="Microsoft.Maui.Controls.CheckBox"/>'s <see cref="Microsoft.Maui.Controls.CheckBox.CheckedChanged"/> event.</summary>
	public class CheckedChangedEventArgs : EventArgs
	{
		/// <summary>Creates a new <see cref="Microsoft.Maui.Controls.CheckedChangedEventArgs"/> specifying whether the <see cref="Microsoft.Maui.Controls.CheckBox"/> is checked.</summary>
		/// <param name="value">Boolean value indicating whether the <see cref="Microsoft.Maui.Controls.CheckBox"/> is checked.</param>
		public CheckedChangedEventArgs(bool value)
		{
			Value = value;
		}

		/// <summary>Boolean value indicating whether the <see cref="Microsoft.Maui.Controls.CheckBox"/> is checked.</summary>
		public bool Value { get; private set; }
	}


	/// <summary>Event args for the <see cref="CheckBox.CheckedChanged"/> event.</summary>
	public class CheckBoxCheckedChangedEventArgs : CheckedChangedEventArgs
	{
		/// <summary>Creates a new <see cref="CheckBoxCheckedChangedEventArgs"/> specifying the checked state.</summary>
		/// <param name="value"><see langword="true"/> if checked, <see langword="false"/> if unchecked, or <see langword="null"/> if indeterminate.</param>
		public CheckBoxCheckedChangedEventArgs(bool? value)
			: base(value == true)
		{
			Value = value;
		}

		/// <summary>Gets the checked state: <see langword="true"/> if checked, <see langword="false"/> if unchecked, or <see langword="null"/> if indeterminate.</summary>
		public new bool? Value { get; }
	}
}