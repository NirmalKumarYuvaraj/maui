using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class PathTests : BaseTestFixture
{
	[Fact]
	public void GetPathHandlesNullAndReflectsGeometryChangesWithoutSharingOutput()
	{
		var path = new Path();
		Assert.Empty(path.GetPath().Points);
		var geometry = new LineGeometry(new Point(10, 20), new Point(30, 40));
		path.Data = geometry;
		var original = path.GetPath();

		geometry.EndPoint = new Point(50, 60);
		var updated = path.GetPath();

		Assert.NotSame(original, updated);
		Assert.Equal(new[] { new PointF(10, 20), new PointF(30, 40) }, original.Points);
		Assert.Equal(new[] { new PointF(10, 20), new PointF(50, 60) }, updated.Points);
		updated.SetPoint(0, -100, -200);
		Assert.Equal(new PointF(10, 20), path.GetPath().FirstPoint);
		path.Data = null;
		Assert.Empty(path.GetPath().Points);
	}

	[Fact]
	public void ReplacingDataDetachesOldGeometryAndClearingDetachesNewGeometry()
	{
		var oldGeometry = new LineGeometry();
		var newGeometry = new LineGeometry();
		var path = new Path(oldGeometry);
		var notifications = 0;
		path.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				notifications++;
		};

		path.Data = newGeometry;
		notifications = 0;
		oldGeometry.EndPoint = new Point(1, 2);
		Assert.Equal(0, notifications);
		newGeometry.EndPoint = new Point(3, 4);
		Assert.Equal(1, notifications);

		path.Data = null;
		notifications = 0;
		newGeometry.EndPoint = new Point(5, 6);
		Assert.Equal(0, notifications);
	}

	[Theory]
	[InlineData("Line")]
	[InlineData("Arc")]
	[InlineData("Bezier")]
	[InlineData("Quadratic")]
	[InlineData("PolyLine")]
	[InlineData("PolyBezier")]
	[InlineData("PolyQuadratic")]
	public void SegmentPropertyChangesPropagateToAllPathsSharingGeometry(string kind)
	{
		PathSegment segment = kind switch
		{
			"Line" => new LineSegment(),
			"Arc" => new ArcSegment(),
			"Bezier" => new BezierSegment(),
			"Quadratic" => new QuadraticBezierSegment(),
			"PolyLine" => new PolyLineSegment(),
			"PolyBezier" => new PolyBezierSegment(),
			_ => new PolyQuadraticBezierSegment()
		};
		var geometry = new PathGeometry { Figures = { new PathFigure { Segments = { segment } } } };
		var first = new Path(geometry);
		var second = new Path(geometry);
		var firstNotifications = 0;
		var secondNotifications = 0;
		first.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				firstNotifications++;
		};
		second.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				secondNotifications++;
		};

		var point = new Point(10, 20);
		switch (segment)
		{
			case LineSegment line:
				line.Point = point;
				break;
			case ArcSegment arc:
				arc.Point = point;
				break;
			case BezierSegment bezier:
				bezier.Point3 = point;
				break;
			case QuadraticBezierSegment quadratic:
				quadratic.Point2 = point;
				break;
			case PolyLineSegment polyLine:
				polyLine.Points = new PointCollection { point };
				break;
			case PolyBezierSegment polyBezier:
				polyBezier.Points = new PointCollection { point };
				break;
			case PolyQuadraticBezierSegment polyQuadratic:
				polyQuadratic.Points = new PointCollection { point };
				break;
		}

		Assert.Equal(1, firstNotifications);
		Assert.Equal(1, secondNotifications);
	}

	[Theory]
	[InlineData("Line")]
	[InlineData("Bezier")]
	[InlineData("Quadratic")]
	public void EditingPolySegmentPointCollectionNotifiesPath(string kind)
	{
		var points = new PointCollection { new Point(1, 2), new Point(3, 4), new Point(5, 6) };
		PathSegment segment = kind switch
		{
			"Line" => new PolyLineSegment(points),
			"Bezier" => new PolyBezierSegment(points),
			_ => new PolyQuadraticBezierSegment(points)
		};
		var geometry = new PathGeometry { Figures = { new PathFigure { Segments = { segment } } } };
		var path = new Path(geometry);
		var notifications = 0;
		path.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				notifications++;
		};

		points[0] = new Point(10, 20);

		Assert.Equal(1, notifications);
		Assert.Equal(new PointF(10, 20), path.GetPath()[1]);
	}

	[Fact]
	public void ChildGeometryChangesInGroupNotifyPath()
	{
		var child = new LineGeometry();
		var group = new GeometryGroup { Children = { child } };
		var path = new Path(group);
		var notifications = 0;
		path.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				notifications++;
		};

		child.EndPoint = new Point(10, 20);

		Assert.Equal(1, notifications);
		Assert.Equal(new PointF(10, 20), path.GetPath().LastPoint);
	}

	[Fact]
	public void ReplacingAndClearingTransformMovesValueChangeSubscription()
	{
		var oldTransform = new TranslateTransform();
		var newTransform = new ScaleTransform();
		var path = new Path { RenderTransform = oldTransform };
		var notifications = 0;
		path.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.RenderTransform))
				notifications++;
		};

		path.RenderTransform = newTransform;
		notifications = 0;
		oldTransform.X = 10;
		Assert.Equal(0, notifications);
		newTransform.ScaleX = 2;
		Assert.Equal(1, notifications);

		path.RenderTransform = null;
		notifications = 0;
		newTransform.ScaleX = 3;
		Assert.Equal(0, notifications);
	}
}
