using System;
using System.IO;
using Xunit;

namespace Microsoft.Maui.UnitTests.ImageSource
{
	public class UriImageDiskCacheTests
	{
		[Fact]
		public void IsFresh_MaxValue_NeverExpires()
		{
			var path = WriteTempFile();
			try
			{
				// Set mtime far in the past — MaxValue should still report fresh.
				File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddYears(-50));

				Assert.True(UriImageDiskCache.IsFresh(path, TimeSpan.MaxValue));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void IsFresh_Zero_AlwaysExpired()
		{
			var path = WriteTempFile();
			try
			{
				File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
				Assert.False(UriImageDiskCache.IsFresh(path, TimeSpan.Zero));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void IsFresh_Negative_AlwaysExpired()
		{
			var path = WriteTempFile();
			try
			{
				File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
				Assert.False(UriImageDiskCache.IsFresh(path, TimeSpan.FromMinutes(-5)));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void IsFresh_WithinValidity_ReturnsTrue()
		{
			var path = WriteTempFile();
			try
			{
				File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
				Assert.True(UriImageDiskCache.IsFresh(path, TimeSpan.FromMinutes(5)));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void IsFresh_Expired_ReturnsFalse()
		{
			var path = WriteTempFile();
			try
			{
				File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-10));
				Assert.False(UriImageDiskCache.IsFresh(path, TimeSpan.FromMinutes(5)));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void IsFresh_AdditionOverflow_TreatedAsInfinite()
		{
			var path = WriteTempFile();
			try
			{
				// mtime near DateTime.MaxValue + huge validity would overflow; we expect "fresh".
				File.SetLastWriteTimeUtc(path, DateTime.UtcNow);

				// A very large validity that, when added to UtcNow, would exceed DateTime.MaxValue.
				var huge = TimeSpan.FromTicks(long.MaxValue);

				Assert.True(UriImageDiskCache.IsFresh(path, huge));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void IsFresh_MissingFile_ReturnsFalse()
		{
			var bogus = Path.Combine(Path.GetTempPath(), "definitely-missing-" + Guid.NewGuid().ToString("N"));
			Assert.False(UriImageDiskCache.IsFresh(bogus, TimeSpan.FromMinutes(5)));
		}

		[Theory]
		[InlineData("https://test.com/file", "")]
		[InlineData("https://test.com/file.png", ".png")]
		[InlineData("https://test.com/file.jpg?id=123", ".jpg")]
		public void GetCachedFileName_HasExpectedExtension(string uri, string ext)
		{
			var name = UriImageDiskCache.GetCachedFileName(new TestSource(new Uri(uri)));
			Assert.EndsWith(ext, name, StringComparison.Ordinal);
		}

		static string WriteTempFile()
		{
			var path = Path.Combine(Path.GetTempPath(), "uri-disk-cache-test-" + Guid.NewGuid().ToString("N"));
			File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
			return path;
		}

		sealed class TestSource : IUriImageSource
		{
			public TestSource(Uri uri) => Uri = uri;
			public Uri Uri { get; }
			public TimeSpan CacheValidity { get; set; } = TimeSpan.FromDays(1);
			public bool CachingEnabled { get; set; } = true;
			public bool IsEmpty => Uri is null;
		}
	}
}
