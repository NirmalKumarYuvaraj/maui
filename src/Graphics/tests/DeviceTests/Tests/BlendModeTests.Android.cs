using System;
using Android.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Xunit;
using AColor = Android.Graphics.Color;

namespace Microsoft.Maui.Graphics.DeviceTests;

[Trait("Category", "BlendMode")]
public class BlendModeTests
{
	[Theory]
	[InlineData(BlendMode.Multiply, 85, 0, 85, 192)]
	[InlineData(BlendMode.Difference, 170, 0, 170, 192)]
	[InlineData(BlendMode.PlusDarker, 127, 0, 127, 255)]
	[InlineData(BlendMode.Clear, 0, 0, 0, 0)]
	[InlineData(BlendMode.Copy, 0, 0, 255, 128)]
	[InlineData(BlendMode.DestinationIn, 255, 0, 0, 64)]
	public void BlendUsesTransparentDestinationAndPreservesOutsideGeometry(BlendMode mode, int red, int green, int blue, int alpha)
	{
		using var context = new PlatformBitmapExportContext(12, 12);
		var canvas = context.Canvas;
		canvas.FillColor = new Color(1f, 0f, 0f, 128 / 255f);
		canvas.FillRectangle(0, 0, 12, 12);
		canvas.BlendMode = mode;
		canvas.FillColor = new Color(0f, 0f, 1f, 128 / 255f);
		canvas.FillRectangle(4, 4, 4, 4);

		AssertPixel(context.Bitmap.GetPixel(5, 5), red, green, blue, alpha);
		AssertPixel(context.Bitmap.GetPixel(1, 1), 255, 0, 0, 128);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FallbackPreservesClipTransformAndSavedBlendMode(bool export)
	{
		using var context = new PlatformBitmapExportContext(16, 16);
		using var target = export ? null : Bitmap.CreateBitmap(16, 16, Bitmap.Config.Argb8888);
		using var nativeCanvas = export ? null : new Canvas(target);
		using var platformCanvas = export ? null : new PlatformCanvas { Canvas = nativeCanvas };
		ICanvas canvas = export ? context.Canvas : platformCanvas;
		var bitmap = export ? context.Bitmap : target;

		canvas.FillColor = Colors.Red;
		canvas.FillRectangle(0, 0, 16, 16);
		canvas.BlendMode = BlendMode.Copy;
		canvas.SaveState();
		canvas.Translate(4, 4);
		canvas.ClipRectangle(0, 0, 4, 4);
		canvas.BlendMode = BlendMode.PlusDarker;
		canvas.FillColor = Colors.Blue;
		canvas.FillRectangle(0, 0, 16, 16);
		AssertPixel(bitmap.GetPixel(5, 5), 0, 0, 0, 255);
		AssertPixel(bitmap.GetPixel(1, 1), 255, 0, 0, 255);
		canvas.RestoreState();
		canvas.FillColor = Colors.Transparent;
		canvas.FillRectangle(0, 0, 2, 2);
		AssertPixel(bitmap.GetPixel(1, 1), 0, 0, 0, 0);
		canvas.ResetState();
		canvas.FillColor = Colors.Lime;
		canvas.FillRectangle(0, 0, 2, 2);
		AssertPixel(bitmap.GetPixel(1, 1), 0, 255, 0, 255);
	}

	[Theory]
	[InlineData(BlendMode.Multiply)]
	[InlineData(BlendMode.PlusDarker)]
	public void ImagesUseBlendMode(BlendMode mode)
	{
		using var context = new PlatformBitmapExportContext(8, 8);
		using var image = new PlatformImage(Bitmap.CreateBitmap(2, 2, Bitmap.Config.Argb8888));
		image.PlatformRepresentation.EraseColor(AColor.Blue);
		context.Canvas.FillColor = Colors.Red;
		context.Canvas.FillRectangle(0, 0, 8, 8);
		context.Canvas.BlendMode = mode;
		context.Canvas.DrawImage(image, 2, 2, 4, 4);
		AssertPixel(context.Bitmap.GetPixel(3, 3), 0, 0, 0, 255);
		AssertPixel(context.Bitmap.GetPixel(0, 0), 255, 0, 0, 255);
	}

	static void AssertPixel(int pixel, int red, int green, int blue, int alpha)
	{
		var actual = new AColor(pixel);
		Assert.InRange(Math.Abs(actual.A - alpha), 0, 2);
		Assert.InRange(Math.Abs(actual.R - red), 0, 2);
		Assert.InRange(Math.Abs(actual.G - green), 0, 2);
		Assert.InRange(Math.Abs(actual.B - blue), 0, 2);
	}
}
