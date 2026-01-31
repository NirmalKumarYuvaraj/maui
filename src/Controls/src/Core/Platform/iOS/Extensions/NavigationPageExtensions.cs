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
			if (platformView.NavigationBar is null)
			{
				return;
			}

			if (OperatingSystem.IsIOSVersionAtLeast(11) || OperatingSystem.IsMacCatalystVersionAtLeast(11))
			{
				var prefersLargeTitles = navigationPage.OnThisPlatform().PrefersLargeTitles();
				platformView.NavigationBar.PrefersLargeTitles = prefersLargeTitles;
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
			if (platformView.NavigationBar is null)
			{
				return;
			}

			var navigationBar = platformView.NavigationBar;
			var barBackgroundColor = navigationPage.BarBackgroundColor;

			if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13))
			{
				var navigationBarAppearance = navigationBar.StandardAppearance;

				if (barBackgroundColor is null)
				{
					navigationBarAppearance.ConfigureWithOpaqueBackground();
					navigationBarAppearance.BackgroundColor = Maui.Platform.ColorExtensions.BackgroundColor;
				}
				else
				{
					if (barBackgroundColor.Alpha < 1f)
					{
						navigationBarAppearance.ConfigureWithTransparentBackground();
						navigationBar.Translucent = true;
					}
					else
					{
						navigationBarAppearance.ConfigureWithOpaqueBackground();
						navigationBar.Translucent = false;
					}

					navigationBarAppearance.BackgroundColor = barBackgroundColor.ToPlatform();
				}

				navigationBar.CompactAppearance = navigationBarAppearance;
				navigationBar.StandardAppearance = navigationBarAppearance;
				navigationBar.ScrollEdgeAppearance = navigationBarAppearance;
			}
			else
			{
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
		}

		/// <summary>
		/// Updates the navigation bar text color for a NavigationPage.
		/// </summary>
		/// <param name="platformView">The UINavigationController to update.</param>
		/// <param name="navigationPage">The NavigationPage with the color settings.</param>
		internal static void UpdateBarTextColor(this UINavigationController platformView, NavigationPage navigationPage)
		{
			if (platformView.NavigationBar is null)
			{
				return;
			}

			var navigationBar = platformView.NavigationBar;
			var barTextColor = navigationPage.BarTextColor;

			// Determine new title text attributes via global static data
			var globalTitleTextAttributes = UINavigationBar.Appearance.TitleTextAttributes;
			var titleTextAttributes = new UIStringAttributes
			{
				ForegroundColor = barTextColor == null ? globalTitleTextAttributes?.ForegroundColor : barTextColor.ToPlatform(),
				Font = globalTitleTextAttributes?.Font
			};

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
				navigationBar.CompactAppearance.TitleTextAttributes = titleTextAttributes;
				navigationBar.CompactAppearance.LargeTitleTextAttributes = largeTitleTextAttributes;

				navigationBar.StandardAppearance.TitleTextAttributes = titleTextAttributes;
				navigationBar.StandardAppearance.LargeTitleTextAttributes = largeTitleTextAttributes;

				navigationBar.ScrollEdgeAppearance.TitleTextAttributes = titleTextAttributes;
				navigationBar.ScrollEdgeAppearance.LargeTitleTextAttributes = largeTitleTextAttributes;
			}
			else
			{
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
		}
	}
}