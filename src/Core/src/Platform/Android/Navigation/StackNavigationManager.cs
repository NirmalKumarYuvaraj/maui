using System;
using System.Collections.Generic;
using Android.Animation;
using Android.Content;
using Android.OS;
using Android.Transitions;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using AndroidX.Navigation;
using AndroidX.Navigation.Fragment;
using AndroidX.Navigation.UI;
using AToolbar = AndroidX.AppCompat.Widget.Toolbar;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform
{
	public class StackNavigationManager
	{
		NavHostFragment? _navHost;
		FragmentNavigator? _fragmentNavigator;
		NavGraph? _navGraph;
		IView? _currentPage;
		Callbacks? _fragmentLifecycleCallbacks;
		FragmentManager? _fragmentManager;
		FragmentContainerView? _fragmentContainerView;
		Type _navigationViewFragmentType = typeof(NavigationViewFragment);
		Queue<NavigationRequest> _navigationQueue = new Queue<NavigationRequest>();
		readonly Dictionary<IView, SharedElementNavigation> _sharedElementNavigations = new();
		readonly Dictionary<AView, string?> _originalTransitionNames = new();
		readonly List<Action> _restoreFragmentTransitions = new();
		readonly List<EasingTimeInterpolator> _sharedTransitionInterpolators = new();
		NavigationViewFragment? _incomingSharedElementFragment;

		bool _processingQueue;

		internal IView? VirtualView { get; private set; }
		internal IStackNavigation? NavigationView { get; private set; }
		internal bool IsNavigating => ActiveRequestedArgs != null;
		internal bool IsInitialNavigation { get; private set; }
		internal bool? IsPopping { get; private set; }
		internal bool IsAnimated { get; set; } = true;
		internal bool HasSharedElementTransition { get; private set; }
		internal IReadOnlyList<SharedElementPair> ActiveSharedElementPairs { get; private set; } = Array.Empty<SharedElementPair>();
		internal NavigationRequest? ActiveRequestedArgs { get; private set; }
		internal NavigationRequest? OnResumeRequestedArgs { get; private set; }
		public IReadOnlyList<IView> NavigationStack { get; private set; } = [];

		public NavHostFragment NavHost =>
			_navHost ?? throw new InvalidOperationException($"NavHost cannot be null");

		public NavController NavController =>
			NavHost.NavController ?? throw new InvalidOperationException($"NavHost cannot be null");

		public FragmentNavigator FragmentNavigator =>
			_fragmentNavigator ?? throw new InvalidOperationException($"FragmentNavigator cannot be null");

		public NavGraph NavGraph => _navGraph ??
			throw new InvalidOperationException($"NavGraph cannot be null");

		public bool HasNavHost => _navHost is not null;

		public IView CurrentPage
			=> _currentPage ?? throw new InvalidOperationException("CurrentPage cannot be null");

		public IMauiContext MauiContext { get; }

		protected IToolbarElement? ToolbarElement =>
			MauiContext.GetNavigationRootManager().ToolbarElement;

		public StackNavigationManager(IMauiContext mauiContext)
		{
			MauiContext = mauiContext;
		}

		/*
		 * The important thing to know going into reading this method is that it's not possible to
		 * modify the backstack. You can only push and pop to and from the top of the stack.
		 * So if a user uses an API like `RemovePage` or `InsertPage` we will typically ignore processing those here
		 * unless it requires changes to the NavBar (i.e removing the first page with only 2 pages on the stack).
		 * Once the user performs an operation that changes the currently visible page then we process any stack changes
		 * that have occurred.
		 * Let's say the user has pages A,B,C,D on the stack 
		 * If they remove Page B and Page C then we don't do anything. Then if the user pushes E onto the stack
		 * we just transform A,B,C,D into A,D,E.
		 * Platform wise that's a "pop" but we use the correct animation for a "push" so visually it looks like a push.
		 * This is also the reason why we aren't using the custom animation features on the navigation component itself.
		 * Because we might be popping but visually pushing.
		 * 
		 * The Fragments that are on the stack also do not have a hard connection to the page they originally rendered.
		 * Whenever a fragment is the "visible" fragment it just figures out what the current page is and displays that.
		 * Fragments are recreated every time they are pushed on the stack but the handler renderer is not.
		 * It's just attached to a new fragment
		 * */
		void ApplyNavigationRequest(NavigationRequest args)
		{
			if (IsNavigating && OnResumeRequestedArgs is null)
			{
				// Queue the navigation request to be processed after the current one completes
				_navigationQueue.Enqueue(args);
				return;
			}

			if (args.NavigationStack.Count == 0)
			{
				throw new InvalidOperationException("NavigationStack cannot be empty");
			}

			ActiveRequestedArgs = args;

			if (_fragmentManager?.IsStateSaved == true)
			{
				OnResumeRequestedArgs = args;
				return;
			}

			OnResumeRequestedArgs = null;

			IReadOnlyList<IView> newPageStack = args.NavigationStack;
			bool animated = args.Animated;
			var navController = NavController;
			var previousNavigationStack = NavigationStack;
			var previousNavigationStackCount = previousNavigationStack.Count;
			bool initialNavigation = NavigationStack.Count == 0;

			// This updates the graphs public navigation stack property so it's outwardly correct
			// But we've saved off the previous stack so we can correctly interpret navigation
			UpdateNavigationStack(newPageStack);

			// This indicates that this is the first navigation request so we need to initialize the graph
			if (initialNavigation)
			{
				IsInitialNavigation = true;
				Initialize(args.NavigationStack);
				return;
			}

			// If the new stack isn't changing the visible page or the app bar then we just ignore
			// the changes because there's no point to applying these to the platform back stack
			// We only apply changes when the currently visible page changes and/or the appbar
			// will change (gain a back button)
			if (newPageStack[newPageStack.Count - 1] == previousNavigationStack[previousNavigationStackCount - 1])
			{
				if (newPageStack.Count != previousNavigationStackCount ||
					!HasMatchingPrefix(previousNavigationStack, newPageStack, newPageStack.Count))
					_sharedElementNavigations.Clear();

				PruneSharedElementNavigations(newPageStack);
				NavigationFinished(NavigationView);

				// There's only one page on the stack then we trigger back button visibility logic
				// so that it can add a back button if it needs to
				if (previousNavigationStackCount == 1 || newPageStack.Count == 1)
					TriggerBackButtonVisibleUpdate();

				return;
			}

			// The incoming fragment uses these variables to pick the correct animation for the current
			// incoming navigation request
			if (newPageStack[newPageStack.Count - 1] == previousNavigationStack[previousNavigationStackCount - 1])
			{
				IsPopping = null;
			}
			else
			{

				IsPopping = newPageStack.Count < previousNavigationStackCount;
			}

			IsAnimated = animated;
			HasSharedElementTransition = false;

			var fragmentNavDestinations = new List<FragmentNavigator.Destination>();
			navController.IterateBackStack(d => fragmentNavDestinations.Add(d));

			// Current BackStack has less entries then incoming new page stack
			// This will add Back Stack Entries until the back stack and the new stack 
			// match up
			if (fragmentNavDestinations.Count < newPageStack.Count)
			{
				var sourcePage = previousNavigationStack[previousNavigationStackCount - 1];
				var destinationPage = newPageStack[newPageStack.Count - 1];
				_sharedElementNavigations.Remove(destinationPage);
				bool isSimplePush = IsPopping == false &&
					newPageStack.Count == previousNavigationStackCount + 1 &&
					fragmentNavDestinations.Count == previousNavigationStackCount &&
					HasMatchingPrefix(previousNavigationStack, newPageStack, previousNavigationStackCount);

				if (!isSimplePush)
					_sharedElementNavigations.Clear();

				for (int i = fragmentNavDestinations.Count; i < newPageStack.Count; i++)
				{
					var dest = AddFragmentDestination();
					var extras = isSimplePush && i == newPageStack.Count - 1
						? CreateSharedElementExtras(sourcePage, destinationPage)
						: null;

					if (extras is null)
						navController.Navigate(dest.Id);
					else
					{
						bool navigated = false;
						try
						{
							navController.Navigate(dest.Id, null, null, extras);
							navigated = true;
						}
						finally
						{
							if (!navigated)
							{
								_sharedElementNavigations.Remove(destinationPage);
								CleanupSharedElementTransition();
							}
						}
					}
				}
			}
			// User just wants to replace the currently visible page but the number
			// of items in the stack are still the same. 
			// In theory we could just prompt the currently active fragment to swap out the new PageView
			// But this way we get an animation
			else if (newPageStack.Count == fragmentNavDestinations.Count)
			{
				_sharedElementNavigations.Clear();
				int lastFragId = fragmentNavDestinations[newPageStack.Count - 1].Id;
				navController.PopBackStack();
				navController.Navigate(lastFragId);
			}
			// Our back stack has more entries on it then  
			else
			{
				int popToId = fragmentNavDestinations[newPageStack.Count - 1].Id;
				bool isSimplePop = IsPopping == true &&
					previousNavigationStackCount == newPageStack.Count + 1 &&
					fragmentNavDestinations.Count == previousNavigationStackCount &&
					HasMatchingPrefix(previousNavigationStack, newPageStack, newPageStack.Count);

				if (isSimplePop)
					PrepareSharedElementReturn(previousNavigationStack[previousNavigationStackCount - 1], newPageStack[newPageStack.Count - 1]);
				else
					_sharedElementNavigations.Clear();

				// Remove the popped destinations from the graph right away. IterateBackStack below looks up every
				// destination of the graph on the back stack, which throws an IllegalArgumentException for a popped one
				bool popped = false;
				try
				{
					popped = navController.PopBackStack(popToId, false);
				}
				finally
				{
					if (!popped)
						CleanupSharedElementTransition();
				}

				if (popped)
				{
					for (int i = newPageStack.Count; i < fragmentNavDestinations.Count; i++)
						NavGraph.Remove(fragmentNavDestinations[i]);
				}
			}

			PruneSharedElementNavigations(newPageStack);

			// We only keep destinations around that are on the backstack
			// This iterates over the new backstack and removes any destinations
			// that are no longer apart of the back stack

			var iterateNewStack = NavController.Graph.Iterator();
			int startId = -1;

			navController.IterateBackStack(nvd =>
			{
				if (startId == -1)
					startId = nvd.Id;
				fragmentNavDestinations.Remove(nvd);
			});

			foreach (var activeDestinations in fragmentNavDestinations)
			{
				NavGraph.Remove(activeDestinations);
			}

			// If we end up removing the destination that was initially the StartDestination
			// The Navigation Graph can get really confused
#pragma warning disable CS0618 // Obsolete
			if (NavGraph.StartDestination != startId)
				NavGraph.StartDestination = startId;
#pragma warning restore CS0618 // Obsolete

			// The NavigationIcon on the toolbar gets set inside the Navigate call so this is the earliest
			// point in time that we can setup toolbar colors for the incoming page
			TriggerBackButtonVisibleUpdate();
		}

		static bool HasMatchingPrefix(IReadOnlyList<IView> first, IReadOnlyList<IView> second, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (!ReferenceEquals(first[i], second[i]))
					return false;
			}

			return true;
		}

		FragmentNavigator.Extras? CreateSharedElementExtras(IView sourcePage, IView destinationPage)
		{
			if (!IsAnimated || IsPopping != false ||
				ActiveRequestedArgs is not { SharedElementPairs.Count: > 0 } request ||
				ChildFragmentManager?.PrimaryNavigationFragment is not NavigationViewFragment currentFragment)
				return null;

			var selected = SelectSharedElements(request.SharedElementPairs, null);
			if (selected.Count == 0)
				return null;

			var extrasBuilder = new FragmentNavigator.Extras.Builder();
			var keys = new HashSet<string>(StringComparer.Ordinal);
			bool prepared = false;

			try
			{
				foreach (var (pair, sourceView) in selected)
				{
					SetSharedElementTransitionName(sourceView, pair.Key);
					extrasBuilder.AddSharedElement(sourceView, pair.Key);
					keys.Add(pair.Key);
				}

				var extras = extrasBuilder.Build();
				ActivateSharedElementTransition(selected, currentFragment, isPopping: false);
				_sharedElementNavigations[destinationPage] = new SharedElementNavigation(sourcePage, keys);
				prepared = true;
				return extras;
			}
			finally
			{
				if (!prepared)
					CleanupSharedElementTransition();
			}
		}

		void PrepareSharedElementReturn(IView sourcePage, IView destinationPage)
		{
			if (!IsAnimated ||
				ActiveRequestedArgs is not { SharedElementPairs.Count: > 0 } request ||
				!_sharedElementNavigations.TryGetValue(sourcePage, out var navigation) ||
				!ReferenceEquals(navigation.SourcePage, destinationPage) ||
				ChildFragmentManager?.PrimaryNavigationFragment is not NavigationViewFragment currentFragment)
				return;

			var selected = SelectSharedElements(request.SharedElementPairs, navigation.Keys);
			if (selected.Count == 0)
				return;

			bool prepared = false;
			try
			{
				foreach (var (pair, sourceView) in selected)
					SetSharedElementTransitionName(sourceView, pair.Key);

				ActivateSharedElementTransition(selected, currentFragment, isPopping: true);
				prepared = true;
			}
			finally
			{
				if (!prepared)
					CleanupSharedElementTransition();
			}
		}

		static List<(SharedElementPair Pair, AView SourceView)> SelectSharedElements(
			IReadOnlyList<SharedElementPair> pairs, HashSet<string>? allowedKeys)
		{
			var selected = new List<(SharedElementPair, AView)>();
			var keys = new HashSet<string>(StringComparer.Ordinal);
			var views = new HashSet<AView>();

			foreach (var pair in pairs)
			{
				if (allowedKeys is not null && !allowedKeys.Contains(pair.Key))
					continue;

				if (pair.Source.Handler?.PlatformView is not AView sourceView || !sourceView.IsAttachedToWindow || !sourceView.IsShown)
					continue;

				if (!keys.Add(pair.Key) || !views.Add(sourceView))
				{
					System.Diagnostics.Debug.WriteLine($"SharedTransition: duplicate native shared element '{pair.Key}' was skipped.");
					continue;
				}

				selected.Add((pair, sourceView));
			}

			return selected;
		}

		void ActivateSharedElementTransition(List<(SharedElementPair Pair, AView SourceView)> selected, NavigationViewFragment currentFragment, bool isPopping)
		{
			var pairs = new List<SharedElementPair>(selected.Count);
			foreach (var (pair, _) in selected)
				pairs.Add(pair);

			ActiveSharedElementPairs = pairs;
			HasSharedElementTransition = true;

			if (isPopping)
			{
				if (!ConfigureEnterTransition(currentFragment))
					return;

				if (!Equals(currentFragment.ReturnTransition, currentFragment.EnterTransition) ||
					!Equals(currentFragment.SharedElementReturnTransition, currentFragment.SharedElementEnterTransition))
				{
					System.Diagnostics.Debug.WriteLine("SharedTransition: custom return transitions take precedence; using regular navigation.");
					CleanupSharedElementTransition();
				}
			}
			else
				ConfigureExitTransition(currentFragment);
		}

		internal void ConfigureIncomingSharedElementTransition(NavigationViewFragment fragment)
		{
			_incomingSharedElementFragment = fragment;

			if (IsPopping == true)
			{
				ConfigureExitTransition(fragment);
				if (!Equals(fragment.ReenterTransition, fragment.ExitTransition))
				{
					System.Diagnostics.Debug.WriteLine("SharedTransition: a custom reenter transition takes precedence; using regular navigation.");
					CleanupSharedElementTransition();
				}
			}
			else
				ConfigureEnterTransition(fragment);
		}

		bool ConfigureEnterTransition(NavigationViewFragment fragment)
		{
			var sharedElementTransition = CreateSharedElementTransition();
			if (sharedElementTransition is null)
			{
				CleanupSharedElementTransition();
				return false;
			}

			var previousEnter = fragment.EnterTransition;
			var previousSharedEnter = fragment.SharedElementEnterTransition;
			_restoreFragmentTransitions.Add(() =>
			{
				if (fragment.IsAlive())
				{
					fragment.EnterTransition = previousEnter;
					fragment.SharedElementEnterTransition = previousSharedEnter;
				}
			});

			fragment.EnterTransition = new Fade();
			fragment.SharedElementEnterTransition = sharedElementTransition;
			return true;
		}

		TransitionSet? CreateSharedElementTransition()
		{
			var transitions = new TransitionSet();
			int transitionCount = 0;

			foreach (var pair in ActiveSharedElementPairs)
			{
				if (pair.Source.Handler?.PlatformView is not AView sourceView ||
					pair.Destination.Handler?.PlatformView is not AView destinationView ||
					!HasImageContent(sourceView) ||
					!HasImageContent(destinationView))
					continue;

				var info = new SharedTransitionInfo(
					pair.Name,
					IsPopping == true,
					pair.Duration,
					pair.Easing,
					sourceView,
					destinationView);

				var transition = OnCreateSharedElementTransition(info);
				if (transition is null)
					continue;

				var interpolator = new EasingTimeInterpolator(pair.Easing);
				transition.SetDuration(pair.Duration);
				transition.SetInterpolator(interpolator);
				_sharedTransitionInterpolators.Add(interpolator);
				transition.AddTarget(sourceView);
				transition.AddTarget(destinationView);
				transitions.AddTransition(transition);
				transitionCount++;
			}

			return transitionCount > 0 ? transitions : null;
		}

		static bool HasImageContent(AView view) =>
			view is not ImageView imageView || imageView.Drawable is not null;

		/// <summary>
		/// Creates the native transition for a shared element. Override to customize its animation.
		/// The returned transition is scoped to the source and destination views in <paramref name="info"/>.
		/// </summary>
		/// <remarks>Return a new transition for each invocation. Duration, easing, and view targets are applied automatically.</remarks>
		/// <param name="info">The shared element and resolved duration and easing.</param>
		/// <returns>The transition to use, or <see langword="null"/> to omit this shared element.</returns>
		protected virtual Transition? OnCreateSharedElementTransition(SharedTransitionInfo info) =>
			NavigationViewFragment.CreateSharedElementTransition();

		sealed class EasingTimeInterpolator : Java.Lang.Object, ITimeInterpolator
		{
			readonly Easing _easing;

			public EasingTimeInterpolator(Easing easing) => _easing = easing;

			public float GetInterpolation(float input) => (float)_easing.Ease(input);
		}

		void ConfigureExitTransition(NavigationViewFragment fragment)
		{
			var previousExit = fragment.ExitTransition;
			_restoreFragmentTransitions.Add(() =>
			{
				if (fragment.IsAlive())
					fragment.ExitTransition = previousExit;
			});

			fragment.ExitTransition = new Fade();
		}

		internal void SetSharedElementTransitionName(AView view, string name)
		{
			if (!_originalTransitionNames.ContainsKey(view))
				_originalTransitionNames.Add(view, ViewCompat.GetTransitionName(view));

			ViewCompat.SetTransitionName(view, name);
		}

		void CleanupSharedElementTransition()
		{
			var incomingFragment = _incomingSharedElementFragment;
			_incomingSharedElementFragment = null;
			if (incomingFragment is not null && incomingFragment.IsAlive())
				incomingFragment.CleanupSharedTransition();

			for (int i = _restoreFragmentTransitions.Count - 1; i >= 0; i--)
				_restoreFragmentTransitions[i]();

			_restoreFragmentTransitions.Clear();
			_sharedTransitionInterpolators.Clear();

			foreach (var (view, originalName) in _originalTransitionNames)
			{
				if (view.IsAlive())
					ViewCompat.SetTransitionName(view, originalName);
			}

			_originalTransitionNames.Clear();
			ActiveSharedElementPairs = Array.Empty<SharedElementPair>();
			HasSharedElementTransition = false;
		}

		void PruneSharedElementNavigations(IReadOnlyList<IView> pages)
		{
			List<IView>? removed = null;
			foreach (var entry in _sharedElementNavigations)
			{
				bool present = false;
				foreach (var page in pages)
				{
					if (ReferenceEquals(entry.Key, page))
					{
						present = true;
						break;
					}
				}

				if (!present)
					(removed ??= new List<IView>()).Add(entry.Key);
			}

			if (removed is not null)
			{
				foreach (var page in removed)
					_sharedElementNavigations.Remove(page);
			}
		}

		sealed class SharedElementNavigation
		{
			public SharedElementNavigation(IView sourcePage, HashSet<string> keys)
			{
				SourcePage = sourcePage;
				Keys = keys;
			}

			public IView SourcePage { get; }
			public HashSet<string> Keys { get; }
		}

		void TriggerBackButtonVisibleUpdate()
		{
			if (NavigationView != null)
			{
				ToolbarElement?.Toolbar?.Handler?.UpdateValue(nameof(IToolbar.BackButtonVisible));
			}
		}

		public void SetNavigationViewFragmentType(Type type)
		{
			ArgumentNullException.ThrowIfNull(type);
			if (!typeof(NavigationViewFragment).IsAssignableFrom(type))
			{
				throw new ArgumentException($"Type {type.FullName} must be assignable to {nameof(NavigationViewFragment)}");
			}

			_navigationViewFragmentType = type;
		}

		public virtual FragmentNavigator.Destination AddFragmentDestination()
		{
			var destination = new FragmentNavigator.Destination(FragmentNavigator);
			var canonicalName = Java.Lang.Class.FromType(_navigationViewFragmentType).CanonicalName;

			if (canonicalName != null)
				destination.SetClassName(canonicalName);

			destination.Id = AView.GenerateViewId();
			NavGraph.AddDestination(destination);
			return destination;
		}

		/// <summary>
		/// Creates the animation for a navigation transition. Override to provide custom page transition animations.
		/// Called by <see cref="NavigationViewFragment.OnCreateAnimation"/> during push/pop navigation.
		/// </summary>
		/// <param name="context">The Android context for loading animation resources.</param>
		/// <param name="isPopping">True if navigating back, false if navigating forward.</param>
		/// <param name="enter">True for the entering fragment's animation, false for the exiting fragment's animation.</param>
		/// <returns>The animation to use, or null to skip animation.</returns>
		public virtual Animation? OnCreateNavigationAnimation(Context context, bool isPopping, bool enter)
		{
			int id;
			if (isPopping)
			{
				id = enter
					? Resource.Animation.nav_default_pop_enter_anim
					: Resource.Animation.nav_default_pop_exit_anim;
			}
			else
			{
				id = enter
					? Resource.Animation.nav_default_enter_anim
					: Resource.Animation.nav_default_exit_anim;
			}

			return id > 0 ? AnimationUtils.LoadAnimation(context, id) : null;
		}

		internal void NavigationFinished(IStackNavigation? navigationView)
		{
			IsInitialNavigation = false;
			CleanupSharedElementTransition();
			IsPopping = null;
			ActiveRequestedArgs = null;
			navigationView?.NavigationFinished(NavigationStack);

			// Process queued navigation requests
			ProcessNavigationQueue();
		}

		void ProcessNavigationQueue()
		{
			if (_processingQueue || _navigationQueue.Count == 0)
				return;

			_processingQueue = true;

			try
			{
				while (_navigationQueue.Count > 0 && !IsNavigating)
				{
					var nextRequest = _navigationQueue.Dequeue();
					ApplyNavigationRequest(nextRequest);
				}
			}
			finally
			{
				_processingQueue = false;
			}
		}

		// This occurs when the navigation page is first being renderer so we sync up the
		// Navigation Stack on the INavigationView to our platform stack
		List<int> Initialize(IReadOnlyList<IView> pages)
		{
			var navController = NavController;

			List<int> destinations = new List<int>();

			NavDestination navDestination;

			foreach (var page in pages)
			{
				navDestination = AddFragmentDestination();
				destinations.Add(navDestination.Id);
			}

			NavGraph.StartDestination = destinations[0];
			navController.SetGraph(NavGraph, null);

			int PlatformNavigationStackCount = 0;

			navController.IterateBackStack(_ => PlatformNavigationStackCount++);

			// set this to one because when the graph is first attached to the controller
			// it will add the graph and the first destination
			if (PlatformNavigationStackCount < 0)
				PlatformNavigationStackCount = 1;

			for (var i = PlatformNavigationStackCount; i < pages.Count; i++)
			{
				var dest = destinations[i];
				navController.Navigate(dest);
			}

			UpdateNavigationStack(pages);
			return destinations;
		}

		void UpdateNavigationStack(IReadOnlyList<IView> newPageStack)
		{
			NavigationStack = new List<IView>(newPageStack);
			_currentPage = NavigationStack[NavigationStack.Count - 1];
		}

		public virtual void Disconnect()
		{
			_navigationQueue.Clear();

			if (IsNavigating)
				NavigationFinished(NavigationView);
			else
				CleanupSharedElementTransition();

			_sharedElementNavigations.Clear();

			if (_fragmentContainerView is not null)
			{
				_fragmentContainerView.ViewAttachedToWindow -= OnNavigationPlatformViewAttachedToWindow;
				_fragmentContainerView.ChildViewAdded -= OnNavigationHostViewAdded;
			}

			_fragmentLifecycleCallbacks?.Disconnect();
			_fragmentLifecycleCallbacks = null;

			VirtualView = null;
			NavigationView = null;
			SetNavHost(null);
			_fragmentNavigator = null;
			_currentPage = null;
			NavigationStack = [];
			_fragmentContainerView = null;
			_fragmentManager = null;
		}

		// Used by NavigationPage: connects the navigation view without an external
		// FragmentContainerView (NavigationPage owns and manages its own container internally).
		// To intercept all Connect calls in a subclass, override the two-arg overload below.
		public virtual void Connect(IView navigationView) => Connect(navigationView, null);

		// Shell provides an external FragmentContainerView; NavigationPage passes null,
		// falling back to navigationView.Handler.PlatformView as the container.
		public virtual void Connect(IView navigationView, FragmentContainerView? fragmentContainerView = null)
		{
			VirtualView = navigationView;
			NavigationView = (IStackNavigation)navigationView;

			// For Shell sections the FragmentContainerView is created externally
			// so we accept it as a parameter.
			if (fragmentContainerView is not null)
			{
				_fragmentContainerView = fragmentContainerView;
			}
			else
			{
				_fragmentContainerView = navigationView.Handler?.PlatformView as FragmentContainerView;
			}

			_fragmentManager = MauiContext?.GetFragmentManager();

			_ = _fragmentManager ?? throw new InvalidOperationException($"GetFragmentManager returned null");
			_ = NavigationView ?? throw new InvalidOperationException($"VirtualView cannot be null");

			// Use the container's actual ID instead of hardcoded Resource.Id.nav_host
			// This allows each Shell tab to have its own unique navigation container
			var containerId = _fragmentContainerView?.Id ?? Resource.Id.nav_host;

			var navHostFragment = _fragmentManager.FindFragmentById(containerId);

			SetNavHost(navHostFragment as NavHostFragment);

			if (_navHost is null)
			{
				throw new InvalidOperationException($"No NavHostFragment found");
			}

			if (_fragmentContainerView is not null)
			{
				_fragmentContainerView.ViewAttachedToWindow += OnNavigationPlatformViewAttachedToWindow;
				_fragmentContainerView.ChildViewAdded += OnNavigationHostViewAdded;
			}
		}

		void OnNavigationPlatformViewAttachedToWindow(object? sender, AView.ViewAttachedToWindowEventArgs e)
		{
			// If the previous Navigation Host Fragment was destroyed then we need to add a new one
			if (_fragmentManager.IsDestroyed(MauiContext.Context) &&
				_fragmentContainerView is not null &&
				_fragmentContainerView.Fragment is null)
			{
				var fragmentManager = MauiContext.GetFragmentManager();

				if (fragmentManager.IsDestroyed(MauiContext.Context))
					return;

				var navHostFragment = new MauiNavHostFragment()
				{
					StackNavigationManager = this
				};

				// We can't call CheckForFragmentChange right away. The Fragment has to finish attaching
				// before we can start interacting with the Navigation Host.
				// OnNavigationHostViewAdded takes care of calling CheckForFragmentChange once the
				// view has been added
				fragmentManager
					.BeginTransactionEx()
					.AddEx(_fragmentContainerView.Id, navHostFragment)
					.Commit();
			}
		}

		void OnNavigationHostViewAdded(object? sender, ViewGroup.ChildViewAddedEventArgs e)
		{
			CheckForFragmentChange();
		}

		internal void CheckForFragmentChange()
		{
			if (_fragmentContainerView?.Fragment is null)
				return;

			var fragmentManager = MauiContext.GetFragmentManager();
			var navHostFragment = _fragmentContainerView?.Fragment;

			if ((navHostFragment != null && _navHost != navHostFragment) || (fragmentManager != _fragmentManager))
			{
				System.Diagnostics.Debug.WriteLine($"CheckForFragmentChange: {_fragmentContainerView}");

				_fragmentManager = fragmentManager;
				_ = _fragmentManager ?? throw new InvalidOperationException($"GetFragmentManager returned null");

				navHostFragment = navHostFragment ?? _fragmentManager.FindFragmentById(Resource.Id.nav_host);

				_fragmentManager = MauiContext.GetFragmentManager();
				_fragmentLifecycleCallbacks?.Disconnect();
				_fragmentLifecycleCallbacks = null;
				SetNavHost(navHostFragment as NavHostFragment);

				if (_navHost == null)
					throw new InvalidOperationException($"No NavHostFragment found");

				_fragmentNavigator =
					(FragmentNavigator)NavController
						.NavigatorProvider
						.GetNavigator(Java.Lang.Class.FromType(typeof(FragmentNavigator)));

				NavController.SetGraph(NavGraph, null);
				_fragmentLifecycleCallbacks = new Callbacks(this, NavController, ChildFragmentManager);
			}
		}

		public virtual void RequestNavigation(NavigationRequest e)
		{
			if (MauiContext == null)
				return;

			CheckForFragmentChange();

			if (_navGraph == null)
			{
				var navGraphNavigator =
				   (NavGraphNavigator)NavController
					   .NavigatorProvider
					   .GetNavigator(Java.Lang.Class.FromType(typeof(NavGraphNavigator)));

				_navGraph = new NavGraph(navGraphNavigator);
			}

			if (_fragmentLifecycleCallbacks == null)
			{
				_fragmentLifecycleCallbacks = new Callbacks(this, NavController, ChildFragmentManager);
			}

			ApplyNavigationRequest(e);
		}

		// Fragments are always destroyed if they aren't visible
		// The Handler/PlatformView associated with the visible IView remain intact
		// The performance hit of destroying/recreating fragments should be negligible
		// Hopefully this behavior survives implementation
		// This will need to be tested with Maps and WebViews to make sure they behave efficiently
		// being removed and then added back to a different Fragment
		// 
		// I'm firing NavigationFinished from here instead of FragmentAnimationFinished because
		// this event appears to fire slightly after `FragmentAnimationFinished` and it also fires
		// if we aren't using animations
		protected virtual void OnNavigationViewFragmentDestroyed(AndroidX.Fragment.App.FragmentManager fm, NavigationViewFragment navHostPageFragment)
		{
			_ = NavigationView ?? throw new InvalidOperationException($"NavigationView cannot be null");

			if (IsNavigating)
			{
				NavigationFinished(NavigationView);
			}
		}

		protected virtual void OnNavigationViewFragmentResumed(AndroidX.Fragment.App.FragmentManager fm, NavigationViewFragment navHostPageFragment)
		{
			if (IsInitialNavigation)
			{
				NavigationFinished(NavigationView);
			}
		}

		protected virtual void OnDestinationChanged(NavController navController, NavDestination navDestination, Bundle? bundle)
		{
		}

		FragmentManager? ChildFragmentManager
		{
			get
			{
				// If you try to access `ChildFragmentManager` and the `NavHost`
				// isn't attached to a context then android will throw an IllegalStateException
				if (_navHost.IsAlive() &&
					_navHost.Context is not null &&
					_navHost.ChildFragmentManager.IsAlive())
				{
					return _navHost.ChildFragmentManager;
				}

				return null;
			}
		}

		void SetNavHost(NavHostFragment? navHost)
		{
			if (_navHost == navHost)
				return;

			if (_navHost is MauiNavHostFragment oldHost)
				oldHost.StackNavigationManager = null;

			if (navHost is MauiNavHostFragment newHost)
				newHost.StackNavigationManager = this;

			_navHost = navHost;

			if (_navHost != null)
			{
				_fragmentNavigator =
					(FragmentNavigator)NavController
						.NavigatorProvider
						.GetNavigator(Java.Lang.Class.FromType(typeof(FragmentNavigator)));

				foreach (var fragment in _navHost.ChildFragmentManager.Fragments)
				{
					if (fragment is NavigationViewFragment nvf)
					{
						nvf.NavigationManager = this;
					}
				}
			}
			else
			{
				_fragmentNavigator = null;
			}
		}

		class Callbacks :
			AndroidX.Fragment.App.FragmentManager.FragmentLifecycleCallbacks,
			NavController.IOnDestinationChangedListener
		{
			StackNavigationManager? _stackNavigationManager;
			NavController _navController;
			FragmentManager? _childFragmentManager;

			public Callbacks(StackNavigationManager navigationLayout, NavController navController, FragmentManager? childFragmentManager)
			{
				_stackNavigationManager = navigationLayout;
				_navController = navController;
				_childFragmentManager = childFragmentManager;

				_navController.AddOnDestinationChangedListener(this);
				_childFragmentManager?.RegisterFragmentLifecycleCallbacks(this, false);
			}

			#region IOnDestinationChangedListener

			void NavController.IOnDestinationChangedListener.OnDestinationChanged(
				NavController p0, NavDestination p1, Bundle? p2)
			{
				_stackNavigationManager?.OnDestinationChanged(p0, p1, p2);
			}

			#endregion

			#region FragmentLifecycleCallbacks
			public override void OnFragmentResumed(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f)
			{
				if (_stackNavigationManager?.VirtualView == null)
					return;

				if (_stackNavigationManager.OnResumeRequestedArgs is not null)
				{
					_stackNavigationManager.ApplyNavigationRequest(_stackNavigationManager.OnResumeRequestedArgs);
					return;
				}

				if (f is NavigationViewFragment pf)
					_stackNavigationManager.OnNavigationViewFragmentResumed(fm, pf);

				AToolbar? platformToolbar = null;
				IToolbar? toolbar = null;

				if (_stackNavigationManager.ToolbarElement?.Toolbar is IToolbar tb &&
					tb?.Handler?.PlatformView is AToolbar ntb)
				{
					platformToolbar = ntb;
					toolbar = tb;
				}

				// Wire up the toolbar to the currently made visible Fragment
				var controller = NavHostFragment.FindNavController(f);
				_ = new AppBarConfiguration
						.Builder(_stackNavigationManager.NavGraph);

				if (platformToolbar != null && toolbar != null && toolbar.Handler?.MauiContext != null)
				{
					if (toolbar.Handler is ToolbarHandler th)
					{
						th.SetupWithNavController(controller, _stackNavigationManager);
					}
				}
			}

			public override void OnFragmentViewDestroyed(
				AndroidX.Fragment.App.FragmentManager fm,
				AndroidX.Fragment.App.Fragment f)
			{
				if (_stackNavigationManager?.VirtualView == null)
					return;

				if (f is NavigationViewFragment pf)
				{
					pf.CleanupSharedTransition();
					_stackNavigationManager.OnNavigationViewFragmentDestroyed(fm, pf);
				}

				base.OnFragmentViewDestroyed(fm, f);
			}

			public override void OnFragmentCreated(FragmentManager fm, Fragment f, Bundle? savedInstanceState)
			{
				if (f is NavigationViewFragment pf && _stackNavigationManager != null)
					pf.NavigationManager = _stackNavigationManager;

				base.OnFragmentCreated(fm, f, savedInstanceState);
			}

			public override void OnFragmentPreCreated(FragmentManager fm, Fragment f, Bundle? savedInstanceState)
			{
				if (f is NavigationViewFragment pf && _stackNavigationManager != null)
					pf.NavigationManager = _stackNavigationManager;

				base.OnFragmentPreCreated(fm, f, savedInstanceState);
			}

			public override void OnFragmentPreAttached(FragmentManager fm, Fragment f, Context context)
			{
				base.OnFragmentPreAttached(fm, f, context);
			}

			public override void OnFragmentStarted(FragmentManager fm, Fragment f)
			{
				base.OnFragmentStarted(fm, f);
			}

			public override void OnFragmentAttached(FragmentManager fm, Fragment f, Context context)
			{
				base.OnFragmentAttached(fm, f, context);
			}

			public override void OnFragmentSaveInstanceState(FragmentManager fm, Fragment f, Bundle outState)
			{
				base.OnFragmentSaveInstanceState(fm, f, outState);
			}

			public override void OnFragmentViewCreated(FragmentManager fm, Fragment f, AView v, Bundle? savedInstanceState)
			{
				base.OnFragmentViewCreated(fm, f, v, savedInstanceState);
			}

			#endregion

			internal void Disconnect()
			{
				_stackNavigationManager = null;

				if (_navController != null && _navController.IsAlive())
					_navController.RemoveOnDestinationChangedListener(this);

				_childFragmentManager?.UnregisterFragmentLifecycleCallbacks(this);
			}
		}
	}
}