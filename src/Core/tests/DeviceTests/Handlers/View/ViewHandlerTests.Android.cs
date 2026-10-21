using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Handlers;
using Xunit;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	public partial class ViewHandlerTests
	{
		[Theory]
		[InlineData(MotionEventActions.Up)]
		[InlineData(MotionEventActions.Cancel)]
		public async Task ContainerTouchPreservesAncestorRecipientAndCoordinates(MotionEventActions finalAction)
		{
			await InvokeOnMainThreadAsync(() =>
			{
				using var root = new FrameLayout(MauiContext.Context);
				using var parent = new FrameLayout(MauiContext.Context);
				using var child = new LayoutViewGroup(MauiContext.Context);
				root.AddView(parent);
				parent.AddView(child);
				parent.Layout(0, 0, 200, 200);
				child.Layout(20, 30, 120, 130);
				child.PivotX = 0;
				child.PivotY = 0;
				child.ScaleX = 2;
				child.ScaleY = 3;
				parent.ScrollTo(3, 4);

				var actions = new List<MotionEventActions>();
				var rootEventCount = 0;
				ContainerTouchHandler.SetGestureHandler(root, e =>
				{
					rootEventCount++;
					return true;
				});
				ContainerTouchHandler.SetGestureHandler(parent, e =>
				{
					actions.Add(e.ActionMasked);
					Assert.Equal(37f, e.GetX(), 1);
					Assert.Equal(56f, e.GetY(), 1);
					return e.ActionMasked == MotionEventActions.Down;
				});

				try
				{
					var time = global::Android.OS.SystemClock.UptimeMillis();
					foreach (var action in new[] { MotionEventActions.Down, MotionEventActions.Move, finalAction })
					{
						using var e = MotionEvent.Obtain(time, time, action, 10, 10, 0);
						try
						{
							Assert.True(child.OnTouchEvent(e));
							Assert.Equal(10f, e.GetX());
							Assert.Equal(10f, e.GetY());
						}
						finally
						{
							e.Recycle();
						}
					}

					Assert.Equal(new[] { MotionEventActions.Down, MotionEventActions.Move, finalAction }, actions);
					Assert.Equal(0, rootEventCount);

					actions.Clear();
					using var afterGesture = MotionEvent.Obtain(time, time, MotionEventActions.Move, 10, 10, 0);
					try
					{
						Assert.True(child.OnTouchEvent(afterGesture));
						Assert.Empty(actions);
					}
					finally
					{
						afterGesture.Recycle();
					}
				}
				finally
				{
					ContainerTouchHandler.SetGestureHandler(parent, null);
					ContainerTouchHandler.SetGestureHandler(root, null);
				}
			});
		}

		[Fact]
		public async Task ChildIsVisibleIfWrapperIsVisible()
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var child = new Button(MauiContext.Context);
				child.Visibility = ViewStates.Gone;

				var wrapper = new WrapperView(MauiContext.Context);
				wrapper.Visibility = ViewStates.Gone;
				wrapper.AddView(child);

				Assert.Equal(ViewStates.Gone, wrapper.Visibility);
				Assert.Equal(ViewStates.Gone, child.Visibility);

				wrapper.Visibility = ViewStates.Visible;

				Assert.Equal(ViewStates.Visible, wrapper.Visibility);
				Assert.Equal(ViewStates.Visible, child.Visibility);
			});
		}

		[Fact]
		public async Task ShadowInvalidationOnlyFollowsImmediateChild()
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var wrapper = new WrapperView(MauiContext.Context);
				var child = new AView(MauiContext.Context);
				var descendant = new AView(MauiContext.Context);

				wrapper.SetSolidShadow(10, 0, 0, AColor.Black.ToArgb());

				Assert.True(wrapper.ShouldInvalidateShadow(child, child));
				Assert.False(wrapper.ShouldInvalidateShadow(child, descendant));

				wrapper.SetNoShadow();

				Assert.False(wrapper.ShouldInvalidateShadow(child, child));
			});
		}
	}
}