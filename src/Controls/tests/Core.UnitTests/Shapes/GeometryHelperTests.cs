using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class GeometryHelperTests : BaseTestFixture
{
	[Fact]
	public void FlattenNullGeometryReturnsEmptyPath()
	{
		Assert.Empty(GeometryHelper.FlattenGeometry(null, 1).Figures);
	}

	[Fact]
	public void FlattenNestedGroupAppendsTransformedFiguresInOrder()
	{
		var line = new LineGeometry(new Point(1, 2), new Point(3, 4));
		var rectangle = new RectangleGeometry(new Rect(10, 20, 30, 40));
		var group = new GeometryGroup
		{
			Children = { line, new GeometryGroup { Children = { rectangle } } }
		};
		var existingFigure = new PathFigure();
		var destination = new PathGeometry { Figures = { existingFigure } };

		GeometryHelper.FlattenGeometry(destination, group, 1, new Matrix(2, 0, 0, 3, 5, -7));

		Assert.Equal(3, destination.Figures.Count);
		Assert.Same(existingFigure, destination.Figures[0]);
		var lineFigure = destination.Figures[1];
		Assert.False(lineFigure.IsClosed);
		Assert.Equal(new Point(7, -1), lineFigure.StartPoint);
		Assert.Equal(new Point(11, 5), Assert.Single(Assert.IsType<PolyLineSegment>(Assert.Single(lineFigure.Segments)).Points));
		var rectangleFigure = destination.Figures[2];
		Assert.True(rectangleFigure.IsClosed);
		Assert.Equal(new Point(25, 53), rectangleFigure.StartPoint);
		Assert.Equal(new[] { new Point(85, 53), new Point(85, 173), new Point(25, 173), new Point(25, 53) },
			Assert.IsType<PolyLineSegment>(Assert.Single(rectangleFigure.Segments)).Points);
		Assert.Equal(new Point(1, 2), line.StartPoint);
		Assert.Equal(new Rect(10, 20, 30, 40), rectangle.Rect);
	}

	[Theory]
	[InlineData(FillRule.EvenOdd)]
	[InlineData(FillRule.Nonzero)]
	public void FlattenGroupPreservesFillRule(FillRule fillRule)
	{
		var group = new GeometryGroup
		{
			FillRule = fillRule,
			Children = { new RectangleGeometry(new Rect(0, 0, 10, 10)), new RectangleGeometry(new Rect(2, 2, 6, 6)) }
		};

		var flattened = GeometryHelper.FlattenGeometry(group, 1);

		Assert.Equal(2, flattened.Figures.Count);
		Assert.Equal(fillRule, flattened.FillRule);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(4)]
	public void FlattenEllipseProducesClosedTransformedPointsOnEllipse(double tolerance)
	{
		var geometry = new EllipseGeometry(new Point(10, 20), 8, 4);
		var flattened = new PathGeometry();

		GeometryHelper.FlattenGeometry(flattened, geometry, tolerance, new Matrix(2, 0, 0, 3, 5, -7));

		var figure = Assert.Single(flattened.Figures);
		Assert.True(figure.IsClosed);
		Assert.Equal(new Point(25, 41), figure.StartPoint);
		var points = Assert.IsType<PolyLineSegment>(Assert.Single(figure.Segments)).Points;
		Assert.NotEmpty(points);
		Assert.All(points.Prepend(figure.StartPoint), point =>
		{
			var x = (point.X - 25) / 16;
			var y = (point.Y - 53) / 12;
			Assert.Equal(1, x * x + y * y, 10);
		});
	}

	[Fact]
	public void FlattenSmallEllipseRetainsItsLocationAtCoarseTolerance()
	{
		var geometry = new EllipseGeometry(new Point(10, 20), 0.1, 0.1);

		var figure = Assert.Single(GeometryHelper.FlattenGeometry(geometry, 1).Figures);

		Assert.Equal(10, figure.StartPoint.X, 10);
		Assert.Equal(19.9, figure.StartPoint.Y, 10);
		Assert.True(figure.IsClosed);
	}

	[Theory]
	[InlineData("Line")]
	[InlineData("PolyLine")]
	[InlineData("Bezier")]
	[InlineData("PolyBezier")]
	[InlineData("Quadratic")]
	[InlineData("PolyQuadratic")]
	[InlineData("Arc")]
	public void FlattenPathConvertsEverySegmentToIndependentPolylines(string kind)
	{
		var sourceSegment = CreateSegment(kind);
		var source = new PathGeometry
		{
			FillRule = FillRule.Nonzero,
			Figures = { new PathFigure { IsClosed = true, IsFilled = false, Segments = { sourceSegment } } }
		};

		var flattened = GeometryHelper.FlattenGeometry(source, 1);

		Assert.NotSame(source, flattened);
		Assert.Equal(FillRule.Nonzero, flattened.FillRule);
		var figure = Assert.Single(flattened.Figures);
		Assert.NotSame(source.Figures[0], figure);
		Assert.True(figure.IsClosed);
		Assert.False(figure.IsFilled);
		Assert.Equal(Point.Zero, figure.StartPoint);
		var segment = Assert.IsType<PolyLineSegment>(Assert.Single(figure.Segments));
		Assert.NotSame(sourceSegment, segment);
		Assert.NotEmpty(segment.Points);
		Assert.Equal(10, segment.Points.Last().X, 8);
		Assert.Equal(10, segment.Points.Last().Y, 8);
		Assert.All(segment.Points, point =>
		{
			Assert.True(double.IsFinite(point.X));
			Assert.True(double.IsFinite(point.Y));
		});

		segment.Points.Clear();
		var originalPath = new PathF();
		source.AppendPath(originalPath);
		Assert.Equal(10, originalPath.LastPoint.X, 4);
		Assert.Equal(10, originalPath.LastPoint.Y, 4);
	}

	[Theory]
	[InlineData("Line")]
	[InlineData("PolyLine")]
	[InlineData("Bezier")]
	[InlineData("PolyBezier")]
	[InlineData("Quadratic")]
	[InlineData("PolyQuadratic")]
	[InlineData("Arc")]
	public void FlattenPathTransformsSegmentEndpoints(string kind)
	{
		var source = new PathGeometry { Figures = { new PathFigure { Segments = { CreateSegment(kind) } } } };
		var destination = new PathGeometry();

		GeometryHelper.FlattenGeometry(destination, source, 1, new Matrix(1, 0, 0, 1, 5, -3));

		var figure = Assert.Single(destination.Figures);
		Assert.Equal(new Point(5, -3), figure.StartPoint);
		var points = Assert.IsType<PolyLineSegment>(Assert.Single(figure.Segments)).Points;
		Assert.Equal(15, points.Last().X, 8);
		Assert.Equal(7, points.Last().Y, 8);
	}

	[Fact]
	public void FlattenMixedSegmentsTracksCurrentPointAcrossCurvesAndArc()
	{
		var geometry = new PathGeometry
		{
			Figures =
			{
				new PathFigure
				{
					StartPoint = new Point(-20, -30),
					Segments =
					{
						new LineSegment(new Point(10, 0)),
						new BezierSegment(new Point(10, 5), new Point(5, 10), new Point(0, 10)),
						new ArcSegment(new Point(-10, 0), new Size(10, 10), 0, SweepDirection.Clockwise, false),
						new QuadraticBezierSegment(new Point(-10, -10), new Point(0, -10))
					}
				}
			}
		};

		var figure = Assert.Single(GeometryHelper.FlattenGeometry(geometry, 1).Figures);

		Assert.Equal(new Point(-20, -30), figure.StartPoint);
		Assert.Equal(4, figure.Segments.Count);
		var segments = figure.Segments.Select(segment => Assert.IsType<PolyLineSegment>(segment)).ToArray();
		Assert.Equal(new Point(10, 0), segments[0].Points.Last());
		Assert.Equal(new Point(0, 10), segments[1].Points.Last());
		Assert.Equal(-10, segments[2].Points.Last().X, 8);
		Assert.Equal(0, segments[2].Points.Last().Y, 8);
		Assert.All(segments[2].Points, point => Assert.Equal(100, point.X * point.X + point.Y * point.Y, 8));
		Assert.Equal(new Point(0, -10), segments[3].Points.Last());
	}

	static PathSegment CreateSegment(string kind) => kind switch
	{
		"Line" => new LineSegment(new Point(10, 10)),
		"PolyLine" => new PolyLineSegment(new PointCollection { new Point(4, 6), new Point(10, 10) }),
		"Bezier" => new BezierSegment(new Point(0, 8), new Point(8, 10), new Point(10, 10)),
		"PolyBezier" => new PolyBezierSegment(new PointCollection { new Point(0, 4), new Point(4, 6), new Point(5, 5), new Point(6, 8), new Point(8, 10), new Point(10, 10) }),
		"Quadratic" => new QuadraticBezierSegment(new Point(0, 8), new Point(10, 10)),
		"PolyQuadratic" => new PolyQuadraticBezierSegment(new PointCollection { new Point(0, 4), new Point(5, 5), new Point(6, 8), new Point(10, 10) }),
		_ => new ArcSegment(new Point(10, 10), new Size(10, 10), 0, SweepDirection.Clockwise, false)
	};

	[Theory]
	[InlineData(false, 0)]
	[InlineData(false, 1)]
	[InlineData(false, 2)]
	[InlineData(false, 3)]
	[InlineData(false, 4)]
	[InlineData(false, 5)]
	[InlineData(false, 6)]
	[InlineData(false, 7)]
	[InlineData(true, 0)]
	[InlineData(true, 1)]
	[InlineData(true, 2)]
	[InlineData(true, 3)]
	[InlineData(true, 4)]
	[InlineData(true, 5)]
	public void FlattenPolyCurvesIgnoresIncompleteTrailingControlPoints(bool quadratic, int count)
	{
		var points = new PointCollection(Enumerable.Range(1, count).Select(i => new Point(i, i)).ToArray());
		PathSegment segment = quadratic ? new PolyQuadraticBezierSegment(points) : new PolyBezierSegment(points);
		var geometry = new PathGeometry { Figures = { new PathFigure { Segments = { segment } } } };

		var flattened = GeometryHelper.FlattenGeometry(geometry, 1);

		var result = Assert.IsType<PolyLineSegment>(Assert.Single(Assert.Single(flattened.Figures).Segments));
		var completePointCount = count / (quadratic ? 2 : 3) * (quadratic ? 2 : 3);
		if (completePointCount == 0)
			Assert.Empty(result.Points);
		else
			Assert.Equal(new Point(completePointCount, completePointCount), result.Points.Last());
		Assert.Equal(count, points.Count);
	}

	[Theory]
	[InlineData(false, 7.5)]
	[InlineData(true, 5)]
	public void FlattenBezierPreservesEndpointsAndMathematicalMidpoint(bool quadratic, double midpointY)
	{
		var points = new List<Point>();
		var coarsePoints = new List<Point>();
		Flatten(points, 1);
		Flatten(coarsePoints, 5);

		Assert.Equal(Point.Zero, points.First());
		Assert.Equal(new Point(20, 0), points.Last());
		Assert.Equal(10, points[points.Count / 2].X, 10);
		Assert.Equal(midpointY, points[points.Count / 2].Y, 10);
		Assert.True(points.Count > coarsePoints.Count);
		Assert.All(points, point =>
		{
			Assert.InRange(point.X, 0, 20);
			Assert.InRange(point.Y, 0, midpointY);
		});

		void Flatten(List<Point> destination, double tolerance)
		{
			if (quadratic)
				GeometryHelper.FlattenQuadraticBezier(destination, Point.Zero, new Point(10, 10), new Point(20, 0), tolerance);
			else
				GeometryHelper.FlattenCubicBezier(destination, Point.Zero, new Point(0, 10), new Point(20, 10), new Point(20, 0), tolerance);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FlattenCoincidentBezierPointsAppendsFiniteEndpoints(bool quadratic)
	{
		var point = new Point(3, -4);
		var sentinel = new Point(-100, 200);
		var points = new List<Point> { sentinel };

		if (quadratic)
			GeometryHelper.FlattenQuadraticBezier(points, point, point, point, 1);
		else
			GeometryHelper.FlattenCubicBezier(points, point, point, point, point, 1);

		Assert.Equal(new[] { sentinel, point, point }, points);
	}

	[Theory]
	[InlineData(false, -1)]
	[InlineData(true, 1)]
	public void FlattenSemicircleHonorsSweepDirection(bool counterclockwise, int ySign)
	{
		var points = new List<Point>();

		GeometryHelper.FlattenArc(points, new Point(-10, 0), new Point(10, 0), 10, 10, 0, false, counterclockwise, 1);

		Assert.Equal(-10, points.First().X, 8);
		Assert.Equal(10, points.Last().X, 8);
		Assert.Equal(0, points.First().Y, 8);
		Assert.Equal(0, points.Last().Y, 8);
		Assert.Contains(points, point => point.Y * ySign > 9);
		Assert.All(points, point =>
		{
			Assert.InRange(point.Y * ySign, -0.00000001, 10.00000001);
			Assert.Equal(100, point.X * point.X + point.Y * point.Y, 8);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FlattenLargeArcTakesLongerRouteBetweenSameEndpoints(bool counterclockwise)
	{
		var small = new List<Point>();
		var large = new List<Point>();
		GeometryHelper.FlattenArc(small, new Point(10, 0), new Point(0, 10), 10, 10, 0, false, counterclockwise, 1);
		GeometryHelper.FlattenArc(large, new Point(10, 0), new Point(0, 10), 10, 10, 0, true, counterclockwise, 1);

		Assert.True(Length(large) > 2 * Length(small));
		Assert.Equal(small.First().X, large.First().X, 8);
		Assert.Equal(small.First().Y, large.First().Y, 8);
		Assert.Equal(small.Last().X, large.Last().X, 8);
		Assert.Equal(small.Last().Y, large.Last().Y, 8);

		static double Length(List<Point> points) => points.Zip(points.Skip(1), (a, b) => a.Distance(b)).Sum();
	}
}
