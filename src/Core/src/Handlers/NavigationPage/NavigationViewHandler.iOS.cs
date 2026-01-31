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
			Debug.WriteLine("ShellUnification: NavigationViewHandler.CreatePlatformView() called");
			
			// Check if unified handler is enabled
			if (!RuntimeFeature.UseUnifiedNavigationHandler)
			{
				Debug.WriteLine("ShellUnification: UseUnifiedNavigationHandler is FALSE - throwing NotImplementedException");
				throw new NotImplementedException("Use Microsoft.Maui.Controls.Handlers.Compatibility.NavigationRenderer");
			}

			Debug.WriteLine("ShellUnification: UseUnifiedNavigationHandler is TRUE - creating unified handler");
			_navigationController = new MauiNavigationController();
			_navigationManager = new StackNavigationManager(
				MauiContext ?? throw new InvalidOperationException("MauiContext cannot be null"));

			Debug.WriteLine($"ShellUnification: Created UINavigationController: {_navigationController.GetHashCode()}");
			Debug.WriteLine($"ShellUnification: Created StackNavigationManager: {_navigationManager.GetHashCode()}");
			Debug.WriteLine($"ShellUnification: Returning View: {_navigationController.View?.GetHashCode()}, Frame: {_navigationController.View?.Frame}");
			
			return _navigationController.View!;
		}

		/// <inheritdoc/>
		protected override void ConnectHandler(UIView platformView)
		{
			Debug.WriteLine($"ShellUnification: NavigationViewHandler.ConnectHandler() called, platformView: {platformView.GetHashCode()}, Frame: {platformView.Frame}");
			
			if (_navigationManager is not null && _navigationController is not null)
			{
				Debug.WriteLine($"ShellUnification: Connecting StackNavigationManager to NavigationController");
				Debug.WriteLine($"ShellUnification: VirtualView type: {VirtualView?.GetType().Name}");
				
				_navigationManager.Connect(VirtualView, _navigationController);
			}
			else
			{
				Debug.WriteLine($"ShellUnification: WARNING - _navigationManager is null: {_navigationManager is null}, _navigationController is null: {_navigationController is null}");
			}
			
			base.ConnectHandler(platformView);
			
			// Defer view controller containment setup until the view is in the hierarchy
			// The view isn't added to the window yet when ConnectHandler is called
			_isContainmentSetup = false;
			_containmentRetryCount = 0;
			DispatchQueue.MainQueue.DispatchAsync(() => TrySetupViewControllerContainment(platformView));
			
			Debug.WriteLine($"ShellUnification: ConnectHandler complete. View hierarchy: Window={platformView.Window?.GetHashCode()}, Superview={platformView.Superview?.GetHashCode()}");
		}
		
		void TrySetupViewControllerContainment(UIView platformView)
		{
			if (_isContainmentSetup || _navigationController is null)
				return;
			
			_containmentRetryCount++;
			Debug.WriteLine($"ShellUnification: TrySetupContainment (attempt {_containmentRetryCount}/{MaxContainmentRetries}) - Window: {platformView.Window?.GetHashCode()}, Superview: {platformView.Superview?.GetHashCode()}");
				
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
			
			Debug.WriteLine($"ShellUnification: TrySetupContainment - Found parentVC: {parentVC?.GetType().Name ?? "NULL"} (skipped self)");
			
			if (parentVC is not null && _navigationController.ParentViewController is null)
			{
				Debug.WriteLine($"ShellUnification: Adding NavigationController as child of {parentVC.GetType().Name}");
				parentVC.AddChildViewController(_navigationController);
				_navigationController.DidMoveToParentViewController(parentVC);
				_isContainmentSetup = true;
				Debug.WriteLine($"ShellUnification: NavigationController added as child. ParentViewController now: {_navigationController.ParentViewController?.GetType().Name ?? "NULL"}");
			}
			else if (parentVC is null && _containmentRetryCount < MaxContainmentRetries)
			{
				// Still no parent, try again on next run loop (with retry limit)
				Debug.WriteLine($"ShellUnification: TrySetupContainment - parentVC still null, will retry on next run loop");
				DispatchQueue.MainQueue.DispatchAsync(() => TrySetupViewControllerContainment(platformView));
			}
			else if (parentVC is null)
			{
				// Max retries reached - proceed without containment
				// This can happen when the navigation controller's view is embedded directly
				// in a view hierarchy without a parent view controller (e.g., as root of a Window)
				Debug.WriteLine($"ShellUnification: TrySetupContainment - Max retries reached ({MaxContainmentRetries}). Proceeding without containment.");
				Debug.WriteLine($"ShellUnification: This is acceptable when NavigationController is the root or embedded directly in a view.");
				_isContainmentSetup = true; // Mark as done to prevent further attempts
			}
		}

		/// <inheritdoc/>
		protected override void DisconnectHandler(UIView platformView)
		{
			Debug.WriteLine($"ShellUnification: NavigationViewHandler.DisconnectHandler() called");
			
			if (_navigationManager is not null && _navigationController is not null)
			{
				_navigationManager.Disconnect(VirtualView, _navigationController);
				
				// Remove from parent view controller if it was added
				if (_navigationController.ParentViewController is not null)
				{
					Debug.WriteLine($"ShellUnification: Removing NavigationController from parent");
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
			Debug.WriteLine($"ShellUnification: NavigationViewHandler.RequestNavigation() called");
			Debug.WriteLine($"ShellUnification: arg1 type: {arg1?.GetType().Name}, arg3 type: {arg3?.GetType().Name}");
			
			// Check if unified handler is enabled
			if (!RuntimeFeature.UseUnifiedNavigationHandler)
			{
				Debug.WriteLine("ShellUnification: UseUnifiedNavigationHandler is FALSE in RequestNavigation");
				throw new NotImplementedException("Use Microsoft.Maui.Controls.Handlers.Compatibility.NavigationRenderer");
			}

			if (arg1 is NavigationViewHandler platformHandler && arg3 is NavigationRequest nr)
			{
				Debug.WriteLine($"ShellUnification: Calling NavigateTo with request. Stack count: {nr.NavigationStack?.Count}");
				platformHandler._navigationManager?.NavigateTo(nr);
			}
			else
			{
				Debug.WriteLine($"ShellUnification: ERROR - Invalid args. arg1 is NavigationViewHandler: {arg1 is NavigationViewHandler}, arg3 is NavigationRequest: {arg3 is NavigationRequest}");
				throw new InvalidOperationException("Args must be NavigationRequest");
			}
		}
	}
	
	/// <summary>
	/// Custom UINavigationController that provides callbacks for view lifecycle events.
	/// </summary>
	internal class MauiNavigationController : UINavigationController
	{
		public MauiNavigationController() : base()
		{
		}
		
		public override void ViewDidAppear(bool animated)
		{
			Debug.WriteLine($"ShellUnification: MauiNavigationController.ViewDidAppear - ViewControllers count: {ViewControllers?.Length ?? 0}");
			base.ViewDidAppear(animated);
		}
		
		public override void ViewWillAppear(bool animated)
		{
			Debug.WriteLine($"ShellUnification: MauiNavigationController.ViewWillAppear - ViewControllers count: {ViewControllers?.Length ?? 0}");
			base.ViewWillAppear(animated);
		}
	}
}
