using Android.Content;
using Android.Runtime;
using Android.Util;
using Google.Android.Material.TextField;

namespace Microsoft.Maui.Platform;

internal class MauiMaterialPicker : TextInputEditText
{
    public MauiMaterialPicker(Context context) : base(MauiMaterialContextThemeWrapper.Create(context))
    {
        Initialize();
    }

    protected MauiMaterialPicker(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public MauiMaterialPicker(Context context, IAttributeSet? attrs) : base(MauiMaterialContextThemeWrapper.Create(context), attrs)
    {
        Initialize();
    }

    public MauiMaterialPicker(Context context, IAttributeSet? attrs, int defStyleAttr) : base(MauiMaterialContextThemeWrapper.Create(context), attrs, defStyleAttr)
    {
        Initialize();
    }

    void Initialize()
    {
        Clickable = true;
        Focusable = false;
        KeyListener = null;
    }
}
