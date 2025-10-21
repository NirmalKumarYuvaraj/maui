#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Controls
{
	/// <include file="../../docs/Microsoft.Maui.Controls/UriImageSource.xml" path="Type[@FullName='Microsoft.Maui.Controls.UriImageSource']/Docs/*" />
	public sealed partial class UriImageSource : ImageSource, IStreamImageSource
	{
		internal const string CacheFolderName = "ImageLoaderCache";
		static readonly object s_syncHandle = new object();
		static readonly Dictionary<string, SemaphoreSlim> s_semaphores = new Dictionary<string, SemaphoreSlim>();
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

		/// <include file="../../docs/Microsoft.Maui.Controls/UriImageSource.xml" path="//Member[@MemberName='IsEmpty']/Docs/*" />
		public override bool IsEmpty => Uri == null;

		/// <include file="../../docs/Microsoft.Maui.Controls/UriImageSource.xml" path="//Member[@MemberName='CacheValidity']/Docs/*" />
		public TimeSpan CacheValidity
		{
			get => (TimeSpan)GetValue(CacheValidityProperty);
			set => SetValue(CacheValidityProperty, value);
		}

		/// <include file="../../docs/Microsoft.Maui.Controls/UriImageSource.xml" path="//Member[@MemberName='CachingEnabled']/Docs/*" />
		public bool CachingEnabled
		{
			get => (bool)GetValue(CachingEnabledProperty);
			set => SetValue(CachingEnabledProperty, value);
		}

		/// <include file="../../docs/Microsoft.Maui.Controls/UriImageSource.xml" path="//Member[@MemberName='Uri']/Docs/*" />
		[System.ComponentModel.TypeConverter(typeof(UriTypeConverter))]
		public Uri Uri
		{
			get => (Uri)GetValue(UriProperty);
			set => SetValue(UriProperty, value);
		}

		async Task<Stream> IStreamImageSource.GetStreamAsync(CancellationToken userToken)
		{
			if (IsEmpty)
				return null;

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

		/// <include file="../../docs/Microsoft.Maui.Controls/UriImageSource.xml" path="//Member[@MemberName='ToString']/Docs/*" />
		public override string ToString()
		{
			return $"Uri: {Uri}";
		}

		async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			Stream stream = null;

			if (CachingEnabled)
				stream = await GetStreamFromCacheAsync(uri, cancellationToken).ConfigureAwait(false);

			if (stream == null)
			{
				try
				{
					stream = await DownloadStreamAsync(uri, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Application.Current?.FindMauiContext()?.CreateLogger<UriImageSource>()?.LogWarning(ex, "Error getting stream for {Uri}", Uri);
					stream = null;
				}
			}

			return stream;
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

		static string GetCacheKey(Uri uri)
		{
			return Crc64.ComputeHashString(uri.AbsoluteUri);
		}

		async Task<bool> GetHasLocallyCachedCopyAsync(string key, bool checkValidity = true)
		{
			var now = DateTime.UtcNow;
			var lastWriteTime = await GetLastWriteTimeUtcAsync(key).ConfigureAwait(false);
			return lastWriteTime.HasValue && (!checkValidity || now - lastWriteTime.Value < CacheValidity);
		}

		static async Task<DateTime?> GetLastWriteTimeUtcAsync(string key)
		{
			var path = Path.Combine(FileSystem.CacheDirectory, CacheFolderName, key);
			if (!File.Exists(path))
				return null;

			try
			{
				return (await Task.Run(() => File.GetLastWriteTimeUtc(path)).ConfigureAwait(false));
			}
			catch
			{
				return null;
			}
		}

		async Task<Stream> GetStreamAsyncUnchecked(string key, Uri uri, CancellationToken cancellationToken)
		{
			var cacheDir = Path.Combine(FileSystem.CacheDirectory, CacheFolderName);
			var cachePath = Path.Combine(cacheDir, key);

			if (await GetHasLocallyCachedCopyAsync(key).ConfigureAwait(false))
			{
				var retry = 5;
				while (retry >= 0)
				{
					var backoff = 0;
					try
					{
						var result = File.OpenRead(cachePath);
						return result;
					}
					catch (IOException)
					{
						// Multiple readers may be trying to access the file, back off for a random amount of time
						backoff = new Random().Next(1, 5);
						retry--;
					}

					if (backoff > 0)
					{
						await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
					}
				}
				return null;
			}

			Stream stream;
			try
			{
				stream = await DownloadStreamAsync(uri, cancellationToken).ConfigureAwait(false);
				if (stream == null)
					return null;
			}
			catch (Exception ex)
			{
				Application.Current?.FindMauiContext()?.CreateLogger<UriImageSource>()?.LogWarning(ex, "Error getting stream for {Uri}", uri);
				return null;
			}

			if (stream == null || !stream.CanRead)
			{
				stream?.Dispose();
				return null;
			}

			try
			{
				// Ensure cache directory exists
				Directory.CreateDirectory(cacheDir);

				// Cache the stream
				using (var writeStream = File.Create(cachePath))
				{
					await stream.CopyToAsync(writeStream, 16384, cancellationToken).ConfigureAwait(false);
				}

				stream.Dispose();
				return File.OpenRead(cachePath);
			}
			catch (Exception ex)
			{
				Application.Current?.FindMauiContext()?.CreateLogger<UriImageSource>()?.LogWarning(ex, "Error caching stream for {Uri}", uri);
				stream?.Dispose();
				return null;
			}
		}

		async Task<Stream> GetStreamFromCacheAsync(Uri uri, CancellationToken cancellationToken)
		{
			var key = GetCacheKey(uri);
			SemaphoreSlim sem;

			lock (s_syncHandle)
			{
				if (s_semaphores.ContainsKey(key))
					sem = s_semaphores[key];
				else
					s_semaphores.Add(key, sem = new SemaphoreSlim(1, 1));
			}

			try
			{
				await sem.WaitAsync(cancellationToken).ConfigureAwait(false);
				var stream = await GetStreamAsyncUnchecked(key, uri, cancellationToken).ConfigureAwait(false);
				if (stream == null || stream.Length == 0 || !stream.CanRead)
				{
					sem.Release();
					return null;
				}

				// Create a wrapper that releases the semaphore when disposed
				var wrapped = new StreamWrapper(stream, new SemaphoreReleaser(sem));
				return wrapped;
			}
			catch (OperationCanceledException)
			{
				sem.Release();
				throw;
			}
			catch
			{
				sem.Release();
				throw;
			}
		}

		// Helper class to ensure semaphore is released when the stream is disposed
		private sealed class SemaphoreReleaser : IDisposable
		{
			private readonly SemaphoreSlim _semaphore;
			private bool _disposed;

			public SemaphoreReleaser(SemaphoreSlim semaphore)
			{
				_semaphore = semaphore;
			}

			public void Dispose()
			{
				if (!_disposed)
				{
					_semaphore?.Release();
					_disposed = true;
				}
			}
		}

		void OnUriChanged()
		{
			CancellationTokenSource?.Cancel();

			OnSourceChanged();
		}
	}
}
