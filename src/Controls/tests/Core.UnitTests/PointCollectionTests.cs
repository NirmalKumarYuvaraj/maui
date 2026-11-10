using System;
using System.Globalization;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Shapes.UnitTests
{
	public class PointCollectionTests
	{
		PointCollectionConverter _pointCollectionConverter;


		public PointCollectionTests()
		{
			_pointCollectionConverter = new PointCollectionConverter();
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("  ,  ")]
		public void EmptyInputReturnsIndependentEmptyCollections(string input)
		{
			var first = Assert.IsType<PointCollection>(_pointCollectionConverter.ConvertFromInvariantString(input));
			var second = Assert.IsType<PointCollection>(_pointCollectionConverter.ConvertFromInvariantString(input));

			Assert.Empty(first);
			Assert.NotSame(first, second);
			first.Add(new Point(1, 2));
			Assert.Empty(second);
		}

		[Fact]
		public void ConvertsSignedDecimalCoordinatesWithInvariantCulture()
		{
			var points = Assert.IsType<PointCollection>(_pointCollectionConverter.ConvertFrom(null, CultureInfo.GetCultureInfo("fr-FR"), " -1.5, +2.25  , 3.75 -4.5 "));

			Assert.Equal(new[] { new Point(-1.5, 2.25), new Point(3.75, -4.5) }, points);
			var serialized = _pointCollectionConverter.ConvertToInvariantString(points);
			Assert.Equal(points, Assert.IsType<PointCollection>(_pointCollectionConverter.ConvertFromInvariantString(serialized)));
		}

		[Theory]
		[InlineData("1")]
		[InlineData("1 2 3")]
		[InlineData("1 invalid")]
		public void InvalidOrUnpairedCoordinateThrows(string input)
		{
			Assert.Throws<InvalidOperationException>(() => _pointCollectionConverter.ConvertFromInvariantString(input));
		}

		[Fact]
		public void ConvertsArrayByCopyingPoints()
		{
			var source = new[] { new Point(1, 2), new Point(3, 4) };

			var result = Assert.IsType<PointCollection>(_pointCollectionConverter.ConvertFrom(source));
			source[0] = new Point(10, 20);

			Assert.Equal(new[] { new Point(1, 2), new Point(3, 4) }, result);
		}

		[Fact]
		public void ConvertStringToPointCollectionTest()
		{
			PointCollection result = _pointCollectionConverter.ConvertFromInvariantString("0 48, 0 144, 96 150, 100 0, 192 0, 192 96, 50 96, 48 192, 150 200 144 48") as PointCollection;

			Assert.NotNull(result);
			Assert.Equal(10, result.Count);
		}
	}
}