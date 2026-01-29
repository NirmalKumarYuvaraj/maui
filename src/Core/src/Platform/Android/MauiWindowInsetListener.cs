using System;
using System.Collections.Generic;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.AppBar;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Manages window insets and safe area handling for Android views.
	/// This class can be used as a global listener (one per activity) or as local listeners
	/// attached to specific views for better isolation in complex navigation scenarios.
	///
	/// Thread Safety: All public methods should be called on the UI thread.
	/// Android view operations are not thread-safe and must execute on the main thread.
	/// </summary>
	internal class MauiWindowInsetListener : WindowInsetsAnimationCompat.Callback, IOnApplyWindowInsetsListener
	{
		bool IsKeyboardVisible { get; set; }
		AView? _view;
		WindowInsetsCompat? _windowInsetsCompat;
		SoftInput _softInput;

		/// <summary>
		/// Sets up a view to use this listener for inset handling.
		/// This method registers the view and attaches the listener.
		/// Must be called on UI thread.
		/// </summary>
		/// <param name="view">The view to set up</param>
		/// <returns>The same view for method chaining</returns>
		internal static AView SetupViewWithLocalListener(AView view, MauiWindowInsetListener? listener = null)
		{
			listener ??= new MauiWindowInsetListener();
			ViewCompat.SetOnApplyWindowInsetsListener(view, listener);
			ViewCompat.SetWindowInsetsAnimationCallback(view, listener);
			return view;
		}

		/// <summary>
		/// Removes the local listener from a view and properly cleans up.
		/// This resets all tracked views and unregisters the view.
		/// Must be called on UI thread.
		/// </summary>
		/// <param name="view">The view to clean up</param>
		internal static void RemoveViewWithLocalListener(AView view)
		{
			// Remove the listener from the view
			ViewCompat.SetOnApplyWindowInsetsListener(view, null);
			ViewCompat.SetWindowInsetsAnimationCallback(view, null);
		}

		public MauiWindowInsetListener() : base(DispatchModeStop)
		{
		}

		public virtual WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
		{
			if (insets is null || !insets.HasInsets || v is null)
			{
				return insets;
			}

			// Get safe area padding from system bars and display cutout
			var safeAreaPadding = insets.ToSafeAreaInsetsPx(v.Context);
			var keyboardInsets = insets.GetKeyboardInsetsPx(v.Context);

			// Track keyboard visibility
			IsKeyboardVisible = insets.IsVisible(WindowInsetsCompat.Type.Ime());

			// For non-CoordinatorLayout views, apply SafeAreaEdges-based filtering
			var crossPlatformLayout = (v as ICrossPlatformLayoutBacking)?.CrossPlatformLayout;

			double left = safeAreaPadding.Left;
			double top = safeAreaPadding.Top;
			double right = safeAreaPadding.Right;
			double bottom = safeAreaPadding.Bottom;

			if (crossPlatformLayout is not null)
			{
				// Apply per-edge filtering based on SafeAreaRegions
				left = SafeAreaExtensions.GetSafeAreaForEdge(
					SafeAreaExtensions.GetSafeAreaRegionForEdge(0, crossPlatformLayout),
					safeAreaPadding.Left, 0, IsKeyboardVisible, keyboardInsets);

				top = SafeAreaExtensions.GetSafeAreaForEdge(
					SafeAreaExtensions.GetSafeAreaRegionForEdge(1, crossPlatformLayout),
					safeAreaPadding.Top, 1, IsKeyboardVisible, keyboardInsets);

				right = SafeAreaExtensions.GetSafeAreaForEdge(
					SafeAreaExtensions.GetSafeAreaRegionForEdge(2, crossPlatformLayout),
					safeAreaPadding.Right, 2, IsKeyboardVisible, keyboardInsets);

				bottom = SafeAreaExtensions.GetSafeAreaForEdge(
					SafeAreaExtensions.GetSafeAreaRegionForEdge(3, crossPlatformLayout),
					safeAreaPadding.Bottom, 3, IsKeyboardVisible, keyboardInsets);

				v.SetPadding((int)left, (int)top, (int)right, (int)bottom);

				if (left > 0 || top > 0 || right > 0 || bottom > 0)
				{
					return WindowInsetsCompat.Consumed;
				}
			}
			else
			{
				// Co-OrdinatorLayout
				//ApplyAppbarLogic
				_view = v;
				_windowInsetsCompat = insets;
				_softInput = GetAdjustMode();
				if (IsKeyboardVisible)
				{


					switch (_softInput)
					{
						case SoftInput.AdjustPan:
							// Do nothing - system pans the window
							// Just consume the IME insets to prevent child handling
							return WindowInsetsCompat.Consumed;

						case SoftInput.AdjustResize:
						case SoftInput.AdjustNothing:
							// Apply keyboard insets as bottom padding
							var imeInsets = insets.GetKeyboardInsetsPx(v.Context);
							var bottomPadding = IsKeyboardVisible ? imeInsets.Bottom : 0;
							v.SetPadding(
								v.PaddingLeft,
								v.PaddingTop,
								v.PaddingRight,
								(int)bottomPadding);
							return WindowInsetsCompat.Consumed;

						default: // AdjustUnspecified
								 // Let system decide, pass through
							return insets;
					}
				}
				//Apply BottomnavigationView Logic

			}
			// Pass through insets to children - don't consume them
			// Children need insets to apply their own SafeAreaEdges settings
			return insets;
		}


		SoftInput GetAdjustMode()
		{
			var context = _view?.Context;
			if (context?.GetActivity()?.Window is Window window &&
				window?.Attributes is WindowManagerLayoutParams attr)
			{
				// Use MaskAdjust to extract only the adjustment portion
				return attr.SoftInputMode & SoftInput.MaskAdjust;
			}
			return SoftInput.AdjustUnspecified;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		public override void OnPrepare(WindowInsetsAnimationCompat? animation)
		{

		}

		public override WindowInsetsCompat? OnProgress(WindowInsetsCompat? insets, IList<WindowInsetsAnimationCompat>? runningAnimations)
		{
			return insets;
		}

		public override void OnEnd(WindowInsetsAnimationCompat? animation)
		{

			// Re-apply insets to force padding reset when keyboard hides
			if (_windowInsetsCompat != null && _view != null)
			{
				ViewCompat.DispatchApplyWindowInsets(_view, _windowInsetsCompat);
			}
		}
	}
}