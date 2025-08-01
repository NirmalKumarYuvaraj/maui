using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Microsoft.Maui.Platform
{
	public class ContentViewGroup : PlatformContentViewGroup, ICrossPlatformLayoutBacking, IVisualTreeElementProvidable
	{
		IBorderStroke? _clip;
		readonly Context _context;
		SafeAreaPadding _safeArea = SafeAreaPadding.Empty;
		bool _safeAreaInvalidated = true;

		// Keyboard tracking
		SafeAreaPadding _keyboardInsets = SafeAreaPadding.Empty;
		bool _isKeyboardShowing;

		public ContentViewGroup(Context context) : base(context)
		{
			_context = context;
			SetupWindowInsetsHandling();
		}

		public ContentViewGroup(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			var context = Context;
			ArgumentNullException.ThrowIfNull(context);
			_context = context;
			SetupWindowInsetsHandling();
		}

		public ContentViewGroup(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			_context = context;
			SetupWindowInsetsHandling();
		}

		public ContentViewGroup(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
		{
			_context = context;
			SetupWindowInsetsHandling();
		}

		public ContentViewGroup(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
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
			{
				return false;
			}

			var respondsToSafeArea = CrossPlatformLayout is ISafeAreaView2;

			// If we respond to safe area, check if any edge actually needs safe area handling
			if (respondsToSafeArea && CrossPlatformLayout is ISafeAreaView2 safeAreaLayout)
			{
				// Check if any edge has a region other than None or Default
				for (int edge = 0; edge < 4; edge++)
				{
					var region = safeAreaLayout.GetSafeAreaRegionsForEdge(edge);
					if (region == SafeAreaRegions.All)
					{
						_safeAreaInvalidated = true;
						return true;
					}
				}

				return false;
			}

			return false;
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

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (CrossPlatformLayout is null)
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

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			if (CrossPlatformLayout is null)
			{
				return;
			}

			var destination = _context.ToCrossPlatformRectInReferenceFrame(left, top, right, bottom);

			// Apply safe area adjustments if needed
			if (RespondsToSafeArea())
			{
				destination = AdjustForSafeArea(destination);
			}

			CrossPlatformArrange(destination);
		}

		internal IBorderStroke? Clip
		{
			get => _clip;
			set
			{
				_clip = value;
				// NOTE: calls PostInvalidate()
				SetHasClip(_clip is not null);
			}
		}

		protected override Path? GetClipPath(int width, int height)
		{
			if (Clip is null || Clip?.Shape is null)
				return null;

			float density = _context.GetDisplayDensity();
			float strokeThickness = (float)Clip.StrokeThickness;

			// We need to inset the content clipping by the width of the stroke on both sides
			// (top and bottom, left and right), so we remove it twice from the total width/height 
			var strokeInset = 2 * strokeThickness;
			float w = (width / density) - strokeInset;
			float h = (height / density) - strokeInset;
			float x = strokeThickness;
			float y = strokeThickness;
			IShape clipShape = Clip.Shape;

			var bounds = new Graphics.RectF(x, y, w, h);

			Path? platformPath = clipShape.ToPlatform(bounds, strokeThickness, density, true);
			return platformPath;
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

		internal class WindowInsetsListener : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
		{
			private readonly ContentViewGroup _owner;

			public WindowInsetsListener(ContentViewGroup owner)
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