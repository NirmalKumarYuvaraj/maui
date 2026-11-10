using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class ShapeTests : BaseTestFixture
{
	public static IEnumerable<object[]> AllShapeKinds() =>
		new[] { "Ellipse", "Line", "Rectangle", "RoundRectangle", "Polygon", "Polyline", "Path" }
			.Select(k => new object[] { k });

	static Shape CreateShape(string kind) => kind switch
	{
		"Ellipse" => new Ellipse(),
		"Line" => new Line(),
		"Rectangle" => new Rectangle(),
		"RoundRectangle" => new RoundRectangle(),
		"Polygon" => new Polygon(),
		"Polyline" => new Polyline(),
		"Path" => new Path(),
		_ => throw new ArgumentOutOfRangeException(nameof(kind)),
	};

	static Polygon CreateRectPolygon(double x = 0, double y = 0, double width = 100, double height = 50) =>
		new Polygon(new PointCollection
		{
			new Point(x, y),
			new Point(x + width, y),
			new Point(x + width, y + height),
			new Point(x, y + height),
		});

	static void AssertBounds(PathF path, double x, double y, double width, double height)
	{
		var bounds = path.GetBoundsByFlattening(1);
		Assert.Equal(x, bounds.X, 3);
		Assert.Equal(y, bounds.Y, 3);
		Assert.Equal(width, bounds.Width, 3);
		Assert.Equal(height, bounds.Height, 3);
	}

	static void AssertSize(Size actual, double width, double height)
	{
		Assert.Equal(width, actual.Width, 3);
		Assert.Equal(height, actual.Height, 3);
	}

	static int Version(Shape shape) => ((IVersionedShape)shape).Version;

	[Theory]
	[MemberData(nameof(AllShapeKinds))]
	public void BaseBindablePropertiesHaveExpectedDefaults(string kind)
	{
		var shape = CreateShape(kind);

		Assert.Null(shape.Fill);
		Assert.Null(shape.Stroke);
		Assert.Equal(1.0, shape.StrokeThickness);
		Assert.NotNull(shape.StrokeDashArray);
		Assert.Empty(shape.StrokeDashArray);
		Assert.Empty(shape.StrokeDashPattern);
		Assert.Equal(0.0, shape.StrokeDashOffset);
		Assert.Equal(PenLineCap.Flat, shape.StrokeLineCap);
		Assert.Equal(PenLineJoin.Miter, shape.StrokeLineJoin);
		Assert.Equal(10.0, shape.StrokeMiterLimit);
	}

	[Theory]
	[InlineData("Ellipse", Stretch.Fill)]
	[InlineData("Rectangle", Stretch.Fill)]
	[InlineData("RoundRectangle", Stretch.Fill)]
	[InlineData("Line", Stretch.None)]
	[InlineData("Polygon", Stretch.None)]
	[InlineData("Polyline", Stretch.None)]
	[InlineData("Path", Stretch.None)]
	public void AspectDefaultDependsOnShapeKind(string kind, Stretch expected)
	{
		Assert.Equal(expected, CreateShape(kind).Aspect);
	}

	[Fact]
	public void DefaultStrokeDashArrayIsUniquePerInstance()
	{
		var first = new Line();
		var second = new Line();

		Assert.NotSame(first.StrokeDashArray, second.StrokeDashArray);
		first.StrokeDashArray.Add(2);
		Assert.Empty(second.StrokeDashArray);
	}

	[Fact]
	public void ConcreteShapePropertiesHaveExpectedDefaults()
	{
		var line = new Line();
		Assert.Equal(0.0, line.X1);
		Assert.Equal(0.0, line.Y1);
		Assert.Equal(0.0, line.X2);
		Assert.Equal(0.0, line.Y2);

		var rectangle = new Rectangle();
		Assert.Equal(0.0, rectangle.RadiusX);
		Assert.Equal(0.0, rectangle.RadiusY);

		Assert.Equal(new CornerRadius(), new RoundRectangle().CornerRadius);

		var polygon = new Polygon();
		Assert.NotNull(polygon.Points);
		Assert.Empty(polygon.Points);
		Assert.Equal(FillRule.EvenOdd, polygon.FillRule);

		var polyline = new Polyline();
		Assert.NotNull(polyline.Points);
		Assert.Empty(polyline.Points);
		Assert.Equal(FillRule.EvenOdd, polyline.FillRule);

		var path = new Path();
		Assert.Null(path.Data);
		Assert.Null(path.RenderTransform);
	}

	[Fact]
	public void DefaultPointCollectionsAreUniquePerInstance()
	{
		Assert.NotSame(new Polygon().Points, new Polygon().Points);
		Assert.NotSame(new Polyline().Points, new Polyline().Points);
	}

	[Fact]
	public void LineConstructorSetsEndpoints()
	{
		var line = new Line(1, 2, 3, 4);

		Assert.Equal(1.0, line.X1);
		Assert.Equal(2.0, line.Y1);
		Assert.Equal(3.0, line.X2);
		Assert.Equal(4.0, line.Y2);
	}

	[Theory]
	[InlineData(Stretch.None, PathAspect.None)]
	[InlineData(Stretch.Fill, PathAspect.Stretch)]
	[InlineData(Stretch.Uniform, PathAspect.AspectFit)]
	[InlineData(Stretch.UniformToFill, PathAspect.AspectFill)]
	[InlineData((Stretch)42, PathAspect.None)]
	public void ShapeViewAspectMapsFromStretch(Stretch stretch, PathAspect expected)
	{
		var shape = new Line { Aspect = stretch };

		Assert.Equal(expected, ((IShapeView)shape).Aspect);
	}

	[Theory]
	[InlineData(PenLineCap.Flat, LineCap.Butt)]
	[InlineData(PenLineCap.Round, LineCap.Round)]
	[InlineData(PenLineCap.Square, LineCap.Square)]
	[InlineData((PenLineCap)42, LineCap.Butt)]
	public void StrokeLineCapMapsFromPenLineCap(PenLineCap cap, LineCap expected)
	{
		var shape = new Line { StrokeLineCap = cap };

		Assert.Equal(expected, ((IStroke)shape).StrokeLineCap);
	}

	[Theory]
	[InlineData(PenLineJoin.Miter, LineJoin.Miter)]
	[InlineData(PenLineJoin.Bevel, LineJoin.Bevel)]
	[InlineData(PenLineJoin.Round, LineJoin.Round)]
	[InlineData((PenLineJoin)42, LineJoin.Round)]
	public void StrokeLineJoinMapsFromPenLineJoin(PenLineJoin join, LineJoin expected)
	{
		var shape = new Line { StrokeLineJoin = join };

		Assert.Equal(expected, ((IStroke)shape).StrokeLineJoin);
	}

	[Fact]
	public void StrokeDashPatternConvertsToFloatsAndTracksCollection()
	{
		var shape = new Line { StrokeDashArray = new DoubleCollection { 1, 2.5, 0.1 } };

		Assert.Equal(new[] { 1f, 2.5f, 0.1f }, shape.StrokeDashPattern);
		Assert.Equal(new[] { 1f, 2.5f, 0.1f }, ((IStroke)shape).StrokeDashPattern);

		shape.StrokeDashArray.Add(4);

		Assert.Equal(new[] { 1f, 2.5f, 0.1f, 4f }, shape.StrokeDashPattern);
	}

	[Fact]
	public void StrokeOffsetAndMiterLimitAreProjectedAsFloats()
	{
		var shape = new Line { StrokeDashOffset = 1.5, StrokeMiterLimit = 3.25 };
		var stroke = (IStroke)shape;

		Assert.Equal(1.5f, stroke.StrokeDashOffset);
		Assert.Equal(3.25f, stroke.StrokeMiterLimit);
	}

	[Fact]
	public void ShapeViewProjectsBrushesAndItself()
	{
		var shape = new Ellipse
		{
			Fill = new SolidColorBrush(Colors.Red),
			Stroke = new SolidColorBrush(Colors.Blue),
			StrokeThickness = 3,
		};

		var fill = Assert.IsType<SolidPaint>(((IShapeView)shape).Fill);
		var stroke = Assert.IsType<SolidPaint>(((IStroke)shape).Stroke);

		Assert.Equal(Colors.Red, fill.Color);
		Assert.Equal(Colors.Blue, stroke.Color);
		Assert.Equal(3.0, ((IStroke)shape).StrokeThickness);
		Assert.Same(shape, ((IShapeView)shape).Shape);
	}

	[Fact]
	public void ShapeViewBrushesAreNullWhenUnset()
	{
		var shape = new Ellipse();

		Assert.Null(((IShapeView)shape).Fill);
		Assert.Null(((IStroke)shape).Stroke);
	}

	[Theory]
	[InlineData(nameof(Shape.Fill))]
	[InlineData(nameof(Shape.Stroke))]
	[InlineData(nameof(Shape.StrokeThickness))]
	[InlineData(nameof(Shape.StrokeDashArray))]
	[InlineData(nameof(Shape.StrokeDashOffset))]
	[InlineData(nameof(Shape.StrokeLineCap))]
	[InlineData(nameof(Shape.StrokeLineJoin))]
	[InlineData(nameof(Shape.StrokeMiterLimit))]
	[InlineData(nameof(Shape.Aspect))]
	[InlineData(nameof(Line.X2))]
	public void VersionIncrementsOnEveryPropertyChange(string property)
	{
		var shape = new Line();
		var before = Version(shape);

		switch (property)
		{
			case nameof(Shape.Fill):
				shape.Fill = new SolidColorBrush(Colors.Red);
				break;
			case nameof(Shape.Stroke):
				shape.Stroke = new SolidColorBrush(Colors.Red);
				break;
			case nameof(Shape.StrokeThickness):
				shape.StrokeThickness = 5;
				break;
			case nameof(Shape.StrokeDashArray):
				shape.StrokeDashArray = new DoubleCollection { 1 };
				break;
			case nameof(Shape.StrokeDashOffset):
				shape.StrokeDashOffset = 2;
				break;
			case nameof(Shape.StrokeLineCap):
				shape.StrokeLineCap = PenLineCap.Round;
				break;
			case nameof(Shape.StrokeLineJoin):
				shape.StrokeLineJoin = PenLineJoin.Bevel;
				break;
			case nameof(Shape.StrokeMiterLimit):
				shape.StrokeMiterLimit = 4;
				break;
			case nameof(Shape.Aspect):
				shape.Aspect = Stretch.Uniform;
				break;
			case nameof(Line.X2):
				shape.SetValue(Line.X2Property, 12.0);
				break;
		}

		Assert.True(Version(shape) > before);
	}

	[Fact]
	public void VersionIsUpdatedBeforePropertyChangedIsRaised()
	{
		var shape = new Rectangle();
		var start = Version(shape);
		int? observed = null;
		shape.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Rectangle.RadiusX))
				observed = Version(shape);
		};

		shape.RadiusX = 4;

		Assert.NotNull(observed);
		Assert.True(observed > start);
		Assert.Equal(Version(shape), observed);
	}

	[Fact]
	public void VersionIncrementsWhenBrushIsMutated()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var shape = new Ellipse { Fill = brush };
		var before = Version(shape);

		brush.Color = Colors.Green;

		Assert.True(Version(shape) > before);
	}

	[Theory]
	[InlineData(nameof(Shape.Fill))]
	[InlineData(nameof(Shape.Stroke))]
	public void MutatingSolidColorBrushRaisesShapePropertyChanged(string property)
	{
		var brush = new SolidColorBrush(Colors.Red);
		var shape = new Ellipse();
		shape.SetValue(property == nameof(Shape.Fill) ? Shape.FillProperty : Shape.StrokeProperty, brush);
		var changes = new List<string>();
		shape.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		brush.Color = Colors.Green;

		Assert.Contains(property, changes);
	}

	[Theory]
	[InlineData(nameof(Shape.Fill))]
	[InlineData(nameof(Shape.Stroke))]
	public void MutatingGradientBrushRaisesShapePropertyChanged(string property)
	{
		var stop = new GradientStop(Colors.Red, 0);
		var brush = new LinearGradientBrush { GradientStops = new GradientStopCollection { stop } };
		var shape = new Ellipse();
		shape.SetValue(property == nameof(Shape.Fill) ? Shape.FillProperty : Shape.StrokeProperty, brush);
		var count = 0;
		shape.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == property)
				count++;
		};

		brush.GradientStops.Add(new GradientStop(Colors.Blue, 1));
		Assert.Equal(1, count);

		stop.Color = Colors.Yellow;
		Assert.Equal(2, count);

		brush.GradientStops = new GradientStopCollection();
		Assert.True(count >= 3);
	}

	[Theory]
	[InlineData(nameof(Shape.Fill))]
	[InlineData(nameof(Shape.Stroke))]
	public void ReplacedBrushNoLongerNotifiesShape(string property)
	{
		var bp = property == nameof(Shape.Fill) ? Shape.FillProperty : Shape.StrokeProperty;
		var oldBrush = new SolidColorBrush(Colors.Red);
		var newBrush = new SolidColorBrush(Colors.Blue);
		var shape = new Ellipse();
		shape.SetValue(bp, oldBrush);
		shape.SetValue(bp, newBrush);
		var count = 0;
		shape.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == property)
				count++;
		};

		oldBrush.Color = Colors.Green;
		Assert.Equal(0, count);

		newBrush.Color = Colors.Green;
		Assert.Equal(1, count);
	}

	[Theory]
	[InlineData(nameof(Shape.Fill))]
	[InlineData(nameof(Shape.Stroke))]
	public void ClearedBrushNoLongerNotifiesShape(string property)
	{
		var bp = property == nameof(Shape.Fill) ? Shape.FillProperty : Shape.StrokeProperty;
		var brush = new SolidColorBrush(Colors.Red);
		var shape = new Ellipse();
		shape.SetValue(bp, brush);
		shape.SetValue(bp, null);
		var count = 0;
		shape.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == property)
				count++;
		};

		brush.Color = Colors.Green;

		Assert.Equal(0, count);
	}

	[Fact]
	public void SameBrushOnFillAndStrokeNotifiesBoth()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var shape = new Ellipse { Fill = brush, Stroke = brush };
		var changes = new List<string>();
		shape.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		brush.Color = Colors.Green;

		Assert.Contains(nameof(Shape.Fill), changes);
		Assert.Contains(nameof(Shape.Stroke), changes);
	}

	[Fact]
	public void BindingContextPropagatesToFillAndStrokeBrushes()
	{
		var context = new object();
		var fill = new SolidColorBrush(Colors.Red);
		var stroke = new LinearGradientBrush();
		var shape = new Ellipse { Fill = fill, Stroke = stroke };

		shape.BindingContext = context;

		Assert.Same(context, fill.BindingContext);
		Assert.Same(context, stroke.BindingContext);
	}

	[Fact]
	public void BrushAssignedAfterBindingContextInheritsIt()
	{
		var context = new object();
		var shape = new Ellipse { BindingContext = context };
		var fill = new SolidColorBrush(Colors.Red);
		var stroke = new SolidColorBrush(Colors.Blue);

		shape.Fill = fill;
		shape.Stroke = stroke;

		Assert.Same(context, fill.BindingContext);
		Assert.Same(context, stroke.BindingContext);
	}

	[Fact]
	public void ReplacedBrushHasBindingContextCleared()
	{
		var context = new object();
		var oldFill = new SolidColorBrush(Colors.Red);
		var oldStroke = new SolidColorBrush(Colors.Blue);
		var shape = new Ellipse { Fill = oldFill, Stroke = oldStroke, BindingContext = context };

		shape.Fill = new SolidColorBrush(Colors.Green);
		shape.Stroke = null;

		Assert.Null(oldFill.BindingContext);
		Assert.Null(oldStroke.BindingContext);
		Assert.Same(context, shape.Fill.BindingContext);
	}

	[Fact]
	public void BindingOnBrushResolvesAgainstShapeBindingContext()
	{
		var brush = new SolidColorBrush();
		brush.SetBinding(SolidColorBrush.ColorProperty, new Binding("Color"));
		var shape = new Ellipse { Fill = brush };

		shape.BindingContext = new { Color = Colors.Purple };

		Assert.Equal(Colors.Purple, brush.Color);
	}

	[Fact]
	public void ImmutableBrushAssignedAfterBindingContextIsNotGivenContext()
	{
		var brush = new ImmutableBrush(Colors.Red);
		var shape = new Ellipse { BindingContext = new object() };

		shape.Fill = brush;
		shape.Stroke = brush;

		Assert.Null(brush.BindingContext);
		Assert.Same(brush, shape.Fill);
		Assert.Same(brush, shape.Stroke);
	}

	[Fact(Skip = "Product bug: Shape.PropagateBindingContextToBrush does not skip ImmutableBrush, so shared cached brushes (e.g. Brush.Red, Colors.X conversions) capture the shape's BindingContext")]
	public void ImmutableBrushDoesNotReceiveBindingContextWhenShapeContextChanges()
	{
		var brush = new ImmutableBrush(Colors.Red);
		var shape = new Ellipse { Fill = brush, Stroke = brush };

		shape.BindingContext = new object();

		Assert.Null(brush.BindingContext);
	}

	[Fact]
	public void ImmutableBrushCanBeReplacedAndCleared()
	{
		var shape = new Ellipse { Fill = Brush.Red, Stroke = Brush.Blue };

		shape.Fill = new SolidColorBrush(Colors.Green);
		shape.Stroke = null;

		Assert.Null(shape.Stroke);
		Assert.Equal(Colors.Green, ((SolidColorBrush)shape.Fill).Color);
	}

	[Theory]
	[InlineData("Line")]
	[InlineData("Ellipse")]
	[InlineData("Rectangle")]
	[InlineData("Path")]
	public void TogglingStrokeBetweenNullAndNonNullInvalidatesMeasure(string kind)
	{
		var shape = CreateShape(kind);
		var invalidations = 0;
		shape.MeasureInvalidated += (_, _) => invalidations++;

		shape.Stroke = new SolidColorBrush(Colors.Black);
		Assert.Equal(1, invalidations);

		shape.Stroke = null;
		Assert.Equal(2, invalidations);
	}

	[Theory]
	[InlineData("Line")]
	[InlineData("Polygon")]
	[InlineData("RoundRectangle")]
	public void ReplacingNonNullStrokeDoesNotInvalidateMeasure(string kind)
	{
		var shape = CreateShape(kind);
		shape.Stroke = new SolidColorBrush(Colors.Black);
		var invalidated = false;
		shape.MeasureInvalidated += (_, _) => invalidated = true;

		shape.Stroke = new SolidColorBrush(Colors.Red);
		shape.Stroke = Brush.Blue;

		Assert.False(invalidated);
	}

	[Fact]
	public void LineGetPathUsesEndpoints()
	{
		var path = new Line(1, 2, 30, 40).GetPath();

		Assert.Equal(new[] { new PointF(1, 2), new PointF(30, 40) }, path.Points);
		Assert.Equal(PathOperation.Move, path.GetSegmentType(0));
		Assert.Equal(PathOperation.Line, path.GetSegmentType(1));
		Assert.False(path.Closed);
	}

	[Fact]
	public void LineGetPathIgnoresStrokeAndSize()
	{
		var line = new Line(1, 2, 30, 40) { Stroke = Colors.Black, StrokeThickness = 6 };
		line.Layout(new Rect(0, 0, 200, 200));

		Assert.Equal(new[] { new PointF(1, 2), new PointF(30, 40) }, line.GetPath().Points);
	}

	[Theory]
	[InlineData(nameof(Line.X1), 9, 2, 30, 40)]
	[InlineData(nameof(Line.Y1), 1, 9, 30, 40)]
	[InlineData(nameof(Line.X2), 1, 2, 9, 40)]
	[InlineData(nameof(Line.Y2), 1, 2, 30, 9)]
	public void ChangingLineEndpointRaisesPropertyChangedAndUpdatesPath(string property, float x1, float y1, float x2, float y2)
	{
		var line = new Line(1, 2, 30, 40);
		var changes = new List<string>();
		line.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		var bp = property switch
		{
			nameof(Line.X1) => Line.X1Property,
			nameof(Line.Y1) => Line.Y1Property,
			nameof(Line.X2) => Line.X2Property,
			_ => Line.Y2Property,
		};
		line.SetValue(bp, 9.0);

		Assert.Equal(new[] { property }, changes);
		Assert.Equal(new[] { new PointF(x1, y1), new PointF(x2, y2) }, line.GetPath().Points);
	}

	[Fact]
	public void RectangleGetPathIsPlainRectangleWithoutRadius()
	{
		var rectangle = new Rectangle();
		rectangle.Layout(new Rect(0, 0, 40, 20));

		var path = rectangle.GetPath();

		AssertBounds(path, 0, 0, 40, 20);
		Assert.DoesNotContain(PathOperation.Cubic, path.SegmentTypes);
		Assert.True(path.Closed);
	}

	[Theory]
	[InlineData(5, 0)]
	[InlineData(0, 5)]
	[InlineData(3, 5)]
	public void RectangleRadiusProducesRoundedCorners(double radiusX, double radiusY)
	{
		var rectangle = new Rectangle { RadiusX = radiusX, RadiusY = radiusY };
		rectangle.Layout(new Rect(0, 0, 40, 20));

		var path = rectangle.GetPath();
		var expected = new PathF();
		expected.AppendRoundedRectangle(0, 0, 40, 20, 5);

		AssertBounds(path, 0, 0, 40, 20);
		Assert.Contains(PathOperation.Cubic, path.SegmentTypes);
		Assert.Equal(expected.Points, path.Points);
	}

	[Theory]
	[InlineData(nameof(Rectangle.RadiusX))]
	[InlineData(nameof(Rectangle.RadiusY))]
	public void ChangingRectangleRadiusRaisesPropertyChangedAndUpdatesPath(string property)
	{
		var rectangle = new Rectangle();
		rectangle.Layout(new Rect(0, 0, 40, 20));
		var before = rectangle.GetPath();
		var changes = new List<string>();
		rectangle.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		rectangle.SetValue(property == nameof(Rectangle.RadiusX) ? Rectangle.RadiusXProperty : Rectangle.RadiusYProperty, 4.0);

		Assert.Equal(new[] { property }, changes);
		Assert.NotEqual(before.Points, rectangle.GetPath().Points);
	}

	[Fact]
	public void EllipseGetPathIsSizedByArrangedBounds()
	{
		var ellipse = new Ellipse();
		ellipse.Layout(new Rect(5, 7, 30, 10));

		AssertBounds(ellipse.GetPath(), 0, 0, 30, 10);
	}

	[Theory]
	[InlineData("Ellipse")]
	[InlineData("Rectangle")]
	[InlineData("RoundRectangle")]
	public void UnarrangedShapeUsesFallbackSizeFromPathForBounds(string kind)
	{
		var shape = CreateShape(kind);

		AssertBounds(shape.GetPath(), 0, 0, 0, 0);

		var forBounds = ((IShape)shape).PathForBounds(new Rect(0, 0, 30, 10));

		AssertBounds(forBounds, 0, 0, 30, 10);
		AssertBounds(shape.GetPath(), 0, 0, 30, 10);
	}

	[Theory]
	[InlineData("Ellipse")]
	[InlineData("Rectangle")]
	[InlineData("RoundRectangle")]
	public void ArrangedSizeTakesPrecedenceOverFallbackSize(string kind)
	{
		var shape = CreateShape(kind);
		shape.Layout(new Rect(0, 0, 50, 40));

		((IShape)shape).PathForBounds(new Rect(0, 0, 30, 10));

		AssertBounds(shape.GetPath(), 0, 0, 50, 40);
	}

	[Fact]
	public void PathForBoundsWithStrokeInsetsEllipse()
	{
		var ellipse = new Ellipse { StrokeThickness = 4 };

		var path = ((IShapeWithStroke)ellipse).PathForBounds(new Rect(0, 0, 30, 20), includeStroke: true);

		AssertBounds(path, 2, 2, 26, 16);
	}

	[Fact]
	public void RoundRectangleGetPathIgnoresStroke()
	{
		var roundRectangle = new RoundRectangle
		{
			Stroke = Colors.Black,
			StrokeThickness = 4,
			CornerRadius = new CornerRadius(5),
		};
		roundRectangle.Layout(new Rect(0, 0, 40, 20));

		AssertBounds(roundRectangle.GetPath(), 0, 0, 40, 20);
		AssertBounds(((IRoundRectangle)roundRectangle).InnerPath(), 2, 2, 36, 16);
	}

	[Fact]
	public void RoundRectangleCornerRadiusChangeRaisesPropertyChangedAndUpdatesPath()
	{
		var roundRectangle = new RoundRectangle();
		roundRectangle.Layout(new Rect(0, 0, 40, 20));
		var before = roundRectangle.GetPath();
		var changes = new List<string>();
		roundRectangle.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		roundRectangle.CornerRadius = new CornerRadius(1, 2, 3, 4);

		var expected = new PathF();
		expected.AppendRoundedRectangle(0, 0, 40, 20, 1, 2, 3, 4);
		var after = roundRectangle.GetPath();

		Assert.Equal(new[] { nameof(RoundRectangle.CornerRadius) }, changes);
		Assert.NotEqual(before.Points, after.Points);
		Assert.Equal(expected.Points, after.Points);
	}

	[Fact]
	public void RoundRectangleInnerPathReducesCornerRadiusByStroke()
	{
		var roundRectangle = new RoundRectangle
		{
			Stroke = Colors.Black,
			StrokeThickness = 2,
			CornerRadius = new CornerRadius(5, 1, 3, 10),
		};
		roundRectangle.Layout(new Rect(0, 0, 40, 30));

		var expected = new PathF();
		expected.AppendRoundedRectangle(1, 1, 38, 28, 3, 0, 1, 8);

		Assert.Equal(expected.Points, ((IRoundRectangle)roundRectangle).InnerPath().Points);
	}

	[Fact]
	public void PolygonGetPathIsClosed()
	{
		var path = CreateRectPolygon(0, 0, 10, 5).GetPath();

		Assert.True(path.Closed);
		Assert.Equal(new[] { new PointF(0, 0), new PointF(10, 0), new PointF(10, 5), new PointF(0, 5) }, path.Points);
	}

	[Fact]
	public void PolylineGetPathIsOpen()
	{
		var polyline = new Polyline(new PointCollection { new Point(0, 0), new Point(10, 0), new Point(10, 5) });

		var path = polyline.GetPath();

		Assert.False(path.Closed);
		Assert.Equal(new[] { new PointF(0, 0), new PointF(10, 0), new PointF(10, 5) }, path.Points);
	}

	[Theory]
	[InlineData("Polygon")]
	[InlineData("Polyline")]
	public void PointShapesWithNoOrNullPointsProduceEmptyPath(string kind)
	{
		var shape = CreateShape(kind);
		var bp = kind == "Polygon" ? Polygon.PointsProperty : Polyline.PointsProperty;

		Assert.Empty(shape.GetPath().Points);

		shape.SetValue(bp, null);

		Assert.Empty(shape.GetPath().Points);
	}

	[Theory]
	[InlineData("Polygon")]
	[InlineData("Polyline")]
	public void ReplacingPointsRaisesPropertyChangedAndUpdatesPath(string kind)
	{
		var shape = CreateShape(kind);
		var bp = kind == "Polygon" ? Polygon.PointsProperty : Polyline.PointsProperty;
		var changes = new List<string>();
		shape.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		shape.SetValue(bp, new PointCollection { new Point(1, 1), new Point(4, 5) });

		Assert.Equal(new[] { bp.PropertyName }, changes);
		Assert.Equal(new[] { new PointF(1, 1), new PointF(4, 5) }, shape.GetPath().Points);
	}

	[Theory]
	[InlineData("Polygon")]
	[InlineData("Polyline")]
	public void MutatingPointsIsReflectedInGetPath(string kind)
	{
		var shape = CreateShape(kind);
		var points = (PointCollection)shape.GetValue(kind == "Polygon" ? Polygon.PointsProperty : Polyline.PointsProperty);

		points.Add(new Point(1, 2));
		points.Add(new Point(3, 4));
		Assert.Equal(new[] { new PointF(1, 2), new PointF(3, 4) }, shape.GetPath().Points);

		points[1] = new Point(7, 8);
		Assert.Equal(new[] { new PointF(1, 2), new PointF(7, 8) }, shape.GetPath().Points);
	}

	[Theory(Skip = "Product bug: Polygon/Polyline do not subscribe to Points.CollectionChanged, so in-place edits never raise PropertyChanged(Points) and the handler is not asked to redraw")]
	[InlineData("Polygon")]
	[InlineData("Polyline")]
	public void MutatingPointsRaisesPropertyChanged(string kind)
	{
		var shape = CreateShape(kind);
		var bp = kind == "Polygon" ? Polygon.PointsProperty : Polyline.PointsProperty;
		var points = (PointCollection)shape.GetValue(bp);
		var changes = new List<string>();
		shape.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		points.Add(new Point(1, 2));

		Assert.Contains(bp.PropertyName, changes);
	}

	[Theory]
	[InlineData("Polygon")]
	[InlineData("Polyline")]
	public void FillRuleChangeRaisesPropertyChangedAndIncrementsVersion(string kind)
	{
		var shape = CreateShape(kind);
		var bp = kind == "Polygon" ? Polygon.FillRuleProperty : Polyline.FillRuleProperty;
		var before = Version(shape);
		var changes = new List<string>();
		shape.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		shape.SetValue(bp, FillRule.Nonzero);

		Assert.Equal(new[] { nameof(Polygon.FillRule) }, changes);
		Assert.Equal(FillRule.Nonzero, shape.GetValue(bp));
		Assert.True(Version(shape) > before);
	}

	[Fact]
	public void PathGetPathAppendsDataGeometry()
	{
		var path = new Path(new RectangleGeometry(new Rect(10, 20, 30, 40)));

		AssertBounds(path.GetPath(), 10, 20, 30, 40);
	}

	[Fact]
	public void PathGetPathDoesNotApplyRenderTransform()
	{
		var path = new Path(new RectangleGeometry(new Rect(10, 20, 30, 40)))
		{
			RenderTransform = new TranslateTransform(100, 200),
		};

		AssertBounds(path.GetPath(), 10, 20, 30, 40);
	}

	[Fact]
	public void SettingRenderTransformRaisesPropertyChangedAndIncrementsVersion()
	{
		var path = new Path();
		var before = Version(path);
		var changes = new List<string>();
		path.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		path.RenderTransform = new ScaleTransform(2, 2);

		Assert.Equal(new[] { nameof(Path.RenderTransform) }, changes);
		Assert.True(Version(path) > before);
	}

	[Theory]
	[InlineData(0, 0, 100, 50, 100, 50)]
	[InlineData(10, 20, 100, 50, 110, 70)]
	public void MeasureWithAspectNoneUsesPathExtent(double x, double y, double w, double h, double expectedWidth, double expectedHeight)
	{
		var polygon = CreateRectPolygon(x, y, w, h);

		AssertSize(polygon.Measure(double.PositiveInfinity, double.PositiveInfinity), expectedWidth, expectedHeight);
		AssertSize(polygon.Measure(20, 20), expectedWidth, expectedHeight);
	}

	[Fact]
	public void MeasureAddsStrokeThicknessOnlyWhenStrokeIsSet()
	{
		var polygon = CreateRectPolygon();
		polygon.StrokeThickness = 4;

		AssertSize(polygon.Measure(double.PositiveInfinity, double.PositiveInfinity), 100, 50);

		polygon.Stroke = Colors.Black;

		AssertSize(polygon.Measure(double.PositiveInfinity, double.PositiveInfinity), 104, 54);
	}

	[Theory]
	[InlineData(200, 300, 0, 200, 300)]
	[InlineData(200, 300, 4, 200, 300)]
	[InlineData(double.PositiveInfinity, double.PositiveInfinity, 0, 100, 50)]
	[InlineData(double.PositiveInfinity, double.PositiveInfinity, 4, 104, 54)]
	[InlineData(200, double.PositiveInfinity, 0, 200, 50)]
	public void MeasureWithAspectFillUsesConstraints(double widthConstraint, double heightConstraint, double strokeThickness, double expectedWidth, double expectedHeight)
	{
		var polygon = CreateRectPolygon();
		polygon.Aspect = Stretch.Fill;
		if (strokeThickness > 0)
		{
			polygon.Stroke = Colors.Black;
			polygon.StrokeThickness = strokeThickness;
		}

		AssertSize(polygon.Measure(widthConstraint, heightConstraint), expectedWidth, expectedHeight);
	}

	[Theory]
	[InlineData(double.PositiveInfinity, double.PositiveInfinity, 80, 40)]
	[InlineData(200, 300, 80, 40)]
	public void MeasureWithAspectFillPrefersSizeRequests(double widthConstraint, double heightConstraint, double widthRequest, double heightRequest)
	{
		var polygon = CreateRectPolygon();
		polygon.Aspect = Stretch.Fill;
		polygon.WidthRequest = widthRequest;
		polygon.HeightRequest = heightRequest;

		AssertSize(polygon.Measure(widthConstraint, heightConstraint), widthRequest, heightRequest);
	}

	[Fact]
	public void MeasureWithAspectFillMixesRequestAndConstraint()
	{
		var polygon = CreateRectPolygon();
		polygon.Aspect = Stretch.Fill;
		polygon.WidthRequest = 80;

		AssertSize(polygon.Measure(200, 300), 80, 300);
	}

	[Theory]
	[InlineData(200, 300, 0, 200, 100)]
	[InlineData(300, 100, 0, 200, 100)]
	[InlineData(50, 300, 0, 50, 25)]
	[InlineData(200, 300, 4, 200, 102)]
	public void MeasureWithAspectUniformScalesByMinimumRatio(double widthConstraint, double heightConstraint, double strokeThickness, double expectedWidth, double expectedHeight)
	{
		var polygon = CreateRectPolygon();
		polygon.Aspect = Stretch.Uniform;
		if (strokeThickness > 0)
		{
			polygon.Stroke = Colors.Black;
			polygon.StrokeThickness = strokeThickness;
		}

		AssertSize(polygon.Measure(widthConstraint, heightConstraint), expectedWidth, expectedHeight);
	}

	[Theory(Skip = "Product bug: Shape.MeasureOverride maps infinite scale factors to 0 before the Uniform case, so its IsInfinity(minScale) guard is dead code and a Uniform shape measured with an unbounded constraint collapses to 0x0")]
	[InlineData(double.PositiveInfinity, double.PositiveInfinity, 100, 50)]
	[InlineData(200, double.PositiveInfinity, 200, 100)]
	public void MeasureWithAspectUniformHandlesInfiniteConstraints(double widthConstraint, double heightConstraint, double expectedWidth, double expectedHeight)
	{
		var polygon = CreateRectPolygon();
		polygon.Aspect = Stretch.Uniform;

		AssertSize(polygon.Measure(widthConstraint, heightConstraint), expectedWidth, expectedHeight);
	}

	[Theory]
	[InlineData(200, 300, 600, 300)]
	[InlineData(300, 100, 300, 150)]
	[InlineData(200, double.PositiveInfinity, 200, 100)]
	[InlineData(double.PositiveInfinity, 150, 300, 150)]
	[InlineData(double.PositiveInfinity, double.PositiveInfinity, 100, 50)]
	public void MeasureWithAspectUniformToFillScalesByMaximumRatio(double widthConstraint, double heightConstraint, double expectedWidth, double expectedHeight)
	{
		var polygon = CreateRectPolygon();
		polygon.Aspect = Stretch.UniformToFill;

		AssertSize(polygon.Measure(widthConstraint, heightConstraint), expectedWidth, expectedHeight);
	}

	[Fact]
	public void MeasureWithAspectUniformToFillAccountsForStroke()
	{
		var polygon = CreateRectPolygon();
		polygon.Aspect = Stretch.UniformToFill;
		polygon.Stroke = Colors.Black;
		polygon.StrokeThickness = 4;

		// Constraints shrink to 196x296, scale = max(1.96, 5.92) = 5.92
		AssertSize(polygon.Measure(200, 300), 596, 300);
	}

	[Theory]
	[InlineData("Rectangle")]
	[InlineData("Ellipse")]
	[InlineData("RoundRectangle")]
	public void UnarrangedFillShapesMeasureToConstraintsOrRequests(string kind)
	{
		var shape = CreateShape(kind);

		AssertSize(shape.Measure(120, 80), 120, 80);
		AssertSize(shape.Measure(double.PositiveInfinity, double.PositiveInfinity), 0, 0);

		shape.WidthRequest = 50;
		shape.HeightRequest = 30;

		AssertSize(shape.Measure(double.PositiveInfinity, double.PositiveInfinity), 50, 30);
	}

	[Fact]
	public void LineMeasureIncludesMarginAndStroke()
	{
		var line = new Line(0, 0, 100, 50) { Margin = new Thickness(1, 2, 3, 4) };

		AssertSize(line.Measure(double.PositiveInfinity, double.PositiveInfinity), 104, 56);

		line.Stroke = Colors.Black;
		line.StrokeThickness = 2;

		AssertSize(line.Measure(double.PositiveInfinity, double.PositiveInfinity), 106, 58);
	}

	[Fact]
	public void PolylineMeasureIncludesMargin()
	{
		var polyline = new Polyline(new PointCollection { new Point(0, 0), new Point(100, 0), new Point(100, 50) })
		{
			Margin = new Thickness(5),
		};

		AssertSize(polyline.Measure(double.PositiveInfinity, double.PositiveInfinity), 110, 60);
	}

	[Fact]
	public void PathMeasureIncludesGeometryOffsetAndMargin()
	{
		var path = new Path(new RectangleGeometry(new Rect(10, 20, 30, 40)))
		{
			Margin = new Thickness(1, 2, 3, 4),
		};

		AssertSize(path.Measure(double.PositiveInfinity, double.PositiveInfinity), 44, 66);
	}

	[Fact]
	public void PathMeasureWithAspectFillIncludesMargin()
	{
		var path = new Path(new RectangleGeometry(new Rect(10, 20, 30, 40)))
		{
			Aspect = Stretch.Fill,
			Margin = new Thickness(5),
		};

		AssertSize(path.Measure(200, 100), 210, 110);
	}

	[Fact]
	public void PathWithoutDataMeasuresToZero()
	{
		AssertSize(new Path().Measure(double.PositiveInfinity, double.PositiveInfinity), 0, 0);
	}

	[Fact]
	public void MeasureReflectsUpdatedGeometry()
	{
		var line = new Line(0, 0, 10, 10);
		AssertSize(line.Measure(double.PositiveInfinity, double.PositiveInfinity), 10, 10);

		line.X2 = 40;

		AssertSize(line.Measure(double.PositiveInfinity, double.PositiveInfinity), 40, 10);
	}
}
