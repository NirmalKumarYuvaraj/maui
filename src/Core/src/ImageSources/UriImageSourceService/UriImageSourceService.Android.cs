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
					var cacheValidityMilliseconds = GetCacheValidityMilliseconds(uriImageSource.CacheValidity);

					PlatformInterop.LoadImageFromUri(imageView, uriImageSource.Uri.OriginalString, uriImageSource.CachingEnabled, cacheValidityMilliseconds, callback);

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
					var cacheValidityMilliseconds = GetCacheValidityMilliseconds(uriImageSource.CacheValidity);

					PlatformInterop.LoadImageFromUri(context, uriImageSource.Uri.OriginalString, uriImageSource.CachingEnabled, cacheValidityMilliseconds, drawableCallback);

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

		static long GetCacheValidityMilliseconds(TimeSpan cacheValidity)
		{
			if (cacheValidity <= TimeSpan.Zero)
				return 0;

			var totalMilliseconds = cacheValidity.TotalMilliseconds;
			if (totalMilliseconds >= long.MaxValue)
				return long.MaxValue;

			return (long)totalMilliseconds;
		}
	}
}
