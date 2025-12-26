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
			// Reset to defaults - let theme handle background via MauiAppBarLayout style
			SetColors(tabLayout, null, null, null, null);
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
			// Only set colors if explicitly provided - otherwise let theme styles handle it
			if (title != null || unselected != null)
			{
				var titleArgb = title?.ToPlatform().ToArgb() ?? ShellRenderer.DefaultTitleColor.ToPlatform().ToArgb();
				var unselectedArgb = unselected?.ToPlatform().ToArgb() ?? ShellRenderer.DefaultUnselectedColor.ToPlatform().ToArgb();
				tabLayout.SetTabTextColors(unselectedArgb, titleArgb);
			}

			// Only set background if explicitly provided by ShellAppearance
			if (background != null)
			{
				tabLayout.SetBackground(new ColorDrawable(background.ToPlatform()));
			}

			// Only set indicator color if explicitly provided
			if (foreground != null)
			{
				tabLayout.SetSelectedTabIndicatorColor(foreground.ToPlatform());
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