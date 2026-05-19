#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using WImageSource = Microsoft.UI.Xaml.Media.ImageSource;

namespace Microsoft.Maui
{
	public partial class UriImageSourceService
	{
		public override Task<IImageSourceServiceResult<WImageSource>?> GetImageSourceAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default) =>
			GetImageSourceAsync((IUriImageSource)imageSource, scale, cancellationToken);

		public async Task<IImageSourceServiceResult<WImageSource>?> GetImageSourceAsync(IUriImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
		{
			if (imageSource.IsEmpty)
				return null;

			if (imageSource is not IStreamImageSource streamImageSource)
				throw new InvalidOperationException("Unable to load URI as a stream.");

			try
			{
				if (imageSource.CachingEnabled && UriImageDiskCache.TryGetValidPath(imageSource, out var freshPath))
				{
					using var cached = File.OpenRead(freshPath);
					return await CreateResultFromStreamAsync(cached).ConfigureAwait(true);
				}

				using var stream = await streamImageSource.GetStreamAsync(cancellationToken);
				if (stream is null)
					throw new InvalidOperationException("Unable to load image stream.");

				if (imageSource.CachingEnabled)
				{
					// Tee the network stream into the cache, then load the BitmapImage from the cached file
					// so we don't have to buffer the whole image in memory twice.
					var cachedPath = await UriImageDiskCache.WriteAsync(imageSource, stream, cancellationToken).ConfigureAwait(false);
					if (cachedPath is not null)
					{
						using var cached = File.OpenRead(cachedPath);
						return await CreateResultFromStreamAsync(cached).ConfigureAwait(true);
					}
				}
				else
				{
					UriImageDiskCache.Invalidate(imageSource);
				}

				return await CreateResultFromStreamAsync(stream).ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				Logger?.LogWarning(ex, "Unable to load image URI '{Uri}'.", imageSource.Uri);
				throw;
			}
		}

		static async Task<IImageSourceServiceResult<WImageSource>?> CreateResultFromStreamAsync(Stream stream)
		{
			var image = new BitmapImage();
			using var ras = stream.AsRandomAccessStream();
			await image.SetSourceAsync(ras);
			return new ImageSourceServiceResult(image);
		}
	}
}