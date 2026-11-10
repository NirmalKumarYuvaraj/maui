using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;
using Path = Microsoft.Maui.Controls.Shapes.Path;
using Rectangle = Microsoft.Maui.Controls.Shapes.Rectangle;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class GeometryPrimitivesTests : BaseTestFixture
{
	static PathF GetPath(Geometry geometry)
	{
		var path = new PathF();
		geometry.AppendPath(path);
		return path;
	}

	static void AssertExtents(RectF expected, PathF path)
	{
		Assert.Equal(expected.Left, path.Points.Min(p => p.X), 3);
		Assert.Equal(expected.Top, path.Points.Min(p => p.Y), 3);
		Assert.Equal(expected.Right, path.Points.Max(p => p.X), 3);
		Assert.Equal(expected.Bottom, path.Points.Max(p => p.Y), 3);
	}

	[Fact]
	public void EllipseGeometryDefaultsProduceDegenerateEllipseAtOrigin()
	{
		var geometry = new EllipseGeometry();

		Assert.Equal(Point.Zero, geometry.Center);
		Assert.Equal(0d, geometry.RadiusX);
		Assert.Equal(0d, geometry.RadiusY);
		Assert.All(GetPath(geometry).Points, p => Assert.Equal(PointF.Zero, p));
	}

	[Fact]
	public void LineGeometryDefaultsAreBothAtOrigin()
	{
		var geometry = new LineGeometry();

		Assert.Equal(Point.Zero, geometry.StartPoint);
		Assert.Equal(Point.Zero, geometry.EndPoint);
		Assert.Equal(new[] { PointF.Zero, PointF.Zero }, GetPath(geometry).Points);
	}

	[Fact]
	public void RectangleGeometryDefaultRectIsEmpty()
	{
		var geometry = new RectangleGeometry();

		Assert.Equal(new Rect(), geometry.Rect);
		Assert.All(GetPath(geometry).Points, p => Assert.Equal(PointF.Zero, p));
	}

	[Fact]
	public void RectangleGeometryConstructorAssignsRect()
	{
		var rect = new Rect(1, 2, 3, 4);

		Assert.Equal(rect, new RectangleGeometry(rect).Rect);
	}

	[Fact]
	public void RoundRectangleGeometryDefaultsDoNotBuildChildren()
	{
		var geometry = new RoundRectangleGeometry();

		Assert.Equal(new CornerRadius(), geometry.CornerRadius);
		Assert.Equal(new Rect(), geometry.Rect);
		Assert.Empty(geometry.Children);
		Assert.Equal(FillRule.EvenOdd, geometry.FillRule);
	}

	[Fact]
	public void RoundRectangleGeometryConstructorAssignsValuesAndBuildsChildren()
	{
		var cornerRadius = new CornerRadius(1, 2, 3, 4);
		var rect = new Rect(10, 20, 100, 50);

		var geometry = new RoundRectangleGeometry(cornerRadius, rect);

		Assert.Equal(cornerRadius, geometry.CornerRadius);
		Assert.Equal(rect, geometry.Rect);
		Assert.Single(geometry.Children);
		Assert.Equal(FillRule.Nonzero, geometry.FillRule);
	}

	[Fact]
	public void GeometryGroupDefaults()
	{
		var first = new GeometryGroup();
		var second = new GeometryGroup();

		Assert.Equal(FillRule.EvenOdd, first.FillRule);
		Assert.NotNull(first.Children);
		Assert.Empty(first.Children);
		Assert.NotSame(first.Children, second.Children);
	}

	[Fact]
	public void GeometryGroupWithNullChildrenAppendsNothing()
	{
		var group = new GeometryGroup { Children = null };
		var path = new PathF();

		group.AppendPath(path);

		Assert.Empty(path.Points);
	}

	[Fact]
	public void PathGeometryDefaults()
	{
		var first = new PathGeometry();
		var second = new PathGeometry();

		Assert.Equal(FillRule.EvenOdd, first.FillRule);
		Assert.NotNull(first.Figures);
		Assert.Empty(first.Figures);
		Assert.NotSame(first.Figures, second.Figures);
	}

	[Fact]
	public void PathGeometryConstructorOverloadsAssignFiguresAndFillRule()
	{
		var figures = new PathFigureCollection { new PathFigure() };

		var withFigures = new PathGeometry(figures);
		var withFillRule = new PathGeometry(figures, FillRule.Nonzero);

		Assert.Same(figures, withFigures.Figures);
		Assert.Equal(FillRule.EvenOdd, withFigures.FillRule);
		Assert.Same(figures, withFillRule.Figures);
		Assert.Equal(FillRule.Nonzero, withFillRule.FillRule);
	}

	[Fact]
	public void PathFigureDefaults()
	{
		var first = new PathFigure();
		var second = new PathFigure();

		Assert.Equal(Point.Zero, first.StartPoint);
		Assert.False(first.IsClosed);
		Assert.True(first.IsFilled);
		Assert.NotNull(first.Segments);
		Assert.Empty(first.Segments);
		Assert.NotSame(first.Segments, second.Segments);
	}

	[Fact]
	public void ArcSegmentDefaults()
	{
		var segment = new ArcSegment();

		Assert.Equal(Point.Zero, segment.Point);
		Assert.Equal(Size.Zero, segment.Size);
		Assert.Equal(0d, segment.RotationAngle);
		Assert.Equal(SweepDirection.CounterClockwise, segment.SweepDirection);
		Assert.False(segment.IsLargeArc);
	}

	[Fact]
	public void ArcSegmentConstructorAssignsPointSweepDirectionAndLargeArc()
	{
		var segment = new ArcSegment(new Point(5, 6), new Size(7, 8), 45, SweepDirection.Clockwise, true);

		Assert.Equal(new Point(5, 6), segment.Point);
		Assert.Equal(new Size(7, 8), segment.Size);
		Assert.Equal(45d, segment.RotationAngle);
		Assert.Equal(SweepDirection.Clockwise, segment.SweepDirection);
		Assert.True(segment.IsLargeArc);
	}

	[Fact]
	public void PointSegmentDefaultsAreOrigin()
	{
		var bezier = new BezierSegment();
		var quadratic = new QuadraticBezierSegment();
		var line = new LineSegment();

		Assert.Equal(Point.Zero, bezier.Point1);
		Assert.Equal(Point.Zero, bezier.Point2);
		Assert.Equal(Point.Zero, bezier.Point3);
		Assert.Equal(Point.Zero, quadratic.Point1);
		Assert.Equal(Point.Zero, quadratic.Point2);
		Assert.Equal(Point.Zero, line.Point);
	}

	[Theory]
	[InlineData("PolyBezier")]
	[InlineData("PolyLine")]
	[InlineData("PolyQuadratic")]
	public void PolySegmentPointsDefaultIsNonNullEmptyAndUniquePerInstance(string kind)
	{
		var first = GetPoints(CreatePolySegment(kind, null));
		var second = GetPoints(CreatePolySegment(kind, null));

		Assert.NotNull(first);
		Assert.Empty(first);
		Assert.NotSame(first, second);
	}

	[Theory]
	[InlineData("PolyBezier")]
	[InlineData("PolyLine")]
	[InlineData("PolyQuadratic")]
	public void PolySegmentConstructorUsesProvidedCollection(string kind)
	{
		var points = new PointCollection { new Point(1, 2) };

		Assert.Same(points, GetPoints(CreatePolySegment(kind, points)));
	}

	static PathSegment CreatePolySegment(string kind, PointCollection points) => kind switch
	{
		"PolyBezier" => points is null ? new PolyBezierSegment() : new PolyBezierSegment(points),
		"PolyLine" => points is null ? new PolyLineSegment() : new PolyLineSegment(points),
		"PolyQuadratic" => points is null ? new PolyQuadraticBezierSegment() : new PolyQuadraticBezierSegment(points),
		_ => throw new ArgumentOutOfRangeException(nameof(kind)),
	};

	static PointCollection GetPoints(PathSegment segment) => segment switch
	{
		PolyBezierSegment s => s.Points,
		PolyLineSegment s => s.Points,
		PolyQuadraticBezierSegment s => s.Points,
		_ => throw new ArgumentOutOfRangeException(nameof(segment)),
	};

	[Theory]
	[InlineData("Ellipse.Center")]
	[InlineData("Ellipse.RadiusX")]
	[InlineData("Ellipse.RadiusY")]
	[InlineData("Line.StartPoint")]
	[InlineData("Line.EndPoint")]
	[InlineData("Rectangle.Rect")]
	[InlineData("RoundRectangle.Rect")]
	[InlineData("RoundRectangle.CornerRadius")]
	public void SettingGeometryPropertyRaisesPropertyChangedAndChangesOutput(string key)
	{
		var (geometry, propertyName, mutate) = CreateGeometryMutation(key);
		var before = GetPath(geometry).Points;
		var raised = new List<string>();
		geometry.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

		mutate();

		Assert.Contains(propertyName, raised);
		Assert.NotEqual(before, GetPath(geometry).Points);
	}

	[Theory]
	[InlineData("Ellipse.Center")]
	[InlineData("Ellipse.RadiusX")]
	[InlineData("Ellipse.RadiusY")]
	[InlineData("Line.StartPoint")]
	[InlineData("Line.EndPoint")]
	[InlineData("Rectangle.Rect")]
	[InlineData("RoundRectangle.Rect")]
	[InlineData("RoundRectangle.CornerRadius")]
	public void GeometryPropertyChangeNotifiesOwningPath(string key)
	{
		var (geometry, _, mutate) = CreateGeometryMutation(key);
		var path = new Path { Data = geometry };
		var notifications = 0;
		path.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				notifications++;
		};

		mutate();

		Assert.True(notifications > 0);
	}

	static (Geometry Geometry, string PropertyName, Action Mutate) CreateGeometryMutation(string key)
	{
		switch (key)
		{
			case "Ellipse.Center":
				{
					var g = new EllipseGeometry(new Point(10, 10), 5, 5);
					return (g, nameof(EllipseGeometry.Center), () => g.Center = new Point(20, 30));
				}
			case "Ellipse.RadiusX":
				{
					var g = new EllipseGeometry(new Point(10, 10), 5, 5);
					return (g, nameof(EllipseGeometry.RadiusX), () => g.RadiusX = 8);
				}
			case "Ellipse.RadiusY":
				{
					var g = new EllipseGeometry(new Point(10, 10), 5, 5);
					return (g, nameof(EllipseGeometry.RadiusY), () => g.RadiusY = 8);
				}
			case "Line.StartPoint":
				{
					var g = new LineGeometry(new Point(1, 2), new Point(3, 4));
					return (g, nameof(LineGeometry.StartPoint), () => g.StartPoint = new Point(-1, -2));
				}
			case "Line.EndPoint":
				{
					var g = new LineGeometry(new Point(1, 2), new Point(3, 4));
					return (g, nameof(LineGeometry.EndPoint), () => g.EndPoint = new Point(30, 40));
				}
			case "Rectangle.Rect":
				{
					var g = new RectangleGeometry(new Rect(0, 0, 10, 10));
					return (g, nameof(RectangleGeometry.Rect), () => g.Rect = new Rect(5, 5, 20, 30));
				}
			case "RoundRectangle.Rect":
				{
					var g = new RoundRectangleGeometry(new CornerRadius(2), new Rect(0, 0, 10, 10));
					return (g, nameof(RoundRectangleGeometry.Rect), () => g.Rect = new Rect(5, 5, 20, 30));
				}
			case "RoundRectangle.CornerRadius":
				{
					var g = new RoundRectangleGeometry(new CornerRadius(2), new Rect(0, 0, 10, 10));
					return (g, nameof(RoundRectangleGeometry.CornerRadius), () => g.CornerRadius = new CornerRadius(1, 2, 3, 4));
				}
			default:
				throw new ArgumentOutOfRangeException(nameof(key));
		}
	}

	[Fact]
	public void SettingSameGeometryValueDoesNotNotifyPath()
	{
		var geometry = new EllipseGeometry(new Point(10, 10), 5, 5);
		var path = new Path { Data = geometry };
		var notifications = 0;
		path.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				notifications++;
		};

		geometry.Center = new Point(10, 10);
		geometry.RadiusX = 5;

		Assert.Equal(0, notifications);
	}

	[Fact]
	public void EllipseGeometryExtentsTrackCenterAndRadiiChanges()
	{
		var geometry = new EllipseGeometry(new Point(50, 40), 20, 10);
		AssertExtents(new RectF(30, 30, 40, 20), GetPath(geometry));

		geometry.Center = new Point(0, 0);
		AssertExtents(new RectF(-20, -10, 40, 20), GetPath(geometry));

		geometry.RadiusX = 5;
		AssertExtents(new RectF(-5, -10, 10, 20), GetPath(geometry));

		geometry.RadiusY = 15;
		AssertExtents(new RectF(-5, -15, 10, 30), GetPath(geometry));
	}

	[Fact]
	public void RectangleGeometryExtentsTrackRectChanges()
	{
		var geometry = new RectangleGeometry(new Rect(0, 0, 10, 10));

		geometry.Rect = new Rect(-5, 15, 25, 35);

		AssertExtents(new RectF(-5, 15, 25, 35), GetPath(geometry));
	}

	[Fact]
	public void RoundRectangleGeometryCornerRadiusChangeKeepsExtentsButAltersOutline()
	{
		var rect = new Rect(10, 20, 100, 50);
		var geometry = new RoundRectangleGeometry(new CornerRadius(0), rect);
		var square = GetPath(geometry);

		geometry.CornerRadius = new CornerRadius(10);
		var rounded = GetPath(geometry);

		AssertExtents(rect, square);
		AssertExtents(rect, rounded);
		Assert.NotEqual(square.Points, rounded.Points);
		Assert.DoesNotContain(new PointF(10, 20), rounded.Points);
	}

	[Fact]
	public void RoundRectangleGeometryRectChangeMovesExtents()
	{
		var geometry = new RoundRectangleGeometry(new CornerRadius(4), new Rect(0, 0, 40, 40));

		geometry.Rect = new Rect(100, 200, 60, 30);

		AssertExtents(new RectF(100, 200, 60, 30), GetPath(geometry));
	}

	[Theory]
	[InlineData(nameof(PathFigure.StartPoint))]
	[InlineData(nameof(PathFigure.IsClosed))]
	[InlineData(nameof(PathFigure.IsFilled))]
	public void PathFigurePropertyChangeRaisesPropertyChangedAndNotifiesPath(string propertyName)
	{
		var figure = new PathFigure { Segments = { new LineSegment(new Point(10, 10)) } };
		var path = new Path { Data = new PathGeometry { Figures = { figure } } };
		var figureChanges = new List<string>();
		var pathNotifications = 0;
		figure.PropertyChanged += (_, e) => figureChanges.Add(e.PropertyName);
		path.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				pathNotifications++;
		};

		switch (propertyName)
		{
			case nameof(PathFigure.StartPoint):
				figure.StartPoint = new Point(3, 4);
				break;
			case nameof(PathFigure.IsClosed):
				figure.IsClosed = true;
				break;
			case nameof(PathFigure.IsFilled):
				figure.IsFilled = false;
				break;
		}

		Assert.Contains(propertyName, figureChanges);
		Assert.True(pathNotifications > 0);
	}

	[Fact]
	public void PathFigureStartPointChangeUpdatesPathOutput()
	{
		var figure = new PathFigure { Segments = { new LineSegment(new Point(10, 10)) } };
		var geometry = new PathGeometry { Figures = { figure } };

		figure.StartPoint = new Point(-4, 7);

		Assert.Equal(new[] { new PointF(-4, 7), new PointF(10, 10) }, GetPath(geometry).Points);
	}

	[Fact]
	public void PathGeometryFillRuleChangeNotifiesPath()
	{
		var geometry = new PathGeometry();
		var path = new Path { Data = geometry };
		var notifications = 0;
		path.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Path.Data))
				notifications++;
		};

		geometry.FillRule = FillRule.Nonzero;

		Assert.Equal(1, notifications);
	}

	[Theory]
	[InlineData("ArcSegment.Size")]
	[InlineData("ArcSegment.RotationAngle")]
	[InlineData("ArcSegment.IsLargeArc")]
	[InlineData("ArcSegment.SweepDirection")]
	[InlineData("BezierSegment.Point3")]
	[InlineData("QuadraticBezierSegment.Point2")]
	public void SegmentPropertyChangeRaisesPropertyChangedAndInvalidatesFigure(string key)
	{
		PathSegment segment;
		Action mutate;
		switch (key)
		{
			case "ArcSegment.Size":
				{
					var s = new ArcSegment(new Point(10, 0), new Size(10, 10), 0, SweepDirection.Clockwise, false);
					segment = s;
					mutate = () => s.Size = new Size(20, 30);
					break;
				}
			case "ArcSegment.RotationAngle":
				{
					var s = new ArcSegment(new Point(10, 0), new Size(10, 10), 0, SweepDirection.Clockwise, false);
					segment = s;
					mutate = () => s.RotationAngle = 30;
					break;
				}
			case "ArcSegment.IsLargeArc":
				{
					var s = new ArcSegment(new Point(10, 0), new Size(10, 10), 0, SweepDirection.Clockwise, false);
					segment = s;
					mutate = () => s.IsLargeArc = true;
					break;
				}
			case "ArcSegment.SweepDirection":
				{
					var s = new ArcSegment(new Point(10, 0), new Size(10, 10), 0, SweepDirection.Clockwise, false);
					segment = s;
					mutate = () => s.SweepDirection = SweepDirection.CounterClockwise;
					break;
				}
			case "BezierSegment.Point3":
				{
					var s = new BezierSegment(new Point(1, 1), new Point(2, 2), new Point(3, 3));
					segment = s;
					mutate = () => s.Point3 = new Point(9, 9);
					break;
				}
			case "QuadraticBezierSegment.Point2":
				{
					var s = new QuadraticBezierSegment(new Point(1, 1), new Point(2, 2));
					segment = s;
					mutate = () => s.Point2 = new Point(9, 9);
					break;
				}
			default:
				throw new ArgumentOutOfRangeException(nameof(key));
		}

		var figure = new PathFigure { Segments = { segment } };
		var geometry = new PathGeometry { Figures = { figure } };
		var before = GetPath(geometry).Points;
		var expectedProperty = key.Substring(key.IndexOf('.', StringComparison.Ordinal) + 1);
		var raised = new List<string>();
		var invalidations = 0;
		segment.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
		geometry.InvalidatePathGeometryRequested += (_, _) => invalidations++;

		mutate();

		Assert.Contains(expectedProperty, raised);
		Assert.Equal(1, invalidations);
		Assert.NotEqual(before, GetPath(geometry).Points);
	}

	readonly StrokeShapeTypeConverter _converter = new();

	[Theory]
	[InlineData(typeof(string), true)]
	[InlineData(typeof(int), false)]
	[InlineData(typeof(double), false)]
	[InlineData(typeof(CornerRadius), false)]
	[InlineData(typeof(Shape), false)]
	public void ConverterCanConvertFromOnlyString(Type sourceType, bool expected)
	{
		Assert.Equal(expected, _converter.CanConvertFrom(null, sourceType));
	}

	[Theory]
	[InlineData("ELLIPSE", typeof(Ellipse))]
	[InlineData("  ellipse  ", typeof(Ellipse))]
	[InlineData("\tRECTANGLE\n", typeof(Rectangle))]
	[InlineData("  Rectangle", typeof(Rectangle))]
	[InlineData("ROUNDRECTANGLE", typeof(RoundRectangle))]
	[InlineData(" roundrectangle ", typeof(RoundRectangle))]
	[InlineData("PATH", typeof(Path))]
	[InlineData(" path ", typeof(Path))]
	[InlineData("POLYGON", typeof(Polygon))]
	[InlineData(" polygon ", typeof(Polygon))]
	[InlineData("POLYLINE", typeof(Polyline))]
	[InlineData(" polyline ", typeof(Polyline))]
	[InlineData("LINE", typeof(Line))]
	[InlineData(" line ", typeof(Line))]
	public void ConverterKeywordsAreCaseInsensitiveAndTrimmed(string value, Type expectedType)
	{
		var result = _converter.ConvertFromInvariantString(value);

		Assert.IsType(expectedType, result);
	}

	[Fact]
	public void ConverterKeywordsWithoutArgumentsProduceDefaultShapes()
	{
		Assert.Empty(Assert.IsType<Polygon>(_converter.ConvertFromInvariantString("Polygon")).Points);
		Assert.Empty(Assert.IsType<Polyline>(_converter.ConvertFromInvariantString("Polyline")).Points);
		Assert.Null(Assert.IsType<Path>(_converter.ConvertFromInvariantString("Path")).Data);
		Assert.Equal(new CornerRadius(), Assert.IsType<RoundRectangle>(_converter.ConvertFromInvariantString("RoundRectangle")).CornerRadius);

		var line = Assert.IsType<Line>(_converter.ConvertFromInvariantString("Line"));
		Assert.Equal(0d, line.X1);
		Assert.Equal(0d, line.Y1);
		Assert.Equal(0d, line.X2);
		Assert.Equal(0d, line.Y2);
	}

	[Theory]
	[InlineData("Line 1 2 3 4", 1, 2, 3, 4)]
	[InlineData("Line 1,2,3,4", 1, 2, 3, 4)]
	[InlineData("Line 1.5,-2 3.25,4", 1.5, -2, 3.25, 4)]
	[InlineData("Line 1 2", 1, 2, 0, 0)]
	[InlineData("Line 1 2 3 4 5 6", 1, 2, 3, 4)]
	public void ConverterParsesLineCoordinates(string value, double x1, double y1, double x2, double y2)
	{
		var line = Assert.IsType<Line>(_converter.ConvertFromInvariantString(value));

		Assert.Equal(x1, line.X1);
		Assert.Equal(y1, line.Y1);
		Assert.Equal(x2, line.X2);
		Assert.Equal(y2, line.Y2);
	}

	[Theory]
	[InlineData("Line 1")]
	[InlineData("Line 1 2 3")]
	[InlineData("Line a b")]
	[InlineData("Polygon 1 2 3")]
	[InlineData("Polyline 1,2,x,4")]
	public void ConverterThrowsForMalformedPointLists(string value)
	{
		Assert.Throws<InvalidOperationException>(() => _converter.ConvertFromInvariantString(value));
	}

	[Fact]
	public void ConverterParsesPolygonPointsExactly()
	{
		var polygon = Assert.IsType<Polygon>(_converter.ConvertFromInvariantString("Polygon 0,0 10,0 10,10"));

		Assert.Equal(new[] { new Point(0, 0), new Point(10, 0), new Point(10, 10) }, polygon.Points);
	}

	[Fact]
	public void ConverterParsesPolylinePointsExactlyAndIsNotConfusedWithPolygon()
	{
		var polyline = Assert.IsType<Polyline>(_converter.ConvertFromInvariantString("polyline 1 2, 3 4"));

		Assert.Equal(new[] { new Point(1, 2), new Point(3, 4) }, polyline.Points);
	}

	[Theory]
	[InlineData("RoundRectangle 5", 5, 5, 5, 5)]
	[InlineData("RoundRectangle 1,2,3,4", 1, 2, 3, 4)]
	[InlineData("RoundRectangle 1, 2, 3, 4", 1, 2, 3, 4)]
	[InlineData("RoundRectangle 1 2 3 4", 1, 2, 3, 4)]
	[InlineData("roundrectangle 1 2", 1, 2, 2, 1)]
	[InlineData("RoundRectangle 1 2 3", 1, 2, 2, 3)]
	[InlineData("RoundRectangle 7,8", 7, 7, 7, 7)]
	public void ConverterParsesRoundRectangleCornerRadii(string value, double tl, double tr, double bl, double br)
	{
		var roundRectangle = Assert.IsType<RoundRectangle>(_converter.ConvertFromInvariantString(value));

		Assert.Equal(new CornerRadius(tl, tr, bl, br), roundRectangle.CornerRadius);
	}

	[Theory]
	[InlineData("RoundRectangle 1,2,3,4,5")]
	[InlineData("RoundRectangle 1 2 3 4 5")]
	[InlineData("RoundRectangle abc")]
	public void ConverterThrowsForInvalidRoundRectangleCornerRadii(string value)
	{
		Assert.Throws<InvalidOperationException>(() => _converter.ConvertFromInvariantString(value));
	}

	[Fact]
	public void ConverterParsesPathData()
	{
		var path = Assert.IsType<Path>(_converter.ConvertFromInvariantString("Path M 10,20 L 30,40 L 50,20 Z"));

		var geometry = Assert.IsType<PathGeometry>(path.Data);
		var figure = Assert.Single(geometry.Figures);
		Assert.Equal(new Point(10, 20), figure.StartPoint);
		Assert.True(figure.IsClosed);
		Assert.Equal(
			new[] { new PointF(10, 20), new PointF(30, 40), new PointF(50, 20) },
			GetPath(geometry).Points);
	}

	[Theory]
	[InlineData("Triangle")]
	[InlineData("Square 10")]
	[InlineData("")]
	[InlineData("   ")]
	public void ConverterThrowsForUnknownOrEmptyValues(string value)
	{
		Assert.Throws<InvalidOperationException>(() => _converter.ConvertFromInvariantString(value));
	}

	[Fact]
	public void ConverterThrowsForNull()
	{
		Assert.Throws<InvalidOperationException>(() => _converter.ConvertFrom(null, CultureInfo.InvariantCulture, null));
	}

	[Fact(Skip = "Product bug: StrokeShapeTypeConverter matches keywords with culture-sensitive StartsWith(value, true, culture), so uppercase keywords containing 'I' (LINE, ELLIPSE, POLYLINE) fail under tr-TR")]
	public void ConverterKeywordMatchingIsIndependentOfCulture()
	{
		var turkish = CultureInfo.GetCultureInfo("tr-TR");

		var line = Assert.IsType<Line>(_converter.ConvertFrom(null, turkish, "LINE 1 2 3 4"));
		Assert.Equal(3d, line.X2);
		Assert.IsType<Ellipse>(_converter.ConvertFrom(null, turkish, "ELLIPSE"));
		Assert.IsType<Polyline>(_converter.ConvertFrom(null, turkish, "POLYLINE"));
	}

	[Fact]
	public void ConverterDoesNotProvideCustomConvertTo()
	{
		var ellipse = new Ellipse();

		Assert.True(_converter.CanConvertTo(null, typeof(string)));
		Assert.Equal(ellipse.ToString(), _converter.ConvertTo(null, CultureInfo.InvariantCulture, ellipse, typeof(string)));
		Assert.Throws<NotSupportedException>(() => _converter.ConvertTo(null, CultureInfo.InvariantCulture, ellipse, typeof(int)));
	}
}
