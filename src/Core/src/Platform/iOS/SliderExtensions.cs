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
			if (slider.ThumbColor != null)
			{
				// Preserve the existing thumb image
				var originalImage = uiSlider.CurrentThumbImage;

				if (originalImage != null)
				{
					// Apply tint manually to the image
					var tintedImage = originalImage.ApplyTint(slider.ThumbColor.ToPlatform());
					uiSlider.SetThumbImage(tintedImage, UIControlState.Normal);
					uiSlider.SetThumbImage(tintedImage, UIControlState.Highlighted); // Ensure tint stays when held
				}
			}
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

				var tintedImage = UIGraphics.GetImageFromCurrentImageContext();
				UIGraphics.EndImageContext();
				return tintedImage;
			}
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
			}
			else
			{
				uiSlider.SetThumbImage(null, UIControlState.Normal);
				uiSlider.UpdateThumbColor(slider);
			}
		}
	}
}