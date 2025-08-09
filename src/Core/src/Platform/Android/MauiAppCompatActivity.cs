using System;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content.Resources;
using AndroidX.Core.View;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui
{
	public partial class MauiAppCompatActivity : AppCompatActivity
	{
		// Override this if you want to handle the default Android behavior of restoring fragments on an application restart
		protected virtual bool AllowFragmentRestore => false;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			// Enable edge-to-edge before calling the base methods
			WindowCompat.SetDecorFitsSystemWindows(Window, false);

			Microsoft.Maui.PlatformMauiAppCompatActivity.OnCreate(
				this,
				savedInstanceState,
				AllowFragmentRestore,
				Resource.Attribute.maui_splash,
				Resource.Style.Maui_MainTheme_NoActionBar);

			base.OnCreate(savedInstanceState);

			if (IPlatformApplication.Current?.Application is not null)
			{
				this.CreatePlatformWindow(IPlatformApplication.Current.Application, savedInstanceState);
			}

			// Apply window insets for edge-to-edge
			ApplyWindowInsets();
		}

		public override bool DispatchTouchEvent(MotionEvent? e)
		{
			// For current purposes this needs to get called before we propagate
			// this message out. In Controls this dispatch call will unfocus the 
			// current focused element which is important for timing if we should
			// hide/show the softkeyboard.
			// If you move this to after the xplat call then the keyboard will show up
			// then close
			bool handled = base.DispatchTouchEvent(e);

			bool implHandled =
				(this.GetWindow() as IPlatformEventsListener)?.DispatchTouchEvent(e) == true;

			return handled || implHandled;
		}

		void ApplyWindowInsets()
		{
			var decorView = Window?.DecorView;
			if (decorView is not null)
			{
				ViewCompat.SetOnApplyWindowInsetsListener(decorView, new EdgeToEdgeWindowInsetsListener());
			}
		}
	}

	internal class EdgeToEdgeWindowInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
	{
		public WindowInsetsCompat OnApplyWindowInsets(View? v, WindowInsetsCompat? insets)
		{
			if (insets is null)
				return WindowInsetsCompat.ToWindowInsetsCompat(null)!;

			// Find the AppBar layout for applying status bar insets
			var appBarLayout = v?.FindViewById(Resource.Id.navigationlayout_appbar);

			// Get different types of insets
			var statusBarInsets = insets.GetInsets(WindowInsetsCompat.Type.StatusBars());
			var navigationBarInsets = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars());
			var displayCutoutInsets = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());

			// Apply top insets to AppBarLayout (for status bar and display cutout)
			if (appBarLayout is not null)
			{
				var topInset = Math.Max(statusBarInsets?.Top ?? 0, displayCutoutInsets?.Top ?? 0);
				appBarLayout.SetPadding(0, topInset, 0, 0);
			}

			// Find the bottom navigation area and apply bottom insets
			var bottomNavigation = v?.FindViewById(Resource.Id.navigationlayout_bottomtabs);
			if (bottomNavigation is not null)
			{
				var bottomInset = Math.Max(navigationBarInsets?.Bottom ?? 0, displayCutoutInsets?.Bottom ?? 0);
				bottomNavigation.SetPadding(0, 0, 0, bottomInset);
			}

			// Find the main content area and apply side insets for display cutouts
			var contentLayout = v?.FindViewById(Resource.Id.navigationlayout_content);
			if (contentLayout is not null)
			{
				var leftInset = Math.Max(displayCutoutInsets?.Left ?? 0, 0);
				var rightInset = Math.Max(displayCutoutInsets?.Right ?? 0, 0);
				contentLayout.SetPadding(leftInset, 0, rightInset, 0);
			}

			// Don't consume the insets, let child views handle them if needed
			return insets;
		}
	}
}