using System.Collections.ObjectModel;

namespace Microsoft.Maui.Controls.Shapes
{
	/// <summary>
	/// A collection of <see cref="PathSegment"/> objects that define the geometry of a <see cref="PathFigure"/>.
	/// </summary>
	public sealed class PathSegmentCollection : ObservableCollection<PathSegment>
	{
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			for (int i = Count - 1; i >= 0; i--)
				RemoveAt(i);
		}

	}
}