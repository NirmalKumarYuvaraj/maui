using Android.Content;
using Android.Runtime;
using Android.Util;
using Google.Android.Material.ProgressIndicator;

namespace Microsoft.Maui.Platform;

internal class MauiMaterialProgressBar : LinearProgressIndicator
{
    public MauiMaterialProgressBar(Context context) : base(MauiMaterialContextThemeWrapper.Create(context))
    {
        ShowAnimationBehavior = 0;
        HideAnimationBehavior = 0;
    }

    protected MauiMaterialProgressBar(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public MauiMaterialProgressBar(Context context, IAttributeSet? attrs) : base(MauiMaterialContextThemeWrapper.Create(context), attrs)
    {
        ShowAnimationBehavior = 0;
        HideAnimationBehavior = 0;
    }

    public MauiMaterialProgressBar(Context context, IAttributeSet? attrs, int defStyleAttr) : base(MauiMaterialContextThemeWrapper.Create(context), attrs, defStyleAttr)
    {
        ShowAnimationBehavior = 0;
        HideAnimationBehavior = 0;
    }
}
