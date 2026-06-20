using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Views;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.AppBar;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Registry entry for tracking view instances and their associated listeners.
	/// Uses WeakReference to avoid memory leaks when views are disposed.
	/// </summary>
	internal record ViewEntry(WeakReference<object> View, MauiWindowInsetListener Listener);

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
		bool IsImeAnimating { get; set; }
		public MauiWindowInsetListener() : base(DispatchModeStop)
		{
		}

		public virtual WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
		{
			if (insets is null || !insets.HasInsets || v is null || IsImeAnimating)
			{
				return insets;
			}

			if (v is IHandleWindowInsets customHandler)
			{
				return customHandler.HandleWindowInsets(insets);
			}

			// Apply default window insets for standard views
			return ApplyDefaultWindowInsets(v, insets);
		}

		static WindowInsetsCompat? ApplyDefaultWindowInsets(AView v, WindowInsetsCompat insets)
		{
			var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
			var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

			// Find AppBarLayout - check direct child first, then first two children
			var appBarLayout = v.FindViewById<AppBarLayout>(Resource.Id.navigationlayout_appbar);
			if (appBarLayout is null && v is ViewGroup group)
			{
				if (group.ChildCount > 0 && group.GetChildAt(0) is AppBarLayout firstChild)
				{
					appBarLayout = firstChild;
				}
				else if (group.ChildCount > 1 && group.GetChildAt(1) is AppBarLayout secondChild)
				{
					appBarLayout = secondChild;
				}
			}

			// Check if AppBarLayout has meaningful content
			bool appBarHasContent = appBarLayout?.MeasuredHeight > 0;
			if (!appBarHasContent && appBarLayout is not null)
			{
				for (int i = 0; i < appBarLayout.ChildCount; i++)
				{
					var child = appBarLayout.GetChildAt(i);
					if (child?.MeasuredHeight > 0)
					{
						appBarHasContent = true;
						break;
					}
				}
			}

			// Apply padding to AppBarLayout based on content and system insets
			if (appBarLayout is not null)
			{
				if (appBarHasContent)
				{
					var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
					appBarLayout.SetPadding(systemBars?.Left ?? 0, topInset, systemBars?.Right ?? 0, 0);
				}
				else
				{
					appBarLayout.SetPadding(0, 0, 0, 0);
				}
			}

			var bottomTabContainer = v.FindViewById<ViewGroup>(Resource.Id.navigationlayout_bottomtabs);
			var hasBottomNav = bottomTabContainer?.MeasuredHeight > 0;
			var contentView = v.FindViewById(Resource.Id.navigationlayout_content);

			if (hasBottomNav)
			{
				var bottomInset = Math.Max(systemBars?.Bottom ?? 0, displayCutout?.Bottom ?? 0);

				// Only pad the bottom of contentView to prevent content from sliding under the
				// BottomNavigationView + system navigation bar. Left/right are intentionally
				// excluded: landscape cutout padding on the content area is handled by
				// SafeAreaExtensions which applies per-view overlap logic.
				contentView?.SetPadding(0, 0, 0, bottomInset);
			}
			else
			{
				// Reset contentView padding when bottom navigation is removed dynamically
				contentView?.SetPadding(0, 0, 0, 0);
			}

			// Consume top inset when AppBar is visible — it already pads itself, so downstream
			// views must not receive a top inset or SafeAreaExtensions will double-apply it.
			// Bottom inset is passed through unconsumed so BottomNavigationView can extend its
			// background into the system navigation bar area (issue #33344).
			var newSystemBars = Insets.Of(
				systemBars?.Left ?? 0,
				appBarHasContent ? 0 : systemBars?.Top ?? 0,
				systemBars?.Right ?? 0,
				systemBars?.Bottom ?? 0) ?? Insets.None;

			var newDisplayCutout = Insets.Of(
				displayCutout?.Left ?? 0,
				appBarHasContent ? 0 : displayCutout?.Top ?? 0,
				displayCutout?.Right ?? 0,
				displayCutout?.Bottom ?? 0) ?? Insets.None;

			return new WindowInsetsCompat.Builder(insets)
			?.SetInsets(WindowInsetsCompat.Type.SystemBars(), newSystemBars)
			?.SetInsets(WindowInsetsCompat.Type.DisplayCutout(), newDisplayCutout)
			?.Build() ?? insets;
		}

		public override void OnPrepare(WindowInsetsAnimationCompat? animation)
		{
			if (IsImeAnimation(animation))
			{
				IsImeAnimating = true;
			}
		}

		public override WindowInsetsAnimationCompat.BoundsCompat? OnStart(WindowInsetsAnimationCompat? animation, WindowInsetsAnimationCompat.BoundsCompat? bounds)
		{
			if (IsImeAnimation(animation))
			{
				IsImeAnimating = true;
			}

			return bounds;
		}

		public override WindowInsetsCompat? OnProgress(WindowInsetsCompat? insets, IList<WindowInsetsAnimationCompat>? runningAnimations)
		{
			if (runningAnimations?.Count > 0)
			{
				//ApplyWindowInsets(_pendingView, insets);
			}

			return insets;
		}

		static void ApplyWindowInsets(View view, WindowInsetsCompat? insets)
		{
			if (insets is not null)
			{
				var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
				var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());
				var bottom = Math.Max(systemBars?.Bottom ?? 0, ime?.Bottom ?? 0);
				view?.SetPadding(systemBars?.Left ?? 0, 0, systemBars?.Right ?? 0, bottom);
			}
		}

		public override void OnEnd(WindowInsetsAnimationCompat? animation)
		{
			if (!IsImeAnimation(animation))
			{
				return;
			}
			IsImeAnimating = false;
		}

		/// <summary>
		/// Helper method to check if an animation involves the IME
		/// </summary>
		static bool IsImeAnimation(WindowInsetsAnimationCompat? animation) =>
			animation is not null && (animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0;
	}
}

/// <summary>
/// Extension methods to access WindowInsetListener instances.
/// These methods support both the legacy global listener pattern and the new
/// per-view local listener pattern.
/// </summary>
internal static class MauiWindowInsetListenerExtensions
{
	/// <summary>
	/// Sets a MauiWindowInsetListener for the specified view, allowing it to handle window insets and safe area adjustments.
	/// If no listener is provided, a new instance will be created and attached to the view.
	/// This is typically used for child views that need to handle their own insets behavior.
	/// </summary>
	/// <param name="view"></param>
	/// <param name="listener"></param>
	public static void SetMauiWindowInsetListener(this View view, MauiWindowInsetListener? listener = null)
	{
		listener ??= new MauiWindowInsetListener();
		ViewCompat.SetOnApplyWindowInsetsListener(view, listener);
		ViewCompat.SetWindowInsetsAnimationCallback(view, listener);
	}

	/// <summary>
	/// Removes the MauiWindowInsetListener from the specified view and resets its tracked state.
	/// This should be called when a view is being detached to ensure proper cleanup.
	/// </summary>
	/// <param name="view">The Android view to remove the listener from</param>
	public static void RemoveMauiWindowInsetListener(this View view)
	{
		// Clear the listeners first
		ViewCompat.SetOnApplyWindowInsetsListener(view, null);
		ViewCompat.SetWindowInsetsAnimationCallback(view, null);
	}
}