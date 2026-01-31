#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui.Handlers
{
	public partial class NavigationViewHandler : ViewHandler<IStackNavigationView, UIView>, IPlatformViewHandler
	{
		StackNavigationManager? _navigationManager;
		UINavigationController? _navigationController;

		/// <summary>
		/// Gets the navigation view from the VirtualView.
		/// </summary>
		public IStackNavigationView NavigationView => ((IStackNavigationView)VirtualView);

		/// <summary>
		/// Gets the current navigation stack.
		/// </summary>
		public IReadOnlyList<IView> NavigationStack => _navigationManager?.NavigationStack ?? new List<IView>();

		/// <inheritdoc/>
		protected override UIView CreatePlatformView()
		{
			// Check if unified handler is enabled
			if (!RuntimeFeature.UseUnifiedNavigationHandler)
			{
				throw new NotImplementedException("Use Microsoft.Maui.Controls.Handlers.Compatibility.NavigationRenderer");
			}

			_navigationController = new UINavigationController();
			_navigationManager = new StackNavigationManager(
				MauiContext ?? throw new InvalidOperationException("MauiContext cannot be null"));

			return _navigationController.View!;
		}

		/// <inheritdoc/>
		protected override void ConnectHandler(UIView platformView)
		{
			if (_navigationManager is not null && _navigationController is not null)
			{
				_navigationManager.Connect(VirtualView, _navigationController);
			}
			base.ConnectHandler(platformView);
		}

		/// <inheritdoc/>
		protected override void DisconnectHandler(UIView platformView)
		{
			if (_navigationManager is not null && _navigationController is not null)
			{
				_navigationManager.Disconnect(VirtualView, _navigationController);
			}
			base.DisconnectHandler(platformView);
		}

		/// <summary>
		/// Handles navigation requests from the cross-platform layer.
		/// </summary>
		public static void RequestNavigation(INavigationViewHandler arg1, IStackNavigation arg2, object? arg3)
		{
			// Check if unified handler is enabled
			if (!RuntimeFeature.UseUnifiedNavigationHandler)
			{
				throw new NotImplementedException("Use Microsoft.Maui.Controls.Handlers.Compatibility.NavigationRenderer");
			}

			if (arg1 is NavigationViewHandler platformHandler && arg3 is NavigationRequest nr)
			{
				platformHandler._navigationManager?.NavigateTo(nr);
			}
			else
			{
				throw new InvalidOperationException("Args must be NavigationRequest");
			}
		}
	}
}
