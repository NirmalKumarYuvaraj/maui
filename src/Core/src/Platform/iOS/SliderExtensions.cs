using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Extensions.DependencyInjection;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public static class SliderExtensions
	{
		public static void UpdateMinimum(this UISlider uiSlider, ISlider slider)
		{
			uiSlider.MinValue = (float)slider.Minimum;
		}

		public static void UpdateMaximum(this UISlider uiSlider, ISlider slider)
		{
			uiSlider.MaxValue = (float)slider.Maximum;
		}

		public static void UpdateValue(this UISlider uiSlider, ISlider slider)
		{
			if ((float)slider.Value != uiSlider.Value)
				uiSlider.Value = (float)slider.Value;
		}

		public static void UpdateMinimumTrackColor(this UISlider uiSlider, ISlider slider)
		{
			if (slider.MinimumTrackColor != null)
				uiSlider.MinimumTrackTintColor = slider.MinimumTrackColor.ToPlatform();
		}

		public static void UpdateMaximumTrackColor(this UISlider uiSlider, ISlider slider)
		{
			if (slider.MaximumTrackColor != null)
				uiSlider.MaximumTrackTintColor = slider.MaximumTrackColor.ToPlatform();
		}

		public static void UpdateThumbColor(this UISlider uiSlider, ISlider slider)
		{
			if (slider.ThumbColor is null)
			{
				return; // Exit early if no color is set
			}

			var originalImage = uiSlider.CurrentThumbImage ?? CreateDefaultThumbImage(uiSlider);
			if (originalImage is null)
			{
				uiSlider.SetThumbImage(null, UIControlState.Normal);
				return;
			}

			var tintedImage = originalImage.ApplyTint(slider.ThumbColor.ToPlatform());

			// Set the tinted image for different UI states
			uiSlider.SetThumbImage(tintedImage, UIControlState.Normal);
			uiSlider.SetThumbImage(tintedImage, UIControlState.Highlighted);
		}

		static UIImage CreateDefaultThumbImage(UISlider uiSlider)
		{
			var size = CalculateThumbSize(uiSlider);
			UIGraphics.BeginImageContextWithOptions(size, false, 0);

			UIColor.White.SetFill(); // Set the default thumb color
			var rect = new CGRect(0, 0, size.Width, size.Height);
			UIBezierPath.FromOval(rect).Fill();

			var defaultThumb = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();

			return defaultThumb;
		}

		static CGSize CalculateThumbSize(UISlider uiSlider)
		{
			var trackRect = uiSlider.TrackRectForBounds(uiSlider.Bounds);
			var thumbRect = uiSlider.ThumbRectForBounds(uiSlider.Bounds, trackRect, 0);
			return thumbRect.Size;
		}

		static UIImage ApplyTint(this UIImage image, UIColor color)
		{
			UIGraphics.BeginImageContextWithOptions(image.Size, false, image.CurrentScale);

			using (var context = UIGraphics.GetCurrentContext())
			{
				context.TranslateCTM(0, image.Size.Height);
				context.ScaleCTM(1.0f, -1.0f);
				context.SetBlendMode(CGBlendMode.Normal);

				var rect = new CGRect(0, 0, image.Size.Width, image.Size.Height);
				context.ClipToMask(rect, image.CGImage);

				color.SetFill();
				context.FillRect(rect);
			}

			var tintedImage = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();

			return tintedImage;
		}

		public static async Task UpdateThumbImageSourceAsync(this UISlider uiSlider, ISlider slider, IImageSourceServiceProvider provider)
		{
			var thumbImageSource = slider.ThumbImageSource;

			if (thumbImageSource != null)
			{
				var service = provider.GetRequiredImageSourceService(thumbImageSource);
				var scale = uiSlider.GetDisplayDensity();
				var result = await service.GetImageAsync(thumbImageSource, scale);
				var thumbImage = result?.Value;

				uiSlider.SetThumbImage(thumbImage, UIControlState.Normal);
				uiSlider.UpdateThumbColor(slider);
			}
			else
			{
				uiSlider.SetThumbImage(null, UIControlState.Normal);
				uiSlider.UpdateThumbColor(slider);
			}
		}
	}
}