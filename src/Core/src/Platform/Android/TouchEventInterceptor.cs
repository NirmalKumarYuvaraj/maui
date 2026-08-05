using Android.Views;

namespace Microsoft.Maui.Platform
{
	static class TouchEventInterceptor
	{
		public static bool PrepareDispatch(ITouchInterceptingView platformView)
		{
			if (platformView is WrapperView { InputTransparent: true })
				return false;

			platformView.TouchEventNotReallyHandled = false;
			return true;
		}

		public static bool CompleteDispatch<T>(
			T platformView,
			MotionEvent? motionEvent,
			bool handled,
			View.IOnTouchListener? touchListener)
			where T : View, ITouchInterceptingView
		{
			if (!handled || !platformView.TouchEventNotReallyHandled)
				return handled;

			return (touchListener?.OnTouch(platformView, motionEvent) ?? false) ||
				platformView.OnTouchEvent(motionEvent);
		}

		public static bool OnTouchEvent(View platformView, MotionEvent? motionEvent)
		{
			if (motionEvent is null || motionEvent.Action == MotionEventActions.Cancel)
				return false;

			if (platformView.Parent is not ITouchInterceptingView parent || ShouldPassThrough(platformView))
				return false;

			parent.TouchEventNotReallyHandled = true;
			return true;
		}

		static bool ShouldPassThrough(View platformView)
		{
			if (platformView is LayoutViewGroup layout)
				return layout.InputTransparent && !layout.TouchEventNotReallyHandled;

			return platformView is WrapperView { InputTransparent: true };
		}
	}

	interface ITouchInterceptingView
	{
		bool TouchEventNotReallyHandled { get; set; }
	}
}
