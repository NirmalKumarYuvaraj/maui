using AndroidX.Core.View;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Interface for views that need to handle their own window insets behavior
	/// </summary>
	internal interface IHandleWindowInsets
	{
		/// <summary>
		/// Handles window insets for this view
		/// </summary>
		/// <param name="insets">The window insets</param>
		/// <returns>The processed window insets</returns>
		WindowInsetsCompat? HandleWindowInsets(WindowInsetsCompat insets);

		bool IsInsetListenerSet { get; set; }

		bool DidSafeAreaEdgeConfigurationChange { get; set; }
	}
}