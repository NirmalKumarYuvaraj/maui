#nullable disable
using System;

namespace Microsoft.Maui.Controls
{
	/// <summary>Event args for <see cref="CheckBox.CheckStateChanged"/>.</summary>
	public class CheckStateChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of <see cref="CheckStateChangedEventArgs"/>.
		/// </summary>
		/// <param name="checkState">The new <see cref="Microsoft.Maui.CheckState"/>.</param>
		public CheckStateChangedEventArgs(CheckState checkState)
		{
			CheckState = checkState;
		}

		/// <summary>Gets the new check state of the <see cref="CheckBox"/>.</summary>
		public CheckState CheckState { get; }
	}
}
