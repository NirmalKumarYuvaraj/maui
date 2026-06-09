using System.Collections.ObjectModel;

namespace Microsoft.Maui.Controls.Shapes
{
	/// <summary>
	/// A collection of <see cref="Transform"/> objects.
	/// </summary>
	public sealed class TransformCollection : ObservableCollection<Transform>
	{
		// Override to remove items one-by-one so that CollectionChanged fires per-item Remove events
		// (with OldItems populated) rather than a single Reset event (which has OldItems == null).
		// This lets TransformGroup unsubscribe its PropertyChanged handlers on cleared transforms —
		// preventing memory leaks when a transform is shared/externally rooted.
		// See https://github.com/dotnet/maui/issues/35809.
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			for (int i = Count - 1; i >= 0; i--)
				RemoveAt(i);
		}

	}
}