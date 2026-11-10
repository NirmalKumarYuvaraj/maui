using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{

	public class GeometryTests : BaseTestFixture
	{
		[Theory]
		[InlineData(12, -8, 41, 27)]
		[InlineData(0, 0, 100, 50)]
		[InlineData(-5, 6, -5, 6)]
		public void LineAppendPathStartsNewSubpathWithoutMovingExistingPoints(float x1, float y1, float x2, float y2)
		{
			var path = new PathF();
			path.MoveTo(-100, -200);
			path.LineTo(-90, -180);

			new LineGeometry(new Point(x1, y1), new Point(x2, y2)).AppendPath(path);

			Assert.Equal(new[] { PathOperation.Move, PathOperation.Line, PathOperation.Move, PathOperation.Line }, path.SegmentTypes);
			Assert.Equal(new[] { new PointF(-100, -200), new PointF(-90, -180), new PointF(x1, y1), new PointF(x2, y2) }, path.Points);
			Assert.Equal(2, path.SubPathCount);
			Assert.False(path.Closed);
		}

		[Theory]
		[InlineData(20, -10, 7, 13)]
		[InlineData(-20, 30, 0, 8)]
		[InlineData(20, 30, 8, 0)]
		public void EllipseAppendPathUsesCenterAndIndependentRadii(float x, float y, float radiusX, float radiusY)
		{
			var path = new PathF();

			new EllipseGeometry(new Point(x, y), radiusX, radiusY).AppendPath(path);

			Assert.True(path.Closed);
			Assert.Equal(x - radiusX, path.Points.Min(p => p.X));
			Assert.Equal(x + radiusX, path.Points.Max(p => p.X));
			Assert.Equal(y - radiusY, path.Points.Min(p => p.Y));
			Assert.Equal(y + radiusY, path.Points.Max(p => p.Y));
			Assert.Equal(new[] { PathOperation.Move, PathOperation.Cubic, PathOperation.Cubic, PathOperation.Cubic, PathOperation.Cubic, PathOperation.Close }, path.SegmentTypes);
			Assert.Equal(new PointF(x - radiusX, y), path.FirstPoint);
			Assert.Equal(new PointF(x, y - radiusY), path.GetPointsForSegment(1).Last());
			Assert.Equal(new PointF(x + radiusX, y), path.GetPointsForSegment(2).Last());
			Assert.Equal(new PointF(x, y + radiusY), path.GetPointsForSegment(3).Last());
			Assert.Equal(path.FirstPoint, path.LastPoint);
		}

		[Theory]
		[InlineData(-12, 9, 40, 25)]
		[InlineData(10, -20, 0, 25)]
		[InlineData(10, -20, 40, 0)]
		public void RectangleAppendPathUsesAllFourCorners(float x, float y, float width, float height)
		{
			var path = new PathF();

			new RectangleGeometry(new Rect(x, y, width, height)).AppendPath(path);

			Assert.Equal(new[] { PathOperation.Move, PathOperation.Line, PathOperation.Line, PathOperation.Line, PathOperation.Close }, path.SegmentTypes);
			Assert.Equal(new[] { new PointF(x, y), new PointF(x + width, y), new PointF(x + width, y + height), new PointF(x, y + height) }, path.Points);
			Assert.True(path.Closed);
		}

		[Theory]
		[InlineData(3, 5, 7, 11)]
		[InlineData(0, 0, 0, 0)]
		[InlineData(100, 100, 100, 100)]
		public void RoundRectangleAppendPathMapsEachCornerAndClampsOversizedRadii(float tl, float tr, float bl, float br)
		{
			var geometry = new RoundRectangleGeometry(new CornerRadius(tl, tr, bl, br), new Rect(10, -20, 80, 60));
			var path = new PathF();

			geometry.AppendPath(path);

			Assert.True(path.Closed);
			Assert.Equal(10, path.Points.Min(p => p.X));
			Assert.Equal(90, path.Points.Max(p => p.X));
			Assert.Equal(-20, path.Points.Min(p => p.Y));
			Assert.Equal(40, path.Points.Max(p => p.Y));
			Assert.Equal(new PointF(10, -20 + System.Math.Min(tl, 30)), path.FirstPoint);
			Assert.Equal(new PointF(10 + System.Math.Min(tl, 30), -20), path.GetPointsForSegment(1).Last());
			Assert.Equal(new PointF(90, -20 + System.Math.Min(tr, 30)), path.GetPointsForSegment(3).Last());
			Assert.Equal(new PointF(90 - System.Math.Min(br, 30), 40), path.GetPointsForSegment(5).Last());
			Assert.Equal(new PointF(10, 40 - System.Math.Min(bl, 30)), path.GetPointsForSegment(7).Last());
		}

		[Fact]
		public void RoundRectangleRebuildsChildrenWhenBoundsAndCornersChange()
		{
			var geometry = new RoundRectangleGeometry(new CornerRadius(2), new Rect(0, 0, 20, 20));
			var oldChild = Assert.Single(geometry.Children);

			geometry.Rect = new Rect(10, 20, 100, 80);
			geometry.CornerRadius = new CornerRadius(3, 5, 7, 11);

			Assert.NotSame(oldChild, Assert.Single(geometry.Children));
			var group = Assert.IsType<GeometryGroup>(geometry.Children[0]);
			Assert.Equal(FillRule.Nonzero, geometry.FillRule);
			Assert.Equal(FillRule.Nonzero, group.FillRule);
			Assert.Equal(5, group.Children.Count);
			var flattened = GeometryHelper.FlattenGeometry(geometry, 1);
			var path = new PathF();
			flattened.AppendPath(path);
			Assert.Equal(new RectF(10, 20, 100, 80), path.Bounds);
		}

		[Theory]
		[InlineData("Line")]
		[InlineData("Ellipse")]
		[InlineData("Rectangle")]
		[InlineData("RoundRectangle")]
		[InlineData("Group")]
		[InlineData("Path")]
		public void GeometryPathForBoundsPreservesCoordinatesAndReturnsIndependentPaths(string kind)
		{
			Geometry geometry = kind switch
			{
				"Line" => new LineGeometry(new Point(10, 20), new Point(50, 60)),
				"Ellipse" => new EllipseGeometry(new Point(30, 40), 20, 10),
				"Rectangle" => new RectangleGeometry(new Rect(10, 20, 40, 30)),
				"RoundRectangle" => new RoundRectangleGeometry(new CornerRadius(5), new Rect(10, 20, 40, 30)),
				"Group" => new GeometryGroup { Children = { new RectangleGeometry(new Rect(10, 20, 40, 30)) } },
				_ => new PathGeometry { Figures = { new PathFigure { StartPoint = new Point(10, 20), Segments = { new LineSegment(new Point(50, 60)) } } } }
			};
			var expected = new PathF();
			geometry.AppendPath(expected);

			var first = ((IShape)geometry).PathForBounds(new Rect(100, 200, 300, 400));
			var second = ((IShape)geometry).PathForBounds(Rect.Zero);

			Assert.Equal(expected.SegmentTypes, first.SegmentTypes);
			Assert.Equal(expected.Points, first.Points);
			first.SetPoint(0, -100, -200);
			Assert.NotSame(first, second);
			Assert.Equal(expected.Points, second.Points);
		}

		[Theory]
		[InlineData(0, true)]
		[InlineData(0, false)]
		[InlineData(45, true)]
		[InlineData(45, false)]
		[InlineData(180, true)]
		[InlineData(180, false)]
		[InlineData(270, true)]
		[InlineData(270, false)]
		public void FlattenArcTest(double angle, bool isLargeArc)
		{
			var path = new Path
			{
				HeightRequest = 200,
				WidthRequest = 200,
				Stroke = Brush.Black
			};

			PathFigure figure = new PathFigure();

			ArcSegment arcSegment = new ArcSegment
			{
				Point = new Point(10, 100),
				Size = new Size(100, 50),
				RotationAngle = angle,
				IsLargeArc = isLargeArc
			};

			figure.Segments.Add(arcSegment);

			path.Data = new PathGeometry
			{
				Figures = new PathFigureCollection
				{
					figure
				}
			};

			List<Point> points = new List<Point>();

			GeometryHelper.FlattenArc(
				points,
				Point.Zero,
				arcSegment.Point,
				arcSegment.Size.Width,
				arcSegment.Size.Height,
				arcSegment.RotationAngle,
				arcSegment.IsLargeArc,
				arcSegment.SweepDirection == SweepDirection.CounterClockwise,
				1);

			Assert.NotEmpty(points);
		}

		[Fact]
		public void FlattenSmallArcProducesFinitePoints()
		{
			var points = new List<Point>();

			GeometryHelper.FlattenArc(
				points,
				Point.Zero,
				new Point(-0.55999, -0.44),
				1,
				1,
				0,
				false,
				false,
				1);

			Assert.Equal(2, points.Count);
			Assert.All(points, point =>
			{
				Assert.True(double.IsFinite(point.X));
				Assert.True(double.IsFinite(point.Y));
			});
		}

		[Fact]
		public void FlattenSmallCubicBezierProducesFinitePoints()
		{
			var points = new List<Point>();

			GeometryHelper.FlattenCubicBezier(
				points,
				Point.Zero,
				new Point(0.1, 0.1),
				new Point(0.2, 0.1),
				new Point(0.3, 0),
				1);

			Assert.Equal(2, points.Count);
			Assert.All(points, point =>
			{
				Assert.True(double.IsFinite(point.X));
				Assert.True(double.IsFinite(point.Y));
			});
		}

		[Fact]
		public void FlattenSmallQuadraticBezierProducesFinitePoints()
		{
			var points = new List<Point>();

			GeometryHelper.FlattenQuadraticBezier(
				points,
				Point.Zero,
				new Point(0.2, 0.2),
				new Point(0.4, 0),
				1);

			Assert.Equal(2, points.Count);
			Assert.All(points, point =>
			{
				Assert.True(double.IsFinite(point.X));
				Assert.True(double.IsFinite(point.Y));
			});
		}

		[Fact]
		public void TestRoundLineGeometryConstruction()
		{
			var lineGeometry = new LineGeometry(new Point(0, 0), new Point(100, 100));

			Assert.NotNull(lineGeometry);
			Assert.Equal(0, lineGeometry.StartPoint.X);
			Assert.Equal(0, lineGeometry.StartPoint.Y);
			Assert.Equal(100, lineGeometry.EndPoint.X);
			Assert.Equal(100, lineGeometry.EndPoint.Y);
		}

		[Fact]
		public void TestEllipseGeometryConstruction()
		{
			var ellipseGeometry = new EllipseGeometry(new Point(50, 50), 10, 20);

			Assert.NotNull(ellipseGeometry);
			Assert.Equal(50, ellipseGeometry.Center.X);
			Assert.Equal(50, ellipseGeometry.Center.Y);
			Assert.Equal(10, ellipseGeometry.RadiusX);
			Assert.Equal(20, ellipseGeometry.RadiusY);
		}

		[Fact]
		public void TestRectangleGeometryConstruction()
		{
			var rectangleGeometry = new RectangleGeometry(new Rect(0, 0, 150, 150));

			Assert.NotNull(rectangleGeometry);
			Assert.Equal(150, rectangleGeometry.Rect.Height);
			Assert.Equal(150, rectangleGeometry.Rect.Width);
		}

		[Fact]
		public void TestRoundRectangleGeometryConstruction()
		{
			var roundRectangleGeometry = new RoundRectangleGeometry(new CornerRadius(12, 0, 0, 12), new Rect(0, 0, 150, 150));

			Assert.NotNull(roundRectangleGeometry);
			Assert.Equal(12, roundRectangleGeometry.CornerRadius.TopLeft);
			Assert.Equal(0, roundRectangleGeometry.CornerRadius.TopRight);
			Assert.Equal(0, roundRectangleGeometry.CornerRadius.BottomLeft);
			Assert.Equal(12, roundRectangleGeometry.CornerRadius.BottomRight);
			Assert.Equal(150, roundRectangleGeometry.Rect.Height);
			Assert.Equal(150, roundRectangleGeometry.Rect.Width);
		}
	}
}
