#nullable disable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Maui.Controls
{
	/// <summary>An <see cref="ImageSource"/> that loads an image from a URI, with caching support.</summary>
	public sealed partial class UriImageSource : ImageSource, IStreamImageSource
	{
		static readonly ConcurrentDictionary<string, CacheEntry> StreamCache = new ConcurrentDictionary<string, CacheEntry>();
		static readonly ConcurrentDictionary<string, SemaphoreSlim> DownloadLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

		/// <summary>Bindable property for <see cref="Uri"/>.</summary>
		public static readonly BindableProperty UriProperty = BindableProperty.Create(
			nameof(Uri), typeof(Uri), typeof(UriImageSource), default(Uri),
			propertyChanged: (bindable, oldvalue, newvalue) => ((UriImageSource)bindable).OnUriChanged(),
			validateValue: (bindable, value) => value == null || ((Uri)value).IsAbsoluteUri);

		/// <summary>Bindable property for <see cref="CacheValidity"/>.</summary>
		public static readonly BindableProperty CacheValidityProperty = BindableProperty.Create(
			nameof(CacheValidity), typeof(TimeSpan), typeof(UriImageSource), TimeSpan.FromDays(1));

		/// <summary>Bindable property for <see cref="CachingEnabled"/>.</summary>
		public static readonly BindableProperty CachingEnabledProperty = BindableProperty.Create(
			nameof(CachingEnabled), typeof(bool), typeof(UriImageSource), true);

		/// <summary>Gets a value indicating whether this image source is empty.</summary>
		public override bool IsEmpty => Uri == null;

		/// <summary>Gets or sets how long the cached image remains valid. This is a bindable property.</summary>
		public TimeSpan CacheValidity
		{
			get => (TimeSpan)GetValue(CacheValidityProperty);
			set => SetValue(CacheValidityProperty, value);
		}

		/// <summary>Gets or sets whether caching is enabled. This is a bindable property.</summary>
		public bool CachingEnabled
		{
			get => (bool)GetValue(CachingEnabledProperty);
			set => SetValue(CachingEnabledProperty, value);
		}

		/// <summary>Gets or sets the URI of the image to load. This is a bindable property.</summary>
		[System.ComponentModel.TypeConverter(typeof(UriTypeConverter))]
		public Uri Uri
		{
			get => (Uri)GetValue(UriProperty);
			set => SetValue(UriProperty, value);
		}

		async Task<Stream> IStreamImageSource.GetStreamAsync(CancellationToken userToken)
		{
			if (IsEmpty)
			{
				return null;
			}

			await OnLoadingStarted();
			userToken.Register(CancellationTokenSource.Cancel);
			Stream stream;

			try
			{
				stream = await GetStreamAsync(Uri, CancellationTokenSource.Token);
				await OnLoadingCompleted(false);
			}
			catch (OperationCanceledException)
			{
				await OnLoadingCompleted(true);
				throw;
			}
			catch (Exception ex)
			{
				Application.Current?.FindMauiContext()?.CreateLogger<UriImageSource>()?.LogWarning(ex, "Error getting stream for {Uri}", Uri);
				throw;
			}

			return stream;
		}

		/// <summary>Returns a string representation of this <see cref="UriImageSource"/>.</summary>
		public override string ToString()
		{
			return $"Uri: {Uri}";
		}

		async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!CachingEnabled || CacheValidity <= TimeSpan.Zero)
			{
				return await DownloadStreamAsync(uri, cancellationToken).ConfigureAwait(false);
			}

			var key = GetCacheKey(uri);

			if (TryGetFromCache(key, out var cachedStream))
			{
				return cachedStream;
			}

			var downloadLock = DownloadLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
			await downloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				if (TryGetFromCache(key, out cachedStream))
				{
					return cachedStream;
				}

				using var downloadedStream = await DownloadStreamAsync(uri, cancellationToken).ConfigureAwait(false);
				if (downloadedStream is null)
				{
					return null;
				}

				var bytes = await ReadAllBytesAsync(downloadedStream, cancellationToken).ConfigureAwait(false);
				var expiration = GetExpiration(CacheValidity);

				StreamCache[key] = new CacheEntry(bytes, expiration);

				return CreateMemoryStream(bytes);
			}
			finally
			{
				downloadLock.Release();
			}
		}

		static string GetCacheKey(Uri uri) => Crc64.ComputeHashString(uri.OriginalString);

		static DateTimeOffset GetExpiration(TimeSpan cacheValidity)
		{
			if (cacheValidity == TimeSpan.MaxValue)
			{
				return DateTimeOffset.MaxValue;
			}

			try
			{
				return DateTimeOffset.UtcNow.Add(cacheValidity);
			}
			catch (ArgumentOutOfRangeException)
			{
				return DateTimeOffset.MaxValue;
			}
		}

		static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
		{
			using var memoryStream = new MemoryStream();
			await stream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
			return memoryStream.ToArray();
		}

		static Stream CreateMemoryStream(byte[] bytes) => new MemoryStream(bytes, writable: false);

		static bool TryGetFromCache(string key, out Stream stream)
		{
			stream = null;

			if (!StreamCache.TryGetValue(key, out var entry))
			{
				return false;
			}

			if (entry.Expiration <= DateTimeOffset.UtcNow)
			{
				StreamCache.TryRemove(key, out _);
				return false;
			}

			stream = CreateMemoryStream(entry.Bytes);
			return true;
		}

		async Task<Stream> DownloadStreamAsync(Uri uri, CancellationToken cancellationToken)
		{
			try
			{
				using var client = new HttpClient();

				// Do not remove this await otherwise the client will dispose before
				// the stream even starts
				return await StreamWrapper.GetStreamAsync(uri, cancellationToken, client).ConfigureAwait(false);
			}
			catch (Exception ex)
			{

				Application.Current?.FindMauiContext()?.CreateLogger<UriImageSource>()?.LogWarning(ex, "Error getting stream for {Uri}", Uri);
				return null;
			}
		}

		void OnUriChanged()
		{
			CancellationTokenSource?.Cancel();

			OnSourceChanged();
		}

		readonly struct CacheEntry
		{
			public CacheEntry(byte[] bytes, DateTimeOffset expiration)
			{
				Bytes = bytes;
				Expiration = expiration;
			}

			public byte[] Bytes { get; }
			public DateTimeOffset Expiration { get; }
		}
	}
}
