#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui
{
	/// <summary>
	/// Internal file-based disk cache for <see cref="IUriImageSource"/> downloads.
	/// Honors <see cref="IUriImageSource.CacheValidity"/> as a time-to-live and writes files atomically.
	/// </summary>
	internal static class UriImageDiskCache
	{
		const string CacheFolderName = "MauiUriImages";
		const string CacheRootFolderName = "com.microsoft.maui";

		static readonly ConcurrentDictionary<string, SemaphoreSlim> s_locks = new(StringComparer.Ordinal);

		static string? s_cacheDirectory;

		/// <summary>Returns the cache root directory or <c>null</c> if the platform does not expose a cache directory (e.g. netstandard).</summary>
		internal static string? CacheDirectory
		{
			get
			{
				if (s_cacheDirectory is not null)
					return s_cacheDirectory;

				try
				{
					s_cacheDirectory = Path.Combine(FileSystem.CacheDirectory, CacheRootFolderName, CacheFolderName);
					return s_cacheDirectory;
				}
				catch
				{
					return null;
				}
			}
		}

		internal static string GetCachedFileName(IUriImageSource imageSource)
		{
			var hash = Crc64.ComputeHashString(imageSource.Uri.OriginalString);
			var ext = Path.GetExtension(imageSource.Uri.AbsolutePath);
			return $"{hash}{ext}";
		}

		/// <summary>Returns the absolute cache file path for the given image source, or <c>null</c> if the platform has no cache directory.</summary>
		internal static string? GetCachedFilePath(IUriImageSource imageSource)
		{
			var dir = CacheDirectory;
			if (dir is null)
				return null;
			return Path.Combine(dir, GetCachedFileName(imageSource));
		}

		/// <summary>Returns <c>true</c> if a non-expired cache file exists for the source.</summary>
		internal static bool TryGetValidPath(IUriImageSource imageSource, out string path)
		{
			path = string.Empty;
			var candidate = GetCachedFilePath(imageSource);
			if (candidate is null || !File.Exists(candidate))
				return false;

			if (!IsFresh(candidate, imageSource.CacheValidity))
				return false;

			path = candidate;
			return true;
		}

		internal static bool IsFresh(string path, TimeSpan cacheValidity)
		{
			// TimeSpan.MaxValue (or any overflow) ⇒ never expires.
			if (cacheValidity == TimeSpan.MaxValue)
				return true;

			// Non-positive validity ⇒ always expired (force refresh).
			if (cacheValidity <= TimeSpan.Zero)
				return false;

			try
			{
				var writtenAt = File.GetLastWriteTimeUtc(path);
				var expiresAt = writtenAt + cacheValidity;
				return DateTime.UtcNow < expiresAt;
			}
			catch (ArgumentOutOfRangeException)
			{
				// writtenAt + cacheValidity overflowed -> effectively infinite.
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>Atomically writes the bytes to the cache file for the given source. Returns the written path, or <c>null</c> if caching is unavailable.</summary>
		internal static async Task<string?> WriteAsync(IUriImageSource imageSource, Stream content, CancellationToken cancellationToken = default)
		{
			var path = GetCachedFilePath(imageSource);
			if (path is null)
				return null;

			var dir = Path.GetDirectoryName(path);
			if (string.IsNullOrEmpty(dir))
				return null;

			// Short-circuit if a fresh file already exists. Prevents redundant writes when the
			// content stream itself originates from the cache file (which would otherwise
			// cause a self-overwrite on Windows where a file open for read can't be deleted).
			if (File.Exists(path) && IsFresh(path, imageSource.CacheValidity))
				return path;

			var gate = s_locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
			await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				// Re-check inside the lock — another writer may have just finished.
				if (File.Exists(path) && IsFresh(path, imageSource.CacheValidity))
					return path;

				Directory.CreateDirectory(dir);

				var tmp = path + ".tmp";
				using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					await content.CopyToAsync(fs, 81920, cancellationToken).ConfigureAwait(false);
				}

				if (File.Exists(path))
					File.Delete(path);
				File.Move(tmp, path);
				File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
				return path;
			}
			finally
			{
				gate.Release();
			}
		}

		/// <summary>Deletes the cache file (if present) for the given source.</summary>
		internal static void Invalidate(IUriImageSource imageSource)
		{
			var path = GetCachedFilePath(imageSource);
			if (path is null)
				return;

			try
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			catch
			{
				// best-effort
			}
		}
	}
}
