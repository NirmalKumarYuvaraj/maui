#nullable disable
using System;
using System.Diagnostics;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;

namespace Microsoft.Maui.Controls
{
	public partial class NavigationPage
	{
		internal static new void RemapForControls()
		{
			// Adjust the mappings to preserve Controls.NavigationPage legacy behaviors
#if IOS || MACCATALYST
			NavigationViewHandler.Mapper.ReplaceMapping<NavigationPage, NavigationViewHandler>(PlatformConfiguration.iOSSpecific.NavigationPage.PrefersLargeTitlesProperty.PropertyName, MapPrefersLargeTitles);
			NavigationViewHandler.Mapper.ReplaceMapping<NavigationPage, NavigationViewHandler>(nameof(BarBackgroundColor), MapBarBackgroundColor);
			NavigationViewHandler.Mapper.ReplaceMapping<NavigationPage, NavigationViewHandler>(nameof(BarTextColor), MapBarTextColor);
#endif
		}

#if IOS || MACCATALYST
		static void MapBarBackgroundColor(NavigationViewHandler handler, NavigationPage navigationPage)
		{			
			// Only apply for unified handler
			if (!Microsoft.Maui.RuntimeFeature.UseUnifiedNavigationHandler)
			{
				return;
			}

			var navigationController = GetNavigationController(handler);
			
			if (navigationController != null)
			{
				navigationController.UpdateBarBackgroundColor(navigationPage);
			}
		}

		static void MapBarTextColor(NavigationViewHandler handler, NavigationPage navigationPage)
		{			
			// Only apply for unified handler
			if (!Microsoft.Maui.RuntimeFeature.UseUnifiedNavigationHandler)
			{
				return;
			}

			var navigationController = GetNavigationController(handler);
			
			if (navigationController != null)
			{
				navigationController.UpdateBarTextColor(navigationPage);
			}
		}

		static UIKit.UINavigationController GetNavigationController(NavigationViewHandler handler)
		{
			// Access the internal NavigationController property via reflection or internal accessor
			// The handler stores the UINavigationController that we need
			return handler.NavigationController;
		}
#endif
	}
}
