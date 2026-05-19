using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.DeviceTests.Stubs;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.ImageSource)]
	public class UriImageDiskCacheDeviceTests
	{
		// Every test uses a unique URI so cache files from previous runs don't interfere.
		static UriImageSourceStub MakeSource(TimeSpan validity, bool cachingEnabled = true)
		{
			var uri = new Uri($"https://maui-tests.invalid/cache-{Guid.NewGuid():N}.bin");
			return new UriImageSourceStub(uri)
			{
				CacheValidity = validity,
				CachingEnabled = cachingEnabled,
			};
		}

		static readonly byte[] PayloadV1 = Encoding.UTF8.GetBytes("payload-v1");

		[Fact]
		public async Task WrittenFile_IsRetrievableWithinValidity()
		{
			var src = MakeSource(TimeSpan.FromMinutes(5));
			try
			{
				using var content = new MemoryStream(PayloadV1);
				var written = await UriImageDiskCache.WriteAsync(src, content);

				Assert.NotNull(written);
				Assert.True(File.Exists(written));

				Assert.True(UriImageDiskCache.TryGetValidPath(src, out var fresh));
				Assert.Equal(written, fresh);
				Assert.Equal(PayloadV1, File.ReadAllBytes(fresh));
			}
			finally
			{
				UriImageDiskCache.Invalidate(src);
			}
		}

		[Fact]
		public async Task ExpiredFile_IsTreatedAsCacheMiss()
		{
			var src = MakeSource(TimeSpan.FromSeconds(1));
			try
			{
				using var content = new MemoryStream(PayloadV1);
				var written = await UriImageDiskCache.WriteAsync(src, content);
				Assert.NotNull(written);

				// Force the cached file to look older than CacheValidity.
				File.SetLastWriteTimeUtc(written, DateTime.UtcNow.AddMinutes(-1));

				Assert.False(UriImageDiskCache.TryGetValidPath(src, out _));

				// File itself still exists (we only invalidate logically via TTL).
				Assert.True(File.Exists(written));
			}
			finally
			{
				UriImageDiskCache.Invalidate(src);
			}
		}

		[Fact]
		public async Task MaxValueValidity_NeverExpires()
		{
			var src = MakeSource(TimeSpan.MaxValue);
			try
			{
				using var content = new MemoryStream(PayloadV1);
				var written = await UriImageDiskCache.WriteAsync(src, content);
				Assert.NotNull(written);

				File.SetLastWriteTimeUtc(written, DateTime.UtcNow.AddYears(-10));

				Assert.True(UriImageDiskCache.TryGetValidPath(src, out _));
			}
			finally
			{
				UriImageDiskCache.Invalidate(src);
			}
		}

		[Fact]
		public async Task ZeroValidity_AlwaysExpired()
		{
			var src = MakeSource(TimeSpan.Zero);
			try
			{
				using var content = new MemoryStream(PayloadV1);
				var written = await UriImageDiskCache.WriteAsync(src, content);
				Assert.NotNull(written);

				// Even immediately after writing, validity=0 means refresh.
				Assert.False(UriImageDiskCache.TryGetValidPath(src, out _));
			}
			finally
			{
				UriImageDiskCache.Invalidate(src);
			}
		}

		[Fact]
		public async Task Invalidate_DeletesCacheFile()
		{
			var src = MakeSource(TimeSpan.FromMinutes(5));
			using var content = new MemoryStream(PayloadV1);
			var written = await UriImageDiskCache.WriteAsync(src, content);
			Assert.NotNull(written);
			Assert.True(File.Exists(written));

			UriImageDiskCache.Invalidate(src);

			Assert.False(File.Exists(written));
			Assert.False(UriImageDiskCache.TryGetValidPath(src, out _));
		}

		[Fact]
		public async Task RewriteWhileFresh_IsShortCircuited()
		{
			// When a fresh cache file already exists, WriteAsync must short-circuit and
			// not overwrite. This is what prevents the Windows self-overwrite hazard when
			// the content stream itself originates from the cache file.
			var src = MakeSource(TimeSpan.FromMinutes(5));
			try
			{
				using (var content = new MemoryStream(PayloadV1))
				{
					Assert.NotNull(await UriImageDiskCache.WriteAsync(src, content));
				}

				Assert.True(UriImageDiskCache.TryGetValidPath(src, out var path));
				var originalMtime = File.GetLastWriteTimeUtc(path);

				await Task.Delay(50);

				var different = Encoding.UTF8.GetBytes("payload-v2-should-be-ignored");
				using (var content = new MemoryStream(different))
				{
					var result = await UriImageDiskCache.WriteAsync(src, content);
					Assert.Equal(path, result);
				}

				// Original bytes and mtime preserved – the short-circuit dropped the redundant write.
				Assert.Equal(PayloadV1, File.ReadAllBytes(path));
				Assert.Equal(originalMtime, File.GetLastWriteTimeUtc(path));
			}
			finally
			{
				UriImageDiskCache.Invalidate(src);
			}
		}

		[Fact]
		public async Task CacheFilePath_IsRootedInPlatformCacheDirectory()
		{
			var src = MakeSource(TimeSpan.FromMinutes(5));
			try
			{
				using var content = new MemoryStream(PayloadV1);
				var written = await UriImageDiskCache.WriteAsync(src, content);
				Assert.NotNull(written);

				// Sanity-check the cache location is the canonical "MauiUriImages" folder.
				var dir = Path.GetDirectoryName(written);
				Assert.NotNull(dir);
				Assert.EndsWith("MauiUriImages", dir, StringComparison.Ordinal);
			}
			finally
			{
				UriImageDiskCache.Invalidate(src);
			}
		}
	}
}
