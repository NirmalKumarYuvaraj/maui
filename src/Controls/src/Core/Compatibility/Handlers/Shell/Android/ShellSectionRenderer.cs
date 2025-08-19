#nullable disable
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using AndroidX.ViewPager.Widget;
using AndroidX.ViewPager2.Widget;
using Google.Android.Material.AppBar;
using Google.Android.Material.Tabs;
using AToolbar = AndroidX.AppCompat.Widget.Toolbar;
using AView = Android.Views.View;

namespace Microsoft.Maui.Controls.Platform.Compatibility
{
	public class ShellSectionRenderer : Fragment, IShellSectionRenderer//, ViewPager.IOnPageChangeListener
		, AView.IOnClickListener, IShellObservableFragment, IAppearanceObserver, TabLayoutMediator.ITabConfigurationStrategy
	{
		#region ITabConfigurationStrategy

		void TabLayoutMediator.ITabConfigurationStrategy.OnConfigureTab(TabLayout.Tab tab, int position)
		{
			tab.SetText(new String(SectionController.GetItems()[position].Title));
		}

		void UpdateCurrentItem(ShellContent content)
		{
			if (_toolbarTracker == null)
				return;

			var page = ((IShellContentController)content).GetOrCreateContent();
			if (page == null)
				throw new ArgumentNullException(nameof(page), "Shell Content Page is Null");

			ShellSection.SetValueFromRenderer(ShellSection.CurrentItemProperty, content);
			_toolbarTracker.Page = page;
		}

		#endregion IOnPageChangeListener

		#region IAppearanceObserver

		void IAppearanceObserver.OnAppearanceChanged(ShellAppearance appearance)
		{
			if (appearance == null)
				ResetAppearance();
			else
				SetAppearance(appearance);
		}

		#endregion IAppearanceObserver

		#region IOnClickListener

		void AView.IOnClickListener.OnClick(AView v)
		{
		}

		#endregion IOnClickListener

		readonly IShellContext _shellContext;
		AView _rootView;
		bool _selecting;
		TabLayout _tablayout;
		IShellTabLayoutAppearanceTracker _tabLayoutAppearanceTracker;
		AToolbar _toolbar;
		IShellToolbarAppearanceTracker _toolbarAppearanceTracker;
		IShellToolbarTracker _toolbarTracker;
		ViewPager2 _viewPager;
		bool _disposed;
		IShellController ShellController => _shellContext.Shell;
		public event EventHandler AnimationFinished;
		Fragment IShellObservableFragment.Fragment => this;
		public ShellSection ShellSection { get; set; }
		protected IShellContext ShellContext => _shellContext;
		IShellSectionController SectionController => (IShellSectionController)ShellSection;
		IMauiContext MauiContext => ShellContext.Shell.Handler.MauiContext;

		public ShellSectionRenderer(IShellContext shellContext)
		{
			_shellContext = shellContext;
		}


		public override AView OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var shellSection = ShellSection;
			if (shellSection == null)
				return null;

			if (shellSection.CurrentItem == null)
				throw new InvalidOperationException($"Content not found for active {shellSection}. Title: {shellSection.Title}. Route: {shellSection.Route}.");

			var context = Context;
			var root = PlatformInterop.CreateShellCoordinatorLayout(context);
			var appbar = PlatformInterop.CreateShellAppBar(context, Resource.Attribute.appBarLayoutStyle, root);

			// Configure edge-to-edge support and status bar color coordination
			var isEdgeToEdgeEnabled = OperatingSystem.IsAndroidVersionAtLeast(36);

			if (isEdgeToEdgeEnabled)
			{
				// In edge-to-edge mode, make status bar color match app bar color
				SetStatusBarColorToMatchAppBar(appbar);

				// Instead of removing the background, set it to the same color as status bar
				var appBarColor = GetAppBarBackgroundColor();
				if (appBarColor.HasValue)
				{
					var color = appBarColor.Value;
					var colorInt = (int)((uint)(color.A << 24) | (uint)(color.R << 16) | (uint)(color.G << 8) | (uint)color.B);
					var colorDrawable = new global::Android.Graphics.Drawables.ColorDrawable(new Color(colorInt));
					appbar.SetBackground(colorDrawable);
				}
				else
				{
					appbar.SetBackground(null);
				}
			}

			ViewCompat.SetOnApplyWindowInsetsListener(appbar, new WindowsListener(this));

			int actionBarHeight = context.GetActionBarHeight();

			var shellToolbar = new Toolbar(shellSection);
			ShellToolbarTracker.ApplyToolbarChanges(_shellContext.Shell.Toolbar, shellToolbar);
			_toolbar = (AToolbar)shellToolbar.ToPlatform(_shellContext.Shell.FindMauiContext());
			appbar.AddView(_toolbar);
			_tablayout = PlatformInterop.CreateShellTabLayout(context, appbar, actionBarHeight);

			var pagerContext = MauiContext.MakeScoped(layoutInflater: inflater, fragmentManager: ChildFragmentManager);
			var adapter = new ShellFragmentStateAdapter(shellSection, ChildFragmentManager, pagerContext);
			var pageChangedCallback = new ViewPagerPageChanged(this);
			_viewPager = PlatformInterop.CreateShellViewPager(context, root, _tablayout, this, adapter, pageChangedCallback);

			Page currentPage = null;
			int currentIndex = -1;
			var currentItem = shellSection.CurrentItem;
			var items = SectionController.GetItems();

			while (currentIndex < 0 && items.Count > 0 && shellSection.CurrentItem != null)
			{
				currentItem = shellSection.CurrentItem;
				currentPage = ((IShellContentController)shellSection.CurrentItem).GetOrCreateContent();

				// current item hasn't changed
				if (currentItem == shellSection.CurrentItem)
					currentIndex = items.IndexOf(currentItem);
			}

			_toolbarTracker = _shellContext.CreateTrackerForToolbar(_toolbar);
			_toolbarTracker.SetToolbar(shellToolbar);
			_toolbarTracker.Page = currentPage;

			_viewPager.CurrentItem = currentIndex;

			if (items.Count == 1)
			{
				UpdateTablayoutVisibility();
			}

			_tablayout.LayoutChange += OnTabLayoutChange;

			_tabLayoutAppearanceTracker = _shellContext.CreateTabLayoutAppearanceTracker(ShellSection);
			_toolbarAppearanceTracker = _shellContext.CreateToolbarAppearanceTracker();

			HookEvents();

			return _rootView = root;
		}

		// Temporary workaround:
		// Android 15 / API 36 removed the prior opt‑out path for edge‑to‑edge
		// (legacy "edge to edge ignore" + decor fitting). This placeholder exists
		// so we can keep apps from regressing (content accidentally covered by
		// system bars) until a proper, unified edge‑to‑edge + system bar inset
		// configuration API is implemented in MAUI.
		//
		// NOTE:
		// - Keep this minimal.
		// - Will be replaced by the planned comprehensive window insets solution.
		// - Do not extend; add new logic to the forthcoming implementation instead.
		internal class WindowsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
		{
			readonly ShellSectionRenderer _renderer;

			public WindowsListener()
			{
			}

			public WindowsListener(ShellSectionRenderer renderer)
			{
				_renderer = renderer;
			}

			public WindowInsetsCompat OnApplyWindowInsets(AView v, WindowInsetsCompat insets)
			{
				if (insets == null || v == null)
					return insets;

				// Get system bar and display cutout insets
				var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
				var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

				var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);

				v.SetPadding(0, topInset, 0, 0);

				// Apply status bar color after insets are processed - this might be the correct timing
				var isEdgeToEdgeEnabled = OperatingSystem.IsAndroidVersionAtLeast(36);
				if (_renderer != null && isEdgeToEdgeEnabled)
				{
					_renderer.ApplyStatusBarColorAfterInsets();
				}

				return WindowInsetsCompat.Consumed;
			}
		}

		void ApplyStatusBarColorAfterInsets()
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(36))
			{
				return;
			}

			try
			{
				var activity = Context as AndroidX.AppCompat.App.AppCompatActivity;
				if (activity?.Window == null)
				{
					return;
				}

				var window = activity.Window;

				// Get the app bar color again
				var appBarColor = GetAppBarBackgroundColor();
				if (appBarColor.HasValue)
				{
					var color = appBarColor.Value;
					var colorInt = (int)((uint)(color.A << 24) | (uint)(color.R << 16) | (uint)(color.G << 8) | (uint)color.B);

					// Force the status bar color application - this is our most aggressive attempt
					var androidColor = new Color(colorInt);
					window.SetStatusBarColor(androidColor);

					// Also set the navigation bar color to match for consistency
					window.SetNavigationBarColor(androidColor);

					// Apply multiple times with delays to override system interference
					window.DecorView.Post(() =>
					{
						window.SetStatusBarColor(androidColor);
						window.SetNavigationBarColor(androidColor);
					});

					// Also try applying after a longer delay
					window.DecorView.PostDelayed(() =>
					{
						window.SetStatusBarColor(androidColor);
					}, 100); // 100ms delay

					// Ensure the correct appearance
					var windowInsetsController = WindowCompat.GetInsetsController(window, window.DecorView);
					if (windowInsetsController != null)
					{
						var luminance = GetColorLuminance(appBarColor.Value);
						var shouldUseLightStatusBar = luminance > 0.5;
						windowInsetsController.AppearanceLightStatusBars = shouldUseLightStatusBar;
						windowInsetsController.AppearanceLightNavigationBars = shouldUseLightStatusBar;
					}
				}
			}
			catch
			{
				// Silent failure - status bar coordination is not critical
			}
		}

		void SetStatusBarColorToMatchAppBar(AppBarLayout appbar)
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(36))
			{
				return;
			}

			try
			{
				var activity = Context as AndroidX.AppCompat.App.AppCompatActivity;

				if (activity?.Window == null)
				{
					return;
				}

				// Ensure the window is set up for status bar color changes
				var window = activity.Window;

				// Force clear any conflicting flags that might prevent status bar color changes
				window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
				// Ensure FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS is set for status bar color to work
				window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

				// Also clear system UI visibility flags that might interfere
				window.DecorView.SystemUiFlags = SystemUiFlags.Visible;

				// Get the app bar's background color
				var appBarColor = GetAppBarBackgroundColor();

				if (appBarColor.HasValue)
				{
					// Extract the color value as int for SetStatusBarColor
					var color = appBarColor.Value;
					var colorInt = (int)((uint)(color.A << 24) | (uint)(color.R << 16) | (uint)(color.G << 8) | (uint)color.B);

					// Force the status bar color application multiple times to override system defaults
					var androidColor = new Color(colorInt);
					activity.Window.SetStatusBarColor(androidColor);

					// Apply again after a short delay to override any system interference
					activity.Window.DecorView.Post(() =>
					{
						activity.Window.SetStatusBarColor(androidColor);
					});

					// Configure status bar content style based on the app bar color
					var windowInsetsController = WindowCompat.GetInsetsController(activity.Window, activity.Window.DecorView);
					if (windowInsetsController != null)
					{
						var luminance = GetColorLuminance(appBarColor.Value);

						var shouldUseLightStatusBar = luminance > 0.5;
						windowInsetsController.AppearanceLightStatusBars = shouldUseLightStatusBar;
					}
				}
			}
			catch
			{
				// Log but don't crash - status bar coordination is not critical
			}
		}

		Color? GetAppBarBackgroundColor()
		{
			try
			{
				// First try to get color from Shell's background color
				var shell = ShellContext?.Shell;

				if (shell?.BackgroundColor != null)
				{
					var shellPlatformColor = shell.BackgroundColor.ToPlatform();
					return shellPlatformColor;
				}

				// Fallback: Get the colorPrimary from the current theme (what the app bar uses by default)
				var context = Context;

				if (context == null)
				{
					return null;
				}

				var typedValue = new TypedValue();
				var resolved = context.Theme.ResolveAttribute(Resource.Attribute.colorPrimary, typedValue, true);

				if (resolved)
				{
					var themeColor = new Color(typedValue.Data);
					return themeColor;
				}
			}
			catch
			{
				// Silent failure
			}

			return null;
		}

		double GetColorLuminance(Color color)
		{
			// Calculate relative luminance using the standard formula
			double r = color.R / 255.0;
			double g = color.G / 255.0;
			double b = color.B / 255.0;

			// Apply gamma correction
			r = (r <= 0.03928) ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
			g = (g <= 0.03928) ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
			b = (b <= 0.03928) ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

			// Calculate luminance
			return 0.2126 * r + 0.7152 * g + 0.0722 * b;
		}

		void OnShellContentPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == ShellContent.TitleProperty.PropertyName && sender is ShellContent shellContent)
			{
				UpdateTabTitle(shellContent);
			}
		}

		void UpdateTabTitle(ShellContent shellContent)
		{
			if (_tablayout == null || SectionController.GetItems().Count == 0)
				return;

			int index = SectionController.GetItems().IndexOf(shellContent);
			if (index >= 0)
			{
				var tab = _tablayout.GetTabAt(index);
				tab?.SetText(new string(shellContent.Title));
			}
		}

		void OnTabLayoutChange(object sender, AView.LayoutChangeEventArgs e)
		{
			if (_disposed)
				return;

			var items = SectionController.GetItems();
			for (int i = 0; i < _tablayout.TabCount; i++)
			{
				if (items.Count <= i)
					break;

				var tab = _tablayout.GetTabAt(i);

				if (tab.View != null)
					AutomationPropertiesProvider.AccessibilitySettingsChanged(tab.View, items[i]);
			}
		}

		void Destroy()
		{
			if (_rootView != null)
			{
				UnhookEvents();

				_shellContext?.Shell?.Toolbar?.Handler?.DisconnectHandler();

				//_viewPager.RemoveOnPageChangeListener(this);
				var adapter = _viewPager.Adapter;
				_viewPager.Adapter = null;
				adapter.Dispose();

				_tablayout.LayoutChange -= OnTabLayoutChange;
				_toolbarAppearanceTracker.Dispose();
				_tabLayoutAppearanceTracker.Dispose();
				_toolbarTracker.Dispose();
				_tablayout.Dispose();
				_toolbar.Dispose();
				_viewPager.Dispose();
				_rootView.Dispose();
			}

			_toolbarAppearanceTracker = null;
			_tabLayoutAppearanceTracker = null;
			_toolbarTracker = null;
			_tablayout = null;
			_toolbar = null;
			_viewPager = null;
			_rootView = null;

		}

		// Use OnDestroy instead of OnDestroyView because OnDestroyView will be
		// called before the animation completes. This causes tons of tiny issues.
		public override void OnDestroy()
		{
			Destroy();
			base.OnDestroy();
		}
		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;

			if (disposing)
			{
				Destroy();
			}
		}

		protected virtual void OnAnimationFinished(EventArgs e)
		{
			AnimationFinished?.Invoke(this, e);
		}

		protected virtual void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateTablayoutVisibility();

			if (_viewPager?.Adapter is ShellFragmentStateAdapter adapter)
			{
				adapter.OnItemsCollectionChanged(sender, e);
				SafeNotifyDataSetChanged();
			}
		}

		void SafeNotifyDataSetChanged(int iteration = 0)
		{
			if (_disposed)
				return;

			if (!_viewPager.IsAlive())
				return;

			if (iteration >= 10)
			{
				// It's very unlikely this will happen but just in case there's a scenario
				// where we might hit an infinite loop we're adding an exit strategy
				return;
			}

			if (_viewPager?.Adapter is ShellFragmentStateAdapter adapter)
			{
				// https://stackoverflow.com/questions/43221847/cannot-call-this-method-while-recyclerview-is-computing-a-layout-or-scrolling-wh
				// ViewPager2 is based on RecyclerView which really doesn't like NotifyDataSetChanged when a layout is happening
				if (!_viewPager.IsInLayout)
				{
					adapter.NotifyDataSetChanged();
				}
				else
				{
					_viewPager.Post(() => SafeNotifyDataSetChanged(++iteration));
				}
			}
		}

		void UpdateTablayoutVisibility()
		{
			_tablayout.Visibility = (SectionController.GetItems().Count > 1) ? ViewStates.Visible : ViewStates.Gone;
			if (_tablayout.Visibility == ViewStates.Gone)
			{
				SetViewPager2UserInputEnabled(false);
				_viewPager.ImportantForAccessibility = ImportantForAccessibility.No;
			}
			else
			{
				SetViewPager2UserInputEnabled(true);
				_viewPager.ImportantForAccessibility = ImportantForAccessibility.Auto;
			}
		}

		protected virtual void SetViewPager2UserInputEnabled(bool value)
		{
			_viewPager.UserInputEnabled = value;
		}

		protected virtual void OnShellItemPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_rootView == null)
				return;

			if (e.PropertyName == ShellSection.CurrentItemProperty.PropertyName)
			{
				var newIndex = SectionController.GetItems().IndexOf(ShellSection.CurrentItem);

				if (SectionController.GetItems().Count != _viewPager.ChildCount)
				{
					SafeNotifyDataSetChanged();
				}

				if (newIndex >= 0)
				{
					_viewPager.CurrentItem = newIndex;
				}
			}
		}

		protected virtual void ResetAppearance()
		{
			_toolbarAppearanceTracker.ResetAppearance(_toolbar, _toolbarTracker);
			_tabLayoutAppearanceTracker.ResetAppearance(_tablayout);

			// Update status bar color when appearance resets
			if (OperatingSystem.IsAndroidVersionAtLeast(36))
			{
				UpdateStatusBarColorForCurrentAppearance();
			}
		}

		protected virtual void SetAppearance(ShellAppearance appearance)
		{
			_toolbarAppearanceTracker.SetAppearance(_toolbar, _toolbarTracker, appearance);
			_tabLayoutAppearanceTracker.SetAppearance(_tablayout, appearance);

			// Update status bar color when appearance changes
			if (OperatingSystem.IsAndroidVersionAtLeast(36))
			{
				UpdateStatusBarColorForCurrentAppearance();
			}
		}

		void UpdateStatusBarColorForCurrentAppearance()
		{
			try
			{
				var activity = Context as AndroidX.AppCompat.App.AppCompatActivity;
				if (activity?.Window == null)
				{
					return;
				}

				// Ensure the window is set up for status bar color changes
				var window = activity.Window;
				window.ClearFlags(WindowManagerFlags.TranslucentStatus);
				window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

				// Get the current app bar color after appearance changes
				var appBarColor = GetAppBarBackgroundColor();
				if (appBarColor.HasValue)
				{
					// Extract the color value as int for SetStatusBarColor
					var color = appBarColor.Value;
					var colorInt = (int)((uint)(color.A << 24) | (uint)(color.R << 16) | (uint)(color.G << 8) | (uint)color.B);

					// Force the status bar color application
					var androidColor = new Color(colorInt);
					activity.Window.SetStatusBarColor(androidColor);

					// Apply again with delay to override system interference
					activity.Window.DecorView.Post(() =>
					{
						activity.Window.SetStatusBarColor(androidColor);
					});

					// Update status bar content style
					var windowInsetsController = WindowCompat.GetInsetsController(activity.Window, activity.Window.DecorView);
					if (windowInsetsController != null)
					{
						var luminance = GetColorLuminance(appBarColor.Value);

						var shouldUseLightStatusBar = luminance > 0.5;
						windowInsetsController.AppearanceLightStatusBars = shouldUseLightStatusBar;
					}
				}
			}
			catch
			{
				// Silent failure
			}
		}

		void HookEvents()
		{
			SectionController.ItemsCollectionChanged += OnItemsCollectionChanged;
			((IShellController)_shellContext.Shell).AddAppearanceObserver(this, ShellSection);
			ShellSection.PropertyChanged += OnShellItemPropertyChanged;
			foreach (var item in SectionController.GetItems())
			{
				item.PropertyChanged += OnShellContentPropertyChanged;
			}
		}

		void UnhookEvents()
		{
			SectionController.ItemsCollectionChanged -= OnItemsCollectionChanged;
			((IShellController)_shellContext?.Shell)?.RemoveAppearanceObserver(this);
			ShellSection.PropertyChanged -= OnShellItemPropertyChanged;
			foreach (var item in SectionController.GetItems())
			{
				item.PropertyChanged -= OnShellContentPropertyChanged;
			}
		}

		protected virtual void OnPageSelected(int position)
		{
			if (_selecting)
				return;

			var shellSection = ShellSection;
			var visibleItems = SectionController.GetItems();

			// This mainly happens if all of the items that are part of this shell section 
			// vanish. Android calls `OnPageSelected` with position zero even though the view pager is
			// empty
			if (position >= visibleItems.Count)
				return;

			var shellContent = visibleItems[position];

			if (shellContent == shellSection.CurrentItem)
				return;

			var stack = shellSection.Stack.ToList();
			bool result = ShellController.ProposeNavigation(ShellNavigationSource.ShellContentChanged,
				(ShellItem)shellSection.Parent, shellSection, shellContent, stack, true);

			if (result)
			{
				UpdateCurrentItem(shellContent);
			}
			else if (shellSection?.CurrentItem != null)
			{
				var currentPosition = visibleItems.IndexOf(shellSection.CurrentItem);
				_selecting = true;

				// Android doesn't really appreciate you calling SetCurrentItem inside a OnPageSelected callback.
				// It wont crash but the way its programmed doesn't really anticipate re-entrancy around that method
				// and it ends up going to the wrong location. Thus we must invoke.

				_viewPager.Post(() =>
				{
					if (currentPosition < _viewPager.ChildCount && _toolbarTracker != null)
					{
						_viewPager.SetCurrentItem(currentPosition, false);
						UpdateCurrentItem(shellSection.CurrentItem);
					}

					_selecting = false;
				});
			}
		}

		class ViewPagerPageChanged : ViewPager2.OnPageChangeCallback
		{
			private ShellSectionRenderer _shellSectionRenderer;

			public ViewPagerPageChanged(ShellSectionRenderer shellSectionRenderer)
			{
				_shellSectionRenderer = shellSectionRenderer;
			}

			public override void OnPageSelected(int position)
			{
				base.OnPageSelected(position);
				_shellSectionRenderer.OnPageSelected(position);
			}
		}
	}
}