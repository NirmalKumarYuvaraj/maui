using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Google.Android.Material.TextField;

namespace Microsoft.Maui.Platform;

internal class MauiMaterialTimePicker : TextInputEditText, View.IOnClickListener
{
    public Action? ShowPicker { get; set; }
    public Action? HidePicker { get; set; }

    public MauiMaterialTimePicker(Context context) : base(MauiMaterialContextThemeWrapper.Create(context))
    {
        Initialize();
    }

    protected MauiMaterialTimePicker(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public MauiMaterialTimePicker(Context context, IAttributeSet? attrs) : base(MauiMaterialContextThemeWrapper.Create(context), attrs)
    {
        Initialize();
    }

    public MauiMaterialTimePicker(Context context, IAttributeSet? attrs, int defStyleAttr) : base(MauiMaterialContextThemeWrapper.Create(context), attrs, defStyleAttr)
    {
        Initialize();
    }

    void Initialize()
    {
        Clickable = true;
        Focusable = false;
        SetOnClickListener(this);
    }

    public void OnClick(View? v)
    {
        ShowPicker?.Invoke();
    }
}
