using Android.Content;
using Android.Views;
using AndroidX.Core.View;

namespace Microsoft.Maui.Platform;

internal static class SafeAreaExtensions
{
	internal static WindowInsetsCompat? ApplyAdjustedSafeAreaInsetsPx(
		WindowInsetsCompat windowInsets,
		ICrossPlatformLayout crossPlatformLayout,
		Context context,
		View view)
	{
		var safeAreaView = crossPlatformLayout switch
		{
			ISafeAreaView2 view2 => view2,
			IElementHandler { VirtualView: ISafeAreaView2 virtualView2 } => virtualView2,
			_ => null
		};

		if (safeAreaView is null)
		{
			return windowInsets;
		}

		var baseSafeArea = windowInsets.ToSafeAreaInsetsPx(context);
		var keyboardInsets = windowInsets.GetKeyboardInsetsPx(context);
		var isKeyboardShowing = !keyboardInsets.IsEmpty;
		var left = GetSafeAreaForEdge(safeAreaView.GetSafeAreaRegionsForEdge(0), baseSafeArea.Left, 0, isKeyboardShowing, keyboardInsets);
		var top = GetSafeAreaForEdge(safeAreaView.GetSafeAreaRegionsForEdge(1), baseSafeArea.Top, 1, isKeyboardShowing, keyboardInsets);
		var right = GetSafeAreaForEdge(safeAreaView.GetSafeAreaRegionsForEdge(2), baseSafeArea.Right, 2, isKeyboardShowing, keyboardInsets);
		var bottom = GetSafeAreaForEdge(safeAreaView.GetSafeAreaRegionsForEdge(3), baseSafeArea.Bottom, 3, isKeyboardShowing, keyboardInsets);

		// A parent region has already removed any edge it owns from this inset snapshot.
		// Zero values therefore mean pass-through, not that this view failed an overlap test.
		if (left == 0 && right == 0 && top == 0 && bottom == 0)
		{
			MauiWindowInsetListener.ResetViewInsets(view);
			return windowInsets;
		}

		if (isKeyboardShowing &&
			context.GetActivity()?.Window is Window window &&
			window.Attributes is WindowManagerLayoutParams attr)
		{
			var adjustMode = attr.SoftInputMode & SoftInput.MaskAdjust;
			if (adjustMode == SoftInput.AdjustPan)
			{
				return WindowInsetsCompat.Consumed;
			}
		}

		var builder = new WindowInsetsCompat.Builder(windowInsets);
		var systemBars = windowInsets.GetInsets(WindowInsetsCompat.Type.SystemBars());
		var displayCutout = windowInsets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
		var ime = windowInsets.GetInsets(WindowInsetsCompat.Type.Ime());

		if (systemBars is not null)
		{
			builder.SetInsets(
				WindowInsetsCompat.Type.SystemBars(),
				AndroidX.Core.Graphics.Insets.Of(
					left > 0 ? 0 : systemBars.Left,
					top > 0 ? 0 : systemBars.Top,
					right > 0 ? 0 : systemBars.Right,
					bottom > 0 || isKeyboardShowing ? 0 : systemBars.Bottom));
		}

		if (displayCutout is not null)
		{
			builder.SetInsets(
				WindowInsetsCompat.Type.DisplayCutout(),
				AndroidX.Core.Graphics.Insets.Of(
					left > 0 ? 0 : displayCutout.Left,
					top > 0 ? 0 : displayCutout.Top,
					right > 0 ? 0 : displayCutout.Right,
					bottom > 0 || isKeyboardShowing ? 0 : displayCutout.Bottom));
		}

		if (ime is not null && isKeyboardShowing)
		{
			builder.SetInsets(
				WindowInsetsCompat.Type.Ime(),
				AndroidX.Core.Graphics.Insets.Of(
					0,
					0,
					0,
					bottom >= keyboardInsets.Bottom ? 0 : ime.Bottom));
		}

		view.SetPadding((int)left, (int)top, (int)right, (int)bottom);

		return builder.Build() ?? windowInsets;
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
}
