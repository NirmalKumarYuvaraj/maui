using System;
using System.Collections.Generic;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.AppBar;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Manages window insets and safe area handling for Android views.
	/// Each inset-capable view owns its listener, while navigation roots may provide
	/// specialized listeners for region ownership.
	///
	/// Thread Safety: All public methods should be called on the UI thread.
	/// Android view operations are not thread-safe and must execute on the main thread.
	/// </summary>
	internal class MauiWindowInsetListener : WindowInsetsAnimationCompat.Callback, IOnApplyWindowInsetsListener
	{
		bool IsImeAnimating { get; set; }

		AView? _pendingView;

		internal static bool ShouldSetMauiWindowInsetListener(AView view)
		{
			var parent = view.Parent;
			var isInsideRecyclerEmptyView = false;
			var hasExplicitSafeAreaEdges = HasExplicitSafeAreaEdges(view);

			while (parent is not null)
			{
				if (parent is IMauiRecyclerViewEmptyView)
				{
					isInsideRecyclerEmptyView = true;
				}

				// MaterialToolbar needs its own inset handling, so it is exempt from all listener-suppression branches.
				// Skip listeners for views inside AppBarLayout/MauiScrollView, and for recycler item views
				// unless SafeAreaEdges was explicitly set.
				if (view is not MaterialToolbar &&
					(parent is AppBarLayout ||
						parent is MauiScrollView ||
						(parent is IMauiRecyclerView && !isInsideRecyclerEmptyView)) &&
					!hasExplicitSafeAreaEdges)
				{
					return false;
				}

				parent = parent.Parent;
			}

			return true;
		}

		static bool HasExplicitSafeAreaEdges(AView view)
		{
			return view is ICrossPlatformLayoutBacking { CrossPlatformLayout: ISafeAreaView2 safeAreaView } &&
				safeAreaView.HasExplicitSafeAreaEdges;
		}

		/// <summary>
		/// Sets up a view to use this listener for inset handling.
		/// This method attaches the listener.
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
		/// Removes the local listener from a view.
		/// Must be called on UI thread.
		/// </summary>
		/// <param name="view">The view to clean up</param>
		internal static void RemoveViewWithLocalListener(AView view)
		{
			// Remove the listener from the view
			ViewCompat.SetOnApplyWindowInsetsListener(view, null);
			ViewCompat.SetWindowInsetsAnimationCallback(view, null);
		}

		public MauiWindowInsetListener() : base(DispatchModeContinueOnSubtree)
		{
		}

		public virtual WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
		{
			if (insets is null || v is null || IsImeAnimating)
			{
				if (IsImeAnimating)
				{
					_pendingView = v;
				}

				return insets;
			}

			_pendingView = null;

			// Handle custom inset views first
			if (v is IHandleWindowInsets customHandler)
			{
				return customHandler.HandleWindowInsets(v, insets);
			}

			// Apply default window insets for standard views
			return ApplyDefaultWindowInsets(v, insets);
		}

		protected virtual WindowInsetsCompat? ApplyDefaultWindowInsets(AView v, WindowInsetsCompat insets)
		{
			var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

			// Handle MaterialToolbar special case early
			if (v is MaterialToolbar)
			{
				v.SetPadding(displayCutout?.Left ?? 0, 0, displayCutout?.Right ?? 0, 0);
				return WindowInsetsCompat.Consumed;
			}

			return insets;
		}

		internal static void ResetViewInsets(AView view)
		{
			if (view is IHandleWindowInsets customHandler)
			{
				customHandler.ResetWindowInsets(view);
			}
		}

		public override void OnPrepare(WindowInsetsAnimationCompat? animation)
		{
			base.OnPrepare(animation);
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
			if (insets is null || runningAnimations is null)
			{
				return insets;
			}

			// Process any IME animations
			foreach (var animation in runningAnimations)
			{
				if (IsImeAnimation(animation))
				{
					var imeInsets = insets.GetInsets(WindowInsetsCompat.Type.Ime());
					// IME height available as: imeInsets?.Bottom ?? 0
					break; // Only need to process one IME animation
				}
			}
			return insets;
		}

		public override void OnEnd(WindowInsetsAnimationCompat? animation)
		{
			base.OnEnd(animation);

			if (IsImeAnimation(animation))
			{
				if (_pendingView is AView view)
				{
					_pendingView = null;
					view.Post(() =>
					{
						IsImeAnimating = false;
						ViewCompat.RequestApplyInsets(view);
					});
				}
				else
				{
					IsImeAnimating = false;
				}
			}
		}

		/// <summary>
		/// Helper method to check if an animation involves the IME
		/// </summary>
		static bool IsImeAnimation(WindowInsetsAnimationCompat? animation) =>
			animation is not null && (animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0;
	}
}

/// <summary>
/// Extension methods for attaching per-view window inset listeners.
/// </summary>
internal static class MauiWindowInsetListenerExtensions
{
	/// <summary>
	/// Sets a MauiWindowInsetListener on the specified view when it is eligible.
	/// </summary>
	/// <param name="view">The Android view to set the listener on</param>
	public static bool TrySetMauiWindowInsetListener(this View view)
	{
		if (MauiWindowInsetListener.ShouldSetMauiWindowInsetListener(view))
		{
			var listener = new MauiWindowInsetListener();
			ViewCompat.SetOnApplyWindowInsetsListener(view, listener);
			ViewCompat.SetWindowInsetsAnimationCallback(view, listener);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Refreshes the MauiWindowInsetListener attached to the specified view after SafeAreaEdges eligibility changes.
	/// </summary>
	/// <param name="view">The Android view to refresh the listener on</param>
	public static bool RefreshMauiWindowInsetListener(this View view)
	{
		if (MauiWindowInsetListener.ShouldSetMauiWindowInsetListener(view))
		{
			var listener = new MauiWindowInsetListener();
			ViewCompat.SetOnApplyWindowInsetsListener(view, listener);
			ViewCompat.SetWindowInsetsAnimationCallback(view, listener);
			return true;
		}

		ViewCompat.SetOnApplyWindowInsetsListener(view, null);
		ViewCompat.SetWindowInsetsAnimationCallback(view, null);
		MauiWindowInsetListener.ResetViewInsets(view);
		return false;
	}

	/// <summary>
	/// Removes the MauiWindowInsetListener from the specified view and restores its original padding.
	/// This should be called when a view is being detached to ensure proper cleanup.
	/// </summary>
	/// <param name="view">The Android view to remove the listener from</param>
	public static void RemoveMauiWindowInsetListener(this View view)
	{
		// Clear the listeners first
		ViewCompat.SetOnApplyWindowInsetsListener(view, null);
		ViewCompat.SetWindowInsetsAnimationCallback(view, null);

		MauiWindowInsetListener.ResetViewInsets(view);
	}
}
