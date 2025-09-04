using System;
using System.Collections.Generic;
using Android.Views;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using Google.Android.Material.AppBar;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
    /// <summary>
    /// Centralized orchestrator for managing window insets across different view types.
    /// Provides unified tracking and reset capabilities for views that have insets applied.
    /// </summary>
    internal class WindowInsetsOrchestrator : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        private readonly HashSet<AView> _trackedViews = new();
        private readonly Dictionary<AView, (int left, int top, int right, int bottom)> _originalPadding = new();

        public WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
        {
            if (insets == null || v == null)
                return insets;

            // Reset all previously tracked views before applying new insets
            ResetTrackedViews();

            // First apply default window insets (handles root view, AppBar, etc.)
            var processedInsets = ApplyDefaultWindowInsets(v, insets);

            // Then pass the processed insets to child views that implement IHandleWindowInsets
            var finalInsets = HandleCustomInsetViews(v, processedInsets ?? insets);

            return finalInsets ?? processedInsets;
        }

        private WindowInsetsCompat? HandleCustomInsetViews(AView rootView, WindowInsetsCompat insets)
        {
            // Check if the root view itself implements the interface
            if (rootView is IHandleWindowInsets customHandler)
            {
                TrackView(rootView);
                return customHandler.HandleWindowInsets(rootView, insets);
            }

            // Search for child views that implement IHandleWindowInsets
            var customViews = FindViewsImplementingInterface(rootView);
            if (customViews.Count > 0)
            {
                WindowInsetsCompat? resultInsets = insets;

                foreach (var view in customViews)
                {
                    if (view is IHandleWindowInsets handler)
                    {
                        TrackView(view);
                        // Pass the current processed insets to each handler
                        resultInsets = handler.HandleWindowInsets(view, resultInsets ?? insets);
                    }
                }

                return resultInsets;
            }

            return null; // No custom handlers found, return null so default processed insets are used
        }

        private static List<AView> FindViewsImplementingInterface(AView parent)
        {
            var result = new List<AView>();

            if (parent is ViewGroup viewGroup)
            {
                for (int i = 0; i < viewGroup.ChildCount; i++)
                {
                    var child = viewGroup.GetChildAt(i);
                    if (child != null)
                    {
                        // Check if this child implements the interface
                        if (child is IHandleWindowInsets)
                        {
                            result.Add(child);
                        }

                        // Recursively search children (but don't go deeper if we found a handler)
                        if (!(child is IHandleWindowInsets))
                        {
                            result.AddRange(FindViewsImplementingInterface(child));
                        }
                    }
                }
            }

            return result;
        }

        private WindowInsetsCompat? ApplyDefaultWindowInsets(AView v, WindowInsetsCompat insets)
        {
            var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

            var leftInset = Math.Max(systemBars?.Left ?? 0, displayCutout?.Left ?? 0);
            var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
            var rightInset = Math.Max(systemBars?.Right ?? 0, displayCutout?.Right ?? 0);
            var bottomInset = Math.Max(systemBars?.Bottom ?? 0, displayCutout?.Bottom ?? 0);

            // Handle special cases
            var appBarLayout = v.FindViewById<AppBarLayout>(Resource.Id.navigationlayout_appbar);
            var hasNavigationBar = (appBarLayout?.GetChildAt(0) as MaterialToolbar)?.LayoutParameters?.Height > 0;
            if (appBarLayout != null)
            {
                TrackView(appBarLayout);
                StorePadding(appBarLayout);
                if (hasNavigationBar)
                    appBarLayout.SetPadding(0, topInset, 0, 0);
                else
                    appBarLayout.SetPadding(0, 0, 0, 0);
            }

            // Apply side and bottom insets to root view, but not top
            TrackView(v);
            StorePadding(v);

            if (hasNavigationBar)
                v.SetPadding(leftInset, 0, rightInset, bottomInset);
            else
                v.SetPadding(leftInset, 0, rightInset, 0);

            // Create new insets with consumed values
            var newSystemBars = Insets.Of(
                systemBars?.Left ?? 0,
                hasNavigationBar ? 0 : systemBars?.Top ?? 0,
                systemBars?.Right ?? 0,
                hasNavigationBar ? 0 : systemBars?.Bottom ?? 0
            ) ?? Insets.None;

            var newDisplayCutout = Insets.Of(
                displayCutout?.Left ?? 0,
                hasNavigationBar ? 0 : displayCutout?.Top ?? 0,
                displayCutout?.Right ?? 0,
                hasNavigationBar ? 0 : displayCutout?.Top ?? 0
            ) ?? Insets.None;

            return new WindowInsetsCompat.Builder(insets)
                ?.SetInsets(WindowInsetsCompat.Type.SystemBars(), newSystemBars)
                ?.SetInsets(WindowInsetsCompat.Type.DisplayCutout(), newDisplayCutout)
                ?.Build() ?? insets;
        }

        private void TrackView(AView view)
        {
            _trackedViews.Add(view);
        }

        private void StorePadding(AView view)
        {
            if (!_originalPadding.ContainsKey(view))
            {
                _originalPadding[view] = (view.PaddingLeft, view.PaddingTop, view.PaddingRight, view.PaddingBottom);
            }
        }

        private void ResetTrackedViews()
        {
            foreach (var view in _trackedViews)
            {
                if (view is IHandleWindowInsets customHandler)
                {
                    customHandler.ResetWindowInsets(view);
                }
                else if (_originalPadding.TryGetValue(view, out var padding))
                {
                    view.SetPadding(padding.left, padding.top, padding.right, padding.bottom);
                }
            }

            _trackedViews.Clear();
            _originalPadding.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ResetTrackedViews();
            }
            base.Dispose(disposing);
        }
    }
}
