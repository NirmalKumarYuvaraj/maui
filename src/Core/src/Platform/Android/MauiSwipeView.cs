using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Graphics;
using APointF = Android.Graphics.PointF;
using ARect = Android.Graphics.Rect;
using AView = Android.Views.View;
using AWebView = Android.Webkit.WebView;
using SDebug = System.Diagnostics.Debug;

namespace Microsoft.Maui.Platform;

public class MauiSwipeView : ContentViewGroup
{
	const float OpenSwipeThresholdPercentage = 0.6f; // 60%
	const long SwipeAnimationDuration = 200;

	readonly Dictionary<ISwipeItem, object> _swipeItems;
	readonly Context _context;
	SwipeViewPager? _viewPagerParent;
	AView? _contentView;
	LinearLayoutCompat? _actionView;
	SwipeTransitionMode _swipeTransitionMode;
	float _density;
	bool _isTouchDown;
	bool _isSwiping;
	APointF? _initialPoint;
	SwipeDirection? _swipeDirection;
	float _swipeOffset;
	float _swipeThreshold;
	bool _isSwipeEnabled;
	bool _isResettingSwipe;
	bool _isOpen;
	OpenSwipeItem _previousOpenSwipeItem;
	IMauiContext MauiContext => Element?.Handler?.MauiContext ?? throw new InvalidOperationException("MauiContext cannot be null here");

	internal AView Control { get; }
	internal ISwipeView? Element { get; private set; }

	public MauiSwipeView(Context context) : base(context)
	{
		_context = context;

		_swipeItems = new Dictionary<ISwipeItem, object>();

		SetClipChildren(false);
		SetClipToPadding(false);

		_density = context.GetActivity()?.Resources?.DisplayMetrics?.Density ?? 0;
		Control = new AView(_context);
		AddView(Control, LayoutParams.MatchParent);
	}

	// temporary workaround to make it work
	internal void SetElement(ISwipeView swipeView)
	{
		Element = swipeView;

		bool clipToOutline = Element?.Shadow is null && (Element?.Content as IView)?.Shadow is null;
		this.SetClipToOutline(clipToOutline);
	}

	protected override void OnAttachedToWindow()
	{
		base.OnAttachedToWindow();

		if (Control is not null && Control.Parent is not null && _viewPagerParent is null)
		{
			_viewPagerParent = Control.Parent.GetParentOfType<SwipeViewPager>();
		}
	}

	protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
	{
		base.OnLayout(changed, left, top, right, bottom);

		if (_contentView is null || _actionView is null || GetNativeSwipeItems() is not { Count: > 0 } swipeItems)
		{
			return;
		}

		LayoutSwipeItems(swipeItems);
	}

	public override bool OnTouchEvent(MotionEvent? e)
	{
		base.OnTouchEvent(e);

		if (e?.Action == MotionEventActions.Move && !ShouldInterceptTouch(e))
		{
			return true;
		}

		ProcessSwipingInteractions(e);

		return true;
	}

	bool ShouldInterceptTouch(MotionEvent? e)
	{
		if (_initialPoint is null || e is null)
		{
			return false;
		}

		var interceptPoint = new Point(e.GetX() / _density, e.GetY() / _density);

		var diffX = interceptPoint.X - _initialPoint.X;
		var diffY = interceptPoint.Y - _initialPoint.Y;

		SwipeDirection swipeDirection;

		if (Math.Abs(diffX) > Math.Abs(diffY))
		{
			swipeDirection = diffX > 0 ? SwipeDirection.Right : SwipeDirection.Left;
		}
		else
		{
			swipeDirection = diffY > 0 ? SwipeDirection.Down : SwipeDirection.Up;
		}

		var items = GetSwipeItemsByDirection(swipeDirection);

		if (items is null || items.Count == 0)
		{
			return false;
		}

		return ShouldInterceptScrollChildrenTouch(swipeDirection);
	}

	bool ShouldInterceptScrollChildrenTouch(SwipeDirection swipeDirection)
	{
		if (_contentView is null || _initialPoint is null)
		{
			return false;
		}

		var viewGroup = _contentView as ViewGroup;

		if (viewGroup is not null)
		{
			int x = (int)(_initialPoint.X * _density);
			int y = (int)(_initialPoint.Y * _density);

			bool isHorizontal = swipeDirection == SwipeDirection.Left || swipeDirection == SwipeDirection.Right;

			for (int i = 0; i < viewGroup.ChildCount; i++)
			{
				var child = viewGroup.GetChildAt(i);

				if (child is not null && IsViewInBounds(child, x, y))
				{
					switch (child)
					{
						case AbsListView absListView:
							return ShouldInterceptScrollChildrenTouch(absListView, isHorizontal);
						case RecyclerView recyclerView:
							return ShouldInterceptScrollChildrenTouch(recyclerView, isHorizontal);
						case NestedScrollView scrollView:
							return ShouldInterceptScrollChildrenTouch(scrollView, isHorizontal);
						case AWebView webView:
							return ShouldInterceptScrollChildrenTouch(webView, isHorizontal);
					}
				}
			}
		}

		return true;
	}

	static bool ShouldInterceptScrollChildrenTouch(ViewGroup scrollView, bool isHorizontal)
	{
		AView? scrollViewContent = scrollView.GetChildAt(0);

		if (scrollViewContent is not null)
		{
			if (isHorizontal)
			{
				return scrollView.ScrollX == 0 || scrollView.Width == scrollViewContent.Width + scrollView.PaddingLeft + scrollView.PaddingRight;
			}
			else
			{
				return scrollView.ScrollY == 0 || scrollView.Height == scrollViewContent.Height + scrollView.PaddingTop + scrollView.PaddingBottom;
			}
		}

		return true;
	}

	static bool IsViewInBounds(AView view, int x, int y)
	{
		ARect outRect = new ARect();
		view.GetHitRect(outRect);

		return x > outRect.Left && x < outRect.Right && y > outRect.Top && y < outRect.Bottom;
	}

	public override bool OnInterceptTouchEvent(MotionEvent? e)
	{
		return ShouldInterceptTouch(e);
	}

	public override bool DispatchTouchEvent(MotionEvent? e)
	{
		switch (e?.Action)
		{
			case MotionEventActions.Down:
				_initialPoint = new APointF(e.GetX() / _density, e.GetY() / _density);
				break;
			case MotionEventActions.Move:
				ResetSwipe(e);
				break;
			case MotionEventActions.Up:
				var touchUpPoint = new APointF(e.GetX() / _density, e.GetY() / _density);
				if (CanProcessTouchSwipeItems(touchUpPoint))
				{
					ProcessTouchSwipeItems(touchUpPoint);
				}
				else
				{
					ResetSwipe(e);

					// Prevent parent touch propagation during swipe gestures to avoid interference with swipe functionality.
					if (!_isSwiping)
					{
						PropagateParentTouch();
					}
				}
				break;
		}

		return base.DispatchTouchEvent(e);
	}

	void PropagateParentTouch()
	{
		if (_contentView is null)
		{
			return;
		}

		AView? itemContentView = null;

		var parentFound = _contentView.FindParent(parent =>
		{
			if (parent is RecyclerView)
			{
				return true;
			}

			itemContentView = parent as AView;
			return false;
		});

		if (parentFound is not null)
		{
			itemContentView?.CallOnClick();
		}
	}

	internal void UpdateContent()
	{
		if (_contentView is not null)
		{
			if (!_contentView.IsDisposed())
			{
				_contentView.RemoveFromParent();
				_contentView.Dispose();
			}
			_contentView = null;
		}


		if (Element?.PresentedContent is IView view)
		{
			_contentView = view.ToPlatform(MauiContext);
		}
		else
		{
			_contentView = CreateEmptyContent();
		}

		_contentView.RemoveFromParent();
		AddView(_contentView);
	}

	AView CreateEmptyContent()
	{
		var emptyContentView = new AView(_context);
		emptyContentView.SetBackgroundColor(Colors.Transparent.ToPlatform());

		return emptyContentView;
	}

	ISwipeItems? GetSwipeItemsByDirection()
	{
		if (_swipeDirection.HasValue)
		{
			return GetSwipeItemsByDirection(_swipeDirection.Value);
		}

		return null;
	}

	ISwipeItems? GetSwipeItemsByDirection(SwipeDirection swipeDirection) => swipeDirection switch
	{
		SwipeDirection.Left => Element?.RightItems,
		SwipeDirection.Right => Element?.LeftItems,
		SwipeDirection.Up => Element?.BottomItems,
		SwipeDirection.Down => Element?.TopItems,
		_ => null,
	};

	bool IsHorizontalSwipe()
	{
		return _swipeDirection == SwipeDirection.Left || _swipeDirection == SwipeDirection.Right;
	}

	bool IsNegativeSwipe()
	{
		return _swipeDirection == SwipeDirection.Left || _swipeDirection == SwipeDirection.Up;
	}

	static bool IsValidSwipeItems(ISwipeItems? swipeItems)
	{
		return swipeItems is not null && swipeItems.Any(GetIsVisible);
	}

	bool ProcessSwipingInteractions(MotionEvent? e)
	{
		if (e is null)
		{
			return false;
		}

		bool handled = true;
		var point = new APointF(e.GetX() / _density, e.GetY() / _density);

		switch (e.Action)
		{
			case MotionEventActions.Down:
				handled = HandleTouchInteractions(GestureStatus.Started, point);
				if (handled)
				{
					Parent?.RequestDisallowInterceptTouchEvent(true);
				}

				break;

			case MotionEventActions.Up:
				handled = HandleTouchInteractions(GestureStatus.Completed, point);
				Parent?.RequestDisallowInterceptTouchEvent(false);
				break;

			case MotionEventActions.Move:
				handled = HandleTouchInteractions(GestureStatus.Running, point);
				if (!handled)
				{
					Parent?.RequestDisallowInterceptTouchEvent(true);
				}

				break;

			case MotionEventActions.Cancel:
				handled = HandleTouchInteractions(GestureStatus.Canceled, point);
				Parent?.RequestDisallowInterceptTouchEvent(false);
				break;
		}

		return !handled;
	}

	bool HandleTouchInteractions(GestureStatus status, APointF point)
	{
		if (!_isSwipeEnabled)
		{
			return false;
		}

		switch (status)
		{
			case GestureStatus.Started:
				return !ProcessTouchDown(point);
			case GestureStatus.Running:
				return !ProcessTouchMove(point);
			case GestureStatus.Canceled:
			case GestureStatus.Completed:
				ProcessTouchUp();
				break;
		}

		_isTouchDown = false;

		return true;
	}

	bool ProcessTouchDown(APointF point)
	{
		if (_isSwiping || _isTouchDown || _contentView is null)
		{
			return false;
		}

		_initialPoint = point;
		_isTouchDown = true;

		return true;
	}

	bool ProcessTouchMove(APointF point)
	{
		if (_contentView is null || !TouchInsideContent(point) || _initialPoint is null)
		{
			return false;
		}

		if (!_isOpen)
		{
			ResetSwipeToInitialPosition();

			_swipeDirection = SwipeDirectionHelper.GetSwipeDirection(new Point(_initialPoint.X, _initialPoint.Y), new Point(point.X, point.Y));

			UpdateSwipeItems();
		}

		if (!_isSwiping)
		{
			RaiseSwipeStarted();
			_isSwiping = true;
		}

		if (!ValidateSwipeDirection() || _isResettingSwipe)
		{
			return false;
		}

		EnableParentGesture(false);
		_swipeOffset = GetSwipeOffset(_initialPoint, point);
		UpdateIsOpen(_swipeOffset != 0);

		UpdateSwipeItems();

		if (Math.Abs(_swipeOffset) > double.Epsilon)
		{
			Swipe();
		}

		RaiseSwipeChanging();

		return true;
	}

	bool ProcessTouchUp()
	{
		_isTouchDown = false;

		EnableParentGesture(true);

		if (!_isSwiping)
		{
			return false;
		}

		_isSwiping = false;

		RaiseSwipeEnded();

		if (_isResettingSwipe || !ValidateSwipeDirection())
		{
			return false;
		}

		ValidateSwipeThreshold();

		return false;
	}

	bool CanProcessTouchSwipeItems(APointF point)
	{
		// We only invoke the SwipeItem command if we tap on the SwipeItems area
		// and the SwipeView is fully open.
		return !TouchInsideContent(point) && _swipeOffset == _swipeThreshold;
	}

	bool TouchInsideContent(APointF point)
	{
		if (_contentView is null)
		{
			return false;
		}

		bool touchContent = TouchInsideContent(_contentView.Left + _contentView.TranslationX, _contentView.Top + _contentView.TranslationY, _contentView.Width, _contentView.Height, _context.ToPixels(point.X), _context.ToPixels(point.Y));

		return touchContent;
	}

	static bool TouchInsideContent(double x1, double y1, double x2, double y2, double x, double y)
	{
		return x > x1 && x < (x1 + x2) && y > y1 && y < (y1 + y2);
	}

	bool ValidateSwipeDirection()
	{
		if (_swipeDirection is null)
		{
			return false;
		}

		var swipeItems = GetSwipeItemsByDirection();
		return IsValidSwipeItems(swipeItems);
	}

	float GetSwipeOffset(APointF initialPoint, APointF endPoint)
	{
		float swipeOffset = _swipeDirection switch
		{
			SwipeDirection.Left or SwipeDirection.Right => endPoint.X - initialPoint.X,
			SwipeDirection.Up or SwipeDirection.Down => endPoint.Y - initialPoint.Y,
			_ => 0
		};

		return swipeOffset != 0 ? swipeOffset : GetSwipeContentOffset();
	}

	float GetSwipeContentOffset()
	{
		if (_contentView is null)
		{
			return 0;
		}

		return _swipeDirection switch
		{
			SwipeDirection.Left or SwipeDirection.Right => _contentView.TranslationX,
			SwipeDirection.Up or SwipeDirection.Down => _contentView.TranslationY,
			_ => 0
		};
	}

	void UpdateSwipeItems()
	{
		if (_contentView is null || _contentView.IsDisposed() || _actionView is not null)
		{
			return;
		}

		ISwipeItems? items = GetSwipeItemsByDirection();

		if (items?.Count == 0 || items is null)
		{
			return;
		}

		_actionView = new LinearLayoutCompat(_context);

		using (var layoutParams = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent))
		{
			_actionView.LayoutParameters = layoutParams;
		}

		_actionView.Orientation = LinearLayoutCompat.Horizontal;

		var swipeItems = new List<AView>();

		foreach (var item in items)
		{
			AView? swipeItem = item?.ToPlatform(MauiContext);

			if (swipeItem is not null)
			{
				_actionView.AddView(swipeItem);

				if (item is ISwipeItemView formsSwipeItemView)
				{
					UpdateSwipeItemViewLayout(formsSwipeItemView);
					_swipeItems.Add(formsSwipeItemView, swipeItem);
				}
				else if (item is ISwipeItemMenuItem menuItem)
				{
					_swipeItems.Add(item, swipeItem);
				}

				swipeItems.Add(swipeItem);
			}
		}

		AddView(_actionView);
		if (_contentView is not null)
		{
			_contentView.BringToFront();
			int contextX = (int)_contentView.GetX();
			int contentY = (int)_contentView.GetY();
			int contentWidth = _contentView.Width;
			int contentHeight = _contentView.Height;
			_actionView.Layout(contextX, contentY, contextX + contentWidth, contentY + contentHeight);
		}
		LayoutSwipeItems(swipeItems);
		swipeItems.Clear();
	}

	void LayoutSwipeItems(List<AView> childs)
	{
		if (_actionView is null || childs is null || _contentView is null)
		{
			return;
		}

		var items = GetSwipeItemsByDirection();

		if (items is null || items.Count == 0)
		{
			return;
		}

		int i = 0;
		int previousWidth = 0;

		foreach (var child in childs)
		{
			if (child.Visibility == ViewStates.Visible)
			{
				var item = items[i];
				var swipeItemSize = GetSwipeItemSize(item);

				int contentWidth = _contentView.Width;
				int contentHeight = _contentView.Height;
				var swipeItemHeight = (int)_context.ToPixels(swipeItemSize.Height);
				var swipeItemWidth = (int)_context.ToPixels(swipeItemSize.Width);

				int l, t, r, b;
				switch (_swipeDirection)
				{
					case SwipeDirection.Left:
						// Filling from right to left, align to the top
						l = contentWidth - previousWidth - swipeItemWidth;
						t = 0;
						r = contentWidth - previousWidth;
						b = swipeItemHeight;
						break;
					case SwipeDirection.Right:
					case SwipeDirection.Down:
						// Filling from left to right, align to the top
						l = previousWidth;
						t = 0;
						r = previousWidth + swipeItemWidth;
						b = swipeItemHeight;
						break;
					default:
						SDebug.Assert(_swipeDirection == SwipeDirection.Up);
						// Filling from left to right, align to the bottom
						l = previousWidth;
						t = contentHeight - swipeItemHeight;
						r = previousWidth + swipeItemWidth;
						b = contentHeight;
						break;
				}

				child.Measure(
					MeasureSpec.MakeMeasureSpec(swipeItemWidth, MeasureSpecMode.AtMost),
					MeasureSpec.MakeMeasureSpec(swipeItemHeight, MeasureSpecMode.AtMost)
				);

				child.Layout(l, t, r, b);

				i++;
				previousWidth += swipeItemWidth;
			}
		}
	}

	internal void UpdateIsVisibleSwipeItem(ISwipeItem item)
	{
		if (!_isOpen)
		{
			return;
		}

		_swipeItems.TryGetValue(item, out object? view);

		if (view is not null && view is AView platformView)
		{
			_swipeThreshold = 0;
			LayoutSwipeItems(GetNativeSwipeItems());
			SwipeToThreshold(false);
		}
	}

	List<AView> GetNativeSwipeItems()
	{
		var swipeItems = new List<AView>();

		if (_actionView is null)
		{
			return swipeItems;
		}

		for (int i = 0; i < _actionView.ChildCount; i++)
		{
			var view = _actionView.GetChildAt(i);
			if (view is null)
			{
				continue;
			}

			swipeItems.Add(view);
		}

		return swipeItems;
	}

	void UpdateSwipeItemViewLayout(ISwipeItemView swipeItemView)
	{
		if (swipeItemView?.Handler is not IPlatformViewHandler handler)
		{
			return;
		}

		var swipeItemSize = GetSwipeItemSize(swipeItemView);
		handler.LayoutVirtualView(0, 0, (int)swipeItemSize.Width, (int)swipeItemSize.Height);

		swipeItemView?.Handler?.ToPlatform().InvalidateMeasure(swipeItemView);
	}

	internal void UpdateIsSwipeEnabled(bool isEnabled)
	{
		_isSwipeEnabled = isEnabled;
	}

	internal void UpdateSwipeTransitionMode(SwipeTransitionMode swipeTransitionMode)
	{
		_swipeTransitionMode = swipeTransitionMode;
	}

	void DisposeSwipeItems()
	{
		_isOpen = false;

		foreach (var item in _swipeItems.Keys)
		{
			item.Handler?.DisconnectHandler();
		}

		_swipeItems.Clear();

		if (_actionView is not null)
		{
			_actionView.RemoveFromParent();
			_actionView.Dispose();
			_actionView = null;
		}

		UpdateIsOpen(false);
	}

	void Swipe(bool animated = false)
	{
		if (_contentView is null)
		{
			return;
		}

		var offset = _context.ToPixels(ValidateSwipeOffset(_swipeOffset));
		_isOpen = offset != 0;
		var duration = animated ? SwipeAnimationDuration : 0;
		bool isHorizontal = IsHorizontalSwipe();

		if (_swipeTransitionMode == SwipeTransitionMode.Reveal)
		{
			if (isHorizontal)
			{
				_contentView.Animate()?.TranslationX(offset)?.SetDuration(duration);
			}
			else
			{
				_contentView.Animate()?.TranslationY(offset)?.SetDuration(duration);
			}
		}
		else if (_swipeTransitionMode == SwipeTransitionMode.Drag && Element is not null && _actionView is not null)
		{
			if (isHorizontal)
			{
				_contentView.Animate()?.TranslationX(offset)?.SetDuration(duration);
			}
			else
			{
				_contentView.Animate()?.TranslationY(offset)?.SetDuration(duration);
			}

			int actionSize = isHorizontal
				? (int)_context.ToPixels((GetSwipeItemsByDirection()?.Count ?? 0) * SwipeViewExtensions.SwipeItemWidth)
				: _contentView.Height;
			float actionOffset = (IsNegativeSwipe() ? actionSize : -actionSize) + offset;

			if (isHorizontal)
			{
				_actionView.Animate()?.TranslationX(actionOffset)?.SetDuration(duration);
			}
			else
			{
				_actionView.Animate()?.TranslationY(actionOffset)?.SetDuration(duration);
			}
		}
	}

	void ResetSwipeToInitialPosition()
	{
		_isResettingSwipe = false;
		_isSwiping = false;
		_swipeThreshold = 0;
		_swipeDirection = null;
		DisposeSwipeItems();
	}

	void ResetSwipe(MotionEvent e, bool animated = true)
	{
		if (!_isSwiping && _isOpen)
		{
			var touchPoint = new APointF(e.GetX() / _density, e.GetY() / _density);

			if (TouchInsideContent(touchPoint))
			{
				ResetSwipe(animated);
			}
		}
	}

	void ResetSwipe(bool animated = true)
	{
		if (_contentView is null)
		{
			return;
		}

		_isResettingSwipe = true;
		_isSwiping = false;
		_swipeThreshold = 0;

		if (!animated)
		{
			_contentView.TranslationX = 0;
			_contentView.TranslationY = 0;
			DisposeSwipeItems();
			_isResettingSwipe = false;
			return;
		}

		bool isHorizontal = _swipeDirection == SwipeDirection.Left || _swipeDirection == SwipeDirection.Right;
		bool isVertical = _swipeDirection == SwipeDirection.Up || _swipeDirection == SwipeDirection.Down;

		if (isHorizontal || isVertical)
		{
			_swipeDirection = null;

			var endAction = new Java.Lang.Runnable(() =>
			{
				if (_swipeDirection is null)
				{
					DisposeSwipeItems();
				}

				_isResettingSwipe = false;
			});

			if (isHorizontal)
			{
				_contentView.Animate()?.TranslationX(0)?.SetDuration(SwipeAnimationDuration)?.WithEndAction(endAction);
			}
			else
			{
				_contentView.Animate()?.TranslationY(0)?.SetDuration(SwipeAnimationDuration)?.WithEndAction(endAction);
			}
		}
		else
		{
			_isResettingSwipe = false;
		}
	}

	void SwipeToThreshold(bool animated = true)
	{
		var duration = animated ? SwipeAnimationDuration : 0;
		_swipeOffset = GetSwipeThreshold();
		float swipeThreshold = _context.ToPixels(_swipeOffset);
		float signedThreshold = IsNegativeSwipe() ? -swipeThreshold : swipeThreshold;
		bool isHorizontal = IsHorizontalSwipe();
		var endAction = new Java.Lang.Runnable(() => { _isSwiping = false; });

		if (_swipeTransitionMode == SwipeTransitionMode.Reveal && _contentView is not null)
		{
			if (isHorizontal)
			{
				_contentView.Animate()?.TranslationX(signedThreshold)?.SetDuration(duration)?.WithEndAction(endAction);
			}
			else
			{
				_contentView.Animate()?.TranslationY(signedThreshold)?.SetDuration(duration)?.WithEndAction(endAction);
			}
		}
		else if (_swipeTransitionMode == SwipeTransitionMode.Drag && _contentView is not null && _actionView is not null && Element is not null)
		{
			if (isHorizontal)
			{
				_contentView.Animate()?.TranslationX(signedThreshold)?.SetDuration(duration)?.WithEndAction(endAction);
			}
			else
			{
				_contentView.Animate()?.TranslationY(signedThreshold)?.SetDuration(duration)?.WithEndAction(endAction);
			}

			int actionSize = isHorizontal
				? (int)_context.ToPixels((GetSwipeItemsByDirection()?.Count ?? 0) * SwipeViewExtensions.SwipeItemWidth)
				: _contentView.Height;
			float actionOffset = (IsNegativeSwipe() ? actionSize : -actionSize) + signedThreshold;

			if (isHorizontal)
			{
				_actionView.Animate()?.TranslationX(actionOffset)?.SetDuration(duration);
			}
			else
			{
				_actionView.Animate()?.TranslationY(actionOffset)?.SetDuration(duration);
			}
		}
	}

	float ValidateSwipeOffset(float offset)
	{
		var swipeThreshold = GetSwipeThreshold();

		if (IsNegativeSwipe())
		{
			if (offset > 0 || (_isResettingSwipe && offset < 0))
			{
				offset = 0;
			}

			if (Math.Abs(offset) > swipeThreshold)
			{
				return -swipeThreshold;
			}
		}
		else if (_swipeDirection == SwipeDirection.Right || _swipeDirection == SwipeDirection.Down)
		{
			if (offset < 0 || (_isResettingSwipe && offset > 0))
			{
				offset = 0;
			}

			if (Math.Abs(offset) > swipeThreshold)
			{
				return swipeThreshold;
			}
		}

		return offset;
	}

	void ValidateSwipeThreshold()
	{
		if (_swipeDirection is null)
		{
			return;
		}

		var swipeThresholdPercent = OpenSwipeThresholdPercentage * GetSwipeThreshold();

		if (Math.Abs(_swipeOffset) >= swipeThresholdPercent)
		{
			var swipeItems = GetSwipeItemsByDirection();

			if (swipeItems is null)
			{
				return;
			}

			if (swipeItems.Mode == SwipeMode.Execute)
			{
				foreach (var swipeItem in swipeItems)
				{
					if (GetIsVisible(swipeItem))
					{
						ExecuteSwipeItem(swipeItem);
					}
				}

				if (swipeItems.SwipeBehaviorOnInvoked != SwipeBehaviorOnInvoked.RemainOpen)
				{
					ResetSwipe();
				}
			}
			else
			{
				SwipeToThreshold();
			}
		}
		else
		{
			ResetSwipe();
		}
	}

	float GetSwipeThreshold()
	{
		if (Math.Abs(_swipeThreshold) > double.Epsilon)
		{
			return _swipeThreshold;
		}

		var swipeItems = GetSwipeItemsByDirection();

		if (swipeItems is null)
		{
			return 0;
		}

		_swipeThreshold = GetSwipeThreshold(swipeItems);

		return _swipeThreshold;
	}

	float GetSwipeThreshold(ISwipeItems swipeItems)
	{
		if (Element is null)
		{
			return 0f;
		}

		double threshold = Element.Threshold;

		if (threshold > 0)
		{
			return (float)threshold;
		}

		float swipeThreshold = 0;

		bool isHorizontal = IsHorizontalSwipe();

		if (swipeItems.Mode == SwipeMode.Reveal)
		{
			if (isHorizontal)
			{
				foreach (var swipeItem in swipeItems)
				{
					if (GetIsVisible(swipeItem))
					{
						var swipeItemSize = GetSwipeItemSize(swipeItem);
						swipeThreshold += (float)swipeItemSize.Width;
					}
				}
			}
			else
			{
				swipeThreshold = GetSwipeItemHeight();
			}
		}
		else
		{
			swipeThreshold = CalculateSwipeThreshold();
		}

		return ValidateSwipeThreshold(swipeThreshold);
	}

	static bool GetIsVisible(ISwipeItem swipeItem)
	{
		if (swipeItem is IView view)
		{
			return view.Visibility == Maui.Visibility.Visible;
		}
		else if (swipeItem is ISwipeItemMenuItem menuItem)
		{
			return menuItem.Visibility == Maui.Visibility.Visible;
		}

		return true;
	}

	float CalculateSwipeThreshold()
	{
		var swipeItems = GetSwipeItemsByDirection();
		if (swipeItems is null)
		{
			return SwipeViewExtensions.SwipeThreshold;
		}

		float swipeItemsHeight = 0;
		float swipeItemsWidth = 0;
		bool useSwipeItemsSize = false;

		foreach (var swipeItem in swipeItems)
		{
			if (swipeItem is ISwipeItemView)
			{
				useSwipeItemsSize = true;
			}

			if (GetIsVisible(swipeItem))
			{
				var swipeItemSize = GetSwipeItemSize(swipeItem);
				swipeItemsHeight += (float)swipeItemSize.Height;
				swipeItemsWidth += (float)swipeItemSize.Width;
			}
		}

		if (useSwipeItemsSize)
		{
			var isHorizontalSwipe = IsHorizontalSwipe();

			return isHorizontalSwipe ? swipeItemsWidth : swipeItemsHeight;
		}
		else
		{
			if (_contentView is not null)
			{
				var contentWidth = (float)_context.FromPixels(_contentView.Width);
				var contentWidthSwipeThreshold = contentWidth * 0.8f;

				return contentWidthSwipeThreshold;
			}
		}

		return SwipeViewExtensions.SwipeThreshold;
	}


	float ValidateSwipeThreshold(float swipeThreshold)
	{
		if (_contentView is null)
		{
			return swipeThreshold;
		}

		var contentHeight = (float)_context.FromPixels(_contentView.Height);
		var contentWidth = (float)_context.FromPixels(_contentView.Width);
		bool isHorizontal = IsHorizontalSwipe();

		if (isHorizontal)
		{
			if (swipeThreshold > contentWidth)
			{
				swipeThreshold = contentWidth;
			}

			return swipeThreshold;
		}

		if (swipeThreshold > contentHeight)
		{
			swipeThreshold = contentHeight;
		}

		return swipeThreshold;
	}

	Size GetSwipeItemSize(ISwipeItem swipeItem)
	{
		if (_contentView is null || Element is null)
		{
			return Size.Zero;
		}

		bool isHorizontal = IsHorizontalSwipe();
		var items = GetSwipeItemsByDirection();

		if (items is null)
		{
			return Size.Zero;
		}

		double threshold = Element.Threshold;
		double contentHeight = _context.FromPixels(_contentView.Height);
		double contentWidth = _context.FromPixels(_contentView.Width);

		if (isHorizontal)
		{
			if (swipeItem is ISwipeItemView horizontalSwipeItemView)
			{
				var swipeItemViewSizeRequest = horizontalSwipeItemView.Measure(double.PositiveInfinity, double.PositiveInfinity);

				double swipeItemWidth;

				if (swipeItemViewSizeRequest.Width > 0)
				{
					swipeItemWidth = threshold > swipeItemViewSizeRequest.Width ? threshold : (float)swipeItemViewSizeRequest.Width;
				}
				else
				{
					swipeItemWidth = threshold > SwipeViewExtensions.SwipeItemWidth ? threshold : SwipeViewExtensions.SwipeItemWidth;
				}

				return new Size(swipeItemWidth, contentHeight);
			}

			if (swipeItem is ISwipeItem)
			{
				return new Size(items.Mode == SwipeMode.Execute ? (threshold > 0 ? threshold : contentWidth) / items.Count : (threshold < SwipeViewExtensions.SwipeItemWidth ? SwipeViewExtensions.SwipeItemWidth : threshold), contentHeight);
			}
		}
		else
		{
			if (swipeItem is ISwipeItemView verticalSwipeItemView)
			{
				var swipeItemViewSizeRequest = verticalSwipeItemView.Measure(double.PositiveInfinity, double.PositiveInfinity);

				double swipeItemHeight;

				if (swipeItemViewSizeRequest.Width > 0)
				{
					swipeItemHeight = threshold > swipeItemViewSizeRequest.Height ? threshold : (float)swipeItemViewSizeRequest.Height;
				}
				else
				{
					swipeItemHeight = threshold > contentHeight ? threshold : contentHeight;
				}

				return new Size(contentWidth / items.Count, swipeItemHeight);
			}

			if (swipeItem is ISwipeItem)
			{
				var swipeItemHeight = GetSwipeItemHeight();
				return new Size(contentWidth / items.Count, (threshold > 0 && threshold < swipeItemHeight) ? threshold : swipeItemHeight);
			}
		}

		return Size.Zero;
	}

	float GetSwipeItemHeight()
	{
		if (_contentView is null)
		{
			return 0f;
		}

		var items = GetSwipeItemsByDirection();

		if (items is null)
		{
			return 0f;
		}

		float maxHeight = 0;
		bool hasSwipeItemView = false;

		foreach (var swipeItem in items)
		{
			if (swipeItem is ISwipeItemView swipeItemView)
			{
				hasSwipeItemView = true;
				var sizeRequest = swipeItemView.Measure(double.PositiveInfinity, double.PositiveInfinity);
				maxHeight = Math.Max(maxHeight, (float)sizeRequest.Height);
			}
		}

		return hasSwipeItemView ? maxHeight : (float)_context.FromPixels(_contentView.Height);
	}

	void ProcessTouchSwipeItems(APointF point)
	{
		if (_isResettingSwipe)
		{
			return;
		}

		var swipeItems = GetSwipeItemsByDirection();

		if (swipeItems is null || _actionView is null)
		{
			return;
		}

		for (int i = 0; i < _actionView.ChildCount; i++)
		{
			var swipeButton = _actionView.GetChildAt(i);

			if (swipeButton?.Visibility == ViewStates.Visible)
			{
				var swipeItemX = swipeButton.Left / _density;
				var swipeItemY = swipeButton.Top / _density;
				var swipeItemHeight = swipeButton.Height / _density;
				var swipeItemWidth = swipeButton.Width / _density;

				if (TouchInsideContent(swipeItemX, swipeItemY, swipeItemWidth, swipeItemHeight, point.X, point.Y))
				{
					var swipeItem = swipeItems[i];

					ExecuteSwipeItem(swipeItem);

					if (swipeItems.SwipeBehaviorOnInvoked != SwipeBehaviorOnInvoked.RemainOpen)
					{
						ResetSwipe();
					}

					break;
				}
			}
		}
	}

	static void ExecuteSwipeItem(ISwipeItem item)
	{
		if (item is null)
		{
			return;
		}

		bool isEnabled = item switch
		{
			ISwipeItemMenuItem menuItem => menuItem.IsEnabled,
			ISwipeItemView swipeItemView => swipeItemView.IsEnabled,
			_ => true
		};

		if (isEnabled)
		{
			item.OnInvoked();
		}
	}

	void EnableParentGesture(bool isGestureEnabled)
	{
		_viewPagerParent?.EnableGesture = isGestureEnabled;
	}

	internal void OnOpenRequested(SwipeViewOpenRequest e)
	{
		if (_contentView is null)
		{
			return;
		}

		var openSwipeItem = e.OpenSwipeItem;
		var animated = e.Animated;

		UpdateIsOpen(true);
		ProgrammaticallyOpenSwipeItem(openSwipeItem, animated);
	}

	void ProgrammaticallyOpenSwipeItem(OpenSwipeItem openSwipeItem, bool animated)
	{
		if (_isOpen)
		{
			if (_previousOpenSwipeItem == openSwipeItem)
			{
				return;
			}

			ResetSwipe(false);
		}

		_previousOpenSwipeItem = openSwipeItem;

		switch (openSwipeItem)
		{
			case OpenSwipeItem.BottomItems:
				_swipeDirection = SwipeDirection.Up;
				break;
			case OpenSwipeItem.LeftItems:
				_swipeDirection = SwipeDirection.Right;
				break;
			case OpenSwipeItem.RightItems:
				_swipeDirection = SwipeDirection.Left;
				break;
			case OpenSwipeItem.TopItems:
				_swipeDirection = SwipeDirection.Down;
				break;
		}

		var swipeItems = GetSwipeItemsByDirection();

		if (swipeItems is null || swipeItems.Count == 0)
		{
			return;
		}

		UpdateSwipeItems();

		var swipeThreshold = GetSwipeThreshold();
		UpdateOffset(swipeThreshold);

		Swipe(animated);

		_swipeOffset = Math.Abs(_swipeOffset);
	}

	void UpdateOffset(float swipeOffset)
	{
		switch (_swipeDirection)
		{
			case SwipeDirection.Right:
			case SwipeDirection.Down:
				_swipeOffset = swipeOffset;
				break;
			case SwipeDirection.Left:
			case SwipeDirection.Up:
				_swipeOffset = -swipeOffset;
				break;
		}
	}

	void UpdateIsOpen(bool isOpen)
	{
		if (Element is null)
		{
			return;
		}

		Element.IsOpen = isOpen;
	}

	internal void OnCloseRequested(SwipeViewCloseRequest e)
	{
		var animated = e.Animated;

		UpdateIsOpen(false);
		ResetSwipe(animated);
	}

	void RaiseSwipeStarted()
	{
		if (_swipeDirection is null || !ValidateSwipeDirection())
		{
			return;
		}

		Element?.SwipeStarted(new SwipeViewSwipeStarted(_swipeDirection.Value));
	}

	void RaiseSwipeChanging()
	{
		if (_swipeDirection is null)
		{
			return;
		}

		Element?.SwipeChanging(new SwipeViewSwipeChanging(_swipeDirection.Value, _swipeOffset));
	}

	void RaiseSwipeEnded()
	{
		if (_swipeDirection is null || !ValidateSwipeDirection())
		{
			return;
		}

		bool isOpen = false;

		var swipeThresholdPercent = OpenSwipeThresholdPercentage * GetSwipeThreshold();

		if (Math.Abs(_swipeOffset) >= swipeThresholdPercent)
		{
			isOpen = true;
		}

		Element?.SwipeEnded(new SwipeViewSwipeEnded(_swipeDirection.Value, isOpen));
	}
}
