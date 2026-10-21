using System;
using System.Runtime.CompilerServices;
using Android.Views;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	internal sealed class ContainerTouchHandler
	{
		static readonly ConditionalWeakTable<AView, Func<MotionEvent, bool>> GestureHandlers = new();

		AView? _gestureTarget;
		Func<MotionEvent, bool>? _gestureHandler;

		internal static void SetGestureHandler(AView view, Func<MotionEvent, bool>? handler)
		{
			GestureHandlers.Remove(view);
			if (handler is not null)
				GestureHandlers.Add(view, handler);
		}

		internal void Reset()
		{
			_gestureTarget = null;
			_gestureHandler = null;
		}

		internal bool OnTouchEvent(AView view, MotionEvent? e)
		{
			if (e is null)
				return false;

			if (e.ActionMasked == MotionEventActions.Down)
			{
				Reset();
			}

			try
			{
				if (e.ActionMasked == MotionEventActions.Down || _gestureTarget is not null)
					ForwardToAncestor(view, e);

				// Children have already declined the event. Claim it so Android does not
				// dispatch it to a sibling behind this container.
				return true;
			}
			finally
			{
				if (e.ActionMasked is MotionEventActions.Up or MotionEventActions.Cancel)
				{
					Reset();
				}
			}
		}

		void ForwardToAncestor(AView view, MotionEvent e)
		{
			using var ancestorEvent = MotionEvent.Obtain(e);
			try
			{
				var child = view;
				while (child.Parent is AView parent)
				{
					// Convert from child-local to parent-local coordinates, including
					// scrolling and transforms, without changing the original event.
					if (!child.Matrix.IsIdentity)
						ancestorEvent.Transform(child.Matrix);
					ancestorEvent.OffsetLocation(child.Left - parent.ScrollX, child.Top - parent.ScrollY);

					if (_gestureTarget is not null)
					{
						if (parent == _gestureTarget)
						{
							if (GestureHandlers.TryGetValue(parent, out var currentHandler) && currentHandler == _gestureHandler)
								currentHandler(ancestorEvent);
							return;
						}
					}
					else if (GestureHandlers.TryGetValue(parent, out var handler) && handler(ancestorEvent))
					{
						// Keep the same recipient for the entire gesture, even when its
						// detector returns false for individual move/up events.
						_gestureTarget = parent;
						_gestureHandler = handler;
						return;
					}

					child = parent;
				}
			}
			finally
			{
				ancestorEvent.Recycle();
			}
		}
	}
}
