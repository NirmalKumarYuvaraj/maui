#nullable disable
using System.Diagnostics;
using UIKit;

namespace Microsoft.Maui.Controls
{
	public partial class NavigationPage
	{
		public static void MapPrefersLargeTitles(NavigationViewHandler handler, NavigationPage navigationPage) =>
			MapPrefersLargeTitles((INavigationViewHandler)handler, navigationPage);

		public static void MapPrefersLargeTitles(INavigationViewHandler handler, NavigationPage navigationPage)
		{
			Debug.WriteLine($"ShellUnification: MapPrefersLargeTitles called");
			Debug.WriteLine($"ShellUnification: handler type: {handler?.GetType().Name}");
			Debug.WriteLine($"ShellUnification: handler is IPlatformViewHandler: {handler is IPlatformViewHandler}");
			
			if (handler is IPlatformViewHandler nvh)
			{
				Debug.WriteLine($"ShellUnification: ViewController type: {nvh.ViewController?.GetType().Name ?? "NULL"}");
				Debug.WriteLine($"ShellUnification: ViewController is UINavigationController: {nvh.ViewController is UINavigationController}");
				
				if (nvh.ViewController is UINavigationController navigationController)
				{
					Debug.WriteLine($"ShellUnification: Calling UpdatePrefersLargeTitles on NavigationController");
					Platform.NavigationPageExtensions.UpdatePrefersLargeTitles(navigationController, navigationPage);
				}
				else
				{
					Debug.WriteLine($"ShellUnification: ViewController is NOT UINavigationController, skipping UpdatePrefersLargeTitles");
				}
			}
			else
			{
				Debug.WriteLine($"ShellUnification: handler is NOT IPlatformViewHandler, skipping UpdatePrefersLargeTitles");
			}
		}
	}
}