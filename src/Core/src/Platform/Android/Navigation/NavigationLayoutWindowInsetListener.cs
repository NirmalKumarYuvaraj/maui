using Android.Views;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using Google.Android.Material.AppBar;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform;

internal enum NavigationLayoutRegion
{
	AppBar,
	Content,
	BottomTabs,
}

internal readonly record struct NavigationLayoutInsetOwners(
	NavigationLayoutRegion Top,
	NavigationLayoutRegion Bottom);

internal sealed class NavigationLayoutWindowInsetListener : MauiWindowInsetListener
{
	readonly AView _navigationLayout;
	readonly AppBarLayout? _appBar;
	readonly AView? _content;
	readonly ViewGroup? _bottomTabs;
	readonly WindowInsetsManager _windowInsetsManager = new();
	readonly NavigationContentWindowInsetListener? _contentWindowInsetListener;

	internal NavigationLayoutWindowInsetListener(AView navigationLayout)
	{
		_navigationLayout = navigationLayout;
		_appBar = FindAppBar(navigationLayout);
		_content = navigationLayout.FindViewById(Resource.Id.navigationlayout_content);
		_bottomTabs = navigationLayout.FindViewById<ViewGroup>(Resource.Id.navigationlayout_bottomtabs);

		if (_content is not null)
		{
			_contentWindowInsetListener = new NavigationContentWindowInsetListener(_bottomTabs);
			ViewCompat.SetOnApplyWindowInsetsListener(_content, _contentWindowInsetListener);
		}
	}

	protected override WindowInsetsCompat? ApplyDefaultWindowInsets(AView v, WindowInsetsCompat insets)
	{
		if (!ReferenceEquals(v, _navigationLayout))
		{
			return base.ApplyDefaultWindowInsets(v, insets);
		}

		_windowInsetsManager.Update(insets);
		return Apply(_windowInsetsManager, _appBar, _content, _bottomTabs);
	}

	internal static NavigationLayoutInsetOwners ResolveOwners(bool appBarHasContent, bool bottomTabsHaveContent)
	{
		return new NavigationLayoutInsetOwners(
			appBarHasContent ? NavigationLayoutRegion.AppBar : NavigationLayoutRegion.Content,
			bottomTabsHaveContent ? NavigationLayoutRegion.BottomTabs : NavigationLayoutRegion.Content);
	}

	internal static WindowInsetEdges GetContentConsumedEdges(bool bottomTabsHaveContent) =>
		bottomTabsHaveContent ? WindowInsetEdges.Bottom : WindowInsetEdges.None;

	static WindowInsetsCompat Apply(
		WindowInsetsManager windowInsetsManager,
		AppBarLayout? appBar,
		AView? content,
		ViewGroup? bottomTabs)
	{
		var owners = ResolveOwners(HasVisibleContent(appBar), HasVisibleContent(bottomTabs));
		var appBarOwnsTop = owners.Top == NavigationLayoutRegion.AppBar;

		if (appBar is not null)
		{
			if (appBarOwnsTop)
			{
				appBar.SetPadding(
					windowInsetsManager.SystemBars?.Left ?? 0,
					windowInsetsManager.GetSafeAreaInset(WindowInsetEdges.Top),
					windowInsetsManager.SystemBars?.Right ?? 0,
					0);
			}
			else
			{
				appBar.SetPadding(0, 0, 0, 0);
			}
		}

		if (content is not null)
		{
			if (owners.Bottom == NavigationLayoutRegion.BottomTabs)
			{
				// BottomNavigationView receives the remaining bottom inset and extends its
				// background edge-to-edge. Keep content above that region and system bar.
				content.SetPadding(0, 0, 0, windowInsetsManager.GetSafeAreaInset(WindowInsetEdges.Bottom));
			}
			else
			{
				content.SetPadding(0, 0, 0, 0);
			}
		}

		// Bottom remains available for the BottomNavigationView's Material inset handling.
		// Other descendants receive the same remaining inset snapshot for now; targeted
		// per-region dispatch is required before bottom can be consumed at this boundary.
		return windowInsetsManager.BuildRemaining(
			appBarOwnsTop ? WindowInsetEdges.Top : WindowInsetEdges.None);
	}

	static AppBarLayout? FindAppBar(AView view)
	{
		var appBar = view.FindViewById<AppBarLayout>(Resource.Id.navigationlayout_appbar);
		if (appBar is not null || view is not ViewGroup group)
		{
			return appBar;
		}

		if (group.ChildCount > 0 && group.GetChildAt(0) is AppBarLayout firstChild)
		{
			return firstChild;
		}

		if (group.ChildCount > 1 && group.GetChildAt(1) is AppBarLayout secondChild)
		{
			return secondChild;
		}

		return null;
	}

	internal static bool HasVisibleContent(ViewGroup? region)
	{
		if (region is null || region.Visibility != ViewStates.Visible)
		{
			return false;
		}

		for (int i = 0; i < region.ChildCount; i++)
		{
			if (region.GetChildAt(i) is not AView { Visibility: ViewStates.Visible } child)
			{
				continue;
			}

			if (child is FragmentContainerView fragmentContainer)
			{
				if (HasVisibleContent(fragmentContainer))
				{
					return true;
				}
			}
			else
			{
				return true;
			}
		}

		return false;
	}

	sealed class NavigationContentWindowInsetListener : Java.Lang.Object, IOnApplyWindowInsetsListener
	{
		readonly ViewGroup? _bottomTabs;
		readonly WindowInsetsManager _windowInsetsManager = new();

		internal NavigationContentWindowInsetListener(ViewGroup? bottomTabs)
		{
			_bottomTabs = bottomTabs;
		}

		public WindowInsetsCompat? OnApplyWindowInsets(AView? view, WindowInsetsCompat? insets)
		{
			var bottomTabsHaveContent = HasVisibleContent(_bottomTabs);
			if (insets is null || !bottomTabsHaveContent)
			{
				return insets;
			}

			_windowInsetsManager.Update(insets);
			return _windowInsetsManager.BuildRemaining(
				GetContentConsumedEdges(bottomTabsHaveContent));
		}
	}
}
