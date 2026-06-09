using System.Collections.ObjectModel;

namespace Microsoft.Maui.Controls.Shapes
{
	/// <summary>
	/// A collection of <see cref="PathSegment"/> objects that define the geometry of a <see cref="PathFigure"/>.
	/// </summary>
	public sealed class PathSegmentCollection : ObservableCollection<PathSegment>
	{
		// Override to remove items one-by-one so that CollectionChanged fires per-item Remove events
		// (with OldItems populated) rather than a single Reset event (which has OldItems == null).
		// This lets PathFigure unsubscribe its PropertyChanged handlers on cleared segments —
		// preventing memory leaks when a segment is shared/externally rooted.
		// See https://github.com/dotnet/maui/issues/35809.
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			for (int i = Count - 1; i >= 0; i--)
				RemoveAt(i);
		}

	}
}