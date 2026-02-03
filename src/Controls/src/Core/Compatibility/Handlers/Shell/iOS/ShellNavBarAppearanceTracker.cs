#nullable disable
using System;
using System.ComponentModel;
using System.Runtime.Versioning;
using CoreGraphics;
using Microsoft.Maui.Graphics;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Controls.Platform.Compatibility
{
	public class ShellNavBarAppearanceTracker : IShellNavBarAppearanceTracker
	{
		UIColor _defaultBarTint;
		UIColor _defaultTint;
		UIStringAttributes _defaultTitleAttributes;
		float _shadowOpacity = float.MinValue;
		CGColor _shadowColor;

		public void UpdateLayout(UINavigationController controller)
		{
		}

		public void ResetAppearance(UINavigationController controller)
		{
			if (_defaultTint != null)
			{
				var navBar = controller.NavigationBar;
				navBar.BarTintColor = _defaultBarTint;
				navBar.TintColor = _defaultTint;
				navBar.TitleTextAttributes = _defaultTitleAttributes;

				if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsTvOSVersionAtLeast(13))
					UpdateiOS13NavigationBarAppearance(controller, null);
			}
		}

		public void SetAppearance(UINavigationController controller, ShellAppearance appearance)
		{
			var navBar = controller.NavigationBar;

			if (_defaultTint == null)
			{
				_defaultBarTint = navBar.BarTintColor;
				_defaultTint = navBar.TintColor;
				_defaultTitleAttributes = navBar.TitleTextAttributes;
			}

			if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsTvOSVersionAtLeast(13))
				UpdateiOS13NavigationBarAppearance(controller, appearance);
			else
				UpdateNavigationBarAppearance(controller, appearance);
		}

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
		{
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public virtual void SetHasShadow(UINavigationController controller, bool hasShadow)
		{
			var navigationBar = controller.NavigationBar;
			if (_shadowOpacity == float.MinValue)
			{
				// Don't do anything if user hasn't changed the shadow to true
				if (!hasShadow)
					return;

				_shadowOpacity = navigationBar.Layer.ShadowOpacity;
				_shadowColor = navigationBar.Layer.ShadowColor;
			}

			if (hasShadow)
			{
				navigationBar.Layer.ShadowColor = UIColor.Black.CGColor;
				navigationBar.Layer.ShadowOpacity = 1.0f;
			}
			else
			{
				navigationBar.Layer.ShadowColor = _shadowColor;
				navigationBar.Layer.ShadowOpacity = _shadowOpacity;
			}
		}

		#endregion

		[SupportedOSPlatform("ios13.0")]
		[SupportedOSPlatform("tvos13.0")]
		void UpdateiOS13NavigationBarAppearance(UINavigationController controller, ShellAppearance appearance)
		{
			var navBar = controller.NavigationBar;

			var navigationBarAppearance = new UINavigationBarAppearance();

			if (appearance is null)
			{
				navigationBarAppearance.ConfigureWithOpaqueBackground();
				navBar.Translucent = false;
				navBar.StandardAppearance = navBar.ScrollEdgeAppearance = navigationBarAppearance;
			}
			else
			{
				// Check if we have a Brush background first
				var backgroundBrush = appearance.Background;
				var backgroundColor = appearance.BackgroundColor;

				// since we cannot set the Background Image directly, let's use the alpha in the background color to determine translucence
				if (backgroundColor?.Alpha < 1.0f)
				{
					navigationBarAppearance.ConfigureWithTransparentBackground();
					navBar.Translucent = true;
				}
				else
				{
					navigationBarAppearance.ConfigureWithOpaqueBackground();
					navBar.Translucent = false;
				}

				// Set ForegroundColor
				var foreground = appearance.ForegroundColor;

				if (foreground != null)
					navBar.TintColor = foreground.ToPlatform();

				// Set Background - prioritize Brush over Color
				if (!Brush.IsNullOrEmpty(backgroundBrush))
				{
					// Use brush to create background image for gradient support
					var backgroundImage = navBar.GetBackgroundImage(backgroundBrush);
					if (backgroundImage is not null)
					{
						navigationBarAppearance.BackgroundImage = backgroundImage;
					}
					else if (backgroundBrush is SolidColorBrush solidBrush && solidBrush.Color != null)
					{
						navigationBarAppearance.BackgroundColor = solidBrush.Color.ToPlatform();
					}
				}
				else if (backgroundColor is not null)
				{
					navigationBarAppearance.BackgroundColor = backgroundColor.ToPlatform();
				}

				// Set TitleColor
				var titleColor = appearance.TitleColor;

				if (titleColor != null)
					navigationBarAppearance.TitleTextAttributes = new UIStringAttributes() { ForegroundColor = titleColor.ToPlatform() };

				navBar.StandardAppearance = navBar.ScrollEdgeAppearance = navigationBarAppearance;
			}
		}

		void UpdateNavigationBarAppearance(UINavigationController controller, ShellAppearance appearance)
		{
			var backgroundBrush = appearance.Background;
			var backgroundColor = appearance.BackgroundColor;
			var foreground = appearance.ForegroundColor;
			var titleColor = appearance.TitleColor;

			var navBar = controller.NavigationBar;

			if (backgroundColor?.Alpha == 0f)
			{
				navBar.SetTransparentNavigationBar();
			}
			else
			{
				// Prioritize Brush over Color
				if (!Brush.IsNullOrEmpty(backgroundBrush))
				{
					var backgroundImage = navBar.GetBackgroundImage(backgroundBrush);
					if (backgroundImage != null)
					{
						navBar.SetBackgroundImage(backgroundImage, UIBarMetrics.Default);
					}
					else if (backgroundBrush is SolidColorBrush solidBrush && solidBrush.Color != null)
					{
						navBar.BarTintColor = solidBrush.Color.ToPlatform();
					}
				}
				else if (backgroundColor != null)
				{
					navBar.BarTintColor = backgroundColor.ToPlatform();
				}

				if (foreground != null)
					navBar.TintColor = foreground.ToPlatform();
			}

			if (titleColor != null)
			{
				navBar.TitleTextAttributes = new UIStringAttributes
				{
					ForegroundColor = titleColor.ToPlatform()
				};
			}

			// since we cannot set the Background Image directly, let's use the alpha in the background color to determine translucence
			navBar.Translucent = backgroundColor?.Alpha < 1.0f;
		}
	}
}