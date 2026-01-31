#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Manages navigation stack operations for iOS using UINavigationController.
	/// This unified manager is designed to be used by both NavigationPage and Shell.
	/// </summary>
	internal class StackNavigationManager
	{
		IView? _currentPage;
		IMauiContext _mauiContext;
		UINavigationController? _navigationController;
		Action? _pendingNavigationFinished;
		TaskCompletionSource<bool>? _currentNavigationTask;

		// Pending view controllers - handles iOS timing issue where ViewControllers property
		// doesn't update immediately after SetViewControllers is called
		UIViewController[]? _pendingViewControllers;

		internal IStackNavigation? NavigationView { get; private set; }

		/// <summary>
		/// Gets the current navigation stack.
		/// </summary>
		public IReadOnlyList<IView> NavigationStack { get; private set; } = new List<IView>();

		/// <summary>
		/// Gets the MauiContext used for creating platform views.
		/// </summary>
		public IMauiContext MauiContext => _mauiContext;

		/// <summary>
		/// Gets the current page being displayed.
		/// </summary>
		public IView? CurrentPage => _currentPage;

		/// <summary>
		/// Gets the underlying UINavigationController.
		/// </summary>
		public UINavigationController? NavigationController => _navigationController;

		/// <summary>
		/// Gets a value indicating whether navigation is in progress.
		/// </summary>
		public bool IsNavigating => _currentNavigationTask is not null;

		/// <summary>
		/// Creates a new instance of StackNavigationManager.
		/// </summary>
		/// <param name="mauiContext">The MauiContext to use for view creation.</param>
		public StackNavigationManager(IMauiContext mauiContext)
		{
			_mauiContext = mauiContext;
		}

		/// <summary>
		/// Connects this manager to a navigation view and UINavigationController.
		/// </summary>
		/// <param name="navigationView">The cross-platform navigation view. Can be null for Shell which handles navigation differently.</param>
		/// <param name="navigationController">The iOS UINavigationController to manage.</param>
		public virtual void Connect(IStackNavigation? navigationView, UINavigationController navigationController)
		{
			if (_navigationController != null)
			{
				Disconnect(navigationView, _navigationController);
			}

			_navigationController = navigationController;
			NavigationView = navigationView;
			FirePendingNavigationFinished();
		}

		/// <summary>
		/// Disconnects this manager from the navigation controller.
		/// </summary>
		/// <param name="navigationView">The cross-platform navigation view. Can be null for Shell.</param>
		/// <param name="navigationController">The iOS UINavigationController.</param>
		public virtual void Disconnect(IStackNavigation? navigationView, UINavigationController navigationController)
		{
			FirePendingNavigationFinished();

			_navigationController = null;
			NavigationView = null;
		}

		/// <summary>
		/// Processes a navigation request (used by NavigationPage handler).
		/// </summary>
		/// <param name="args">The navigation request containing the new stack.</param>
		public virtual void NavigateTo(NavigationRequest args)
		{
			if (_navigationController is null)
			{
				throw new InvalidOperationException("NavigationController is not connected.");
			}

			var newPageStack = new List<IView>(args.NavigationStack ?? Array.Empty<IView>());
			var previousNavigationStack = NavigationStack;
			var previousNavigationStackCount = previousNavigationStack.Count;
			bool initialNavigation = NavigationStack.Count == 0;

			// If the current page hasn't changed, just sync the stack
			if (!initialNavigation &&
				newPageStack.Count > 0 &&
				previousNavigationStackCount > 0 &&
				newPageStack[newPageStack.Count - 1] == previousNavigationStack[previousNavigationStackCount - 1])
			{
				SyncBackStackToNavigationStack(newPageStack);
				NavigationStack = newPageStack;
				FireNavigationFinished();
				return;
			}

			_currentPage = newPageStack.Count > 0 ? newPageStack[newPageStack.Count - 1] : null;

			if (_currentPage is null)
			{
				throw new InvalidOperationException("Navigation Request Contains Null Elements");
			}

			NavigationStack = newPageStack;

			if (previousNavigationStackCount < args.NavigationStack!.Count)
			{
				PushPage(_currentPage, args.Animated);
			}
			else if (previousNavigationStackCount == args.NavigationStack.Count)
			{
				ReplacePage(_currentPage, args.Animated);
			}
			else
			{
				PopToPage(_currentPage, args.Animated, newPageStack.Count);
			}
		}

		#region View Controller Stack Management (Shared by Shell and NavigationPage)

		/// <summary>
		/// Gets the currently active view controllers, accounting for pending changes.
		/// </summary>
		public UIViewController[] GetActiveViewControllers()
			=> _pendingViewControllers ?? _navigationController?.ViewControllers ?? Array.Empty<UIViewController>();

		/// <summary>
		/// Clears pending view controllers. Should be called when navigation starts.
		/// </summary>
		public void ClearPendingViewControllers()
			=> _pendingViewControllers = null;

		/// <summary>
		/// Pushes a view controller onto the navigation stack.
		/// </summary>
		/// <param name="viewController">The view controller to push.</param>
		/// <param name="animated">Whether to animate the transition.</param>
		/// <param name="isInMoreTab">Whether the navigation controller is in UITabBarController's More tab.</param>
		/// <param name="parentTabBarController">The parent tab bar controller, if any.</param>
		public virtual void PushViewController(UIViewController viewController, bool animated, bool isInMoreTab = false, UITabBarController? parentTabBarController = null)
		{
			_pendingViewControllers = null;
			if (isInMoreTab && parentTabBarController != null)
			{
				parentTabBarController.MoreNavigationController.PushViewController(viewController, animated);
			}
			else
			{
				_navigationController?.PushViewController(viewController, animated);
			}
		}

		/// <summary>
		/// Pops the top view controller from the navigation stack.
		/// </summary>
		/// <param name="animated">Whether to animate the transition.</param>
		/// <param name="isInMoreTab">Whether the navigation controller is in UITabBarController's More tab.</param>
		/// <param name="parentTabBarController">The parent tab bar controller, if any.</param>
		/// <returns>The view controller that was popped.</returns>
		public virtual UIViewController? PopViewController(bool animated, bool isInMoreTab = false, UITabBarController? parentTabBarController = null)
		{
			_pendingViewControllers = null;
			if (isInMoreTab && parentTabBarController != null)
			{
				return parentTabBarController.MoreNavigationController.PopViewController(animated);
			}
			return _navigationController?.PopViewController(animated);
		}

		/// <summary>
		/// Pops to the root view controller.
		/// </summary>
		/// <param name="animated">Whether to animate the transition.</param>
		/// <returns>The view controllers that were popped.</returns>
		public virtual UIViewController[]? PopToRootViewController(bool animated)
		{
			_pendingViewControllers = null;
			return _navigationController?.PopToRootViewController(animated);
		}

		/// <summary>
		/// Pops to a specific view controller.
		/// </summary>
		/// <param name="viewController">The view controller to pop to.</param>
		/// <param name="animated">Whether to animate the transition.</param>
		/// <returns>The view controllers that were popped.</returns>
		public virtual UIViewController[]? PopToViewController(UIViewController viewController, bool animated)
		{
			_pendingViewControllers = null;
			return _navigationController?.PopToViewController(viewController, animated);
		}

		/// <summary>
		/// Sets the view controllers array directly.
		/// </summary>
		/// <param name="viewControllers">The new view controllers array.</param>
		/// <param name="animated">Whether to animate the transition.</param>
		public virtual void SetViewControllers(UIViewController[] viewControllers, bool animated = false)
		{
			if (_pendingViewControllers != null)
				_pendingViewControllers = viewControllers;

			_navigationController?.SetViewControllers(viewControllers, animated);
		}

		/// <summary>
		/// Removes a view controller from the navigation stack.
		/// </summary>
		/// <param name="viewController">The view controller to remove.</param>
		public virtual void RemoveViewController(UIViewController viewController)
		{
			_pendingViewControllers = _pendingViewControllers ?? _navigationController?.ViewControllers;
			if (_pendingViewControllers != null && _pendingViewControllers.Contains(viewController))
			{
				_pendingViewControllers = _pendingViewControllers.Where(vc => vc != viewController).ToArray();
			}
			SetViewControllers(_pendingViewControllers ?? Array.Empty<UIViewController>(), false);
		}

		/// <summary>
		/// Inserts a view controller at the specified index.
		/// </summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="viewController">The view controller to insert.</param>
		public virtual void InsertViewController(int index, UIViewController viewController)
		{
			_pendingViewControllers = _pendingViewControllers ?? _navigationController?.ViewControllers;
			if (_pendingViewControllers != null)
			{
				var list = _pendingViewControllers.ToList();
				list.Insert(index, viewController);
				_pendingViewControllers = list.ToArray();
			}
			else
			{
				_pendingViewControllers = new[] { viewController };
			}
			SetViewControllers(_pendingViewControllers, false);
		}

		#endregion

		/// <summary>
		/// Pushes a new page onto the navigation stack.
		/// </summary>
		protected virtual void PushPage(IView page, bool animated)
		{

			var viewController = CreateViewControllerForPage(page!);
			if (animated)
			{
				_currentNavigationTask = new TaskCompletionSource<bool>();
			}

			PushViewController(viewController!, animated);

			if (animated)
			{
				// Use CATransaction to detect when animation completes
				CoreAnimation.CATransaction.Begin();
				CoreAnimation.CATransaction.CompletionBlock = () =>
				{
					_currentNavigationTask?.TrySetResult(true);
					_currentNavigationTask = null;
					FireNavigationFinished();
				};
				CoreAnimation.CATransaction.Commit();
			}
			else
			{
				FireNavigationFinished();
			}
		}

		/// <summary>
		/// Replaces the current page with a new page.
		/// </summary>
		protected virtual void ReplacePage(IView page, bool animated)
		{
			var viewController = CreateViewControllerForPage(page!);
			var viewControllers = GetActiveViewControllers();

			if (viewControllers.Length == 0)
			{
				SetViewControllers(new[] { viewController }, animated);
				FireNavigationFinished();
				return;
			}

			// Replace the top view controller
			var newStack = new UIViewController[viewControllers.Length];
			Array.Copy(viewControllers, newStack, viewControllers.Length - 1);
			newStack[newStack.Length - 1] = viewController;

			if (animated)
			{
				_currentNavigationTask = new TaskCompletionSource<bool>();
			}

			SetViewControllers(newStack, animated);

			if (animated)
			{
				CoreAnimation.CATransaction.Begin();
				CoreAnimation.CATransaction.CompletionBlock = () =>
				{
					_currentNavigationTask?.TrySetResult(true);
					_currentNavigationTask = null;
					FireNavigationFinished();
				};
				CoreAnimation.CATransaction.Commit();
			}
			else
			{
				FireNavigationFinished();
			}
		}

		/// <summary>
		/// Pops to a specific page in the stack.
		/// </summary>
		protected virtual void PopToPage(IView page, bool animated, int targetStackCount)
		{
			var viewControllers = GetActiveViewControllers();

			if (viewControllers.Length == 0)
			{
				FireNavigationFinished();
				return;
			}

			if (animated)
			{
				_currentNavigationTask = new TaskCompletionSource<bool>();
			}

			// If we're popping to root or a specific index
			if (targetStackCount == 1)
			{
				PopToRootViewController(animated);
			}
			else if (targetStackCount < viewControllers.Length)
			{
				var targetViewController = viewControllers[targetStackCount - 1];
				PopToViewController(targetViewController, animated);
			}
			else
			{
				PopViewController(animated);
			}

			if (animated)
			{
				CoreAnimation.CATransaction.Begin();
				CoreAnimation.CATransaction.CompletionBlock = () =>
				{
					_currentNavigationTask?.TrySetResult(true);
					_currentNavigationTask = null;
					FireNavigationFinished();
				};
				CoreAnimation.CATransaction.Commit();
			}
			else
			{
				FireNavigationFinished();
			}
		}

		/// <summary>
		/// Creates a view controller for the given page.
		/// </summary>
		public virtual UIViewController CreateViewControllerForPage(IView page)
		{

			_ = page!.ToPlatform(MauiContext);

			if (page.Handler is IPlatformViewHandler handler && handler.ViewController is not null)
			{
				return handler.ViewController;
			}

			// Create a container view controller if needed
			var containerController = new ContainerViewController
			{
				Context = MauiContext,
				CurrentView = (IElement)page
			};

			return containerController;
		}

		/// <summary>
		/// Syncs the back stack to match the navigation stack when only middle entries changed.
		/// </summary>
		protected virtual void SyncBackStackToNavigationStack(IReadOnlyList<IView> pageStack)
		{
			var viewControllers = GetActiveViewControllers();
			var nativeStackCount = viewControllers.Length;

			// If counts already match, nothing to do
			if (nativeStackCount == pageStack.Count)
				return;

			// Build a new view controller array matching the page stack
			var newViewControllers = new List<UIViewController>();

			for (int i = 0; i < pageStack.Count; i++)
			{
				if (i < nativeStackCount)
				{
					// Reuse existing view controller if it matches
					newViewControllers.Add(viewControllers[i]);
				}
				else
				{
					// Create new view controller for new pages
					newViewControllers.Add(CreateViewControllerForPage(pageStack[i]));
				}
			}

			SetViewControllers(newViewControllers.ToArray(), false);
		}

		/// <summary>
		/// Fires the navigation finished callback.
		/// </summary>
		protected void FireNavigationFinished()
		{
			_pendingNavigationFinished = null;
			NavigationView?.NavigationFinished(NavigationStack);
		}

		void FirePendingNavigationFinished()
		{
			Interlocked.Exchange(ref _pendingNavigationFinished, null)?.Invoke();
		}
	}
}
