using System.Collections.ObjectModel;

namespace Microsoft.Maui.Controls.Shapes
{
	/// <summary>
	/// A collection of <see cref="Transform"/> objects.
	/// </summary>
	public sealed class TransformCollection : ObservableCollection<Transform>
	{
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			for (int i = Count - 1; i >= 0; i--)
				RemoveAt(i);
		}

	}
}