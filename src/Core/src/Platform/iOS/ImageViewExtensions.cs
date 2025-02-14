
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public static class ImageViewExtensions
	{
		public static void Clear(this UIImageView imageView)
		{
			// stop the animation if there is one
			imageView.StopAnimating();
			imageView.AnimationImages = null;
			imageView.Image = null;
		}

		public static void UpdateAspect(this UIImageView imageView, IImage image)
		{
			imageView.ContentMode = image.Aspect.ToUIViewContentMode();
			imageView.ClipsToBounds = imageView.ContentMode == UIViewContentMode.ScaleAspectFill || imageView.ContentMode == UIViewContentMode.Center;
		}

		public static void UpdateIsAnimationPlaying(this UIImageView imageView, IImageSourcePart image)
		{
			if (image.IsAnimationPlaying)
			{
				if (!imageView.IsAnimating)
					imageView.StartAnimating();
			}
			else
			{
				if (imageView.IsAnimating)
					imageView.StopAnimating();
			}
		}

		// TODO: This method does not appear to be used, should we obsolete in net9?
		public static void UpdateSource(this UIImageView imageView, UIImage? uIImage, IImageSourcePart image)
		{
			imageView.Image = uIImage;
			imageView.UpdateIsAnimationPlaying(image);
		}

		// TODO: This method does not appear to be used, should we obsolete in net9?
		public static Task<IImageSourceServiceResult<UIImage>?> UpdateSourceAsync(
			this UIImageView imageView,
			IImageSourcePart image,
			IImageSourceServiceProvider services,
			CancellationToken cancellationToken = default)
		{
			float scale = imageView.Window?.GetDisplayDensity() ?? 1.0f;

			imageView.Clear();
			return image.UpdateSourceAsync(imageView, services, (uiImage) =>
			{
				imageView.Image = uiImage;
			}, scale, cancellationToken);
		}

		/// <summary>
		/// Gets the size that fits on the screen for a <see cref="UIImageView"/> to be consistent cross-platform.
		/// </summary>
		/// <remarks>The default iOS implementation of SizeThatFits only returns the image's dimensions and ignores the constraints.</remarks>
		/// <param name="imageSize"></param>
		/// <param name="constraints">The specified size constraints.</param>
		/// <param name="contentMode"></param>
		/// <param name="padding"></param>
		/// <returns>The size where the image would fit depending on the aspect ratio.</returns>
		internal static CGSize SizeThatFitsImage(
			CGSize imageSize,
			CGSize constraints,
			UIViewContentMode contentMode,
			Thickness? padding = null)
		{
			if (imageSize.IsEmpty)
				return CGSize.Empty;

			var calculatedPadding = padding ?? default(Thickness);

			double contentWidth = imageSize.Width + calculatedPadding.HorizontalThickness;
			double contentHeight = imageSize.Height + calculatedPadding.VerticalThickness;

			double constrainedWidth = Math.Min(contentWidth, constraints.Width);
			double constrainedHeight = Math.Min(contentHeight, constraints.Height);

			double widthRatio = constrainedWidth / imageSize.Width;
			double heightRatio = constrainedHeight / imageSize.Height;

			// In cases where the image must fit within its given constraints, 
			// it should be scaled down based on the smallest dimension (scale factor) that allows it to fit.
			if (contentMode == UIViewContentMode.ScaleAspectFit)
			{
				var scaleFactor = Math.Min(widthRatio, heightRatio);
				return new CGSize(
					imageSize.Width * scaleFactor,
					imageSize.Height * scaleFactor);
			}

			// Cases where AspectMode is ScaleToFill or Center
			return new CGSize(constrainedWidth, constrainedHeight);
		}

		/// <summary>
		/// Extension method for UIImageView
		/// </summary>
		internal static CGSize SizeThatFitsImage(this UIImageView imageView, CGSize constraints)
		{
			if (imageView.Image is null)
				return CGSize.Empty;

			return SizeThatFitsImage(
				imageView.Image.Size,
				constraints,
				imageView.ContentMode);
		}

		/// <summary>
		/// Extension method for UIButton with image
		/// </summary>
		internal static CGSize SizeThatFitsImage(this UIButton button, CGSize constraints, Thickness? padding = null)
		{
			if (button.ImageView?.Image is null)
				return CGSize.Empty;

			return SizeThatFitsImage(
				button.ImageView.Image.Size,
				constraints,
				button.ContentMode,
				padding);
		}
	}
}