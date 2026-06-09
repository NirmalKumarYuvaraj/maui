using System.Collections.ObjectModel;

namespace Microsoft.Maui.Controls.Shapes
{
	/// <summary>
	/// A collection of <see cref="PathFigure"/> objects that make up a <see cref="PathGeometry"/>.
	/// </summary>
	public sealed class PathFigureCollection : ObservableCollection<PathFigure>
	{
		// Override to remove items one-by-one so that CollectionChanged fires per-item Remove events
		// (with OldItems populated) rather than a single Reset event (which has OldItems == null).
		// This lets PathGeometry unsubscribe its PropertyChanged/InvalidatePathSegmentRequested
		// handlers on cleared figures — preventing memory leaks when a figure is shared/externally
		// rooted. See https://github.com/dotnet/maui/issues/35809.
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			for (int i = Count - 1; i >= 0; i--)
				RemoveAt(i);
		}

	}
}