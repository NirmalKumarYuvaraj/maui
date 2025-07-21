using Microsoft.Maui.Graphics.Platform;
using UIKit;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Extension methods for MauiImageView to provide properly oriented images.
	/// </summary>
	public static class MauiImageViewExtensions
	{
		/// <summary>
		/// Gets the image with proper orientation applied, suitable for saving or further processing.
		/// This method ensures that the returned UIImage has the correct orientation and can be safely
		/// converted to other formats without losing orientation information.
		/// </summary>
		/// <param name="imageView">The MauiImageView to get the image from.</param>
		/// <returns>A UIImage with proper orientation applied, or null if no image is available.</returns>
		public static UIImage? GetOrientedImage(this MauiImageView imageView)
		{
			var image = imageView.Image;
			if (image == null)
				return null;

			// Use the NormalizeOrientation method from Graphics to ensure proper orientation
			return image.NormalizeOrientation();
		}
	}
}