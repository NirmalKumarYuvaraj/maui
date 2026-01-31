#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CoreFoundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui.Handlers
{
	public partial class NavigationViewHandler : ViewHandler<IStackNavigationView, UIView>, IPlatformViewHandler
	{
		const int MaxContainmentRetries = 10;

		StackNavigationManager? _navigationManager;
		UINavigationController? _navigationController;
		bool _isContainmentSetup;
		int _containmentRetryCount;

		/// <summary>
		/// Gets the navigation view from the VirtualView.
		/// </summary>
		public IStackNavigationView NavigationView => ((IStackNavigationView)VirtualView);

		/// <summary>
		/// Gets the current navigation stack.
		/// </summary>
		public IReadOnlyList<IView> NavigationStack => _navigationManager?.NavigationStack ?? new List<IView>();

		/// <summary>
		/// Gets the underlying UINavigationController.
		/// </summary>
		internal UINavigationController? NavigationController => _navigationController;

		/// <summary>
		/// Gets the ViewController for this handler.
		/// For NavigationPage, this returns the UINavigationController.
		/// </summary>
		UIViewController? IPlatformViewHandler.ViewController => _navigationController;

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

			// Defer view controller containment setup until the view is in the hierarchy
			// The view isn't added to the window yet when ConnectHandler is called
			_isContainmentSetup = false;
			_containmentRetryCount = 0;
			DispatchQueue.MainQueue.DispatchAsync(() => TrySetupViewControllerContainment(platformView));
		}

		void TrySetupViewControllerContainment(UIView platformView)
		{
			if (_isContainmentSetup || _navigationController is null)
				return;

			_containmentRetryCount++;

			// iOS requires proper view controller containment for UINavigationController
			// Find the parent view controller - but skip our own navigation controller!
			// We need to traverse up the responder chain to find a VC that's NOT our navigation controller
			UIViewController? parentVC = null;
			UIResponder? responder = platformView.NextResponder;

			while (responder is not null)
			{
				if (responder is UIViewController vc && vc != _navigationController)
				{
					parentVC = vc;
					break;
				}
				responder = responder.NextResponder;
			}


			if (parentVC is not null && _navigationController.ParentViewController is null)
			{
				parentVC.AddChildViewController(_navigationController);
				_navigationController.DidMoveToParentViewController(parentVC);
				_isContainmentSetup = true;
			}
			else if (parentVC is null && _containmentRetryCount < MaxContainmentRetries)
			{
				// Still no parent, try again on next run loop (with retry limit)
				DispatchQueue.MainQueue.DispatchAsync(() => TrySetupViewControllerContainment(platformView));
			}
			else if (parentVC is null)
			{
				// Max retries reached - proceed without containment
				// This can happen when the navigation controller's view is embedded directly
				// in a view hierarchy without a parent view controller (e.g., as root of a Window)				
				_isContainmentSetup = true; // Mark as done to prevent further attempts
			}
		}

		/// <inheritdoc/>
		protected override void DisconnectHandler(UIView platformView)
		{
			if (_navigationManager is not null && _navigationController is not null)
			{
				_navigationManager.Disconnect(VirtualView, _navigationController);

				// Remove from parent view controller if it was added
				if (_navigationController.ParentViewController is not null)
				{
					_navigationController.WillMoveToParentViewController(null);
					_navigationController.RemoveFromParentViewController();
				}
			}
			_isContainmentSetup = false;
			_containmentRetryCount = 0;
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
