using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics.Drawables;
using Microsoft.Maui.DeviceTests.Stubs;
using Xunit;
using Color = Microsoft.Maui.Graphics.Color;

namespace Microsoft.Maui.DeviceTests
{
	public partial class UriImageSourceServiceTests
	{
		[Theory]
		[InlineData(typeof(FileImageSourceStub))]
		[InlineData(typeof(FontImageSourceStub))]
		[InlineData(typeof(StreamImageSourceStub))]
		public async Task ThrowsForIncorrectTypes(Type type)
		{
			var service = new UriImageSourceService();

			var imageSource = (ImageSourceStub)Activator.CreateInstance(type);

			await Assert.ThrowsAsync<InvalidCastException>(() => service.GetDrawableAsync(imageSource, MauiProgram.DefaultContext));
		}

		[Fact]
		public async Task CachingEnabledFalse_DoesNotUseCachedBitmap()
		{
			var service = new UriImageSourceService();
			var file = CreateBitmapFile(40, 40, Colors.Red);
			var fileUri = new UriBuilder(Uri.UriSchemeFile, string.Empty) { Path = Path.GetFullPath(file) }.Uri;

			var imageSource = new UriImageSourceStub(fileUri)
			{
				CachingEnabled = false,
				CacheValidity = TimeSpan.FromDays(1)
			};

			using (var firstResult = await service.GetDrawableAsync(imageSource, MauiProgram.DefaultContext))
			{
				Assert.NotNull(firstResult);
				Assert.NotNull(firstResult.Value);
				var firstBitmap = Assert.IsType<BitmapDrawable>(firstResult.Value).Bitmap;
				await firstBitmap.AssertContainsColor(Color.FromArgb("#FF0000").ToPlatform()).ConfigureAwait(false);
			}

			CreateBitmapFile(40, 40, Colors.Blue, file);

			using var secondResult = await service.GetDrawableAsync(imageSource, MauiProgram.DefaultContext);
			Assert.NotNull(secondResult);
			Assert.NotNull(secondResult.Value);
			var secondBitmap = Assert.IsType<BitmapDrawable>(secondResult.Value).Bitmap;
			await secondBitmap.AssertContainsColor(Color.FromArgb("#0000FF").ToPlatform()).ConfigureAwait(false);
		}

		[Fact]
		public async Task CacheValidityZero_DisablesCachingEvenWhenEnabled()
		{
			var service = new UriImageSourceService();
			var file = CreateBitmapFile(40, 40, Colors.Red);
			var fileUri = new UriBuilder(Uri.UriSchemeFile, string.Empty) { Path = Path.GetFullPath(file) }.Uri;

			var imageSource = new UriImageSourceStub(fileUri)
			{
				CachingEnabled = true,
				CacheValidity = TimeSpan.Zero
			};

			using (var firstResult = await service.GetDrawableAsync(imageSource, MauiProgram.DefaultContext))
			{
				Assert.NotNull(firstResult);
				Assert.NotNull(firstResult.Value);
				var firstBitmap = Assert.IsType<BitmapDrawable>(firstResult.Value).Bitmap;
				await firstBitmap.AssertContainsColor(Color.FromArgb("#FF0000").ToPlatform()).ConfigureAwait(false);
			}

			CreateBitmapFile(40, 40, Colors.Blue, file);

			using var secondResult = await service.GetDrawableAsync(imageSource, MauiProgram.DefaultContext);
			Assert.NotNull(secondResult);
			Assert.NotNull(secondResult.Value);
			var secondBitmap = Assert.IsType<BitmapDrawable>(secondResult.Value).Bitmap;
			await secondBitmap.AssertContainsColor(Color.FromArgb("#0000FF").ToPlatform()).ConfigureAwait(false);
		}
	}
}