using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Android.Views;
using AndroidX.DrawerLayout.Widget;
using AView = Android.Views.View;

namespace Microsoft.Maui.Controls.Platform
{
	internal sealed class ShellFlyoutNavigationRequest : IDisposable
	{
		readonly Shell _shell;
		readonly DrawerLayout _drawer;
		readonly AView _flyout;
		readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		bool _disposed;

		ShellFlyoutNavigationRequest(Shell shell, DrawerLayout drawer, AView flyout)
		{
			_shell = shell;
			_drawer = drawer;
			_flyout = flyout;
			_drawer.DrawerClosed += OnDrawerClosed;
			_drawer.DrawerOpened += OnDrawerOpened;
			_drawer.DrawerStateChanged += OnDrawerStateChanged;
			_drawer.ViewDetachedFromWindow += OnDetachedFromWindow;
			_shell.HandlerChanging += OnHandlerChanging;
			_shell.Navigating += OnNavigating;
			_shell.PropertyChanged += OnShellPropertyChanged;
		}

		internal bool IsCancelled { get; private set; }

		internal static ShellFlyoutNavigationRequest? TryCreate(Shell shell, DrawerLayout drawer)
		{
			if (!drawer.IsAttachedToWindow)
				return null;

			for (int i = 0; i < drawer.ChildCount; i++)
			{
				var child = drawer.GetChildAt(i);
				if (child?.LayoutParameters is DrawerLayout.LayoutParams parameters &&
					(parameters.Gravity & (int)GravityFlags.HorizontalGravityMask) != 0 &&
					drawer.IsDrawerVisible(child))
				{
					return new ShellFlyoutNavigationRequest(shell, drawer, child);
				}
			}

			return null;
		}

		internal Task<bool> CloseAsync()
		{
			// Subscribe before requesting closure: a non-animated close may complete synchronously.
			if (_shell.FlyoutIsPresented)
				_shell.SetValueFromRenderer(Shell.FlyoutIsPresentedProperty, false);
			else
				_drawer.CloseDrawers();

			if (_disposed)
				return _completion.Task;

			if (!_drawer.IsAttachedToWindow || _flyout.Parent != _drawer)
				Cancel();
			else if (!_drawer.IsDrawerVisible(_flyout))
				_completion.TrySetResult(true);

			return _completion.Task;
		}

		void OnDrawerClosed(object? sender, DrawerLayout.DrawerClosedEventArgs args)
		{
			if (args.DrawerView == _flyout)
				_completion.TrySetResult(true);
		}

		void OnDrawerOpened(object? sender, DrawerLayout.DrawerOpenedEventArgs args)
		{
			if (args.DrawerView == _flyout)
				Cancel();
		}

		void OnDrawerStateChanged(object? sender, DrawerLayout.DrawerStateChangedEventArgs args)
		{
			if (args.NewState != DrawerLayout.StateIdle)
				return;

			if (_flyout.Parent != _drawer || _drawer.IsDrawerVisible(_flyout))
				Cancel();
			else
				_completion.TrySetResult(true);
		}

		void OnDetachedFromWindow(object? sender, AView.ViewDetachedFromWindowEventArgs args) => Cancel();

		void OnHandlerChanging(object? sender, HandlerChangingEventArgs args) => Cancel();

		void OnNavigating(object? sender, ShellNavigatingEventArgs args) => Cancel();

		void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs args)
		{
			if ((args.PropertyName == nameof(Shell.FlyoutIsPresented) && _shell.FlyoutIsPresented) ||
				(args.PropertyName == nameof(Shell.FlyoutBehavior) && _shell.GetEffectiveFlyoutBehavior() != FlyoutBehavior.Flyout))
			{
				Cancel();
			}
		}

		void Cancel()
		{
			IsCancelled = true;
			_completion.TrySetResult(false);
			Dispose();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;
			_drawer.DrawerClosed -= OnDrawerClosed;
			_drawer.DrawerOpened -= OnDrawerOpened;
			_drawer.DrawerStateChanged -= OnDrawerStateChanged;
			_drawer.ViewDetachedFromWindow -= OnDetachedFromWindow;
			_shell.HandlerChanging -= OnHandlerChanging;
			_shell.Navigating -= OnNavigating;
			_shell.PropertyChanged -= OnShellPropertyChanged;
			_completion.TrySetResult(false);
		}
	}
}
