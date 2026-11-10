using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Rect = Microsoft.Maui.Graphics.Rect;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests;

public class GeometryGroupTests : BaseTestFixture
{
	[Fact]
	public void AppendPathPreservesExistingPathAndNestedChildOrder()
	{
		var group = new GeometryGroup
		{
			Children =
			{
				new LineGeometry(new Point(10, 20), new Point(30, 40)),
				new GeometryGroup
				{
					Children = { new LineGeometry(new Point(-10, -20), new Point(-30, -40)) }
				}
			}
		};
		var path = new PathF();
		path.MoveTo(1, 2);
		path.LineTo(3, 4);

		group.AppendPath(path);

		Assert.Equal(3, path.SubPathCount);
		Assert.Equal(new[] { new PointF(1, 2), new PointF(3, 4), new PointF(10, 20), new PointF(30, 40), new PointF(-10, -20), new PointF(-30, -40) }, path.Points);
		Assert.Equal(new[] { PathOperation.Move, PathOperation.Line, PathOperation.Move, PathOperation.Line, PathOperation.Move, PathOperation.Line }, path.SegmentTypes);
	}

	[Fact]
	public void ClearUnsubscribesPreviousChildrenFromPropertyChanged()
	{
		var group = new GeometryGroup();
		var oldChild = new RectangleGeometry(new Rect(0, 0, 10, 10));
		var newChild = new RectangleGeometry(new Rect(0, 0, 5, 5));
		var invalidations = 0;

		group.InvalidateGeometryRequested += (_, _) => invalidations++;

		group.Children.Add(oldChild);
		invalidations = 0;

		group.Children.Clear();
		invalidations = 0;

		// If Reset handling does not unsubscribe old items, this mutation incorrectly invalidates the group.
		oldChild.Rect = new Rect(1, 1, 11, 11);
		Assert.Equal(0, invalidations);

		group.Children.Add(newChild);
		invalidations = 0;

		newChild.Rect = new Rect(2, 2, 6, 6);
		Assert.Equal(1, invalidations);
	}
}