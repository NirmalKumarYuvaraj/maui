using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AndroidX.Core.View;

namespace MauiSafeAreaEdgesDelay;

public partial class MainPage
{
    private void OnLoaded_Platform()
    {
        if (Handler?.PlatformView is Android.Views.View platformView)
        {
            //ViewCompat.SetOnApplyWindowInsetsListener(platformView, new CustomOnApplyWindowInsetsListener());
        }
    }

    class CustomOnApplyWindowInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(Android.Views.View? v, WindowInsetsCompat? insets)
        {
            var resultInsets = insets?.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.Ime());
            if (resultInsets != null)
                v?.SetPadding(resultInsets.Left, resultInsets.Top, resultInsets.Right, resultInsets.Bottom);
            else
                v?.SetPadding(0, 0, 0, 0);

            return insets;
        }
    }
}