using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Google.Android.Material.ProgressIndicator;

namespace Microsoft.Maui.Platform;

internal class MauiMaterialActivityIndicator : CircularProgressIndicator
{
    public MauiMaterialActivityIndicator(Context context) : base(MauiMaterialContextThemeWrapper.Create(context))
    {
        Indeterminate = true;
    }

    protected MauiMaterialActivityIndicator(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public MauiMaterialActivityIndicator(Context context, IAttributeSet? attrs) : base(MauiMaterialContextThemeWrapper.Create(context), attrs)
    {
        Indeterminate = true;
    }

    public MauiMaterialActivityIndicator(Context context, IAttributeSet? attrs, int defStyleAttr) : base(MauiMaterialContextThemeWrapper.Create(context), attrs, defStyleAttr)
    {
        Indeterminate = true;
    }
}
