using AToolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Extension methods for Material 3 Toolbar (TopAppBar) styling.
	/// Material 3 provides small, medium, and large TopAppBar variants.
	/// Currently uses AppCompat Toolbar with Material 3 theming.
	/// </summary>
	internal static class ToolbarExtensions
	{
		public static void UpdateTitle(this AToolbar nativeToolbar, IToolbar toolbar)
		{
			nativeToolbar.Title = toolbar?.Title ?? string.Empty;
		}
	}
}
