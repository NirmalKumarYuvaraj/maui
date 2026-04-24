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
			// Under Material 3, the native Widget.Material3.* tab styles
			// paint the TabLayout correctly — we leave it alone. Under
			// Material 2 we restore the MAUI defaults.
			if (RuntimeFeature.IsMaterial3Enabled)
				return;

			var context = tabLayout.Context;
			SetColors(tabLayout,
				ShellRenderer.GetDefaultForegroundColor(context),
				ShellRenderer.GetDefaultBackgroundColor(context),
				ShellRenderer.GetDefaultTitleColor(context),
				ShellRenderer.GetDefaultUnselectedColor(context));
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
			var context = tabLayout.Context;

			// Fall back to defaults. Under Material 3 these return null,
			// signalling "don't touch" — we preserve the native style for
			// any slot the user didn't explicitly set.
			var effectiveTitle = title ?? ShellRenderer.GetDefaultTitleColor(context);
			var effectiveUnselected = unselected ?? ShellRenderer.GetDefaultUnselectedColor(context);
			var effectiveBackground = background ?? ShellRenderer.GetDefaultBackgroundColor(context);
			var effectiveForeground = foreground ?? ShellRenderer.GetDefaultForegroundColor(context);

			if (effectiveTitle is not null && effectiveUnselected is not null)
				tabLayout.SetTabTextColors(effectiveUnselected.ToPlatform().ToArgb(), effectiveTitle.ToPlatform().ToArgb());

			if (effectiveBackground is not null)
				tabLayout.SetBackground(new ColorDrawable(effectiveBackground.ToPlatform()));

			if (effectiveForeground is not null)
				tabLayout.SetSelectedTabIndicatorColor(effectiveForeground.ToPlatform());
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