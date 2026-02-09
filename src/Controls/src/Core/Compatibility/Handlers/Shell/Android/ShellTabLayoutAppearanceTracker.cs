#nullable disable
using Android.Graphics.Drawables;
using Google.Android.Material.Tabs;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Graphics;
using AColor = Android.Graphics.Color;

namespace Microsoft.Maui.Controls.Platform.Compatibility
{
	public class ShellTabLayoutAppearanceTracker : IShellTabLayoutAppearanceTracker
	{
		bool _disposed;
		IShellContext _shellContext;

		public ShellTabLayoutAppearanceTracker(IShellContext shellContext)
		{
			_shellContext = shellContext;
		}

		public virtual void ResetAppearance(TabLayout tabLayout)
		{
			// When resetting, clear explicit colors to let XML layout theme defaults apply
			// This enables proper Material 3 theming via theme attributes defined in styles.xml
			SetColors(tabLayout, null, null, null, null);
		}

		public virtual void SetAppearance(TabLayout tabLayout, ShellAppearance appearance)
		{
			var foreground = appearance.ForegroundColor;
			var background = appearance.BackgroundColor;
			var titleColor = appearance.TitleColor;
			var unselectedColor = appearance.UnselectedColor;

			SetColors(tabLayout, foreground, background, titleColor, unselectedColor);
		}

		protected virtual void SetColors(TabLayout tabLayout, Color foreground, Color background, Color title, Color unselected)
		{
			// Only apply colors that are explicitly set by the user
			// Null values allow XML layout theme attributes to take effect
			if (title is not null || unselected is not null)
			{
				int titleArgb = title?.ToPlatform().ToArgb() ?? tabLayout.TabTextColors?.DefaultColor ?? AColor.White.ToArgb();
				int unselectedArgb = unselected?.ToPlatform().ToArgb() ?? tabLayout.TabTextColors?.DefaultColor ?? AColor.White.ToArgb();
				tabLayout.SetTabTextColors(unselectedArgb, titleArgb);
			}
			
			if (background is not null)
			{
				tabLayout.SetBackground(new ColorDrawable(background.ToPlatform()));
			}
			
			if (foreground is not null)
			{
				tabLayout.SetSelectedTabIndicatorColor(foreground.ToPlatform());
			}
		}

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;
			_shellContext = null;
		}

		#endregion IDisposable
	}
}