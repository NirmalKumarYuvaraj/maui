using System.Collections.ObjectModel;

namespace Microsoft.Maui.Controls.Shapes
{
	/// <summary>
	/// A collection of <see cref="Geometry"/> objects.
	/// </summary>
	public sealed class GeometryCollection : ObservableCollection<Geometry>
	{
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			for (int i = Count - 1; i >= 0; i--)
				RemoveAt(i);
		}

	}
}