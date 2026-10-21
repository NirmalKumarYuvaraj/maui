using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AView = Android.Views.View;
using MotionEvent = Android.Views.MotionEvent;
using MotionEventActions = Android.Views.MotionEventActions;

namespace Microsoft.Maui.DeviceTests
{
	public partial class LayoutTests
	{
		[Theory]
		[InlineData(false, false)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		[InlineData(true, true)]
		public async Task OverlayBlocksSiblingAndPreservesAncestorTap(bool useContentView, bool hasContent)
		{
			SetupTouchHandlers();

			var button = new Button { Text = "Behind overlay" };
			var overlay = CreateTouchOverlay(useContentView, hasContent ? new Label { Text = "Overlay" } : null);
			var root = new Grid { button, overlay };
			var parentTapCount = 0;
			var recognizer = new TapGestureRecognizer();
			recognizer.Tapped += (_, _) => parentTapCount++;
			root.GestureRecognizers.Add(recognizer);

			await CreateHandlerAndAddToWindow<LayoutHandler>(root, async handler =>
			{
				await handler.PlatformView.WaitForLayoutOrNonZeroSize();
				var buttonTouchCount = 0;
				var platformButton = button.ToPlatform(MauiContext);
				void OnButtonTouched(object? sender, AView.TouchEventArgs args)
				{
					if (args.Event?.ActionMasked == MotionEventActions.Down)
						buttonTouchCount++;
					args.Handled = false;
				}

				platformButton.Touch += OnButtonTouched;
				try
				{
					var downTime = global::Android.OS.SystemClock.UptimeMillis();
					DispatchOverlayGesture(handler.PlatformView, overlay.ToPlatform(MauiContext), downTime);
					Assert.Equal(0, buttonTouchCount);
					Assert.Equal(1, parentTapCount);

					overlay.InputTransparent = true;
					DispatchOverlayGesture(handler.PlatformView, overlay.ToPlatform(MauiContext), downTime + 100);
					Assert.Equal(1, buttonTouchCount);
					Assert.Equal(1, parentTapCount);

					overlay.InputTransparent = false;
					DispatchOverlayGesture(handler.PlatformView, overlay.ToPlatform(MauiContext), downTime + 200);
					Assert.Equal(1, buttonTouchCount);
					Assert.Equal(2, parentTapCount);
				}
				finally
				{
					platformButton.Touch -= OnButtonTouched;
				}
			});
		}

		[Theory]
		[InlineData(false, false)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		public async Task OverlayDoesNotInterceptInteractiveChild(bool useContentView, bool inputTransparent)
		{
			SetupTouchHandlers();

			var child = new Button { Text = "Overlay child" };
			var overlay = CreateTouchOverlay(useContentView, child);
			if (overlay is ContentView contentView)
				contentView.CascadeInputTransparent = false;
			else if (overlay is Grid grid)
				grid.CascadeInputTransparent = false;
			overlay.InputTransparent = inputTransparent;

			var root = new Grid { new Button { Text = "Behind overlay" }, overlay };
			var parentTapCount = 0;
			var recognizer = new TapGestureRecognizer();
			recognizer.Tapped += (_, _) => parentTapCount++;
			root.GestureRecognizers.Add(recognizer);

			await CreateHandlerAndAddToWindow<LayoutHandler>(root, async handler =>
			{
				await handler.PlatformView.WaitForLayoutOrNonZeroSize();
				var childTouchCount = 0;
				var platformChild = child.ToPlatform(MauiContext);
				void OnChildTouched(object? sender, AView.TouchEventArgs args)
				{
					if (args.Event?.ActionMasked == MotionEventActions.Down)
						childTouchCount++;
					args.Handled = false;
				}

				platformChild.Touch += OnChildTouched;
				try
				{
					DispatchOverlayGesture(handler.PlatformView, overlay.ToPlatform(MauiContext), global::Android.OS.SystemClock.UptimeMillis());
					Assert.Equal(1, childTouchCount);
					Assert.Equal(0, parentTapCount);
				}
				finally
				{
					platformChild.Touch -= OnChildTouched;
				}
			});
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task OverlayPreservesAncestorPan(bool useContentView)
		{
			SetupTouchHandlers();

			var overlay = CreateTouchOverlay(useContentView, null);
			var root = new Grid { new Button { Text = "Behind overlay" }, overlay };
			var statuses = new List<GestureStatus>();
			var recognizer = new PanGestureRecognizer();
			recognizer.PanUpdated += (_, args) => statuses.Add(args.StatusType);
			root.GestureRecognizers.Add(recognizer);

			await CreateHandlerAndAddToWindow<LayoutHandler>(root, async handler =>
			{
				await handler.PlatformView.WaitForLayoutOrNonZeroSize();
				DispatchOverlayGesture(handler.PlatformView, overlay.ToPlatform(MauiContext),
					global::Android.OS.SystemClock.UptimeMillis(), pan: true);

				Assert.Contains(GestureStatus.Started, statuses);
				Assert.Contains(GestureStatus.Running, statuses);
				Assert.Contains(GestureStatus.Completed, statuses);
			});
		}

		void SetupTouchHandlers()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});
		}

		static View CreateTouchOverlay(bool useContentView, View? content)
		{
			View overlay;
			if (useContentView)
				overlay = new ContentView { Content = content };
			else
			{
				var grid = new Grid();
				if (content is not null)
					grid.Add(content);
				overlay = grid;
			}

			overlay.BackgroundColor = Colors.Green;
			overlay.HeightRequest = 100;
			return overlay;
		}

		static void DispatchOverlayGesture(AView root, AView overlay, long downTime, bool pan = false)
		{
			Assert.True(overlay.Width > 0 && overlay.Height > 0);
			var rootLocation = new int[2];
			var overlayLocation = new int[2];
			root.GetLocationOnScreen(rootLocation);
			overlay.GetLocationOnScreen(overlayLocation);
			var x = overlayLocation[0] - rootLocation[0] + overlay.Width / 2f;
			var y = overlayLocation[1] - rootLocation[1] + overlay.Height / 2f;
			using var down = MotionEvent.Obtain(downTime, downTime, MotionEventActions.Down, x, y, 0);
			using var up = MotionEvent.Obtain(downTime, downTime + 48, MotionEventActions.Up, pan ? x + 100 : x, y, 0);
			try
			{
				Assert.True(root.DispatchTouchEvent(down));
				if (pan)
				{
					using var firstMove = MotionEvent.Obtain(downTime, downTime + 16, MotionEventActions.Move, x + 50, y, 0);
					using var secondMove = MotionEvent.Obtain(downTime, downTime + 32, MotionEventActions.Move, x + 100, y, 0);
					try
					{
						Assert.True(root.DispatchTouchEvent(firstMove));
						Assert.True(root.DispatchTouchEvent(secondMove));
					}
					finally
					{
						firstMove.Recycle();
						secondMove.Recycle();
					}
				}
				Assert.True(root.DispatchTouchEvent(up));
			}
			finally
			{
				down.Recycle();
				up.Recycle();
			}
		}

		void ValidateInputTransparentOnPlatformView(IView view)
		{
			var handler = view.ToHandler(MauiContext);
			if (handler.PlatformView is LayoutViewGroup lvg)
			{
				Assert.Equal(view.InputTransparent, lvg.InputTransparent);
				if (handler.ContainerView is WrapperView wv)
					Assert.False(wv.InputTransparent);

				return;
			}

			if (view.InputTransparent)
			{
				Assert.True(view.ToPlatform(MauiContext) is WrapperView wv && wv.InputTransparent);
			}
			else
			{
				Assert.True(view.ToPlatform(MauiContext) is not WrapperView wv || !wv.InputTransparent);
			}
		}
	}
}
