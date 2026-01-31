#nullable disable
using System;
using System.Diagnostics;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui.Controls.Platform
{
	public static class NavigationPageExtensions
	{
		public static void UpdatePrefersLargeTitles(this UINavigationController platformView, NavigationPage navigationPage)
		{
			Debug.WriteLine($"ShellUnification: UpdatePrefersLargeTitles called");
			
			if (platformView.NavigationBar is null)
			{
				Debug.WriteLine($"ShellUnification: UpdatePrefersLargeTitles - NavigationBar is NULL, returning");
				return;
			}

			if (OperatingSystem.IsIOSVersionAtLeast(11) || OperatingSystem.IsMacCatalystVersionAtLeast(11))
			{
				var prefersLargeTitles = navigationPage.OnThisPlatform().PrefersLargeTitles();
				Debug.WriteLine($"ShellUnification: UpdatePrefersLargeTitles - PrefersLargeTitles value: {prefersLargeTitles}");
				platformView.NavigationBar.PrefersLargeTitles = prefersLargeTitles;
				Debug.WriteLine($"ShellUnification: UpdatePrefersLargeTitles - Set NavigationBar.PrefersLargeTitles = {platformView.NavigationBar.PrefersLargeTitles}");
			}
			else
			{
				Debug.WriteLine($"ShellUnification: UpdatePrefersLargeTitles - iOS version < 11, skipping");
			}
		}

		internal static void SetTransparentNavigationBar(this UINavigationBar navigationBar)
		{
			navigationBar.SetBackgroundImage(new UIImage(), UIBarMetrics.Default);
			navigationBar.ShadowImage = new UIImage();
			navigationBar.BackgroundColor = UIColor.Clear;
			navigationBar.BarTintColor = UIColor.Clear;
		}

		/// <summary>
		/// Updates the navigation bar background color for a NavigationPage.
		/// </summary>
		/// <param name="platformView">The UINavigationController to update.</param>
		/// <param name="navigationPage">The NavigationPage with the color settings.</param>
		internal static void UpdateBarBackgroundColor(this UINavigationController platformView, NavigationPage navigationPage)
		{
			Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor called");
			Debug.WriteLine($"ShellUnification: NavigationPage: {navigationPage?.GetType().Name}");
			Debug.WriteLine($"ShellUnification: BarBackgroundColor: {navigationPage?.BarBackgroundColor}");
			
			if (platformView.NavigationBar is null)
			{
				Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - NavigationBar is NULL, returning");
				return;
			}

			var navigationBar = platformView.NavigationBar;
			var barBackgroundColor = navigationPage.BarBackgroundColor;
			
			Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - NavigationBar: {navigationBar.GetHashCode()}");
			Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - Color to apply: {barBackgroundColor}");

			if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13))
			{
				Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - Using iOS 13+ appearance API");
				var navigationBarAppearance = navigationBar.StandardAppearance;

				if (barBackgroundColor is null)
				{
					Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - Color is NULL, using default opaque background");
					navigationBarAppearance.ConfigureWithOpaqueBackground();
					navigationBarAppearance.BackgroundColor = Maui.Platform.ColorExtensions.BackgroundColor;
				}
				else
				{
					if (barBackgroundColor.Alpha < 1f)
					{
						Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - Alpha < 1, using transparent background");
						navigationBarAppearance.ConfigureWithTransparentBackground();
						navigationBar.Translucent = true;
					}
					else
					{
						Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - Alpha = 1, using opaque background");
						navigationBarAppearance.ConfigureWithOpaqueBackground();
						navigationBar.Translucent = false;
					}

					navigationBarAppearance.BackgroundColor = barBackgroundColor.ToPlatform();
					Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - Set BackgroundColor to: {barBackgroundColor.ToPlatform()}");
				}

				navigationBar.CompactAppearance = navigationBarAppearance;
				navigationBar.StandardAppearance = navigationBarAppearance;
				navigationBar.ScrollEdgeAppearance = navigationBarAppearance;
				Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - Applied appearance to all states");
			}
			else
			{
				Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - Using legacy API (pre-iOS 13)");
				if (barBackgroundColor?.Alpha == 0f)
				{
					navigationBar.SetTransparentNavigationBar();
				}
				else
				{
					navigationBar.BarTintColor = barBackgroundColor == null
						? UINavigationBar.Appearance.BarTintColor
						: barBackgroundColor.ToPlatform();
				}
			}
			
			Debug.WriteLine($"ShellUnification: UpdateBarBackgroundColor - COMPLETE");
		}

		/// <summary>
		/// Updates the navigation bar text color for a NavigationPage.
		/// </summary>
		/// <param name="platformView">The UINavigationController to update.</param>
		/// <param name="navigationPage">The NavigationPage with the color settings.</param>
		internal static void UpdateBarTextColor(this UINavigationController platformView, NavigationPage navigationPage)
		{
			Debug.WriteLine($"ShellUnification: UpdateBarTextColor called");
			Debug.WriteLine($"ShellUnification: NavigationPage: {navigationPage?.GetType().Name}");
			Debug.WriteLine($"ShellUnification: BarTextColor: {navigationPage?.BarTextColor}");
			
			if (platformView.NavigationBar is null)
			{
				Debug.WriteLine($"ShellUnification: UpdateBarTextColor - NavigationBar is NULL, returning");
				return;
			}

			var navigationBar = platformView.NavigationBar;
			var barTextColor = navigationPage.BarTextColor;
			
			Debug.WriteLine($"ShellUnification: UpdateBarTextColor - NavigationBar: {navigationBar.GetHashCode()}");
			Debug.WriteLine($"ShellUnification: UpdateBarTextColor - Color to apply: {barTextColor}");

			// Determine new title text attributes via global static data
			var globalTitleTextAttributes = UINavigationBar.Appearance.TitleTextAttributes;
			var titleTextAttributes = new UIStringAttributes
			{
				ForegroundColor = barTextColor == null ? globalTitleTextAttributes?.ForegroundColor : barTextColor.ToPlatform(),
				Font = globalTitleTextAttributes?.Font
			};
			
			Debug.WriteLine($"ShellUnification: UpdateBarTextColor - TitleTextAttributes.ForegroundColor: {titleTextAttributes.ForegroundColor}");

			// Determine new large title text attributes via global static data
			var largeTitleTextAttributes = titleTextAttributes;
			if (OperatingSystem.IsIOSVersionAtLeast(11))
			{
				var globalLargeTitleTextAttributes = UINavigationBar.Appearance.LargeTitleTextAttributes;

				largeTitleTextAttributes = new UIStringAttributes
				{
					ForegroundColor = barTextColor == null ? globalLargeTitleTextAttributes?.ForegroundColor : barTextColor.ToPlatform(),
					Font = globalLargeTitleTextAttributes?.Font
				};
			}

			if (OperatingSystem.IsIOSVersionAtLeast(13))
			{
				Debug.WriteLine($"ShellUnification: UpdateBarTextColor - Using iOS 13+ appearance API");
				navigationBar.CompactAppearance.TitleTextAttributes = titleTextAttributes;
				navigationBar.CompactAppearance.LargeTitleTextAttributes = largeTitleTextAttributes;

				navigationBar.StandardAppearance.TitleTextAttributes = titleTextAttributes;
				navigationBar.StandardAppearance.LargeTitleTextAttributes = largeTitleTextAttributes;

				navigationBar.ScrollEdgeAppearance.TitleTextAttributes = titleTextAttributes;
				navigationBar.ScrollEdgeAppearance.LargeTitleTextAttributes = largeTitleTextAttributes;
			}
			else
			{
				Debug.WriteLine($"ShellUnification: UpdateBarTextColor - Using legacy API (pre-iOS 13)");
				navigationBar.TitleTextAttributes = titleTextAttributes;

				if (OperatingSystem.IsIOSVersionAtLeast(11))
					navigationBar.LargeTitleTextAttributes = largeTitleTextAttributes;
			}

			// Set Tint color (i.e. Back Button arrow and Text)
			var iconColor = barTextColor;
			var statusBarColorMode = navigationPage.OnThisPlatform().GetStatusBarTextColorMode();

			navigationBar.TintColor = iconColor == null || statusBarColorMode == StatusBarTextColorMode.DoNotAdjust
				? UINavigationBar.Appearance.TintColor
				: iconColor.ToPlatform();
				
			Debug.WriteLine($"ShellUnification: UpdateBarTextColor - Set TintColor to: {navigationBar.TintColor}");
			Debug.WriteLine($"ShellUnification: UpdateBarTextColor - COMPLETE");
		}
	}
}