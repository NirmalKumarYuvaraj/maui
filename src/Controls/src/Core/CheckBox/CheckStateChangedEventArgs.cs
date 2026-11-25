using System;

namespace Microsoft.Maui.Controls
{
	/// <summary>Event arguments for the <see cref="CheckBox.CheckStateChanged"/> event.</summary>
	public class CheckStateChangedEventArgs : EventArgs
	{
		/// <summary>Creates a new <see cref="CheckStateChangedEventArgs"/> with the new state of the <see cref="CheckBox"/>.</summary>
		/// <param name="value">The new state of the <see cref="CheckBox"/>.</param>
		public CheckStateChangedEventArgs(CheckBoxState value)
		{
			Value = value;
		}

		/// <summary>Gets the new state of the <see cref="CheckBox"/>.</summary>
		public CheckBoxState Value { get; }
	}
}
