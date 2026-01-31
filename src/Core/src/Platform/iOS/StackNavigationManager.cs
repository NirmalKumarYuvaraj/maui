using System;
using System.Collections.Generic;
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
		public IView CurrentPage
			=> _currentPage ?? throw new InvalidOperationException("CurrentPage cannot be null");

		/// <summary>
		/// Gets the underlying UINavigationController.
		/// </summary>
		public UINavigationController NavigationController =>
			_navigationController ?? throw new InvalidOperationException("NavigationController is null");

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
		/// <param name="navigationView">The cross-platform navigation view.</param>
		/// <param name="navigationController">The iOS UINavigationController to manage.</param>
		public virtual void Connect(IStackNavigation navigationView, UINavigationController navigationController)
		{
			if (_navigationController != null)
			{
				Disconnect(navigationView, navigationController);
			}

			_navigationController = navigationController;
			NavigationView = navigationView;

			FirePendingNavigationFinished();
		}

		/// <summary>
		/// Disconnects this manager from the navigation controller.
		/// </summary>
		/// <param name="navigationView">The cross-platform navigation view.</param>
		/// <param name="navigationController">The iOS UINavigationController.</param>
		public virtual void Disconnect(IStackNavigation navigationView, UINavigationController navigationController)
		{
			FirePendingNavigationFinished();

			_navigationController = null;
			NavigationView = null;
		}

		/// <summary>
		/// Processes a navigation request.
		/// </summary>
		/// <param name="args">The navigation request containing the new stack.</param>
		public virtual void NavigateTo(NavigationRequest args)
		{
			if (_navigationController is null)
			{
				throw new InvalidOperationException("NavigationController is not connected.");
			}

			var newPageStack = new List<IView>(args.NavigationStack);
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

			if (previousNavigationStackCount < args.NavigationStack.Count)
			{
				// Push operation
				PushPage(_currentPage, args.Animated);
			}
			else if (previousNavigationStackCount == args.NavigationStack.Count)
			{
				// Replace operation
				ReplacePage(_currentPage, args.Animated);
			}
			else
			{
				// Pop operation
				PopToPage(_currentPage, args.Animated, newPageStack.Count);
			}
		}

		/// <summary>
		/// Pushes a new page onto the navigation stack.
		/// </summary>
		protected virtual void PushPage(IView page, bool animated)
		{
			var viewController = CreateViewControllerForPage(page);

			if (animated)
			{
				_currentNavigationTask = new TaskCompletionSource<bool>();
			}

			_navigationController!.PushViewController(viewController, animated);

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
			var viewController = CreateViewControllerForPage(page);
			var viewControllers = _navigationController!.ViewControllers;

			if (viewControllers is null || viewControllers.Length == 0)
			{
				_navigationController.SetViewControllers(new[] { viewController }, animated);
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

			_navigationController.SetViewControllers(newStack, animated);

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
			var viewControllers = _navigationController!.ViewControllers;

			if (viewControllers is null || viewControllers.Length == 0)
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
				_navigationController.PopToRootViewController(animated);
			}
			else if (targetStackCount < viewControllers.Length)
			{
				var targetViewController = viewControllers[targetStackCount - 1];
				_navigationController.PopToViewController(targetViewController, animated);
			}
			else
			{
				_navigationController.PopViewController(animated);
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
		protected virtual UIViewController CreateViewControllerForPage(IView page)
		{
			var platformPage = page.ToPlatform(MauiContext);

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
			var viewControllers = _navigationController?.ViewControllers;
			if (viewControllers is null)
				return;

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

			_navigationController?.SetViewControllers(newViewControllers.ToArray(), false);
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
