using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	public partial class ImageHandlerTests<TImageHandler, TStub>
	{
		[Theory]
		[InlineData("red.png", false)]
		[InlineData("dotnet_bot.png", false)]
		[InlineData("animated_heart.gif", true)]
		public Task ImageSourcesLoadCorrectly(string filename, bool isAnimated)
		{
			var image = new TStub
			{
				Source = new FileImageSourceStub(filename),
			};

			return InvokeOnMainThreadAsync(async () =>
			{
				var handler = CreateHandler(image);

				await image.WaitUntilLoaded();

				var platformImageView = GetPlatformImageView(handler);

				await platformImageView.AttachAndRun(() =>
				{
					if (isAnimated && UsesAnimatedImages)
					{
						Assert.NotNull(platformImageView.AnimationImages);
						Assert.NotEmpty(platformImageView.AnimationImages);
						Assert.Equal(platformImageView.AnimationImages[0], platformImageView.Image);
					}
					else
					{
						Assert.NotNull(platformImageView.Image);
						Assert.Null(platformImageView.AnimationImages);
					}
				});
			});
		}

		[Theory]
		[InlineData("red.png", "dotnet_bot.png", false)]
		[InlineData("dotnet_bot.png", "animated_heart.gif", true)]
		[InlineData("animated_heart.gif", "numbers.gif", true)]
		[InlineData("animated_heart.gif", "dotnet_bot.png", false)]
		public Task ImageSourcesChangeCorrectly(string initial, string changed, bool isAnimated)
		{
			var image = new TStub
			{
				Source = new FileImageSourceStub(initial),
			};

			return InvokeOnMainThreadAsync(async () =>
			{
				var handler = CreateHandler(image);

				await image.WaitUntilLoaded();

				image.Source = new FileImageSourceStub(changed);
				handler.UpdateValue(nameof(IImage.Source));

				await image.WaitUntilLoaded();

				var platformImageView = GetPlatformImageView(handler);

				await platformImageView.AttachAndRun(() =>
				{
					if (isAnimated && UsesAnimatedImages)
					{
						Assert.NotNull(platformImageView.AnimationImages);
						Assert.NotEmpty(platformImageView.AnimationImages);
						Assert.Equal(platformImageView.AnimationImages[0], platformImageView.Image);
					}
					else
					{
						Assert.NotNull(platformImageView.Image);
						Assert.Null(platformImageView.AnimationImages);
					}
				});
			});
		}

		protected virtual bool UsesAnimatedImages => true;

		UIImageView GetPlatformImageView(IImageHandler imageHandler) =>
			imageHandler.PlatformView;

		bool GetNativeIsAnimationPlaying(IImageHandler imageHandler) =>
			GetPlatformImageView(imageHandler).IsAnimating;

		Aspect GetNativeAspect(IImageHandler imageHandler) =>
			GetPlatformImageView(imageHandler).ContentMode switch
			{
				UIViewContentMode.ScaleAspectFit => Aspect.AspectFit,
				UIViewContentMode.ScaleAspectFill => Aspect.AspectFill,
				UIViewContentMode.ScaleToFill => Aspect.Fill,
				UIViewContentMode.Center => Aspect.Center,
				_ => throw new ArgumentOutOfRangeException("Aspect")
			};

		[Fact]
		public Task GetOrientedImageReturnsValidImage()
		{
			var image = new TStub
			{
				Source = new FileImageSourceStub("dotnet_bot.png"),
			};

			return InvokeOnMainThreadAsync(async () =>
			{
				var handler = CreateHandler(image);

				await image.WaitUntilLoaded();

				var platformImageView = GetPlatformImageView(handler);

				await platformImageView.AttachAndRun(() =>
				{
					Assert.NotNull(platformImageView.Image);
					
					// Test that GetOrientedImage returns a valid image
					var orientedImage = platformImageView.GetOrientedImage();
					Assert.NotNull(orientedImage);
					
					// The oriented image should have the same size or similar dimensions
					// depending on orientation corrections applied
					Assert.True(orientedImage.Size.Width > 0);
					Assert.True(orientedImage.Size.Height > 0);
					
					// The oriented image should have UIImageOrientation.Up after normalization
					Assert.Equal(UIImageOrientation.Up, orientedImage.Orientation);
				});
			});
		}

		[Fact]
		public Task GetOrientedImageHandlesNullImage()
		{
			var image = new TStub();

			return InvokeOnMainThreadAsync(() =>
			{
				var handler = CreateHandler(image);
				var platformImageView = GetPlatformImageView(handler);

				return platformImageView.AttachAndRun(() =>
				{
					// When there's no image, GetOrientedImage should return null
					var orientedImage = platformImageView.GetOrientedImage();
					Assert.Null(orientedImage);
				});
			});
		}
	}
}