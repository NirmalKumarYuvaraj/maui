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
			SetColors(
				toolbar,
				toolbarTracker,
				ShellRenderer.GetDefaultForegroundColor(toolbar.Context),
				ShellRenderer.GetDefaultBackgroundColor(toolbar.Context),
				ShellRenderer.GetDefaultTitleColor(toolbar.Context));
		}

		protected virtual void SetColors(AToolbar toolbar, IShellToolbarTracker toolbarTracker, Color foreground, Color background, Color title)
		{
			if (_disposed)
				return;

			Toolbar shellToolbar = _shellContext?.Shell?.Toolbar;

			if (shellToolbar is null)
				return;

			shellToolbar.BarTextColor = title ?? ShellRenderer.GetDefaultTitleColor(toolbar.Context);
			shellToolbar.BarBackground = new SolidColorBrush(background ?? ShellRenderer.GetDefaultBackgroundColor(toolbar.Context));
			shellToolbar.IconColor = foreground ?? ShellRenderer.GetDefaultForegroundColor(toolbar.Context);
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