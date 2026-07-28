using System;
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
		var safeAreaView = GetSafeAreaView(crossPlatformLayout);

		if (safeAreaView is null)
		{
			return windowInsets;
		}

		if (!ShouldApplySafeAreaInsets(view, safeAreaView))
		{
			MauiWindowInsetListener.ResetViewInsets(view);
			return windowInsets;
		}

		var baseSafeArea = windowInsets.ToSafeAreaInsetsPx(context);
		var keyboardInsets = windowInsets.GetKeyboardInsetsPx(context);
		var isKeyboardVisible = windowInsets.IsVisible(WindowInsetsCompat.Type.Ime());
		var shouldApplyKeyboardInsets = isKeyboardVisible && !IsAdjustPan(context);
		var leftRegion = safeAreaView.GetSafeAreaRegionsForEdge(0);
		var topRegion = safeAreaView.GetSafeAreaRegionsForEdge(1);
		var rightRegion = safeAreaView.GetSafeAreaRegionsForEdge(2);
		var bottomRegion = safeAreaView.GetSafeAreaRegionsForEdge(3);
		var left = GetSafeAreaForEdge(leftRegion, baseSafeArea.Left, 0, shouldApplyKeyboardInsets, keyboardInsets);
		var top = GetSafeAreaForEdge(topRegion, baseSafeArea.Top, 1, shouldApplyKeyboardInsets, keyboardInsets);
		var right = GetSafeAreaForEdge(rightRegion, baseSafeArea.Right, 2, shouldApplyKeyboardInsets, keyboardInsets);
		var bottom = GetSafeAreaForEdge(bottomRegion, baseSafeArea.Bottom, 3, shouldApplyKeyboardInsets, keyboardInsets);

		// A parent region has already removed any edge it owns from this inset snapshot.
		// Zero values therefore mean pass-through, not that this view failed an overlap test.
		if (left == 0 && right == 0 && top == 0 && bottom == 0)
		{
			MauiWindowInsetListener.ResetViewInsets(view);
			return windowInsets;
		}

		var consumedContainerEdges = GetConsumedContainerEdges(
			leftRegion,
			topRegion,
			rightRegion,
			bottomRegion,
			baseSafeArea);
		var consumedImeEdges =
			shouldApplyKeyboardInsets &&
			SafeAreaEdges.IsSoftInput(bottomRegion) &&
			keyboardInsets.Bottom > 0
				? WindowInsetEdges.Bottom
				: WindowInsetEdges.None;
		var consumption = new WindowInsetConsumption(
			consumedContainerEdges,
			consumedContainerEdges,
			consumedImeEdges);

		view.SetPadding((int)left, (int)top, (int)right, (int)bottom);

		return WindowInsetsManager.BuildRemaining(windowInsets, consumption);
	}

	internal static bool CanApplyImeInsets(View view)
	{
		if (view is not ICrossPlatformLayoutBacking { CrossPlatformLayout: { } crossPlatformLayout })
		{
			return false;
		}

		var safeAreaView = GetSafeAreaView(crossPlatformLayout);
		return safeAreaView is not null &&
			ShouldApplySafeAreaInsets(view, safeAreaView) &&
			SafeAreaEdges.IsSoftInput(safeAreaView.GetSafeAreaRegionsForEdge(3)) &&
			!HasImeOwningAncestor(view) &&
			!IsAdjustPan(view.Context);
	}

	static bool HasImeOwningAncestor(View view)
	{
		var parent = view.Parent;
		while (parent is View parentView)
		{
			if (parentView is ICrossPlatformLayoutBacking { CrossPlatformLayout: { } parentLayout } &&
				GetSafeAreaView(parentLayout) is ISafeAreaView2 parentSafeAreaView &&
				parentSafeAreaView.HasExplicitSafeAreaEdges &&
				SafeAreaEdges.IsSoftInput(parentSafeAreaView.GetSafeAreaRegionsForEdge(3)))
			{
				return true;
			}

			parent = parentView.Parent;
		}

		return false;
	}

	static ISafeAreaView2? GetSafeAreaView(ICrossPlatformLayout crossPlatformLayout)
	{
		return crossPlatformLayout switch
		{
			ISafeAreaView2 view => view,
			IElementHandler { VirtualView: ISafeAreaView2 virtualView } => virtualView,
			_ => null
		};
	}

	static WindowInsetEdges GetConsumedContainerEdges(
		SafeAreaRegions leftRegion,
		SafeAreaRegions topRegion,
		SafeAreaRegions rightRegion,
		SafeAreaRegions bottomRegion,
		SafeAreaPadding containerInsets)
	{
		var consumedEdges = WindowInsetEdges.None;

		if (ConsumesContainer(leftRegion) && containerInsets.Left > 0)
			consumedEdges |= WindowInsetEdges.Left;
		if (ConsumesContainer(topRegion) && containerInsets.Top > 0)
			consumedEdges |= WindowInsetEdges.Top;
		if (ConsumesContainer(rightRegion) && containerInsets.Right > 0)
			consumedEdges |= WindowInsetEdges.Right;
		if (ConsumesContainer(bottomRegion) && containerInsets.Bottom > 0)
			consumedEdges |= WindowInsetEdges.Bottom;

		return consumedEdges;
	}

	static bool ConsumesContainer(SafeAreaRegions region) =>
		region != SafeAreaRegions.None && !SafeAreaEdges.IsOnlySoftInput(region);

	static bool IsAdjustPan(Context? context)
	{
		if (context?.GetActivity()?.Window is not Window window ||
			window.Attributes is not WindowManagerLayoutParams attributes)
		{
			return false;
		}

		return (attributes.SoftInputMode & SoftInput.MaskAdjust) == SoftInput.AdjustPan;
	}

	internal static bool ShouldApplySafeAreaInsets(View view, ISafeAreaView2 safeAreaView)
	{
		if (safeAreaView.HasExplicitSafeAreaEdges)
		{
			return true;
		}

		var parent = view.Parent;
		while (parent is View parentView)
		{
			if (parentView is ICrossPlatformLayoutBacking { CrossPlatformLayout: ISafeAreaView2 parentSafeAreaView } &&
				parentSafeAreaView.HasExplicitSafeAreaEdges)
			{
				return false;
			}

			parent = parentView.Parent;
		}

		return true;
	}

	internal static double GetSafeAreaForEdge(SafeAreaRegions safeAreaRegion, double originalSafeArea, int edge, bool isKeyboardShowing, SafeAreaPadding keyBoardInsets)
	{
		// Edge-to-edge content - no safe area padding
		if (safeAreaRegion == SafeAreaRegions.None)
		{
			return 0;
		}

		if (SafeAreaEdges.IsOnlySoftInput(safeAreaRegion) && edge != 3)
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
					return Math.Max(originalSafeArea, keyBoardInsets.Bottom);
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
