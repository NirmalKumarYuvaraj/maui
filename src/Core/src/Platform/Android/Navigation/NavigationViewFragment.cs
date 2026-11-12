using System;
using System.Threading;
using Android.OS;
using Android.Runtime;
using Android.Transitions;
using Android.Views;
using Android.Views.Animations;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	public class NavigationViewFragment : Fragment
	{
		AView? _currentView;
		FragmentContainerView? _fragmentContainerView;
		StackNavigationManager? _navigationManager;
		OneShotPreDrawListener? _sharedTransitionPreDrawListener;
		Java.Lang.Runnable? _sharedTransitionPreDrawRunnable;
		Handler? _sharedTransitionTimeoutHandler;
		Action? _startSharedTransition;
		int _sharedTransitionStarted;
		bool _sharedTransitionPostponed;

		FragmentContainerView FragmentContainerView =>
			_fragmentContainerView ?? throw new InvalidOperationException($"FragmentContainerView cannot be null here");

		MauiNavHostFragment? NavHostFragment =>
			this.ParentFragment as MauiNavHostFragment;

		internal StackNavigationManager NavigationManager
		{
			get => _navigationManager ?? NavHostFragment?.StackNavigationManager ?? throw new InvalidOperationException($"NavigationManager cannot be null here");
			set => _navigationManager = value;
		}

		protected NavigationViewFragment(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		public NavigationViewFragment()
		{
		}

		public override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			if ((_navigationManager ?? NavHostFragment?.StackNavigationManager) is { } manager)
				PrepareSharedTransition(manager);
		}

		public override AView OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
		{
			// A pending fragment transaction can run after the navigation manager has been disconnected,
			// for example when a modal NavigationPage is dismissed while it's still navigating
			if ((_navigationManager ?? NavHostFragment?.StackNavigationManager) is not { HasNavHost: true } manager)
				return null!;

			PrepareSharedTransition(manager);
			_fragmentContainerView ??= container as FragmentContainerView;

			// When shuffling around the back stack sometimes we'll need a page to detach and then reattach.
			// This mainly happens when users are removing or inserting pages. If we only have one page
			// and the user inserts a page at index zero we push a page onto the native backstack so
			// that we can get a toolbar with a back button. 

			// Internally Android destroys the first fragment and then creates a second fragment. 
			// Android removes the view associated with the first fragment and then adds the view 
			// now associated with the second fragment. In our case this is the same view.

			// This is all a bit silly because the page is just getting added and removed to the same
			// view. Unfortunately FragmentContainerView is sealed so we can't inherit from it and influence
			// when the views are being added and removed. If this ends up causing too much shake up
			// Then we can try some other approaches like just modifying the navbar ourselves to include a back button
			// Even if there's only one page on the stack

			_currentView =
				NavigationManager
					.CurrentPage
					.ToPlatform(NavigationManager.MauiContext, RequireContext(), inflater, ChildFragmentManager);

			// This shouldn't typically happen, but if a previous animation hasn't finished from a navigation that was interrupted
			// the opacity of the view will be set to 0. This will reset it to 1.
			NavigationManager.CurrentPage?.Handler?.UpdateValue(nameof(IView.Opacity));

			_currentView.RemoveFromParent();

			if (manager.HasSharedElementTransition && _sharedTransitionPostponed)
			{
				SetDestinationTransitionNames(manager);
				StartPostponedTransitionAfterLayout(_currentView);
			}
			else if (_sharedTransitionPostponed)
			{
				StartSharedTransition();
			}

			return _currentView;
		}

		public override void OnResume()
		{
			if (_currentView == null || !NavigationManager.HasNavHost)
			{
				base.OnResume();
				return;
			}

			if (_currentView.Parent == null)
			{
				// Re-add the view to the container if Android removed it
				// see comment inside OnCreateView for more information
				FragmentContainerView.AddView(_currentView);
			}

			base.OnResume();
		}

		public override void OnDestroy()
		{
			CleanupSharedTransition();
			_currentView = null;
			_fragmentContainerView = null;
			_navigationManager = null;

			base.OnDestroy();
		}

		public override Animation OnCreateAnimation(int transit, bool enter, int nextAnim)
		{
			var manager = _navigationManager ?? NavHostFragment?.StackNavigationManager;
			if (manager is not { HasNavHost: true } || manager.HasSharedElementTransition)
				return null!;

			// This means the operation currently being processed shouldn't be animated
			// This will happen if a user inserts or removes a root page
			if (manager.IsPopping == null || !manager.IsAnimated)
			{
				return null!;
			}

			// Delegate to StackNavigationManager which can be overridden for custom animations
			var animation = manager.OnCreateNavigationAnimation(Context!, manager.IsPopping.Value, enter);

			return (animation ?? base.OnCreateAnimation(transit, enter, 0))!;
		}

		void PrepareSharedTransition(StackNavigationManager manager)
		{
			if (!manager.HasSharedElementTransition || _sharedTransitionPostponed)
				return;

			_sharedTransitionStarted = 0;
			_sharedTransitionPostponed = true;
			PostponeEnterTransition();
			manager.ConfigureIncomingSharedElementTransition(this);
		}

		static void SetDestinationTransitionNames(StackNavigationManager manager)
		{
			foreach (var pair in manager.ActiveSharedElementPairs)
			{
				if (pair.Destination.Handler?.PlatformView is AView destinationView)
					manager.SetSharedElementTransitionName(destinationView, pair.Key);
			}
		}

		internal static TransitionSet CreateSharedElementTransition()
		{
			var transition = new TransitionSet();
			transition.AddTransition(new ChangeBounds());
			transition.AddTransition(new ChangeTransform());
			transition.AddTransition(new ChangeImageTransform());
			transition.AddTransition(new ChangeClipBounds());
			return transition;
		}

		void StartPostponedTransitionAfterLayout(AView view)
		{
			_sharedTransitionPreDrawRunnable = new Java.Lang.Runnable(StartSharedTransition);
			_sharedTransitionPreDrawListener = OneShotPreDrawListener.Add(view, _sharedTransitionPreDrawRunnable);

			_sharedTransitionTimeoutHandler = new Handler(Looper.MainLooper!);
			Action startTransition = StartSharedTransition;
			_startSharedTransition = startTransition;
			_sharedTransitionTimeoutHandler.PostDelayed(startTransition, 1000);
		}

		void StartSharedTransition()
		{
			if (Interlocked.Exchange(ref _sharedTransitionStarted, 1) != 0)
				return;

			CleanupSharedTransition();
		}

		internal void CleanupSharedTransition()
		{
			if (_sharedTransitionTimeoutHandler is not null && _startSharedTransition is not null)
				_sharedTransitionTimeoutHandler.RemoveCallbacks(_startSharedTransition);

			_sharedTransitionTimeoutHandler?.Dispose();
			_sharedTransitionTimeoutHandler = null;
			_startSharedTransition = null;

			if (_sharedTransitionPreDrawListener is { } listener)
			{
				listener.RemoveListener();
				listener.Dispose();
				_sharedTransitionPreDrawListener = null;
			}

			_sharedTransitionPreDrawRunnable?.Dispose();
			_sharedTransitionPreDrawRunnable = null;

			if (_sharedTransitionPostponed)
			{
				_sharedTransitionPostponed = false;
				StartPostponedEnterTransition();
			}
		}
	}
}
