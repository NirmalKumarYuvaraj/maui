#nullable enable
using System;
using Android.OS;
using Android.Views;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Fragment.App;
using AndroidX.ViewPager2.Widget;
using Microsoft.Maui.Controls.Platform;
using AView = Android.Views.View;

namespace Microsoft.Maui.Controls.Handlers
{
    /// <summary>
    /// Wrapper Fragment that hosts the ShellItemHandler's layout.
    /// Inflates shellitemlayout.axml consistent with NavigationViewHandler and FlyoutViewHandler patterns.
    /// The toolbar is managed at the ShellItem level (shared across all sections).
    /// </summary>
    public class ShellItemWrapperFragment : Fragment
    {
        readonly ShellItemHandler? _handler;
        CoordinatorLayout? _rootLayout;

        // Default constructor required by Android's FragmentManager for fragment restoration.
        // Without this, FragmentManager.instantiate() crashes on process-death restoration.
        public ShellItemWrapperFragment()
        {
            _handler = null;
        }

        public ShellItemWrapperFragment(ShellItemHandler handler)
        {
            _handler = handler;
            // Let the handler know about its parent fragment for child fragment management
            _handler.SetParentFragment(this);
        }

        public override AView OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            // If restored without proper handler reference, return empty view.
            // The Shell infrastructure will recreate proper fragments after reconnecting.
            if (_handler is null)
            {
                return new global::Android.Widget.FrameLayout(inflater.Context!);
            }

            // Inflate from XML layout — consistent with NavigationViewHandler/FlyoutViewHandler pattern
            var rootView = inflater.Inflate(Resource.Layout.shellitemlayout, container, false)
                ?? throw new InvalidOperationException("shellitemlayout inflation failed");

            // Get references from inflated layout
            _rootLayout = rootView.FindViewById<CoordinatorLayout>(Resource.Id.shellitem_coordinator)
                ?? throw new InvalidOperationException("shellitem_coordinator not found");
            // NOTE: _appBarLayout is the outer navigationlayout_appbar from
            // navigationlayout.axml, resolved lazily in SetupToolbar().

            // Get ViewPager2 from the inflated layout
            _handler._viewPager = rootView.FindViewById<ViewPager2>(Resource.Id.shellitem_viewpager);

            // BNV is created by TabbedViewManager in SetupTabbedViewManager().
            // It is placed into navigationlayout_bottomtabs via TabbedViewManager.SetTabLayout().

            return rootView;
        }

        public override void OnViewCreated(AView view, Bundle? savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            // Skip setup if restored without handler (parameterless constructor path)
            if (_handler is null)
            {
                return;
            }

            // Setup the shared toolbar
            _handler.SetupToolbar();

            // Setup TabbedViewManager for bottom tab management
            // (creates BNV and populates tabs)
            _handler.SetupTabbedViewManager();

            // Place bottom tabs into navigationlayout_bottomtabs via TabbedViewManager
            _handler._tabbedViewManager?.SetTabLayout();

            // Apply initial badges to bottom navigation tabs
            _handler.UpdateAllBadges();

            // Now that the fragment is attached, setup the ViewPager2 adapter
            _handler.SetupViewPagerAdapter();

            // Register as appearance observer NOW that all views are ready.
            // This must happen after SetupToolbar and SetupTabbedViewManager so that
            // when Shell calls OnAppearanceChanged, the views can receive appearance updates.
            _handler.RegisterAppearanceObserver();

            // Trigger the initial section switch if needed
            if (_handler.VirtualView?.CurrentItem is not null)
            {
                _handler.SwitchToSection(_handler.VirtualView.CurrentItem, animate: false);
            }
        }

        public override void OnDestroyView()
        {
            // Null out view references so we don't hold on to detached views
            // when the fragment is placed on the back stack (view destroyed, fragment alive).
            // Also null _adapter so SetupViewPagerAdapter re-creates it and assigns it
            // to the new ViewPager2 on the next OnViewCreated (adapter reset path).
            if (_handler is not null)
            {
                // Unregister appearance observer before view is gone.
                // RegisterAppearanceObserver() runs again on OnViewCreated via bare List.Add —
                // without removal here, each back-stack return stacks a duplicate registration.
                if (_handler._registeredShell is not null)
                {
                    ((IShellController)_handler._registeredShell).RemoveAppearanceObserver(_handler);
                    _handler._registeredShell = null;
                }

                // Unregister displayed-page observer before view is gone.
                // SwitchToSection() only removes the observer when _shellSection != newSection —
                // on same-section view recreation, no removal happens, causing AddDisplayedPageObserver
                // to stack a duplicate entry on each back-stack return.
                if (_handler._shellSection is not null)
                {
                    ((IShellSectionController)_handler._shellSection).RemoveDisplayedPageObserver(_handler);
                    _handler._shellSection = null;
                }

                // Reset _displayedPage so UpdateDisplayedPage re-runs fully on back-stack return
                // instead of early-returning due to the same page reference, leaving the toolbar unconfigured.
                _handler._displayedPage = null;

                // Null _bottomNavigationView so appearance callbacks don't update stale views
                // while the fragment sits on the back stack.
                _handler._bottomNavigationView = null;

                // Tear down TabbedViewManager before view recreation.
                // SetupTabbedViewManager overwrites _tabbedViewManager/_shellItemAdapter silently —
                // without teardown here, old BNV listeners and event subscriptions leak.
                // Note: SetElement(null) already calls RemoveTabs() internally — no need to call it explicitly.
                if (_handler._tabbedViewManager is not null)
                {
                    _handler._tabbedViewManager.SetElement(null);
                    _handler._tabbedViewManager = null;
                }
                _handler._shellItemAdapter = null;

                // Tear down toolbar to prevent duplicate toolbars and tracker leaks on view recreation.
                // SetupToolbar() runs again on OnViewCreated, so old trackers must be disposed
                // and old toolbar removed from _appBarLayout before that happens.
                _handler._toolbarAppearanceTracker?.Dispose();
                _handler._toolbarAppearanceTracker = null;

                _handler._toolbarTracker?.Dispose();
                _handler._toolbarTracker = null;

                if (_handler._toolbar?.Parent is ViewGroup toolbarParent)
                {
                    toolbarParent.RemoveView(_handler._toolbar);
                }

                _handler._toolbar = null;

                // Null _shellToolbar — mirrors DisconnectHandler's cleanup.
                // SetupToolbar() creates a new one on OnViewCreated.
                _handler._shellToolbar = null;

                // Null _appBarLayout — set in SetupToolbar() from OnViewCreated.
                // Leaving it non-null holds a detached view reference after view destruction.
                _handler._appBarLayout = null;

                // Unregister page-change callback before nulling _viewPager.
                // SetupViewPagerAdapter guards on `if (_pageChangeCallback is null)` —
                // if not nulled here, the new ViewPager2 never receives the callback
                // and toolbar/top-tab sync silently breaks on back-stack return.
                if (_handler._pageChangeCallback is not null && _handler._viewPager is not null)
                {
                    _handler._viewPager.UnregisterOnPageChangeCallback(_handler._pageChangeCallback);
                }
                _handler._pageChangeCallback = null;

                // Clear adapter before nulling _viewPager to detach FragmentStateAdapter.
                // FragmentStateAdapter unregisters lifecycle/adapter observers only when cleared,
                // not when the view is destroyed — mirrors DisconnectHandler's adapter/viewpager cleanup.
                if (_handler._viewPager is not null)
                {
                    _handler._viewPager.Adapter = null;
                }
                _handler._viewPager = null;
                _handler._adapter = null;
            }

            _rootLayout = null;

            base.OnDestroyView();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rootLayout = null;
            }
            base.Dispose(disposing);
        }

    }
}
