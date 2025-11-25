using System;
using Android.Content;
using AndroidX.AppCompat.Widget;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Custom AppCompatEditText for .NET MAUI with selection change tracking.
	/// Compatible with Material 3 TextInputLayout for enhanced text input styling.
	/// Material 3 provides filled and outlined text field variants.
	/// </summary>
	public class MauiAppCompatEditText : AppCompatEditText
	{
		public event EventHandler? SelectionChanged;

		public MauiAppCompatEditText(Context context) : base(context)
		{
		}

		protected override void OnSelectionChanged(int selStart, int selEnd)
		{
			base.OnSelectionChanged(selStart, selEnd);

			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
