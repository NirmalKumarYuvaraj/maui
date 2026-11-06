using System;
using System.Collections.Generic;
using Microsoft.Maui.Graphics.Platform;
using SkiaSharp;
using Xunit;

namespace Microsoft.Maui.Graphics.Tests;

public class BlendModeCompositorTests
{
	public static IEnumerable<object[]> SoftwareModes()
	{
		foreach (var pair in new[]
		{
			(BlendMode.Multiply, SKBlendMode.Multiply),
			(BlendMode.ColorDodge, SKBlendMode.ColorDodge),
			(BlendMode.ColorBurn, SKBlendMode.ColorBurn),
			(BlendMode.SoftLight, SKBlendMode.SoftLight),
			(BlendMode.HardLight, SKBlendMode.HardLight),
			(BlendMode.Difference, SKBlendMode.Difference),
			(BlendMode.Exclusion, SKBlendMode.Exclusion),
			(BlendMode.Hue, SKBlendMode.Hue),
			(BlendMode.Saturation, SKBlendMode.Saturation),
			(BlendMode.Color, SKBlendMode.Color),
			(BlendMode.Luminosity, SKBlendMode.Luminosity),
		})
		{
			foreach (byte alpha in new byte[] { 0, 51, 128, 255 })
				yield return new object[] { pair.Item1, pair.Item2, alpha };
		}
	}

	[Theory]
	[MemberData(nameof(SoftwareModes))]
	public void SoftwareBlendMatchesSkia(BlendMode mode, SKBlendMode nativeMode, byte alpha)
	{
		foreach (var source in new[] { new SKColor(153, 77, 230, alpha), new SKColor(0, 255, 0, alpha), new SKColor(255, 255, 255, alpha) })
		{
			foreach (var destination in new[] { new SKColor(204, 102, 51, 128), new SKColor(0, 0, 0, 255), new SKColor(255, 255, 255, 255), SKColors.Transparent })
			{
				using var bitmap = new SKBitmap(1, 1, SKColorType.Bgra8888, SKAlphaType.Premul);
				using var canvas = new SKCanvas(bitmap);
				canvas.Clear(destination);
				using var paint = new SKPaint { Color = source, BlendMode = nativeMode };
				canvas.DrawRect(0, 0, 1, 1, paint);

				var actual = new SKColor((uint)BlendModeCompositor.Blend((int)(uint)source, (int)(uint)destination, mode));
				AssertClose(bitmap.GetPixel(0, 0), actual);
			}
		}
	}

	[Theory]
	// Premultiplied RGBA reference pixels rendered with CoreGraphics plusDarker.
	[InlineData(51, 51, 72, 35, 56, 102)]
	[InlineData(128, 128, 178, 88, 140, 255)]
	[InlineData(128, 255, 153, 12, 38, 255)]
	[InlineData(255, 128, 127, 0, 128, 255)]
	[InlineData(255, 255, 102, 0, 26, 255)]
	public void PlusDarkerMatchesCoreGraphics(byte sourceAlpha, byte destinationAlpha, int red, int green, int blue, int alpha)
	{
		var source = new SKColor(153, 77, 230, sourceAlpha);
		var destination = new SKColor(204, 102, 51, destinationAlpha);
		var actual = new SKColor((uint)BlendModeCompositor.Blend((int)(uint)source, (int)(uint)destination, BlendMode.PlusDarker));
		Assert.InRange(Math.Abs(actual.Alpha - alpha), 0, 2);
		Assert.InRange(Math.Abs(actual.Red * actual.Alpha / 255f - red), 0, 2);
		Assert.InRange(Math.Abs(actual.Green * actual.Alpha / 255f - green), 0, 2);
		Assert.InRange(Math.Abs(actual.Blue * actual.Alpha / 255f - blue), 0, 2);
	}

	static void AssertClose(SKColor expected, SKColor actual)
	{
		Assert.InRange(Math.Abs(expected.Alpha - actual.Alpha), 0, 1);
		if (expected.Alpha == 0)
			return;
		Assert.InRange(Math.Abs(expected.Red - actual.Red), 0, 3);
		Assert.InRange(Math.Abs(expected.Green - actual.Green), 0, 3);
		Assert.InRange(Math.Abs(expected.Blue - actual.Blue), 0, 3);
	}

	[Fact]
	public void PlusDarkerAppliesGeometryCoverageAfterBlending()
	{
		var source = new SKColor(26, 26, 26, 128);
		var destination = new SKColor(26, 26, 26, 255);
		var actual = new SKColor((uint)BlendModeCompositor.Blend((int)(uint)source, (int)(uint)destination, BlendMode.PlusDarker, 128 / 255f));
		AssertClose(new SKColor(13, 13, 13, 255), actual);
	}
}
