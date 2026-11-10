using System.Linq;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class PathSegmentTests : BaseTestFixture
	{
		[Theory]
		[InlineData("Line", 1, PathOperation.Line, 1)]
		[InlineData("Bezier", 3, PathOperation.Cubic, 3)]
		[InlineData("Quadratic", 2, PathOperation.Quad, 2)]
		[InlineData("PolyLine", 0, PathOperation.Line, 1)]
		[InlineData("PolyLine", 1, PathOperation.Line, 1)]
		[InlineData("PolyLine", 4, PathOperation.Line, 1)]
		[InlineData("PolyBezier", 0, PathOperation.Cubic, 3)]
		[InlineData("PolyBezier", 1, PathOperation.Cubic, 3)]
		[InlineData("PolyBezier", 2, PathOperation.Cubic, 3)]
		[InlineData("PolyBezier", 3, PathOperation.Cubic, 3)]
		[InlineData("PolyBezier", 4, PathOperation.Cubic, 3)]
		[InlineData("PolyBezier", 5, PathOperation.Cubic, 3)]
		[InlineData("PolyBezier", 6, PathOperation.Cubic, 3)]
		[InlineData("PolyBezier", 7, PathOperation.Cubic, 3)]
		[InlineData("PolyQuadratic", 0, PathOperation.Quad, 2)]
		[InlineData("PolyQuadratic", 1, PathOperation.Quad, 2)]
		[InlineData("PolyQuadratic", 2, PathOperation.Quad, 2)]
		[InlineData("PolyQuadratic", 3, PathOperation.Quad, 2)]
		[InlineData("PolyQuadratic", 4, PathOperation.Quad, 2)]
		[InlineData("PolyQuadratic", 5, PathOperation.Quad, 2)]
		public void AppendPathEmitsCompleteSegmentsWithCorrectControlPoints(string kind, int pointCount, PathOperation operation, int pointsPerSegment)
		{
			var points = new PointCollection(Enumerable.Range(1, pointCount).Select(i => new Point(i * 10 + 1, i * 10 + 2)).ToArray());
			PathSegment segment = kind switch
			{
				"Line" => new LineSegment(points[0]),
				"Bezier" => new BezierSegment(points[0], points[1], points[2]),
				"Quadratic" => new QuadraticBezierSegment(points[0], points[1]),
				"PolyLine" => new PolyLineSegment(points),
				"PolyBezier" => new PolyBezierSegment(points),
				_ => new PolyQuadraticBezierSegment(points)
			};
			var geometry = new PathGeometry
			{
				Figures = { new PathFigure { StartPoint = new Point(5, 7), Segments = { segment } } }
			};
			var path = new PathF();

			geometry.AppendPath(path);

			var segmentCount = pointCount / pointsPerSegment;
			Assert.Equal(new[] { PathOperation.Move }.Concat(Enumerable.Repeat(operation, segmentCount)), path.SegmentTypes);
			Assert.Equal(new[] { new PointF(5, 7) }.Concat(points.Take(segmentCount * pointsPerSegment).Select(p => new PointF((float)p.X, (float)p.Y))), path.Points);
			Assert.False(path.Closed);
		}

		[Theory]
		[InlineData(SweepDirection.Clockwise, false, 0)]
		[InlineData(SweepDirection.CounterClockwise, false, 0)]
		[InlineData(SweepDirection.Clockwise, true, 0)]
		[InlineData(SweepDirection.CounterClockwise, true, 0)]
		[InlineData(SweepDirection.Clockwise, false, 45)]
		[InlineData(SweepDirection.CounterClockwise, true, 90)]
		public void ArcAppendPathPreservesEndpointsAndProducesFiniteLines(SweepDirection direction, bool largeArc, double rotation)
		{
			var figure = new PathFigure
			{
				StartPoint = new Point(5, 7),
				Segments = { new ArcSegment(new Point(18, 12), new Size(20, 10), rotation, direction, largeArc) }
			};
			var geometry = new PathGeometry { Figures = { figure } };
			var path = new PathF();

			geometry.AppendPath(path);

			Assert.Equal(new PointF(5, 7), path.FirstPoint);
			Assert.Equal(18, path.LastPoint.X, 4);
			Assert.Equal(12, path.LastPoint.Y, 4);
			Assert.Equal(PathOperation.Move, path.SegmentTypes.First());
			Assert.All(path.SegmentTypes.Skip(1), operation => Assert.Equal(PathOperation.Line, operation));
			Assert.All(path.Points, point =>
			{
				Assert.True(float.IsFinite(point.X));
				Assert.True(float.IsFinite(point.Y));
			});
			Assert.False(path.Closed);
		}

		[Fact]
		public void ReplacingSegmentsDetachesOldCollectionAndAttachesExistingNewSegments()
		{
			var oldSegment = new LineSegment();
			var newSegment = new LineSegment();
			var figure = new PathFigure { Segments = { oldSegment } };
			var oldCollection = figure.Segments;
			var invalidations = 0;
			figure.InvalidatePathSegmentRequested += (_, _) => invalidations++;

			figure.Segments = null;
			oldSegment.Point = new Point(1, 2);
			oldCollection.Add(new LineSegment());
			Assert.Equal(0, invalidations);

			figure.Segments = new PathSegmentCollection { newSegment };
			newSegment.Point = new Point(3, 4);
			Assert.Equal(1, invalidations);
		}

		[Fact]
		public void SegmentReplacementRemovalAndMoveMaintainSubscriptions()
		{
			var first = new LineSegment();
			var second = new LineSegment();
			var replacement = new LineSegment();
			var figure = new PathFigure { Segments = { first, second } };
			var invalidations = 0;
			figure.InvalidatePathSegmentRequested += (_, _) => invalidations++;

			figure.Segments[0] = replacement;
			Assert.Equal(1, invalidations);
			invalidations = 0;
			first.Point = new Point(1, 2);
			Assert.Equal(0, invalidations);
			replacement.Point = new Point(1, 2);
			Assert.Equal(1, invalidations);

			figure.Segments.Move(0, 1);
			invalidations = 0;
			replacement.Point = new Point(3, 4);
			second.Point = new Point(3, 4);
			Assert.Equal(2, invalidations);

			figure.Segments.Remove(replacement);
			invalidations = 0;
			replacement.Point = new Point(5, 6);
			Assert.Equal(0, invalidations);
			second.Point = new Point(5, 6);
			Assert.Equal(1, invalidations);
		}

		[Fact]
		public void ClearingSegmentsAllowsReaddingAndResubscribingSameSegment()
		{
			var segment = new LineSegment();
			var figure = new PathFigure { Segments = { segment } };
			figure.Segments.Clear();
			var invalidations = 0;
			figure.InvalidatePathSegmentRequested += (_, _) => invalidations++;

			figure.Segments.Add(segment);
			Assert.Equal(1, invalidations);
			segment.Point = new Point(1, 2);
			Assert.Equal(2, invalidations);
		}

		[Fact]
		public void RemovingOneDuplicateSegmentKeepsRemainingOccurrenceSubscribed()
		{
			var segment = new LineSegment();
			var figure = new PathFigure { Segments = { segment, segment } };
			figure.Segments.RemoveAt(0);
			var invalidations = 0;
			figure.InvalidatePathSegmentRequested += (_, _) => invalidations++;

			segment.Point = new Point(1, 2);
			Assert.Equal(1, invalidations);

			figure.Segments.RemoveAt(0);
			invalidations = 0;
			segment.Point = new Point(3, 4);
			Assert.Equal(0, invalidations);
		}

		PointCollectionConverter _pointCollectionConverter;

		public PathSegmentTests()
		{
			_pointCollectionConverter = new PointCollectionConverter();
		}

		[Fact]
		public void TestArcSegmentConstructor()
		{
			var arcSegment1 = new ArcSegment();
			Assert.NotNull(arcSegment1);

			var arcSegment2 = new ArcSegment(new Point(0, 0), new Size(100, 100), 90, SweepDirection.Clockwise, false);
			Assert.NotNull(arcSegment2);
			Assert.Equal(90, arcSegment2.RotationAngle);
			Assert.Equal(100, arcSegment2.Size.Height);
			Assert.Equal(100, arcSegment2.Size.Width);
		}

		[Fact]
		public void TestBezierSegmentConstructor()
		{
			var bezierSegment1 = new BezierSegment();
			Assert.NotNull(bezierSegment1);

			var bezierSegment2 = new BezierSegment(new Point(0, 0), new Point(50, 50), new Point(100, 100));
			Assert.NotNull(bezierSegment2);
			Assert.Equal(0, bezierSegment2.Point1.X);
			Assert.Equal(0, bezierSegment2.Point1.Y);
			Assert.Equal(50, bezierSegment2.Point2.X);
			Assert.Equal(50, bezierSegment2.Point2.Y);
			Assert.Equal(100, bezierSegment2.Point3.X);
			Assert.Equal(100, bezierSegment2.Point3.Y);
		}

		[Fact]
		public void TestLineSegmentConstructor()
		{
			var lineSegment1 = new LineSegment();
			Assert.NotNull(lineSegment1);

			var lineSegment2 = new LineSegment(new Point(25, 50));
			Assert.NotNull(lineSegment2);
			Assert.Equal(25, lineSegment2.Point.X);
			Assert.Equal(50, lineSegment2.Point.Y);
		}

		[Theory]
		[InlineData("", 0)]
		[InlineData("0 48", 1)]
		[InlineData("0 48, 0 144", 2)]
		[InlineData("0 48, 0 144, 96 150", 3)]
		[InlineData("0 48, 0 144, 96 150, 100 0", 4)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0", 5)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96", 6)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96", 7)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192", 8)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192, 150 200", 9)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192, 150 200 144 48", 10)]
		public void TestPolyBezierSegmentConstructor(string points, int count)
		{
			var pointCollection = (PointCollection)_pointCollectionConverter.ConvertFromInvariantString(points);

			var polyBezierSegment = new PolyBezierSegment(pointCollection);
			Assert.NotNull(polyBezierSegment);
			Assert.Equal(count, polyBezierSegment.Points.Count);
		}

		[Theory]
		[InlineData("", 0)]
		[InlineData("0 48", 1)]
		[InlineData("0 48, 0 144", 2)]
		[InlineData("0 48, 0 144, 96 150", 3)]
		[InlineData("0 48, 0 144, 96 150, 100 0", 4)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0", 5)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96", 6)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96", 7)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192", 8)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192, 150 200", 9)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192, 150 200 144 48", 10)]
		public void TestPolyLineSegmentConstructor(string points, int count)
		{
			var pointCollection = (PointCollection)_pointCollectionConverter.ConvertFromInvariantString(points);
			var polyLineSegment = new PolyLineSegment(pointCollection);
			Assert.NotNull(polyLineSegment);
			Assert.Equal(count, polyLineSegment.Points.Count);
		}

		[Theory]
		[InlineData("", 0)]
		[InlineData("0 48", 1)]
		[InlineData("0 48, 0 144", 2)]
		[InlineData("0 48, 0 144, 96 150", 3)]
		[InlineData("0 48, 0 144, 96 150, 100 0", 4)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0", 5)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96", 6)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96", 7)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192", 8)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192, 150 200", 9)]
		[InlineData("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192, 150 200 144 48", 10)]
		public void TestPolyQuadraticBezierSegmentConstructor(string points, int count)
		{
			var pointCollection = (PointCollection)_pointCollectionConverter.ConvertFromInvariantString(points);
			var polyQuadraticBezierSegment = new PolyQuadraticBezierSegment(pointCollection);
			Assert.NotNull(polyQuadraticBezierSegment);
			Assert.Equal(count, polyQuadraticBezierSegment.Points.Count);
		}

		[Fact]
		public void TestQuadraticBezierSegmentConstructor()
		{
			var quadraticBezierSegment1 = new QuadraticBezierSegment();
			Assert.NotNull(quadraticBezierSegment1);

			var quadraticBezierSegment2 = new QuadraticBezierSegment(new Point(0, 0), new Point(100, 100));
			Assert.NotNull(quadraticBezierSegment2);
			Assert.Equal(0, quadraticBezierSegment2.Point1.X);
			Assert.Equal(0, quadraticBezierSegment2.Point1.Y);
			Assert.Equal(100, quadraticBezierSegment2.Point2.X);
			Assert.Equal(100, quadraticBezierSegment2.Point2.Y);
		}

		[Fact]
		public void ClearingSegmentsDetachesPropertyChangedFromRemovedSegments()
		{
			var pathFigure = new PathFigure();
			var sharedSegment = new LineSegment();
			var invalidationCount = 0;

			pathFigure.InvalidatePathSegmentRequested += (_, __) => invalidationCount++;
			pathFigure.Segments.Add(sharedSegment);

			pathFigure.Segments.Clear();
			var invalidationCountAfterClear = invalidationCount;

			sharedSegment.Point = new Point(10, 10);

			Assert.Equal(invalidationCountAfterClear, invalidationCount);
		}
	}
}