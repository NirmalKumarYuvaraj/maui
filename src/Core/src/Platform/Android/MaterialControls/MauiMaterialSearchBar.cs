using Android.Content;
using Android.Runtime;
using Android.Util;
using Google.Android.Material.Search;

namespace Microsoft.Maui.Platform;

internal class MauiMaterialSearchBar : SearchBar
{
    public MauiMaterialSearchBar(Context context) : base(MauiMaterialContextThemeWrapper.Create(context))
    {
    }

    protected MauiMaterialSearchBar(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public MauiMaterialSearchBar(Context context, IAttributeSet? attrs) : base(MauiMaterialContextThemeWrapper.Create(context), attrs)
    {
    }

    public MauiMaterialSearchBar(Context context, IAttributeSet? attrs, int defStyleAttr) : base(MauiMaterialContextThemeWrapper.Create(context), attrs, defStyleAttr)
    {
    }
}
