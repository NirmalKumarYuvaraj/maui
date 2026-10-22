using Android.Content;
using Android.Content.Res;
using Android.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Platform;

internal static class MaterialColorTokens
{
    public static int GetThemeAttrColor(View view, int attr, AppTheme? requestedTheme)
    {
        var context = view.Context;
        using var configuration = new Configuration(context?.Resources?.Configuration);
        configuration.UiMode = (configuration.UiMode & ~UiMode.NightMask) | (requestedTheme == AppTheme.Dark ? UiMode.NightYes : UiMode.NightNo);

        using var configuredContext = context?.CreateConfigurationContext(configuration);
        configuredContext?.SetTheme(Resource.Style.Maui_Material3_Theme_NoActionBar);
        return configuredContext?.GetThemeAttrColor(attr) ?? default;
    }
}