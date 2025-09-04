using System;
using Android.Views;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;

namespace Microsoft.Maui.Platform
{
    /// <summary>
    /// Custom window insets handler for FlyoutView that uses margins instead of padding
    /// </summary>
    internal class FlyoutViewInsetsHandler : Java.Lang.Object, IOnApplyWindowInsetsListener, IHandleWindowInsets
    {
        private int _originalTopMargin;
        private bool _hasStoredOriginalMargin;

        public WindowInsetsCompat? OnApplyWindowInsets(View? v, WindowInsetsCompat? insets)
        {
            if (insets == null || v == null)
                return insets;

            return HandleWindowInsets(v, insets);
        }

        public WindowInsetsCompat? HandleWindowInsets(View view, WindowInsetsCompat insets)
        {
            var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
            var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);

            // Here we are using margin because the view passed in is the actual IView from the MAUI xplat control
            // if we set padding it causes the control to inset so we need to use margin
            // realistically this listener will probably go away once we get the SafeAreaEdges code propagated to
            // MauiViews correctly
            if (view.LayoutParameters is DrawerLayout.LayoutParams layoutParams)
            {
                if (!_hasStoredOriginalMargin)
                {
                    _originalTopMargin = layoutParams.TopMargin;
                    _hasStoredOriginalMargin = true;
                }

                layoutParams.TopMargin = topInset;
                view.LayoutParameters = layoutParams;
            }

            return WindowInsetsCompat.Consumed;
        }

        public void ResetWindowInsets(View view)
        {
            if (_hasStoredOriginalMargin && view.LayoutParameters is DrawerLayout.LayoutParams layoutParams)
            {
                layoutParams.TopMargin = _originalTopMargin;
                view.LayoutParameters = layoutParams;
            }
        }
    }
}
