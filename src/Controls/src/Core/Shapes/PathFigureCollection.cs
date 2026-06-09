using System.Collections.ObjectModel;

namespace Microsoft.Maui.Controls.Shapes
{
	/// <summary>
	/// A collection of <see cref="PathFigure"/> objects that make up a <see cref="PathGeometry"/>.
	/// </summary>
	public sealed class PathFigureCollection : ObservableCollection<PathFigure>
	{
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			for (int i = Count - 1; i >= 0; i--)
				RemoveAt(i);
		}

	}
}