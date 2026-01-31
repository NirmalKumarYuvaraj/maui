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
			Debug.WriteLine($"ShellUnification: NavigationPage.RemapForControls() called");
			Debug.WriteLine($"ShellUnification: UseUnifiedNavigationHandler = {Microsoft.Maui.RuntimeFeature.UseUnifiedNavigationHandler}");
			
			NavigationViewHandler.Mapper.ReplaceMapping<NavigationPage, NavigationViewHandler>(PlatformConfiguration.iOSSpecific.NavigationPage.PrefersLargeTitlesProperty.PropertyName, MapPrefersLargeTitles);
			NavigationViewHandler.Mapper.ReplaceMapping<NavigationPage, NavigationViewHandler>(nameof(BarBackgroundColor), MapBarBackgroundColor);
			NavigationViewHandler.Mapper.ReplaceMapping<NavigationPage, NavigationViewHandler>(nameof(BarTextColor), MapBarTextColor);
			
			Debug.WriteLine($"ShellUnification: NavigationPage.RemapForControls() - Mappings registered for BarBackgroundColor and BarTextColor");
#endif
		}

#if IOS || MACCATALYST
		static void MapBarBackgroundColor(NavigationViewHandler handler, NavigationPage navigationPage)
		{
			Debug.WriteLine($"ShellUnification: MapBarBackgroundColor called");
			Debug.WriteLine($"ShellUnification: handler: {handler?.GetType().Name}, navigationPage: {navigationPage?.GetType().Name}");
			Debug.WriteLine($"ShellUnification: UseUnifiedNavigationHandler = {Microsoft.Maui.RuntimeFeature.UseUnifiedNavigationHandler}");
			
			// Only apply for unified handler
			if (!Microsoft.Maui.RuntimeFeature.UseUnifiedNavigationHandler)
			{
				Debug.WriteLine($"ShellUnification: MapBarBackgroundColor - UseUnifiedNavigationHandler is FALSE, returning");
				return;
			}

			var navigationController = GetNavigationController(handler);
			Debug.WriteLine($"ShellUnification: MapBarBackgroundColor - NavigationController: {navigationController?.GetType().Name ?? "NULL"}");
			
			if (navigationController != null)
			{
				navigationController.UpdateBarBackgroundColor(navigationPage);
			}
			else
			{
				Debug.WriteLine($"ShellUnification: MapBarBackgroundColor - NavigationController is NULL, cannot update");
			}
		}

		static void MapBarTextColor(NavigationViewHandler handler, NavigationPage navigationPage)
		{
			Debug.WriteLine($"ShellUnification: MapBarTextColor called");
			Debug.WriteLine($"ShellUnification: handler: {handler?.GetType().Name}, navigationPage: {navigationPage?.GetType().Name}");
			Debug.WriteLine($"ShellUnification: UseUnifiedNavigationHandler = {Microsoft.Maui.RuntimeFeature.UseUnifiedNavigationHandler}");
			
			// Only apply for unified handler
			if (!Microsoft.Maui.RuntimeFeature.UseUnifiedNavigationHandler)
			{
				Debug.WriteLine($"ShellUnification: MapBarTextColor - UseUnifiedNavigationHandler is FALSE, returning");
				return;
			}

			var navigationController = GetNavigationController(handler);
			Debug.WriteLine($"ShellUnification: MapBarTextColor - NavigationController: {navigationController?.GetType().Name ?? "NULL"}");
			
			if (navigationController != null)
			{
				navigationController.UpdateBarTextColor(navigationPage);
			}
			else
			{
				Debug.WriteLine($"ShellUnification: MapBarTextColor - NavigationController is NULL, cannot update");
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
