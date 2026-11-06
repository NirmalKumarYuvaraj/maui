#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Graphics;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;

namespace Microsoft.Maui.Controls.Handlers
{
    /// <summary>
    /// Fragment that presents the managed ShellSection stack using its ShellContent as the root.
    /// </summary>
    public class ShellContentNavigationFragment : Fragment, IStackNavigation
    {
        ShellContent? _shellContent;
        IMauiContext? _mauiContext;
        ShellSectionHandler? _handler;
        StackNavigationManager? _stackNavigationManager;
        FragmentContainerView? _navigationContainer;
        Page? _rootPage;
        int _navigationContainerId;
        ShellContentStackNavigationView? _navigationViewAdapter;
        ShellSection? _subscribedShellSection; // Cached at subscribe time for reliable unsubscribe

        // Default constructor required by Android's FragmentManager for fragment restoration.
        // When FragmentStateAdapter (ViewPager2) saves and restores fragment state during tab switches,
        // it uses Fragment.instantiate() which requires a parameterless constructor.
        public ShellContentNavigationFragment()
        {
        }

        public ShellContentNavigationFragment(ShellContent? shellContent, IMauiContext? mauiContext, ShellSectionHandler? handler)
        {
            _shellContent = shellContent;
            _mauiContext = mauiContext;
            _handler = handler;
        }

        public override void OnCreate(Bundle? savedInstanceState)
        {
            // Always pass null to prevent restoring stale child fragment state.
            // OffscreenPageLimit keeps fragments alive so restoration shouldn't occur,
            // but this is defense-in-depth.
            base.OnCreate(null);
        }

        public override AView OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            // If this fragment was restored by FragmentManager without proper data,
            // return an empty view. The ViewPager2 adapter will recreate it properly.
            if (_shellContent is null || _mauiContext is null)
            {
                return new FrameLayout(inflater.Context!);
            }

            if (_navigationContainer is not null)
            {
                if (_navigationContainer.Parent is ViewGroup containerParent)
                {
                    containerParent.RemoveView(_navigationContainer);
                }

                // Re-subscribe to NavigationRequested if it was unsubscribed in OnDestroyView.
                // This handles the case where the fragment's view is destroyed and recreated
                // (e.g., ViewPager2 offscreen recycling) — without this, navigation silently stops.
                if (_stackNavigationManager is not null && !_subscribedToNavigationRequested)
                {
                    var parentSection = _shellContent?.Parent as ShellSection;
                    if (parentSection is not null)
                    {
                        _subscribedShellSection = parentSection;
                        ((IShellSectionController)parentSection).NavigationRequested += OnNavigationRequested;
                        _subscribedToNavigationRequested = true;
                    }
                }

                // Check if NavHostFragment needs recreation
                var existingNavHost = ChildFragmentManager.FindFragmentById(_navigationContainerId);
                if (existingNavHost is null && _stackNavigationManager is not null)
                {
                    var recreatedNavHost = new MauiNavHostFragment()
                    {
                        StackNavigationManager = _stackNavigationManager
                    };
                    ChildFragmentManager
                        .BeginTransactionEx()
                        .AddEx(_navigationContainerId, recreatedNavHost)
                        .CommitNowAllowingStateLoss();
                }

                if (IsCurrentContent)
                {
                    InitializeNavigation();
                }

                return _navigationContainer;
            }

            // Create FragmentContainerView for navigation stack
            if (_navigationContainerId == 0)
            {
                _navigationContainerId = AView.GenerateViewId();
            }

            _navigationContainer = new FragmentContainerView(_mauiContext!.Context!)
            {
                Id = _navigationContainerId,
                LayoutParameters = new LP(LP.MatchParent, LP.MatchParent)
            };

            // Keep offscreen fragments cached without creating never-selected pages or nav hosts.
            // Previously initialized content still follows the existing view-recreation path.
            if (_rootPage is not null || IsCurrentContent)
            {
                InitializeNavigation();
            }

            return _navigationContainer;
        }

        /// <inheritdoc />
        public override void OnResume()
        {
            base.OnResume();

            if (IsCurrentContent)
            {
                InitializeNavigation();
            }
        }

        public override void OnPause()
        {
            if (!IsCurrentContent)
                SuspendNavigation();
            base.OnPause();
        }

        internal void SuspendNavigation()
        {
            if (_stackNavigationManager?.HasPendingNavigation != true && _pendingNavigationRequests.Count == 0)
                return;

            _stackNavigationManager?.Disconnect();
            _stackNavigationManager = null;
            _navigationViewAdapter = null;
            CancelPendingNavigation();
        }

        bool IsCurrentContent =>
            _shellContent?.Parent is ShellSection shellSection &&
            shellSection.IsVisibleSection &&
            shellSection.CurrentItem == _shellContent;

        internal void InitializeNavigation()
        {
            if (_stackNavigationManager is not null)
            {
                ReconcileNavigationStack();
                return;
            }

            if (_navigationContainer is null || _shellContent is null || _mauiContext is null)
            {
                return;
            }

            _rootPage = ((IShellContentController)_shellContent).GetOrCreateContent();
            if (_rootPage is null)
            {
                return;
            }

            // Subscribe before the host is attached; initialization reconciles the section's stack.
            var shellSection = _shellContent.Parent as ShellSection;
            if (shellSection is not null && !_subscribedToNavigationRequested)
            {
                _subscribedShellSection = shellSection;
                ((IShellSectionController)shellSection).NavigationRequested += OnNavigationRequested;
                _subscribedToNavigationRequested = true;
            }

            // Create StackNavigationManager with scoped context
            var scopedContext = _mauiContext.MakeScoped(fragmentManager: ChildFragmentManager);
            _stackNavigationManager = _handler is not null
                ? new ShellStackNavigationManager(scopedContext, _handler)
                : new StackNavigationManager(scopedContext);

            if (ChildFragmentManager.FindFragmentById(_navigationContainerId) is MauiNavHostFragment retainedHost)
            {
                retainedHost.StackNavigationManager = _stackNavigationManager;
            }
            else
            {
                var navHostFragment = new MauiNavHostFragment()
                {
                    StackNavigationManager = _stackNavigationManager
                };
                ChildFragmentManager
                    .BeginTransactionEx()
                    .AddEx(_navigationContainerId, navHostFragment)
                    .CommitNowAllowingStateLoss();
            }

            if (_navigationContainer.IsAttachedToWindow)
            {
                ConnectAndInitialize();
            }
            else
            {
                _navigationContainer.ViewAttachedToWindow += OnNavigationContainerAttached;
            }
        }

        bool _subscribedToNavigationRequested;

        void OnNavigationContainerAttached(object? sender, AView.ViewAttachedToWindowEventArgs e)
        {
            if (sender != _navigationContainer || !IsAdded)
                return;

            _navigationContainer?.ViewAttachedToWindow -= OnNavigationContainerAttached;
            ConnectAndInitialize();
        }

        void ConnectAndInitialize()
        {
            if (_stackNavigationManager is null || _stackNavigationManager.HasNavHost || _rootPage is null)
            {
                return;
            }

            // Create adapter that wraps the root page and delegates NavigationFinished to this fragment
            _navigationViewAdapter = new ShellContentStackNavigationView(_rootPage, this);

            // Connect using the adapter (which properly implements IStackNavigationView via page delegation)
            _stackNavigationManager.Connect(_navigationViewAdapter, _navigationContainer);

            // Apply background to the navigation container when the page has no explicit Background.
            // Matches old ShellPageContainer constructor behavior.
            if (_rootPage is IView view && view.Background is null && _navigationContainer is not null)
            {
                var context = _mauiContext!.Context!;
                bool isDark = Controls.Application.Current?.RequestedTheme == ApplicationModel.AppTheme.Dark;
                int bgColor = RuntimeFeature.IsMaterial3Enabled
                    ? GetMaterial3Background(context)
                    : GetResourceBackground(context, isDark);
                _navigationContainer.SetBackgroundColor(new global::Android.Graphics.Color(bgColor));
            }

            // Subscribe to navigation events if not already subscribed
            // (may have already been done in InitializeNavigation)
            var shellSection = _shellContent?.Parent as ShellSection;
            if (shellSection is not null && !_subscribedToNavigationRequested)
            {
                _subscribedShellSection = shellSection;
                ((IShellSectionController)shellSection).NavigationRequested += OnNavigationRequested;
                _subscribedToNavigationRequested = true;
            }

            var initialStack = _pendingNavigationRequests.Count == 0
                ? BuildNavigationStack()
                : new List<IView> { _rootPage };
            _stackNavigationManager.RequestNavigation(new NavigationRequest(initialStack, false));

            while (_pendingNavigationRequests.Count > 0)
            {
                var pending = _pendingNavigationRequests.Dequeue();
                _stackNavigationManager.RequestNavigation(pending.Request, pending.Completion);
            }
        }

        void ReconcileNavigationStack()
        {
            if (!IsCurrentContent || _stackNavigationManager is not { HasNavHost: true })
            {
                return;
            }

            var requestedStack = BuildNavigationStack();
            var currentStack = _stackNavigationManager.ActiveRequestedArgs?.NavigationStack
                ?? _stackNavigationManager.NavigationStack;
            if (currentStack.Count == requestedStack.Count)
            {
                int i = 0;
                for (; i < currentStack.Count; i++)
                {
                    if (currentStack[i] != requestedStack[i])
                    {
                        break;
                    }
                }

                if (i == currentStack.Count)
                {
                    return;
                }
            }

            _stackNavigationManager.RequestNavigation(new NavigationRequest(requestedStack, false));
        }

        void OnNavigationRequested(object? sender, NavigationRequestedEventArgs e)
        {
            // Only handle navigation for THIS ShellContent
            // Note: Don't check IsVisible - ViewPager2 may report pages as not visible
            // even when they are the current item
            if (_stackNavigationManager is null)
            {
                return;
            }

            var shellSection = _shellContent?.Parent as ShellSection;
            if (shellSection is null || shellSection.CurrentItem != _shellContent)
            {
                return;
            }

            var request = new NavigationRequest(BuildNavigationStack(), e.Animated);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            e.Task = completion.Task;

            if (!_stackNavigationManager.HasNavHost)
            {
                _pendingNavigationRequests.Enqueue((request, completion));
                return;
            }

            _stackNavigationManager.RequestNavigation(request, completion);
        }

        readonly Queue<(NavigationRequest Request, TaskCompletionSource<bool> Completion)> _pendingNavigationRequests = new();

        List<IView> BuildNavigationStack()
        {
            var stack = new List<IView>
            {
                _rootPage ?? throw new InvalidOperationException("ShellContent root page has not been initialized.")
            };

            // ShellSection applies each mutation before raising NavigationRequested.
            // Its first entry is a root placeholder, resolved by the selected content.
            if (_shellContent?.Parent is ShellSection section && section.CurrentItem == _shellContent)
            {
                var sectionStack = section.Stack;
                for (int i = 1; i < sectionStack.Count; i++)
                {
                    if (sectionStack[i] is Page page)
                    {
                        stack.Add(page);
                    }
                }
            }

            return stack;
        }

        void IStackNavigation.RequestNavigation(NavigationRequest request)
        {
            _stackNavigationManager?.RequestNavigation(request);
        }

        // IStackNavigation.NavigationFinished - delegates to the internal method
        void IStackNavigation.NavigationFinished(IReadOnlyList<IView> newStack)
        {
            OnNavigationFinished(newStack);
        }

        /// <summary>
        /// Called by ShellContentStackNavigationView when navigation completes.
        /// Updates toolbar and tab visibility based on the new navigation stack.
        /// </summary>
        internal void OnNavigationFinished(IReadOnlyList<IView> newStack)
        {
            if (!IsVisible || _handler is null)
            {
                return;
            }

            // Check if this content is currently active
            var shellSection = _shellContent?.Parent as ShellSection;
            var shellItem = shellSection?.Parent as ShellItem;
            var shell = shellItem?.Parent as Shell;

            if (shellSection is null || shellItem is null || shell is null)
            {
                return;
            }

            if (shell.CurrentItem != shellItem ||
                shellItem.CurrentItem != shellSection ||
                shellSection.CurrentItem != _shellContent)
            {
                return;
            }

            // Update toolbar
            if (newStack.Count > 0 && newStack[newStack.Count - 1] is Page currentPage)
            {
                var toolbarTracker = _handler.ToolbarTracker;
                var shellToolbar = _handler.ShellToolbar;
                if (toolbarTracker is not null && shellToolbar is not null)
                {
                    toolbarTracker.CanNavigateBack = newStack.Count > 1;

                    if (toolbarTracker.Page != currentPage)
                    {
                        toolbarTracker.Page = currentPage;
                    }

                    shellToolbar.Handler?.UpdateValue(nameof(IToolbar.BackButtonVisible));

                    if (shell.Toolbar is ShellToolbar st)
                    {
                        st.ApplyChanges();
                    }

                    ((IShellController)shell).AppearanceChanged(currentPage, false);
                }
            }

            // Hide/show content tabs based on navigation depth.
            // Push beyond root → hide top tabs; pop to root → show top tabs.
            // Uses PlaceTopTabs/RemoveTopTabs (fragment add/remove in NRM slot).
            var shouldShowTabs = newStack.Count == 1 && ((IShellSectionController)shellSection).GetItems().Count > 1;

            if (shouldShowTabs)
            {
                _handler.PlaceTopTabs();
            }
            else
            {
                _handler.RemoveTopTabs();
            }
        }

        internal static void CloseActiveActionModes(AView? view)
        {
            if (view is global::Android.Widget.ListView listView)
            {
                // Unwrap HeaderViewListAdapter — ListViewRenderer adds header/footer views
                var adapter = listView.Adapter;
                if (adapter is HeaderViewListAdapter headerAdapter)
                {
                    adapter = headerAdapter.WrappedAdapter;
                }

                if (adapter is Compatibility.CellAdapter cellAdapter)
                {
                    cellAdapter.CloseContextActions();
                }
                return;
            }

            if (view is ViewGroup viewGroup)
            {
                for (int i = 0; i < viewGroup.ChildCount; i++)
                {
                    CloseActiveActionModes(viewGroup.GetChildAt(i));
                }
            }
        }

        public override void OnDestroyView()
        {

            if (_shellContent is null)
            {
                base.OnDestroyView();
                return;
            }

            // Disconnect StackNavigationManager IMMEDIATELY when the fragment view is destroyed.
            // This removes the ViewAttachedToWindow listener before ViewPager2 can recycle/re-attach
            // the view, preventing IllegalStateException from accessing Fragment on a destroyed view.
            _navigationContainer?.ViewAttachedToWindow -= OnNavigationContainerAttached;

            _stackNavigationManager?.Disconnect();

            // Clear cached state so the next OnCreateView goes through the full creation path
            // instead of the reuse path. This ensures ConnectAndInitialize runs properly with
            // a fresh NavHostFragment and StackNavigationManager connection.
            // Without this, the reuse path returns the old container with a disconnected manager,
            // leaving HasNavHost == false and navigation requests queued forever.
            _navigationContainer = null;
            _stackNavigationManager = null;
            _navigationViewAdapter = null;
            CancelPendingNavigation();

            // Disconnect the root page and all its child handlers when the ShellContent has been
            // removed from the Shell hierarchy (e.g. Shell.Items.Clear()).
            // The guard prevents premature disconnection during normal back-navigation where the
            // page is still cached and will be reused — without it, handlers like WebView would
            // be incorrectly released mid-navigation.
            if (_shellContent?.FindParentOfType<Shell>() is null)
            {
                _rootPage?.DisconnectHandlers();
            }

            if (_subscribedToNavigationRequested)
            {
                if (_subscribedShellSection is not null)
                {
                    ((IShellSectionController)_subscribedShellSection).NavigationRequested -= OnNavigationRequested;
                }
                _subscribedToNavigationRequested = false;
            }
            _subscribedShellSection = null;

            base.OnDestroyView();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _shellContent is null)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                _navigationContainer?.ViewAttachedToWindow -= OnNavigationContainerAttached;

                if (_subscribedToNavigationRequested)
                {
                    if (_subscribedShellSection is not null)
                    {
                        ((IShellSectionController)_subscribedShellSection).NavigationRequested -= OnNavigationRequested;
                    }
                    _subscribedToNavigationRequested = false;
                }

                CancelPendingNavigation();
                _navigationViewAdapter = null;
                _stackNavigationManager?.Disconnect();
                _stackNavigationManager = null;
                _navigationContainer = null;
                _subscribedShellSection = null;
                _rootPage = null;
            }
            base.Dispose(disposing);
        }

        void CancelPendingNavigation()
        {
            while (_pendingNavigationRequests.Count > 0)
                _pendingNavigationRequests.Dequeue().Completion.TrySetCanceled();
        }

        static int GetMaterial3Background(Context context)
        {
            // Material3 colorSurface automatically adapts to light/dark theme.
            // The theme resolution happens in GetThemeAttrColor based on the active theme.
            return ContextExtensions.GetThemeAttrColor(context, Resource.Attribute.colorSurface);
        }

        static int GetResourceBackground(Context context, bool isDark)
        {
            return isDark
                ? AndroidX.Core.Content.ContextCompat.GetColor(context, global::Android.Resource.Color.BackgroundDark)
                : AndroidX.Core.Content.ContextCompat.GetColor(context, global::Android.Resource.Color.BackgroundLight);
        }
    }

    /// <summary>
    /// Adapter that wraps a Page (which is an IView) and adds IStackNavigation behavior
    /// by delegating NavigationFinished to the owning fragment.
    /// This is a cleaner approach than having the Fragment implement IStackNavigationView
    /// with 40+ stub properties.
    /// </summary>
    internal class ShellContentStackNavigationView : IStackNavigationView
    {
        readonly Page _page;
        readonly ShellContentNavigationFragment _fragment;

        public ShellContentStackNavigationView(Page page, ShellContentNavigationFragment fragment)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        }

        // IStackNavigation - delegate to fragment
        void IStackNavigation.RequestNavigation(NavigationRequest request)
        {
            // Navigation requests go through the fragment
            ((IStackNavigation)_fragment).RequestNavigation(request);
        }

        // IStackNavigationView - delegate to fragment for the callback
        public void NavigationFinished(IReadOnlyList<IView> newStack)
        {
            _fragment.OnNavigationFinished(newStack);
        }

        // IView - delegate all properties to the underlying Page
        public Size Arrange(Rect bounds) => ((IView)_page).Arrange(bounds);
        public Size Measure(double widthConstraint, double heightConstraint) => ((IView)_page).Measure(widthConstraint, heightConstraint);
        public void InvalidateMeasure() => ((IView)_page).InvalidateMeasure();
        public void InvalidateArrange() => ((IView)_page).InvalidateArrange();
        public bool Focus() => ((IView)_page).Focus();
        public void Unfocus() => ((IView)_page).Unfocus();

        public string AutomationId => ((IView)_page).AutomationId;
        public FlowDirection FlowDirection => ((IView)_page).FlowDirection;
        public Primitives.LayoutAlignment HorizontalLayoutAlignment => ((IView)_page).HorizontalLayoutAlignment;
        public Primitives.LayoutAlignment VerticalLayoutAlignment => ((IView)_page).VerticalLayoutAlignment;
        public Semantics? Semantics => ((IView)_page).Semantics;
        public IShape? Clip => ((IView)_page).Clip;
        public IShadow? Shadow => ((IView)_page).Shadow;
        public bool IsEnabled => ((IView)_page).IsEnabled;
        public bool IsFocused { get => ((IView)_page).IsFocused; set => ((IView)_page).IsFocused = value; }
        public Visibility Visibility => ((IView)_page).Visibility;
        public double Opacity => ((IView)_page).Opacity;
        public Paint? Background => ((IView)_page).Background;
        public Rect Frame { get => ((IView)_page).Frame; set => ((IView)_page).Frame = value; }
        public double Width => ((IView)_page).Width;
        public double MinimumWidth => ((IView)_page).MinimumWidth;
        public double MaximumWidth => ((IView)_page).MaximumWidth;
        public double Height => ((IView)_page).Height;
        public double MinimumHeight => ((IView)_page).MinimumHeight;
        public double MaximumHeight => ((IView)_page).MaximumHeight;
        public Thickness Margin => ((IView)_page).Margin;
        public Size DesiredSize => ((IView)_page).DesiredSize;
        public int ZIndex => ((IView)_page).ZIndex;
        public IViewHandler? Handler { get => _page.Handler; set => _page.Handler = value; }
        public bool InputTransparent => ((IView)_page).InputTransparent;
        IElementHandler? IElement.Handler { get => _page.Handler; set => _page.Handler = value as IViewHandler; }
        public IElement? Parent => ((IElement)_page).Parent;
        public double TranslationX => ((IView)_page).TranslationX;
        public double TranslationY => ((IView)_page).TranslationY;
        public double Scale => ((IView)_page).Scale;
        public double ScaleX => ((IView)_page).ScaleX;
        public double ScaleY => ((IView)_page).ScaleY;
        public double Rotation => ((IView)_page).Rotation;
        public double RotationX => ((IView)_page).RotationX;
        public double RotationY => ((IView)_page).RotationY;
        public double AnchorX => ((IView)_page).AnchorX;
        public double AnchorY => ((IView)_page).AnchorY;
    }
}
