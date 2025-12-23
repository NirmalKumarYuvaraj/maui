using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Google.Android.Material.TextField;

namespace Microsoft.Maui.Platform;

internal class MauiMaterialDatePicker : TextInputEditText, View.IOnClickListener
{
    public Action? ShowPicker { get; set; }
    public Action? HidePicker { get; set; }

    public MauiMaterialDatePicker(Context context) : base(MauiMaterialContextThemeWrapper.Create(context))
    {
        Initialize();
    }

    protected MauiMaterialDatePicker(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public MauiMaterialDatePicker(Context context, IAttributeSet? attrs) : base(MauiMaterialContextThemeWrapper.Create(context), attrs)
    {
        Initialize();
    }

    public MauiMaterialDatePicker(Context context, IAttributeSet? attrs, int defStyleAttr) : base(MauiMaterialContextThemeWrapper.Create(context), attrs, defStyleAttr)
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
