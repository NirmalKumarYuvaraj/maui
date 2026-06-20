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
		var baseSafeArea = windowInsets.ToSafeAreaInsetsPx(context);
		var safeAreaView2 = GetSafeAreaView2(crossPlatformLayout);

		if (safeAreaView2 is not null)
		{
			// Apply safe area selectively per edge based on SafeAreaRegions
			var left = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(0, crossPlatformLayout), baseSafeArea.Left, 0);
			var top = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(1, crossPlatformLayout), baseSafeArea.Top, 1);
			var right = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(2, crossPlatformLayout), baseSafeArea.Right, 2);
			var bottom = GetSafeAreaForEdge(GetSafeAreaRegionForEdge(3, crossPlatformLayout), baseSafeArea.Bottom, 3);

			if (left != 0 || top != 0 || right != 0 || bottom != 0)
			{
				view.SetPadding((int)left, (int)top, (int)right, (int)bottom);
				System.Diagnostics.Debug.WriteLine($"[SafeAreaExtensions] Applied adjusted safe area insets: Left={left}, Top={top}, Right={right}, Bottom={bottom}to {crossPlatformLayout}");
				return WindowInsetsCompat.Consumed;
			}
			view.SetPadding(0, 0, 0, 0);
			System.Diagnostics.Debug.WriteLine($"[SafeAreaExtensions] Reset padding to {crossPlatformLayout}");
		}

		// Fallback: return the base safe area for legacy views
		return windowInsets;
	}

	internal static double GetSafeAreaForEdge(SafeAreaRegions safeAreaRegion, double originalSafeArea, int edge)
	{
		// Edge-to-edge content - no safe area padding
		if (safeAreaRegion == SafeAreaRegions.None)
		{
			return 0;
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
