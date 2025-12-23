using Android.Content;
using Android.Runtime;
using Android.Util;
using Google.Android.Material.RadioButton;

namespace Microsoft.Maui.Platform;

internal class MauiMaterialRadioButton : MaterialRadioButton
{
    public MauiMaterialRadioButton(Context context) : base(MauiMaterialContextThemeWrapper.Create(context))
    {
        SoundEffectsEnabled = false;
    }

    protected MauiMaterialRadioButton(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public MauiMaterialRadioButton(Context context, IAttributeSet? attrs) : base(MauiMaterialContextThemeWrapper.Create(context), attrs)
    {
        SoundEffectsEnabled = false;
    }

    public MauiMaterialRadioButton(Context context, IAttributeSet? attrs, int defStyleAttr) : base(MauiMaterialContextThemeWrapper.Create(context), attrs, defStyleAttr)
    {
        SoundEffectsEnabled = false;
    }
}
