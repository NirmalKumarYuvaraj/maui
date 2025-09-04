using System;
using Android.Views;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;

namespace Microsoft.Maui.Platform
{
    /// <summary>
    /// Custom window insets handler for Shell flyout that only handles display cutout insets
    /// </summary>
    internal class ShellFlyoutInsetsHandler : Java.Lang.Object, IOnApplyWindowInsetsListener, IHandleWindowInsets
    {
        private (int left, int top, int right, int bottom) _originalPadding;
        private bool _hasStoredOriginalPadding;

        public WindowInsetsCompat? OnApplyWindowInsets(View? v, WindowInsetsCompat? insets)
        {
            if (insets == null || v == null)
                return insets;

            return HandleWindowInsets(v, insets);
        }

        public WindowInsetsCompat? HandleWindowInsets(View view, WindowInsetsCompat insets)
        {
            if (!_hasStoredOriginalPadding)
            {
                _originalPadding = (view.PaddingLeft, view.PaddingTop, view.PaddingRight, view.PaddingBottom);
                _hasStoredOriginalPadding = true;
            }

            // The flyout overlaps the status bar so we don't really care about insetting it
            var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

            view.SetPadding(0, displayCutout?.Top ?? 0, 0, 0);

            return WindowInsetsCompat.Consumed;
        }

        public void ResetWindowInsets(View view)
        {
            if (_hasStoredOriginalPadding)
            {
                view.SetPadding(_originalPadding.left, _originalPadding.top, _originalPadding.right, _originalPadding.bottom);
            }
        }
    }
}
