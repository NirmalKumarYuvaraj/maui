using System;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class PathGeometryConverterTests : BaseTestFixture
{
	private readonly PathGeometryConverter _converter = new();

	[Theory]
	[InlineData("M10,20 L30,40", FillRule.EvenOdd)]
	[InlineData("F0 M10,20 L30,40", FillRule.EvenOdd)]
	[InlineData("F1 M10,20 L30,40", FillRule.Nonzero)]
	public void ConvertMarkupPreservesFillRuleAndCoordinates(string markup, FillRule fillRule)
	{
		var geometry = Assert.IsType<PathGeometry>(_converter.ConvertFromInvariantString(markup));

		Assert.Equal(fillRule, geometry.FillRule);
		var figure = Assert.Single(geometry.Figures);
		Assert.Equal(new Point(10, 20), figure.StartPoint);
		Assert.Equal(new Point(30, 40), Assert.IsType<LineSegment>(Assert.Single(figure.Segments)).Point);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" \t\r\n ")]
	public void ConvertEmptyMarkupReturnsEmptyGeometry(string markup)
	{
		Assert.Empty(Assert.IsType<PathGeometry>(_converter.ConvertFromInvariantString(markup)).Figures);
	}

	[Fact]
	public void ConvertMarkupUsesInvariantNumbersRegardlessOfRequestedCulture()
	{
		var geometry = Assert.IsType<PathGeometry>(_converter.ConvertFrom(null, CultureInfo.GetCultureInfo("fr-FR"), "M1.5,2.5 l3.25,-4.75"));

		var figure = Assert.Single(geometry.Figures);
		Assert.Equal(new Point(1.5, 2.5), figure.StartPoint);
		Assert.Equal(new Point(4.75, -2.25), Assert.IsType<LineSegment>(Assert.Single(figure.Segments)).Point);
	}

	[Fact]
	public void ReusingConverterDoesNotShareFiguresOrParserState()
	{
		var first = Assert.IsType<PathGeometry>(_converter.ConvertFromInvariantString("M10,20 l5,6"));
		var second = Assert.IsType<PathGeometry>(_converter.ConvertFromInvariantString("m1,2 l3,4"));

		Assert.NotSame(first.Figures, second.Figures);
		Assert.Equal(new Point(1, 2), second.Figures[0].StartPoint);
		Assert.Equal(new Point(4, 6), Assert.IsType<LineSegment>(Assert.Single(second.Figures[0].Segments)).Point);
		second.Figures.Clear();
		Assert.Single(first.Figures);
	}

	[Fact]
	public void ConvertNullTest()
	{
		var result = _converter.ConvertFromInvariantString(null);
		var pathGeometry = Assert.IsType<PathGeometry>(result);
		Assert.Empty(pathGeometry.Figures);
	}
}