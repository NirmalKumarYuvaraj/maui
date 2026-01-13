using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomNavigation;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Registry entry for tracking view instances and their associated listeners.
	/// Uses WeakReference to avoid memory leaks when views are disposed.
	/// </summary>
	internal record ViewEntry(WeakReference<object> View, MauiWindowInsetListener Listener);

	/// <summary>
	/// Helper class to manage listener registration and attachment for views.
	/// Centralizes all listener-related operations to avoid code duplication.
	/// </summary>
	internal static class WindowInsetListenerHelper
	{
		static readonly List<ViewEntry> _registeredViews = [];

		/// <summary>
		/// Attaches listeners to a view and optionally registers it for child view lookup.
		/// </summary>
		public static void AttachListener(AView view, MauiWindowInsetListener listener, bool registerForChildLookup = true)
		{
			ViewCompat.SetOnApplyWindowInsetsListener(view, listener);
			ViewCompat.SetWindowInsetsAnimationCallback(view, listener);

			if (registerForChildLookup)
			{
				RegisterView(view, listener);
			}
		}

		/// <summary>
		/// Detaches listeners from a view and unregisters it.
		/// </summary>
		public static MauiWindowInsetListener? DetachListener(AView view)
		{
			ViewCompat.SetOnApplyWindowInsetsListener(view, null);
			ViewCompat.SetWindowInsetsAnimationCallback(view, null);

			return UnregisterView(view);
		}

		/// <summary>
		/// Registers a view for child view lookup without attaching listeners to the view itself.
		/// Useful when you want children to find a listener but don't want the parent to consume insets.
		/// </summary>
		public static void RegisterView(AView view, MauiWindowInsetListener listener)
		{
			CleanupDeadReferences();

			// Check if already registered
			foreach (var entry in _registeredViews)
			{
				if (entry.View.TryGetTarget(out var existingView) && existingView == view)
				{
					return;
				}
			}

			_registeredViews.Add(new ViewEntry(new WeakReference<object>(view), listener));
		}

		/// <summary>
		/// Unregisters a view and returns its associated listener.
		/// </summary>
		public static MauiWindowInsetListener? UnregisterView(AView view)
		{
			for (int i = _registeredViews.Count - 1; i >= 0; i--)
			{
				if (_registeredViews[i].View.TryGetTarget(out var registeredView) && registeredView == view)
				{
					var listener = _registeredViews[i].Listener;
					_registeredViews.RemoveAt(i);
					return listener;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the appropriate MauiWindowInsetListener for a view by walking up the view hierarchy.
		/// </summary>
		public static MauiWindowInsetListener? FindListenerForView(AView view)
		{
			var parent = view.Parent;
			while (parent is not null)
			{
				// Skip views inside nested scroll containers or AppBarLayout (except MaterialToolbar)
				if (view is not MaterialToolbar &&
					(parent is AppBarLayout || parent is MauiScrollView || parent is IMauiRecyclerView))
				{
					return null;
				}

				if (parent is AView parentView)
				{
					CleanupDeadReferences();

					foreach (var entry in _registeredViews)
					{
						if (entry.View.TryGetTarget(out var registeredView) && ReferenceEquals(registeredView, parentView))
						{
							return entry.Listener;
						}
					}
				}

				parent = parent.Parent;
			}

			return null;
		}

		static void CleanupDeadReferences()
		{
			for (int i = _registeredViews.Count - 1; i >= 0; i--)
			{
				if (!_registeredViews[i].View.TryGetTarget(out _))
				{
					_registeredViews.RemoveAt(i);
				}
			}
		}
	}

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
		readonly HashSet<AView> _trackedViews = [];
		bool IsImeAnimating { get; set; }
		AView? _pendingView;

		public MauiWindowInsetListener() : base(DispatchModeStop)
		{
		}

		/// <summary>
		/// Sets up a view to use this listener for inset handling.
		/// This method registers the view and attaches the listener.
		/// Must be called on UI thread.
		/// </summary>
		internal static AView SetupViewWithLocalListener(AView view, MauiWindowInsetListener? listener = null)
		{
			listener ??= new MauiWindowInsetListener();
			WindowInsetListenerHelper.AttachListener(view, listener, registerForChildLookup: true);
			return view;
		}

		/// <summary>
		/// Registers a parent view so its children can find an inset listener, without attaching
		/// the listener to the parent itself.
		/// Must be called on UI thread.
		/// </summary>
		internal static MauiWindowInsetListener RegisterParentForChildViews(AView parentView, MauiWindowInsetListener? listener = null)
		{
			listener ??= new MauiWindowInsetListener();
			WindowInsetListenerHelper.RegisterView(parentView, listener);
			return listener;
		}

		/// <summary>
		/// Removes the local listener from a view and properly cleans up.
		/// Must be called on UI thread.
		/// </summary>
		internal static void RemoveViewWithLocalListener(AView view)
		{
			var listener = WindowInsetListenerHelper.DetachListener(view);
			listener?.ResetAppliedSafeAreas(view);
		}

		public virtual WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
		{
			if (insets is null || !insets.HasInsets || v is null || IsImeAnimating)
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

		static WindowInsetsCompat? ApplyDefaultWindowInsets(AView v, WindowInsetsCompat insets)
		{
			var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
			var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
			var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());

			// Handle MaterialToolbar special case early
			if (v is MaterialToolbar)
			{
				v.SetPadding(displayCutout?.Left ?? 0, 0, displayCutout?.Right ?? 0, 0);
				return WindowInsetsCompat.Consumed;
			}

			// Find and configure AppBarLayout
			var appBarLayout = FindAppBarLayout(v);
			var appBarHasContent = HasMeaningfulContent(appBarLayout);

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

			// Handle bottom navigation
			var bottomNavigationView = FindBottomNavigationView(v);
			var hasBottomNav = bottomNavigationView?.MeasuredHeight > 0;

			if (hasBottomNav && bottomNavigationView is not null)
			{
				var bottomInset = Math.Max(systemBars?.Bottom ?? 0, displayCutout?.Bottom ?? 0);
				bottomNavigationView.SetPadding(bottomNavigationView.PaddingLeft, bottomNavigationView.PaddingTop, bottomNavigationView.PaddingRight, bottomInset);
			}

			// Handle keyboard logic
			if (v is CoordinatorLayout cl)
			{
				return HandleKeyboardInsets(cl, insets, systemBars, displayCutout, ime, appBarHasContent, hasBottomNav);
			}

			// Create new insets with consumed values
			return CreateConsumedInsets(insets, systemBars, displayCutout, appBarHasContent, hasBottomNav);
		}

		static AppBarLayout? FindAppBarLayout(AView v)
		{
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
			return appBarLayout;
		}

		static BottomNavigationView? FindBottomNavigationView(AView v)
		{
			var bottomTabsContainer = v.FindViewById(Resource.Id.navigationlayout_bottomtabs);
			if (bottomTabsContainer is ViewGroup container)
			{
				for (int i = 0; i < container.ChildCount; i++)
				{
					if (container.GetChildAt(i) is BottomNavigationView bnv)
					{
						return bnv;
					}
				}
			}
			return null;
		}

		static bool HasMeaningfulContent(AppBarLayout? appBarLayout)
		{
			if (appBarLayout?.MeasuredHeight > 0)
			{
				return true;
			}

			if (appBarLayout is not null)
			{
				for (int i = 0; i < appBarLayout.ChildCount; i++)
				{
					if (appBarLayout.GetChildAt(i)?.MeasuredHeight > 0)
					{
						return true;
					}
				}
			}

			return false;
		}

		static WindowInsetsCompat? HandleKeyboardInsets(CoordinatorLayout cl, WindowInsetsCompat insets, Insets? systemBars, Insets? displayCutout, Insets? ime, bool appBarHasContent, bool hasBottomNav)
		{
			var isKeyboardOpen = ime?.Bottom > 0;

			// Only handle IME adjustments when SoftInputMode is AdjustResize
			if (cl.Context?.GetActivity()?.Window?.Attributes is WindowManagerLayoutParams attr
				&& (attr.SoftInputMode & SoftInput.AdjustResize) == SoftInput.AdjustResize)
			{
				var bottomPadding = isKeyboardOpen ? (ime?.Bottom ?? 0) : 0;
				cl.SetPadding(0, 0, 0, bottomPadding);

				if (isKeyboardOpen)
				{
					var newSystemBars = Insets.Of(
						systemBars?.Left ?? 0,
						appBarHasContent ? 0 : systemBars?.Top ?? 0,
						systemBars?.Right ?? 0,
						0
					) ?? Insets.None;

					var newDisplayCutout = Insets.Of(
						displayCutout?.Left ?? 0,
						appBarHasContent ? 0 : displayCutout?.Top ?? 0,
						displayCutout?.Right ?? 0,
						0
					) ?? Insets.None;

					return new WindowInsetsCompat.Builder(insets)
						?.SetInsets(WindowInsetsCompat.Type.SystemBars(), newSystemBars)
						?.SetInsets(WindowInsetsCompat.Type.DisplayCutout(), newDisplayCutout)
						?.SetInsets(WindowInsetsCompat.Type.Ime(), Insets.None)
						?.Build() ?? insets;
				}
				else if (hasBottomNav)
				{
					// Keyboard closed with tabs: consume bottom insets since tabs handle safe area
					return CreateConsumedInsets(insets, systemBars, displayCutout, appBarHasContent, hasBottomNav);
				}
			}

			return insets;
		}

		static WindowInsetsCompat? CreateConsumedInsets(WindowInsetsCompat insets, Insets? systemBars, Insets? displayCutout, bool appBarHasContent, bool hasBottomNav)
		{
			var newSystemBars = Insets.Of(
				systemBars?.Left ?? 0,
				appBarHasContent ? 0 : systemBars?.Top ?? 0,
				systemBars?.Right ?? 0,
				hasBottomNav ? 0 : systemBars?.Bottom ?? 0
			) ?? Insets.None;

			var newDisplayCutout = Insets.Of(
				displayCutout?.Left ?? 0,
				appBarHasContent ? 0 : displayCutout?.Top ?? 0,
				displayCutout?.Right ?? 0,
				hasBottomNav ? 0 : displayCutout?.Bottom ?? 0
			) ?? Insets.None;

			return new WindowInsetsCompat.Builder(insets)
				?.SetInsets(WindowInsetsCompat.Type.SystemBars(), newSystemBars)
				?.SetInsets(WindowInsetsCompat.Type.DisplayCutout(), newDisplayCutout)
				?.Build() ?? insets;
		}

		public void TrackView(AView view)
		{
			_trackedViews.Add(view);
		}

		public bool HasTrackedView => _trackedViews.Count > 0;

		public bool IsViewTracked(AView view)
		{
			return _trackedViews.Contains(view);
		}

		public void ResetView(AView view)
		{
			if (view is IHandleWindowInsets customHandler)
			{
				customHandler.ResetWindowInsets(view);
			}

			_trackedViews.Remove(view);
		}

		public void ResetAllViews()
		{
			var viewsToReset = _trackedViews.ToArray();
			foreach (var view in viewsToReset)
			{
				ResetView(view);
			}
		}

		public void ResetAppliedSafeAreas(AView view)
		{
			ResetView(view);

			foreach (var trackedView in _trackedViews.ToArray())
			{
				if (IsDescendantOf(trackedView, view))
				{
					ResetView(trackedView);
				}
			}
		}

		static bool IsDescendantOf(AView? child, AView parent)
		{
			if (child is null)
			{
				return false;
			}

			var currentParent = child.Parent;
			while (currentParent is not null)
			{
				if (currentParent == parent)
				{
					return true;
				}

				currentParent = currentParent.Parent;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ResetAllViews();
			}
			base.Dispose(disposing);
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

		static bool IsImeAnimation(WindowInsetsAnimationCompat? animation) =>
			animation is not null && (animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0;
	}
}

internal static class MauiWindowInsetListenerExtensions
{
	public static bool TrySetMauiWindowInsetListener(this View view)
	{
		if (WindowInsetListenerHelper.FindListenerForView(view) is MauiWindowInsetListener listener)
		{
			WindowInsetListenerHelper.AttachListener(view, listener, registerForChildLookup: false);
			return true;
		}

		return false;
	}

	public static void RemoveMauiWindowInsetListener(this View view)
	{
		var listener = WindowInsetListenerHelper.DetachListener(view);
		listener?.ResetView(view);
	}
}
