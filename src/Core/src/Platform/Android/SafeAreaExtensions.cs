using System;
using Android.Content;
using Android.Views;
using AndroidX.Core.View;
using Google.Android.Material.AppBar;

namespace Microsoft.Maui.Platform;

internal static class SafeAreaExtensions
{
	internal static ISafeAreaView2? GetSafeAreaView2(object? layout) =>
		layout switch
		{
			ISafeAreaView2 sav2 => sav2,
			IElementHandler { VirtualView: ISafeAreaView2 virtualSav2 } => virtualSav2,
			_ => null
		};


	// Resolves the safe area region for a single edge with a single pattern-matching
	// traversal instead of two separate interface lookups.
	internal static SafeAreaRegions GetSafeAreaRegionForEdge(int edge, ICrossPlatformLayout crossPlatformLayout) =>
		crossPlatformLayout switch
		{
			ISafeAreaView2 sav2 => sav2.GetSafeAreaRegionsForEdge(edge),
			IElementHandler { VirtualView: ISafeAreaView2 sav2 } => sav2.GetSafeAreaRegionsForEdge(edge),
			ISafeAreaView { IgnoreSafeArea: false } => SafeAreaRegions.Container,
			IElementHandler { VirtualView: ISafeAreaView { IgnoreSafeArea: false } } => SafeAreaRegions.Container,
			_ => SafeAreaRegions.None
		};

	internal static WindowInsetsCompat? ApplyAdjustedSafeAreaInsetsPx(
		WindowInsetsCompat windowInsets,
		ICrossPlatformLayout crossPlatformLayout,
		Context context,
		View view)
	{
		WindowInsetsCompat? newWindowInsets;
		var baseSafeArea = windowInsets.ToSafeAreaInsetsPx(context);
		var keyboardInsets = windowInsets.GetKeyboardInsetsPx(context);
		var isKeyboardShowing = !keyboardInsets.IsEmpty;

		var layout = crossPlatformLayout;
		var safeAreaView2 = GetSafeAreaView2(layout);
		var margins = (safeAreaView2 as IView)?.Margin ?? Thickness.Zero;

		if (safeAreaView2 is not null)
		{
			// Apply safe area selectively per edge based on SafeAreaRegions.
			// We already resolved safeAreaView2 above, so query it directly to avoid
			// repeating the interface discovery four times via GetSafeAreaRegionForEdge.
			var left = GetSafeAreaForEdge(safeAreaView2.GetSafeAreaRegionsForEdge(0), baseSafeArea.Left, 0, isKeyboardShowing, keyboardInsets);
			var top = GetSafeAreaForEdge(safeAreaView2.GetSafeAreaRegionsForEdge(1), baseSafeArea.Top, 1, isKeyboardShowing, keyboardInsets);
			var right = GetSafeAreaForEdge(safeAreaView2.GetSafeAreaRegionsForEdge(2), baseSafeArea.Right, 2, isKeyboardShowing, keyboardInsets);
			var bottom = GetSafeAreaForEdge(safeAreaView2.GetSafeAreaRegionsForEdge(3), baseSafeArea.Bottom, 3, isKeyboardShowing, keyboardInsets);

			var globalWindowInsetsListener = MauiWindowInsetListener.FindListenerForView(view);
			bool hasTrackedViews = globalWindowInsetsListener?.HasTrackedView == true;

			// If this view has no safe area padding to apply, pass insets through to children
			// instead of consuming them. This allows child views with SafeAreaEdges set
			// to properly handle the insets even when the parent has SafeAreaEdges.None
			// However, if this view was previously tracked (had padding before), we need to
			// continue processing to reset the padding to 0
			if (left == 0 && right == 0 && top == 0 && bottom == 0)
			{
				// Only pass through if this view hasn't been tracked yet
				// If it was tracked, we need to reset its padding
				bool isViewTracked = globalWindowInsetsListener?.IsViewTracked(view) == true;
				if (!isViewTracked)
				{
					// Don't consume insets - pass them through for potential child views to handle
					return windowInsets;
				}
			}


			if (isKeyboardShowing &&
				context.GetActivity()?.Window is Window window &&
				window?.Attributes is WindowManagerLayoutParams attr)
			{
				// When AdjustPan is set, the window pans instead of resizing
				// so we should not modify any padding - just consume the insets and return
				// Use MaskAdjust to properly distinguish AdjustPan from AdjustNothing
				var softInputMode = attr.SoftInputMode;
				var adjustMode = softInputMode & SoftInput.MaskAdjust;
				if (adjustMode == SoftInput.AdjustPan)
				{
					return WindowInsetsCompat.Consumed;
				}
			}

			// Check intersection with view bounds to determine which edges actually need padding
			// If we don't have any tracked views yet we will find the first view to pad
			// in order to limit duplicate measures
			var viewWidth = view.Width > 0 ? view.Width : view.MeasuredWidth;
			var viewHeight = view.Height > 0 ? view.Height : view.MeasuredHeight;

			if ((viewHeight > 0 && viewWidth > 0) || !hasTrackedViews)
			{
				if (left == 0 && right == 0 && top == 0 && bottom == 0)
				{
					SetPaddingIfChanged(view, 0, 0, 0, 0);
					return windowInsets;
				}

				// Reduce each edge to the amount the view actually overlaps the safe area,
				// accounting for margins and in-flight Shell/navigation animations.
				(left, top, right, bottom) = AdjustInsetsForViewBounds(
					context, view, left, top, right, bottom, margins, viewWidth, viewHeight, hasTrackedViews);

				newWindowInsets = BuildRemainingInsets(
					windowInsets, left, top, right, bottom, isKeyboardShowing, keyboardInsets);

				// Apply all insets to content view group.
				// Only write padding when it actually changes to avoid triggering an
				// unnecessary layout pass on every inset dispatch.
				SetPaddingIfChanged(view, (int)left, (int)top, (int)right, (int)bottom);
				if (left > 0 || right > 0 || top > 0 || bottom > 0)
				{
					globalWindowInsetsListener?.TrackView(view);
				}
			}
			else
			{
				newWindowInsets = windowInsets;
			}
		}
		else
		{
			newWindowInsets = windowInsets;
		}

		// Fallback: return the base safe area for legacy views
		return newWindowInsets;
	}

	internal static double GetSafeAreaForEdge(SafeAreaRegions safeAreaRegion, double originalSafeArea, int edge, bool isKeyboardShowing, SafeAreaPadding keyBoardInsets)
	{
		// Edge-to-edge content - no safe area padding
		if (safeAreaRegion == SafeAreaRegions.None)
		{
			return 0;
		}

		// Handle SoftInput specifically - only apply keyboard insets for bottom edge when keyboard is showing
		if (edge == 3)
		{
			if (SafeAreaEdges.IsOnlySoftInput(safeAreaRegion))
			{
				// SoftInput only applies padding when keyboard is showing
				return isKeyboardShowing ? keyBoardInsets.Bottom : 0;
			}

			if (isKeyboardShowing)
			{
				// Return keyboard insets for any region that includes SoftInput
				if (SafeAreaEdges.IsSoftInput(safeAreaRegion))
					return keyBoardInsets.Bottom;

				// if the keyboard is showing then we will just return 0 for the bottom inset
				// because that part of the view is covered by the keyboard so we don't want to pad the view
				return 0;
			}
		}

		// All other regions respect safe area in some form
		// This includes:
		// - Default: Platform default behavior
		// - All: Obey all safe area insets  
		// - Container: Content flows under keyboard but stays out of bars/notch
		// - Any combination of the above flags
		return originalSafeArea;
	}

	// Reduces each edge's inset to the amount the view actually overlaps the
	// corresponding safe area, accounting for margins and in-flight Shell/navigation
	// animations. Returns the adjusted (left, top, right, bottom) insets.
	static (double left, double top, double right, double bottom) AdjustInsetsForViewBounds(
		Context context,
		View view,
		double left, double top, double right, double bottom,
		Thickness margins,
		int viewWidth, int viewHeight,
		bool hasTrackedViews)
	{
		// Get view's position on screen
		var viewLocation = new int[2];
		view.GetLocationOnScreen(viewLocation);
		var viewLeft = viewLocation[0];
		var viewTop = viewLocation[1];
		var viewRight = viewLeft + viewWidth;
		var viewBottom = viewTop + viewHeight;

		// Get actual screen dimensions (including system UI)
		// This must be done BEFORE margin adjustment so we can detect
		// off-screen animation state from raw position values.
		var windowManager = context.GetSystemService(Context.WindowService) as IWindowManager;
		if (windowManager?.DefaultDisplay is not null)
		{
			var realMetrics = new global::Android.Util.DisplayMetrics();
			windowManager.DefaultDisplay.GetRealMetrics(realMetrics);
			var screenWidth = realMetrics.WidthPixels;
			var screenHeight = realMetrics.HeightPixels;

			// Detect if view is off-screen BEFORE margin adjustment clamps negative positions
			// to zero via Math.Max, destroying the animation signal.
			// Horizontal: during Shell tab animation, viewLeft=-1 gets clamped to 0,
			// making it impossible to detect animation for the RIGHT edge afterward.
			var viewIsAnimatingHorizontally = viewLeft < 0 || viewRight > screenWidth;

			// Vertical: During Shell navigation animations, the view may be positioned
			// beyond the status bar area (e.g., Y=126 when status bar is 63px) and also
			// extend beyond the screen bottom. This happens because the fragment animation
			// slides the view in from off-screen. We detect this animating state by checking:
			// 1. viewTop > top (view is below the status bar area - normal case would be viewTop <= top)
			// 2. viewBottom > screenHeight (view extends beyond screen - confirms it's not just a small view)
			// 3. viewTop > 0 (view is not at origin)
			// This is DIFFERENT from ScrollView where viewTop = 0 (at origin, not animating).
			// When we detect animation state, apply the full top inset since view will settle at Y=0.
			var viewIsAnimatingVertically = viewTop > top && viewTop > 0 && viewBottom > screenHeight;

			// Adjust for view's position relative to parent (including margins) to calculate
			// safe area insets relative to the parent's position, not the view's visual position.
			// This ensures margins and safe area insets are additive rather than overlapping.
			// For example: 20px margin + 30px safe area = 50px total offset
			// We only take the margins into account if the Width and Height are set
			// If the Width and Height aren't set it means the layout pass hasn't happened yet
			if (view.Width > 0 && view.Height > 0)
			{
				// Convert each margin to pixels once and reuse, instead of calling
				// ToPixels (which performs density conversion) per edge.
				var marginTopPx = (int)context.ToPixels(margins.Top);
				var marginLeftPx = (int)context.ToPixels(margins.Left);
				var marginRightPx = (int)context.ToPixels(margins.Right);
				var marginBottomPx = (int)context.ToPixels(margins.Bottom);

				viewTop = Math.Max(0, viewTop - marginTopPx);
				viewLeft = Math.Max(0, viewLeft - marginLeftPx);
				viewRight += marginRightPx;
				viewBottom += marginBottomPx;
			}

			// Calculate actual overlap for each edge
			// Top: how much the view extends into the top safe area
			// If the viewTop is < 0 that means that it's most likely
			// panned off the top of the screen so we don't want to apply any top inset

			if (top > 0 && viewTop < top && viewTop >= 0)
			{
				// Calculate the actual overlap amount
				top = Math.Min(top - viewTop, top);
			}
			else if (top > 0 && viewIsAnimatingVertically)
			{
				// View is animating - positioned beyond status bar but extends off-screen
				// Apply full top inset since view will settle at Y=0
			}
			else
			{
				if (viewHeight > 0 || hasTrackedViews)
				{
					top = 0;
				}
			}

			// Bottom: how much the view extends into the bottom safe area
			if (bottom > 0 && viewBottom > (screenHeight - bottom))
			{
				// Calculate the actual overlap amount
				var bottomEdge = screenHeight - bottom;
				bottom = Math.Min(viewBottom - bottomEdge, bottom);
			}
			else
			{
				// if the view height is zero because it hasn't done the first pass
				// and we don't have any tracked views yet then we will apply the bottom inset
				if (viewHeight > 0 || hasTrackedViews)
				{
					bottom = 0;
				}
			}

			// Left: how much the view extends into the left safe area
			// During Shell navigation animations, the view slides in from off-screen.
			// We must check animation FIRST because near the end of animation
			// (e.g., viewLeft=1), the overlap check would incorrectly reduce the inset.
			if (left > 0 && viewIsAnimatingHorizontally && viewLeft > 0)
			{
				// View is animating - keep full inset since view will settle at X=0
			}
			else if (left > 0 && viewLeft < left)
			{
				// Calculate the actual overlap amount
				left = Math.Min(left - viewLeft, left);
			}
			else
			{
				if (viewWidth > 0 || hasTrackedViews)
				{
					left = 0;
				}
			}

			// Right: how much the view extends into the right safe area
			// During animation, viewRight may be near screenWidth (e.g., 2991 vs 2992)
			// causing incorrect partial overlap. Check animation before overlap.
			if (right > 0 && viewIsAnimatingHorizontally)
			{
				// View is animating - keep full inset
			}
			else if (right > 0 && viewRight > (screenWidth - right))
			{
				// Calculate the actual overlap amount
				var rightEdge = screenWidth - right;
				right = Math.Min(viewRight - rightEdge, right);
			}
			else
			{
				if (viewWidth > 0 || hasTrackedViews)
				{
					right = 0;
				}
			}
		}

		return (left, top, right, bottom);
	}

	// Rebuilds the window insets, consuming only the portions this view actually
	// applied as padding so the remainder can flow through to child views.
	static WindowInsetsCompat? BuildRemainingInsets(
		WindowInsetsCompat windowInsets,
		double left, double top, double right, double bottom,
		bool isKeyboardShowing,
		SafeAreaPadding keyboardInsets)
	{
		// Build new window insets with unconsumed values
		var builder = new WindowInsetsCompat.Builder(windowInsets);

		// Get original insets for each type
		var systemBars = windowInsets.GetInsets(WindowInsetsCompat.Type.SystemBars());
		var displayCutout = windowInsets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
		var ime = windowInsets.GetInsets(WindowInsetsCompat.Type.Ime());

		// Calculate what's left after consumption
		// For system bars and display cutout, only consume what we're using
		if (systemBars is not null)
		{
			var newSystemBarsLeft = left > 0 ? 0 : systemBars.Left;
			var newSystemBarsTop = top > 0 ? 0 : systemBars.Top;
			var newSystemBarsRight = right > 0 ? 0 : systemBars.Right;
			var newSystemBarsBottom = (bottom > 0 || isKeyboardShowing) ? 0 : systemBars.Bottom;

			builder.SetInsets(WindowInsetsCompat.Type.SystemBars(),
				AndroidX.Core.Graphics.Insets.Of(newSystemBarsLeft, newSystemBarsTop, newSystemBarsRight, newSystemBarsBottom));
		}

		if (displayCutout is not null)
		{
			var newCutoutLeft = left > 0 ? 0 : displayCutout.Left;
			var newCutoutTop = top > 0 ? 0 : displayCutout.Top;
			var newCutoutRight = right > 0 ? 0 : displayCutout.Right;
			var newCutoutBottom = (bottom > 0 || isKeyboardShowing) ? 0 : displayCutout.Bottom;

			builder.SetInsets(WindowInsetsCompat.Type.DisplayCutout(),
				AndroidX.Core.Graphics.Insets.Of(newCutoutLeft, newCutoutTop, newCutoutRight, newCutoutBottom));
		}

		// For keyboard (IME), only consume if we fully handled the keyboard inset.
		// "Fully handled" means the bottom padding we applied covers the entire
		// keyboard inset; a partial bottom inset must be propagated to children.
		if (ime is not null && isKeyboardShowing)
		{
			bool handledImeInsets = bottom > 0 && bottom >= keyboardInsets.Bottom;
			var newImeBottom = handledImeInsets ? 0 : ime.Bottom;
			builder.SetInsets(WindowInsetsCompat.Type.Ime(),
				AndroidX.Core.Graphics.Insets.Of(0, 0, 0, newImeBottom));
		}

		return builder.Build();
	}

	// Writes padding only when it differs from the current values, avoiding a
	// redundant layout pass when an inset dispatch produces the same padding.
	static void SetPaddingIfChanged(View view, int left, int top, int right, int bottom)
	{
		if (view.PaddingLeft != left ||
			view.PaddingTop != top ||
			view.PaddingRight != right ||
			view.PaddingBottom != bottom)
		{
			view.SetPadding(left, top, right, bottom);
		}
	}
}
