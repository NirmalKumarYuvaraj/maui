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

	internal static ISafeAreaView? GetSafeAreaView(object? layout) =>
		layout switch
		{
			ISafeAreaView sav => sav,
			IElementHandler { VirtualView: ISafeAreaView virtualSav } => virtualSav,
			_ => null
		};


	internal static SafeAreaRegions GetSafeAreaRegionForEdge(int edge, ICrossPlatformLayout crossPlatformLayout)
	{
		var layout = crossPlatformLayout;
		var safeAreaView2 = GetSafeAreaView2(layout);

		if (safeAreaView2 is not null)
		{
			return safeAreaView2.GetSafeAreaRegionsForEdge(edge);
		}

		var safeAreaView = GetSafeAreaView(layout);
		return safeAreaView?.IgnoreSafeArea == false ? SafeAreaRegions.Container : SafeAreaRegions.None;
	}

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
			// Apply safe area selectively per edge based on SafeAreaRegions
			var left = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(0, layout), baseSafeArea.Left, 0, isKeyboardShowing, keyboardInsets);
			var top = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(1, layout), baseSafeArea.Top, 1, isKeyboardShowing, keyboardInsets);
			var right = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(2, layout), baseSafeArea.Right, 2, isKeyboardShowing, keyboardInsets);
			var bottom = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(3, layout), baseSafeArea.Bottom, 3, isKeyboardShowing, keyboardInsets);

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
				if (globalWindowInsetsListener?.IsViewTracked(view) != true)
				{
					// Don't consume insets - pass them through for potential child views to handle
					System.Diagnostics.Debug.WriteLine($"[SafeArea] Passing through insets for view {layout} with no safe area padding.");
					return windowInsets;
				}
			}


			if (isKeyboardShowing &&
				context.GetActivity()?.Window is Window window &&
				window?.Attributes is WindowManagerLayoutParams attr)
			{
				var softInputMode = attr.SoftInputMode;

				// Handle AdjustResize: Apply padding at DecorView level, not ContentViewGroup
				if (softInputMode.IsAdjustResize())
				{
					System.Diagnostics.Debug.WriteLine($"[SafeArea] returning because softinput mode is adjustresize for view {layout}.");

					// For AdjustResize, the system automatically resizes the window when keyboard appears.
					// We should apply safe area padding at the DecorView level to avoid conflicts
					// with ContentViewGroup padding and to work with the system's resize behavior.
					// Pass insets through to DecorView handler, don't apply at this level.
					return windowInsets;
				}

				// Handle AdjustPan: Consume insets when window is panned
				if (softInputMode.IsAdjustPan() && bottom == 0)
				{
					// If the window is panned from the keyboard being open
					// and there isn't a bottom inset to apply then just don't touch anything
					System.Diagnostics.Debug.WriteLine($"[SafeArea] returning because softinput mode is adjustpan for view {layout}.");
					return WindowInsetsCompat.Consumed;
				}

				// AdjustNothing and AdjustUnspecified: Continue with normal processing
				// (fall through to existing ContentViewGroup logic below)
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
					view.SetPadding(0, 0, 0, 0);
					System.Diagnostics.Debug.WriteLine($"[SafeArea] returning zero padding for view {layout}.");
					return windowInsets;
				}

				// Get view's position on screen
				var viewLocation = new int[2];
				view.GetLocationOnScreen(viewLocation);
				var viewLeft = viewLocation[0];
				var viewTop = viewLocation[1];
				var viewRight = viewLeft + viewWidth;
				var viewBottom = viewTop + viewHeight;

				// Adjust for view's position relative to parent (including margins) to calculate
				// safe area insets relative to the parent's position, not the view's visual position.
				// This ensures margins and safe area insets are additive rather than overlapping.
				// For example: 20px margin + 30px safe area = 50px total offset
				// We only take the margins into account if the Width and Height are set
				// If the Width and Height aren't set it means the layout pass hasn't happen yet
				if (view.Width > 0 && view.Height > 0)
				{
					viewTop = Math.Max(0, viewTop - (int)context.ToPixels(margins.Top));
					viewLeft = Math.Max(0, viewLeft - (int)context.ToPixels(margins.Left));
					viewRight += (int)context.ToPixels(margins.Right);
					viewBottom += (int)context.ToPixels(margins.Bottom);
				}

				// Get actual screen dimensions (including system UI)
				var windowManager = context.GetSystemService(Context.WindowService) as IWindowManager;
				if (windowManager?.DefaultDisplay is not null)
				{
					var realMetrics = new global::Android.Util.DisplayMetrics();
					windowManager.DefaultDisplay.GetRealMetrics(realMetrics);
					var screenWidth = realMetrics.WidthPixels;
					var screenHeight = realMetrics.HeightPixels;

					// Check if view extends beyond screen bounds - this indicates the view
					// is still being positioned (e.g., during Shell fragment transitions).
					// In this case, consume all insets to prevent children from processing
					// invalid data, and request a re-apply after the view settles.
					bool viewExtendsBeyondScreen = viewRight > screenWidth || viewBottom > screenHeight ||
												   viewLeft < 0 || viewTop < 0;

					if (viewExtendsBeyondScreen)
					{
						// Request insets to be reapplied after the next layout pass
						// when the view should be properly positioned.
						// Don't return early - let processing continue with current insets
						// to avoid visual popping, the re-apply will correct any issues.
						view.Post(() => ViewCompat.RequestApplyInsets(view));
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
					else
					{
						if (viewHeight > 0 || hasTrackedViews)
							top = 0;
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
							bottom = 0;
					}

					// Left: how much the view extends into the left safe area
					if (left > 0 && viewLeft < left)
					{
						// Calculate the actual overlap amount
						left = Math.Min(left - viewLeft, left);
					}
					else
					{
						if (viewWidth > 0 || hasTrackedViews)
							left = 0;
					}

					// Right: how much the view extends into the right safe area
					if (right > 0 && viewRight > (screenWidth - right))
					{
						// Calculate the actual overlap amount
						var rightEdge = screenWidth - right;
						right = Math.Min(viewRight - rightEdge, right);
					}
					else
					{
						if (viewWidth > 0 || hasTrackedViews)
							right = 0;
					}
				}

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

				// For keyboard (IME), only consume if we're handling it
				if (ime is not null && isKeyboardShowing)
				{
					var newImeBottom = (bottom > 0 && bottom >= keyboardInsets.Bottom) ? 0 : ime.Bottom;
					builder.SetInsets(WindowInsetsCompat.Type.Ime(),
						AndroidX.Core.Graphics.Insets.Of(0, 0, 0, newImeBottom));
				}

				newWindowInsets = builder.Build();

				// Apply all insets to content view group
				view.SetPadding((int)left, (int)top, (int)right, (int)bottom);
				System.Diagnostics.Debug.WriteLine($"[SafeArea] padding applied for view {layout} padding vales = left:{left}, top:{top}, right:{right}, bottom:{bottom}.");
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

	/// <summary>
	/// Applies SafeArea insets at the DecorView content root level for AdjustResize mode.
	/// This is the recommended approach for AdjustResize because:
	/// 1. The system already handles window resizing when keyboard appears
	/// 2. We only need to apply system bar/cutout insets, NOT keyboard insets
	/// 3. Applying at DecorView level avoids conflicts with ContentViewGroup padding
	/// </summary>
	/// <param name="window">The Android window</param>
	/// <param name="insets">Window insets to process</param>
	/// <param name="context">Android context</param>
	/// <param name="safeAreaRegions">Safe area regions to apply (typically from SafeAreaView)</param>
	/// <returns>Updated window insets with appropriate consumption</returns>
	internal static WindowInsetsCompat? ApplyAdjustResizeSafeAreaToDecorView(
		Window window,
		WindowInsetsCompat insets,
		Context context,
		SafeAreaRegions safeAreaRegions)
	{
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: Starting");

		if (window?.DecorView is not ViewGroup decorView)
		{
			System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: DecorView is null or not ViewGroup, returning original insets");
			return insets;
		}

		// Get the content root (android.R.id.content)
		var contentRoot = decorView.FindViewById<ViewGroup>(global::Android.Resource.Id.Content);
		if (contentRoot is null)
		{
			System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: ContentRoot is null, returning original insets");
			return insets;
		}

		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: ContentRoot found - ChildCount:{contentRoot.ChildCount}");

		var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
		var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
		var imeInsets = insets.GetInsets(WindowInsetsCompat.Type.Ime());

		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: SystemBars insets = left:{systemBars?.Left}, top:{systemBars?.Top}, right:{systemBars?.Right}, bottom:{systemBars?.Bottom}");
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: DisplayCutout insets = left:{displayCutout?.Left}, top:{displayCutout?.Top}, right:{displayCutout?.Right}, bottom:{displayCutout?.Bottom}");
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: IME insets = left:{imeInsets?.Left}, top:{imeInsets?.Top}, right:{imeInsets?.Right}, bottom:{imeInsets?.Bottom}");
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: SafeAreaRegions = {safeAreaRegions}");

		// Calculate safe area insets for system bars, cutouts, AND keyboard
		// CRITICAL: With SetDecorFitsSystemWindows(false), the system does NOT automatically
		// resize the window for the keyboard. We must manually apply keyboard insets here.
		var left = Math.Max(systemBars?.Left ?? 0, displayCutout?.Left ?? 0);
		var top = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
		var right = Math.Max(systemBars?.Right ?? 0, displayCutout?.Right ?? 0);

		// Bottom padding must include keyboard height for edge-to-edge mode
		var bottom = Math.Max(
			Math.Max(systemBars?.Bottom ?? 0, displayCutout?.Bottom ?? 0),
			imeInsets?.Bottom ?? 0);

		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: Calculated padding = left:{left}, top:{top}, right:{right}, bottom:{bottom}");

		// Log content root dimensions BEFORE applying insets
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: BEFORE - contentRoot dimensions = Width:{contentRoot.Width}, Height:{contentRoot.Height}, MeasuredWidth:{contentRoot.MeasuredWidth}, MeasuredHeight:{contentRoot.MeasuredHeight}");

		// CRITICAL FIX: In edge-to-edge mode, we must NOT consume the insets here.
		// Instead, apply padding to contentRoot and let insets propagate to child views.
		// The child views (MAUI's ContentViewGroup) will handle the insets properly.

		// Apply padding to contentRoot - this provides visual offset
		contentRoot.SetPadding(0, 0, 0, bottom);
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: Applied padding to contentRoot = left:{left}, top:{top}, right:{right}, bottom:{bottom}");

		// Force layout update
		contentRoot.RequestLayout();

		// Log dimensions after
		if (contentRoot.ChildCount > 0 && contentRoot.GetChildAt(0) is View child)
		{
			System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: AFTER - first child dimensions = Width:{child.Width}, Height:{child.Height}, MeasuredWidth:{child.MeasuredWidth}, MeasuredHeight:{child.MeasuredHeight}");
		}
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: AFTER - contentRoot dimensions = Width:{contentRoot.Width}, Height:{contentRoot.Height}, MeasuredWidth:{contentRoot.MeasuredWidth}, MeasuredHeight:{contentRoot.MeasuredHeight}");
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: AFTER - Available content area = Width:{contentRoot.Width - left - right}, Height:{contentRoot.Height - top - bottom}");

		// DO NOT consume insets - pass them through to children
		// The children (especially ContentViewGroup with SafeArea handling) need to see the IME insets
		System.Diagnostics.Debug.WriteLine($"[SafeArea] SafeAreaExtensions.ApplyAdjustResizeSafeAreaToDecorView: Passing insets through to children (NOT consuming)");
		return insets;
	}

	/// <summary>
	/// Resets padding applied to DecorView content by AdjustResize handling.
	/// Call this when switching away from AdjustResize mode or when window is destroyed.
	/// </summary>
	/// <param name="window">The Android window</param>
	internal static void ResetDecorViewSafeArea(Window? window)
	{
		if (window?.DecorView is not ViewGroup decorView)
			return;

		var contentRoot = decorView.FindViewById<ViewGroup>(global::Android.Resource.Id.Content);
		if (contentRoot is null)
			return;

		// Reset padding
		contentRoot.SetPadding(0, 0, 0, 0);
		System.Diagnostics.Debug.WriteLine($"[SafeArea] Reset padding to 0 for content root {contentRoot}.");
	}

	/// <summary>
	/// Alternative approach: Applies SafeArea insets at the ContainerView level for AdjustResize mode.
	/// This approach is more MAUI-specific than DecorView but still avoids ContentViewGroup conflicts.
	/// Use this if DecorView approach causes issues with platform integration.
	/// </summary>
	/// <param name="containerView">The MAUI ContainerView</param>
	/// <param name="insets">Window insets to process</param>
	/// <param name="context">Android context</param>
	/// <returns>Updated window insets with appropriate consumption</returns>
	internal static WindowInsetsCompat? ApplyAdjustResizeSafeAreaToContainerView(
		ContainerView containerView,
		WindowInsetsCompat insets,
		Context context)
	{
		if (containerView is null)
			return insets;

		var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
		var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

		// Calculate safe area insets for system bars and cutouts
		// NOTE: We do NOT apply keyboard insets here - the system already resized the window
		var left = Math.Max(systemBars?.Left ?? 0, displayCutout?.Left ?? 0);
		var top = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
		var right = Math.Max(systemBars?.Right ?? 0, displayCutout?.Right ?? 0);
		var bottom = Math.Max(systemBars?.Bottom ?? 0, displayCutout?.Bottom ?? 0);

		// Apply padding to ContainerView
		containerView.SetPadding(left, top, right, bottom);

		// Build new insets with system bars and cutouts consumed
		var builder = new WindowInsetsCompat.Builder(insets);

		// Consume system bars and display cutout since we've applied them
		if (systemBars is not null)
		{
			builder.SetInsets(WindowInsetsCompat.Type.SystemBars(),
				AndroidX.Core.Graphics.Insets.Of(0, 0, 0, 0));
		}

		if (displayCutout is not null)
		{
			builder.SetInsets(WindowInsetsCompat.Type.DisplayCutout(),
				AndroidX.Core.Graphics.Insets.Of(0, 0, 0, 0));
		}

		// DO NOT consume IME insets - let child views handle keyboard if needed
		return builder.Build();
	}

	/// <summary>
	/// Resets padding applied to ContainerView by AdjustResize handling.
	/// </summary>
	/// <param name="containerView">The MAUI ContainerView</param>
	internal static void ResetContainerViewSafeArea(ContainerView? containerView)
	{
		containerView?.SetPadding(0, 0, 0, 0);
	}
}
