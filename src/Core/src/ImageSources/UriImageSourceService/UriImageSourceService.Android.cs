#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Widget;
using Microsoft.Extensions.Logging;

namespace Microsoft.Maui
{
	public partial class UriImageSourceService
	{
		public override Task<IImageSourceServiceResult?> LoadDrawableAsync(IImageSource imageSource, ImageView imageView, CancellationToken cancellationToken = default)
		{
			var uriImageSource = (IUriImageSource)imageSource;
			if (!uriImageSource.IsEmpty)
			{
				try
				{
					var callback = new ImageLoaderCallback();

					PlatformInterop.LoadImageFromUri(imageView, uriImageSource.Uri.OriginalString, uriImageSource.CachingEnabled, GetCacheValidityMillis(uriImageSource), callback);

					return callback.Result;
				}
				catch (Exception ex)
				{
					Logger?.LogWarning(ex, "Unable to load image uri '{Uri}'.", uriImageSource.Uri.OriginalString);
					throw;
				}
			}

			return Task.FromResult<IImageSourceServiceResult?>(null);
		}

		public override Task<IImageSourceServiceResult<Drawable>?> GetDrawableAsync(IImageSource imageSource, Context context, CancellationToken cancellationToken = default)
		{
			var uriImageSource = (IUriImageSource)imageSource;
			if (!uriImageSource.IsEmpty)
			{
				try
				{
					var drawableCallback = new ImageLoaderResultCallback();

					PlatformInterop.LoadImageFromUri(context, uriImageSource.Uri.OriginalString, uriImageSource.CachingEnabled, GetCacheValidityMillis(uriImageSource), drawableCallback);

					return drawableCallback.Result;
				}
				catch (Exception ex)
				{
					Logger?.LogWarning(ex, "Unable to load image uri '{Uri}'.", uriImageSource.Uri.OriginalString);
					throw;
				}
			}

			return Task.FromResult<IImageSourceServiceResult<Drawable>?>(null);
		}

		// Maps CacheValidity (TimeSpan) to milliseconds for the Java layer.
		//   TimeSpan.MaxValue              -> long.MaxValue  (never expires)
		//   TimeSpan.Zero / negative       -> 0              (always refresh)
		//   otherwise                      -> total ms, clamped to long.MaxValue on overflow
		static long GetCacheValidityMillis(IUriImageSource imageSource)
		{
			var validity = imageSource.CacheValidity;
			if (validity == TimeSpan.MaxValue)
				return long.MaxValue;
			if (validity <= TimeSpan.Zero)
				return 0;

			var ms = validity.TotalMilliseconds;
			if (double.IsNaN(ms) || ms >= long.MaxValue)
				return long.MaxValue;

			return (long)ms;
		}
	}
}