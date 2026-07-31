#nullable disable
using Android.Graphics.Drawables;
using Google.Android.Material.Tabs;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Graphics;

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
			SetColors(tabLayout,
				ShellRenderer.GetDefaultForegroundColor(tabLayout.Context),
				ShellRenderer.GetDefaultBackgroundColor(tabLayout.Context),
				ShellRenderer.GetDefaultTitleColor(tabLayout.Context),
				ShellRenderer.GetDefaultUnselectedColor(tabLayout.Context));
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
			var titleArgb = title.ToPlatform(ShellRenderer.GetDefaultTitleColor(tabLayout.Context)).ToArgb();
			var unselectedArgb = unselected.ToPlatform(ShellRenderer.GetDefaultUnselectedColor(tabLayout.Context)).ToArgb();

			tabLayout.SetTabTextColors(unselectedArgb, titleArgb);
			tabLayout.SetBackground(new ColorDrawable(background.ToPlatform(ShellRenderer.GetDefaultBackgroundColor(tabLayout.Context))));
			tabLayout.SetSelectedTabIndicatorColor(foreground.ToPlatform(ShellRenderer.GetDefaultForegroundColor(tabLayout.Context)));
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