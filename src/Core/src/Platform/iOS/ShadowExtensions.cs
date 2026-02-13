using System.Diagnostics;
using CoreAnimation;
using CoreGraphics;
using CoreImage;
using Foundation;
using Microsoft.Maui.Graphics;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Platform
{
	internal static class ShadowExtensions
	{
		public static void SetShadow(this UIView platformView, IShadow? shadow)
		{
			Debug.WriteLine($"[Shadow] SetShadow(UIView) called - shadow is null: {shadow == null}, paint is null: {shadow?.Paint == null}");
			
			if (shadow == null || shadow.Paint == null)
				return;

			Debug.WriteLine($"[Shadow] SetShadow(UIView) - Paint type: {shadow.Paint.GetType().Name}");
			
			var layer = platformView.Layer;
			layer?.SetShadow(shadow);
		}

		public static void SetShadow(this CALayer layer, IShadow? shadow)
		{
			Debug.WriteLine($"[Shadow] SetShadow(CALayer) called - shadow is null: {shadow == null}");
			
			if (shadow?.Paint is null)
				return;

			Debug.WriteLine($"[Shadow] SetShadow(CALayer) - Paint type: {shadow.Paint.GetType().Name}, IsGradient: {shadow.Paint is GradientPaint}");

			// For gradient paints, we can't use native shadow - need custom rendering
			// The gradient shadow support is handled by WrapperView for views that use it
			// For views without WrapperView, we fall back to the first color
			if (shadow.Paint?.ToColor() is not { } paintColor)
			{
				Debug.WriteLine("[Shadow] SetShadow(CALayer) - ToColor() returned null, aborting");
				return;
			}

			var radius = shadow.Radius;
			var opacity = shadow.Opacity;
			var color = paintColor.ToPlatform();

			var offset = new CGSize(shadow.Offset.X, shadow.Offset.Y);

			Debug.WriteLine($"[Shadow] SetShadow(CALayer) - Applying native shadow: color={paintColor}, radius={radius}, opacity={opacity}, offset=({offset.Width},{offset.Height})");

			layer.ShadowColor = color.CGColor;
			layer.ShadowOpacity = opacity;
			layer.ShadowRadius = radius / 2;
			layer.ShadowOffset = offset;

			layer.SetNeedsDisplay();
		}

		/// <summary>
		/// Creates a gradient shadow layer using Core Image blur effects.
		/// This method is used by WrapperView to support gradient brushes for shadows.
		/// </summary>
		internal static CALayer? CreateGradientShadowLayer(IShadow shadow, CGRect bounds)
		{
			Debug.WriteLine($"[Shadow] CreateGradientShadowLayer called - bounds: {bounds.Width}x{bounds.Height}");
			
			if (shadow.Paint is not GradientPaint gradientPaint)
			{
				Debug.WriteLine($"[Shadow] CreateGradientShadowLayer - Paint is not GradientPaint, it's {shadow.Paint?.GetType().Name}");
				return null;
			}

			Debug.WriteLine($"[Shadow] CreateGradientShadowLayer - GradientPaint type: {gradientPaint.GetType().Name}");

			var radius = shadow.Radius;
			var opacity = shadow.Opacity;
			var offset = shadow.Offset;

			Debug.WriteLine($"[Shadow] CreateGradientShadowLayer - radius={radius}, opacity={opacity}, offset=({offset.X},{offset.Y})");

			// Create the gradient layer
			var gradientLayer = gradientPaint.CreateCALayer(bounds);
			if (gradientLayer is null)
			{
				Debug.WriteLine("[Shadow] CreateGradientShadowLayer - CreateCALayer returned null!");
				return null;
			}

			Debug.WriteLine($"[Shadow] CreateGradientShadowLayer - Created gradient layer: {gradientLayer.GetType().Name}");

			// Apply opacity to the gradient colors
			if (gradientLayer is CAGradientLayer caGradientLayer && caGradientLayer.Colors is not null)
			{
				Debug.WriteLine($"[Shadow] CreateGradientShadowLayer - Processing {caGradientLayer.Colors.Length} gradient colors");
				
				var colors = new CGColor[caGradientLayer.Colors.Length];
				for (int i = 0; i < caGradientLayer.Colors.Length; i++)
				{
					var cgColor = caGradientLayer.Colors[i];
					var components = cgColor.Components;
					if (components is not null && components.Length >= 3)
					{
						colors[i] = new CGColor(
							components[0],
							components[1],
							components[2],
							(components.Length > 3 ? components[3] : 1.0f) * opacity);
						Debug.WriteLine($"[Shadow] CreateGradientShadowLayer - Color[{i}]: R={components[0]:F2}, G={components[1]:F2}, B={components[2]:F2}, A={(components.Length > 3 ? components[3] : 1.0f) * opacity:F2}");
					}
					else
					{
						colors[i] = cgColor;
						Debug.WriteLine($"[Shadow] CreateGradientShadowLayer - Color[{i}]: Using original (components null or < 3)");
					}
				}
				caGradientLayer.Colors = colors;
			}
			else
			{
				Debug.WriteLine("[Shadow] CreateGradientShadowLayer - Not a CAGradientLayer or Colors is null");
			}

			// To create a blurred gradient shadow, we render the gradient to an image,
			// apply a Gaussian blur, and use the result as the layer contents
			Debug.WriteLine("[Shadow] CreateGradientShadowLayer - Calling CreateBlurredGradientShadowLayer");
			var blurredShadowLayer = CreateBlurredGradientShadowLayer(gradientLayer, bounds, radius, offset);
			
			Debug.WriteLine($"[Shadow] CreateGradientShadowLayer - Result: {(blurredShadowLayer != null ? "SUCCESS" : "NULL")}");
			return blurredShadowLayer;
		}

		static StaticCALayer? CreateBlurredGradientShadowLayer(CALayer gradientLayer, CGRect bounds, float blurRadius, Point offset)
		{
			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - bounds: {bounds.Width}x{bounds.Height}, blurRadius: {blurRadius}");
			
			// Native iOS shadow spread is approximately 2.5x the shadow radius
			// We need space around the gradient for the blur effect
			var blurPadding = blurRadius * 2.5f;
			var expandedSize = new CGSize(
				bounds.Width + blurPadding * 2,
				bounds.Height + blurPadding * 2);
			
			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - expandedSize: {expandedSize.Width}x{expandedSize.Height}");
			
			var expandedBounds = new CGRect(0, 0, expandedSize.Width, expandedSize.Height);
			
			// Position the gradient within the expanded bounds (centered)
			gradientLayer.Frame = new CGRect(blurPadding, blurPadding, bounds.Width, bounds.Height);
			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - gradientLayer.Frame: {gradientLayer.Frame}");

			// Create container layer for rendering
			var containerLayer = new CALayer
			{
				Frame = expandedBounds,
				BackgroundColor = UIColor.Clear.CGColor
			};
			containerLayer.AddSublayer(gradientLayer);

			// Render to image
			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - Starting image render, scale: {UIScreen.MainScreen.Scale}");
			UIGraphics.BeginImageContextWithOptions(expandedSize, false, UIScreen.MainScreen.Scale);
			var context = UIGraphics.GetCurrentContext();
			if (context is null)
			{
				Debug.WriteLine("[Shadow] CreateBlurredGradientShadowLayer - FAILED: context is null");
				UIGraphics.EndImageContext();
				return null;
			}

			containerLayer.RenderInContext(context);
			var renderedImage = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();

			if (renderedImage?.CGImage is null)
			{
				Debug.WriteLine("[Shadow] CreateBlurredGradientShadowLayer - FAILED: renderedImage or CGImage is null");
				return null;
			}

			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - Rendered image size: {renderedImage.Size.Width}x{renderedImage.Size.Height}");

			var scale = UIScreen.MainScreen.Scale;
			
			// Apply Gaussian blur using Core Image
			// Note: CIGaussianBlur radius is in pixels, but we specified in points
			var ciImage = new CIImage(renderedImage.CGImage);
			var blurFilter = CIFilter.FromName("CIGaussianBlur");
			if (blurFilter is null)
			{
				Debug.WriteLine("[Shadow] CreateBlurredGradientShadowLayer - FAILED: CIGaussianBlur filter not found");
				return null;
			}

			// Scale the blur radius for the actual pixel density
			var scaledBlurRadius = blurRadius * scale;
			blurFilter.SetValueForKey(ciImage, CIFilterInputKey.Image);
			blurFilter.SetValueForKey(NSNumber.FromFloat((float)scaledBlurRadius), CIFilterInputKey.Radius);

			var outputImage = blurFilter.OutputImage;
			if (outputImage is null)
			{
				Debug.WriteLine("[Shadow] CreateBlurredGradientShadowLayer - FAILED: blur filter output is null");
				return null;
			}

			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - Blur output extent: {outputImage.Extent}");

			// The blur filter extends the image. We need to crop it back to our expected size.
			// The original image was expandedSize * scale pixels
			var expectedPixelRect = new CGRect(
				outputImage.Extent.X + (outputImage.Extent.Width - expandedSize.Width * scale) / 2,
				outputImage.Extent.Y + (outputImage.Extent.Height - expandedSize.Height * scale) / 2,
				expandedSize.Width * scale,
				expandedSize.Height * scale);
			
			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - Cropping to: {expectedPixelRect}");

			// Render the blurred image at the cropped size
			var ciContext = CIContext.Create();
			var blurredCGImage = ciContext.CreateCGImage(outputImage, expectedPixelRect);
			if (blurredCGImage is null)
			{
				Debug.WriteLine("[Shadow] CreateBlurredGradientShadowLayer - FAILED: CreateCGImage returned null");
				return null;
			}

			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - Blurred image size: {blurredCGImage.Width}x{blurredCGImage.Height}");

			// Create the shadow layer with the blurred image
			var shadowLayer = new StaticCALayer
			{
				Contents = blurredCGImage,
				ContentsScale = scale,
				ContentsGravity = CALayer.GravityResize,
				// Position accounts for blur padding and user offset
				Frame = new CGRect(
					offset.X - blurPadding,
					offset.Y - blurPadding,
					expandedSize.Width,
					expandedSize.Height),
				MasksToBounds = false
			};

			Debug.WriteLine($"[Shadow] CreateBlurredGradientShadowLayer - SUCCESS: shadowLayer.Frame: {shadowLayer.Frame}");
			return shadowLayer;
		}

		public static void ClearShadow(this UIView platformView)
		{
			Debug.WriteLine("[Shadow] ClearShadow(UIView) called");
			var layer = platformView.Layer;
			layer?.ClearShadow();
		}

		public static void ClearShadow(this CALayer layer)
		{
			Debug.WriteLine("[Shadow] ClearShadow(CALayer) called");
			layer.ShadowColor = new CGColor(0, 0, 0, 0);
			layer.ShadowRadius = 0;
			layer.ShadowOffset = new CGSize();
			layer.ShadowOpacity = 0;
		}
	}
}