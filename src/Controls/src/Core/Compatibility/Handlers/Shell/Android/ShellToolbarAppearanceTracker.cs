#nullable disable
using Android.Graphics.Drawables;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Graphics;
using AToolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace Microsoft.Maui.Controls.Platform.Compatibility
{

	public class ShellToolbarAppearanceTracker : IShellToolbarAppearanceTracker
	{
		bool _disposed;
		IShellContext _shellContext;

		public ShellToolbarAppearanceTracker(IShellContext shellContext)
		{
			_shellContext = shellContext;
		}

		public virtual void SetAppearance(AToolbar toolbar, IShellToolbarTracker toolbarTracker, ShellAppearance appearance)
		{
			var foreground = appearance.ForegroundColor;
			var background = appearance.BackgroundColor;
			var titleColor = appearance.TitleColor;

			SetColors(toolbar, toolbarTracker, foreground, background, titleColor);
		}

		public virtual void ResetAppearance(AToolbar toolbar, IShellToolbarTracker toolbarTracker)
		{
			// Under Material 3, Widget.Material3.Toolbar paints itself via
			// the platform theme — no MAUI override needed.
			if (RuntimeFeature.IsMaterial3Enabled)
				return;

			var context = toolbar.Context;
			SetColors(toolbar, toolbarTracker,
				ShellRenderer.GetDefaultForegroundColor(context),
				ShellRenderer.GetDefaultBackgroundColor(context),
				ShellRenderer.GetDefaultTitleColor(context));
		}

		protected virtual void SetColors(AToolbar toolbar, IShellToolbarTracker toolbarTracker, Color foreground, Color background, Color title)
		{
			if (_disposed)
				return;

			Toolbar shellToolbar = _shellContext?.Shell?.Toolbar;

			if (shellToolbar is null)
				return;

			// Default helpers return null under Material 3; leave the
			// corresponding toolbar property untouched so the native style
			// shows through. Under Material 2 the helpers supply the
			// legacy MAUI defaults.
			var context = toolbar.Context;
			var effectiveTitle = title ?? ShellRenderer.GetDefaultTitleColor(context);
			var effectiveBackground = background ?? ShellRenderer.GetDefaultBackgroundColor(context);
			var effectiveForeground = foreground ?? ShellRenderer.GetDefaultForegroundColor(context);

			if (effectiveTitle is not null)
				shellToolbar.BarTextColor = effectiveTitle;

			if (effectiveBackground is not null)
				shellToolbar.BarBackground = new SolidColorBrush(effectiveBackground);

			if (effectiveForeground is not null)
				shellToolbar.IconColor = effectiveForeground;
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

			if (disposing)
			{
				_shellContext = null;
			}
		}

		#endregion IDisposable
	}
}