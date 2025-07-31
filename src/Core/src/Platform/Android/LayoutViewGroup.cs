using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Microsoft.Maui.Graphics;
using ARect = Android.Graphics.Rect;
using Rectangle = Microsoft.Maui.Graphics.Rect;
using Size = Microsoft.Maui.Graphics.Size;

namespace Microsoft.Maui.Platform
{
	public class LayoutViewGroup : ViewGroup, ICrossPlatformLayoutBacking, IVisualTreeElementProvidable
	{
		readonly ARect _clipRect = new();
		readonly Context _context;
		SafeAreaPadding _safeArea = SafeAreaPadding.Empty;
		bool _safeAreaInvalidated = true;

		// Keyboard tracking
		SafeAreaPadding _keyboardInsets = SafeAreaPadding.Empty;
		bool _isKeyboardShowing;

		public bool InputTransparent { get; set; }

		public LayoutViewGroup(Context context) : base(context)
		{
			_context = context;
			SetupWindowInsetsHandling();
		}

		public LayoutViewGroup(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			var context = Context;
			ArgumentNullException.ThrowIfNull(context);
			_context = context;
			SetupWindowInsetsHandling();
		}

		public LayoutViewGroup(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			_context = context;
			SetupWindowInsetsHandling();
		}

		public LayoutViewGroup(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
		{
			_context = context;
			SetupWindowInsetsHandling();
		}

		public LayoutViewGroup(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
		{
			_context = context;
			SetupWindowInsetsHandling();
		}

		void SetupWindowInsetsHandling()
		{
			ViewCompat.SetOnApplyWindowInsetsListener(this, new WindowInsetsListener(this));
		}

		// This method should be called when SafeAreaRegions change at runtime
		internal void InvalidateSafeArea()
		{
			_safeAreaInvalidated = true;
			RequestLayout();
		}

		bool ShouldSubscribeToKeyboardNotifications()
		{
			// Only subscribe if any edge has SoftInput regions (matching iOS behavior)
			if (CrossPlatformLayout is ISafeAreaView2 safeAreaPage)
			{
				for (int edge = 0; edge < 4; edge++)
				{
					var region = safeAreaPage.GetSafeAreaRegionsForEdge(edge);
					if (SafeAreaEdges.IsSoftInput(region))
					{
						return true;
					}
				}
			}
			return false;
		}

		static void UpdateKeyboardSubscription()
		{
			// Update keyboard subscription based on current SafeAreaEdges settings (matching iOS behavior)
			// For Android, this is handled automatically through WindowInsetsListener
			// This method exists for API compatibility with iOS pattern
		}

		static void SubscribeToKeyboardNotifications()
		{
			// Keyboard handling is already integrated into WindowInsetsListener
			// This method exists for API compatibility with iOS pattern
		}

		static void UnsubscribeFromKeyboardNotifications()
		{
			// Keyboard handling is already integrated into WindowInsetsListener  
			// This method exists for API compatibility with iOS pattern
		}

		void OnKeyboardChanged(SafeAreaPadding keyboardInsets, bool isShowing)
		{
			_keyboardInsets = keyboardInsets;
			_isKeyboardShowing = isShowing;
			_safeAreaInvalidated = true;
			RequestLayout();
		}

		public bool ClipsToBounds { get; set; }

		public ICrossPlatformLayout? CrossPlatformLayout
		{
			get; set;
		}

		Graphics.Size CrossPlatformMeasure(double widthConstraint, double heightConstraint)
		{
			return CrossPlatformLayout?.CrossPlatformMeasure(widthConstraint, heightConstraint) ?? Graphics.Size.Zero;
		}

		Graphics.Size CrossPlatformArrange(Graphics.Rect bounds)
		{
			return CrossPlatformLayout?.CrossPlatformArrange(bounds) ?? Graphics.Size.Zero;
		}

		bool RespondsToSafeArea()
		{
			// Don't apply safe area if a parent has already handled it
			if (HasSafeAreaHandlingParent())
				return false;

			var respondsToSafeArea = CrossPlatformLayout is ISafeAreaView2;

			// If we respond to safe area, ensure safe area is up to date
			// This handles cases where SafeAreaRegions changed at runtime
			if (respondsToSafeArea)
			{
				// Force a safe area validation to ensure we have current settings
				_safeAreaInvalidated = true;
			}

			return respondsToSafeArea;
		}

		bool HasSafeAreaHandlingParent()
		{
			// Walk up the view hierarchy to check if any parent is handling safe area
			var parent = Parent;
			while (parent != null)
			{
				// Check if parent is a ContentViewGroup or LayoutViewGroup that responds to safe area
				if (parent is ContentViewGroup parentContentGroup &&
					parentContentGroup.CrossPlatformLayout is ISafeAreaView2 parentLayout)
				{
					// Check if the parent actually wants to handle safe area (not all edges are None)
					for (int edge = 0; edge < 4; edge++)
					{
						if (parentLayout.GetSafeAreaRegionsForEdge(edge) != SafeAreaRegions.None)
							return true;
					}
				}
				else if (parent is LayoutViewGroup parentLayoutGroup &&
						 parentLayoutGroup.CrossPlatformLayout is ISafeAreaView2 parentLayoutView)
				{
					// Check if the parent actually wants to handle safe area (not all edges are None)
					for (int edge = 0; edge < 4; edge++)
					{
						if (parentLayoutView.GetSafeAreaRegionsForEdge(edge) != SafeAreaRegions.None)
							return true;
					}
				}

				// Move up to next parent - need to check if it's a View first
				if (parent is View parentView)
					parent = parentView.Parent;
				else
					break;
			}

			return false;
		}

		SafeAreaRegions GetSafeAreaRegionForEdge(int edge)
		{
			if (CrossPlatformLayout is ISafeAreaView2 safeAreaPage)
			{
				return safeAreaPage.GetSafeAreaRegionsForEdge(edge);
			}

			// Fallback to legacy ISafeAreaView behavior
			if (CrossPlatformLayout is ISafeAreaView sav)
			{
				return sav.IgnoreSafeArea ? SafeAreaRegions.None : SafeAreaRegions.Container;
			}

			return SafeAreaRegions.None;
		}

		static double GetSafeAreaForEdge(SafeAreaRegions safeAreaRegion, double originalSafeArea)
		{
			// Edge-to-edge content - no safe area padding (Default behaves like None)
			if (safeAreaRegion == SafeAreaRegions.None || safeAreaRegion == SafeAreaRegions.Default)
				return 0;

			// All should respect all safe area insets
			if (safeAreaRegion == SafeAreaRegions.All)
				return originalSafeArea;

			// Container region - content flows under keyboard but stays out of bars/notch
			if (SafeAreaEdges.IsContainer(safeAreaRegion))
				return originalSafeArea;

			// SoftInput region - handled separately in GetAdjustedSafeAreaInsets for keyboard-specific logic
			if (SafeAreaEdges.IsSoftInput(safeAreaRegion))
				return originalSafeArea;

			// Any other combination of flags - respect safe area
			return originalSafeArea;
		}

		Graphics.Rect AdjustForSafeArea(Graphics.Rect bounds)
		{
			ValidateSafeArea();

			if (_safeArea.IsEmpty)
				return bounds;

			return _safeArea.InsetRectF(bounds);
		}

		SafeAreaPadding GetAdjustedSafeAreaInsets()
		{
			// Get WindowInsets if available
			var rootView = RootView;
			if (rootView == null)
				return SafeAreaPadding.Empty;

			var windowInsets = ViewCompat.GetRootWindowInsets(rootView);
			if (windowInsets == null)
				return SafeAreaPadding.Empty;

			var baseSafeArea = windowInsets.ToSafeAreaInsets(_context);

			// Check if keyboard-aware safe area adjustments are needed (matching iOS logic)
			if (CrossPlatformLayout is ISafeAreaView2 safeAreaPage && _isKeyboardShowing)
			{
				// Check if any edge has SafeAreaRegions.SoftInput set
				var needsKeyboardAdjustment = false;
				for (int edge = 0; edge < 4; edge++)
				{
					var safeAreaRegion = safeAreaPage.GetSafeAreaRegionsForEdge(edge);
					if (SafeAreaEdges.IsSoftInput(safeAreaRegion))
					{
						needsKeyboardAdjustment = true;
						break;
					}
				}

				if (needsKeyboardAdjustment)
				{
					// For SafeAreaRegions.SoftInput: Always pad so content doesn't go under the keyboard
					// Bottom edge is most commonly affected by keyboard
					var bottomEdgeRegion = safeAreaPage.GetSafeAreaRegionsForEdge(3); // 3 = bottom edge
					if (SafeAreaEdges.IsSoftInput(bottomEdgeRegion))
					{
						// Use the larger of the current bottom safe area or the keyboard height
						var adjustedBottom = Math.Max(baseSafeArea.Bottom, _keyboardInsets.Bottom);
						baseSafeArea = new SafeAreaPadding(baseSafeArea.Left, baseSafeArea.Right, baseSafeArea.Top, adjustedBottom);
					}
				}
			}

			// Apply safe area selectively per edge based on SafeAreaRegions
			if (CrossPlatformLayout is ISafeAreaView2)
			{
				var left = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(0), baseSafeArea.Left);
				var top = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(1), baseSafeArea.Top);
				var right = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(2), baseSafeArea.Right);
				var bottom = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(3), baseSafeArea.Bottom);

				return new SafeAreaPadding(left, right, top, bottom);
			}

			// Legacy ISafeAreaView handling
			if (CrossPlatformLayout is ISafeAreaView sav && sav.IgnoreSafeArea)
			{
				return SafeAreaPadding.Empty;
			}

			return baseSafeArea;
		}

		bool ValidateSafeArea()
		{
			if (!_safeAreaInvalidated)
				return true;

			_safeAreaInvalidated = false;

			var oldSafeArea = _safeArea;
			_safeArea = GetAdjustedSafeAreaInsets();

			return oldSafeArea == _safeArea;
		}

		// TODO: Possibly reconcile this code with ViewHandlerExtensions.MeasureVirtualView
		// If you make changes here please review if those changes should also
		// apply to ViewHandlerExtensions.MeasureVirtualView
		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (CrossPlatformMeasure == null)
			{
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
				return;
			}

			var deviceIndependentWidth = widthMeasureSpec.ToDouble(_context);
			var deviceIndependentHeight = heightMeasureSpec.ToDouble(_context);

			var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
			var heightMode = MeasureSpec.GetMode(heightMeasureSpec);

			var measure = CrossPlatformMeasure(deviceIndependentWidth, deviceIndependentHeight);

			// If the measure spec was exact, we should return the explicit size value, even if the content
			// measure came out to a different size
			var width = widthMode == MeasureSpecMode.Exactly ? deviceIndependentWidth : measure.Width;
			var height = heightMode == MeasureSpecMode.Exactly ? deviceIndependentHeight : measure.Height;

			var platformWidth = _context.ToPixels(width);
			var platformHeight = _context.ToPixels(height);

			// Minimum values win over everything
			platformWidth = Math.Max(MinimumWidth, platformWidth);
			platformHeight = Math.Max(MinimumHeight, platformHeight);

			SetMeasuredDimension((int)platformWidth, (int)platformHeight);
		}

		// TODO: Possibly reconcile this code with ViewHandlerExtensions.MeasureVirtualView
		// If you make changes here please review if those changes should also
		// apply to ViewHandlerExtensions.MeasureVirtualView
		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			if (CrossPlatformArrange == null || _context == null)
			{
				return;
			}

			var destination = _context.ToCrossPlatformRectInReferenceFrame(l, t, r, b);

			// Apply safe area adjustments if needed
			if (RespondsToSafeArea())
			{
				destination = AdjustForSafeArea(destination);
			}

			CrossPlatformArrange(destination);

			if (ClipsToBounds)
			{
				_clipRect.Right = r - l;
				_clipRect.Bottom = b - t;
				ClipBounds = _clipRect;
			}
			else
			{
				ClipBounds = null;
			}
		}

		public override bool OnTouchEvent(MotionEvent? e)
		{
			if (InputTransparent)
			{
				return false;
			}

			return base.OnTouchEvent(e);
		}

		IVisualTreeElement? IVisualTreeElementProvidable.GetElement()
		{
			if (CrossPlatformLayout is IVisualTreeElement layoutElement &&
				layoutElement.IsThisMyPlatformView(this))
			{
				return layoutElement;
			}

			return null;
		}

		class WindowInsetsListener : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
		{
			private readonly LayoutViewGroup _owner;

			public WindowInsetsListener(LayoutViewGroup owner)
			{
				_owner = owner;
			}

			public WindowInsetsCompat? OnApplyWindowInsets(View? v, WindowInsetsCompat? insets)
			{
				_owner._safeAreaInvalidated = true;

				// Track keyboard state (matching iOS approach)
				if (insets != null)
				{
					var keyboardInsets = insets.GetKeyboardInsets(_owner._context);
					var wasKeyboardShowing = _owner._isKeyboardShowing;
					_owner._isKeyboardShowing = !keyboardInsets.IsEmpty;
					_owner._keyboardInsets = keyboardInsets;

					// If keyboard state changed, trigger layout update
					if (wasKeyboardShowing != _owner._isKeyboardShowing)
					{
						_owner.RequestLayout();
					}
				}

				_owner.RequestLayout();
				return insets;
			}
		}
	}
}
