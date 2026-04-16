#nullable disable
using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.Controls
{
	public partial class Application
	{
		public static void MapWindowSoftInputModeAdjust(ApplicationHandler handler, Application application)
		{
			Platform.ApplicationExtensions.UpdateWindowSoftInputModeAdjust(handler.PlatformView, application);
		}

		partial void OnRequestedThemeChangedPlatform(AppTheme newTheme)
		{
			// Sync AppCompatDelegate.DefaultNightMode with the programmatically set theme.
			// This ensures the Android night mode state is updated even when the handler
			// is not yet attached or the change is triggered outside the property mapper.
			Platform.ApplicationExtensions.UpdateNightMode(this);
		}
	}
}