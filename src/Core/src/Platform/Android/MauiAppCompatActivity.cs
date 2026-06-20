using System;
using Android.OS;
using Android.Views;
using Android.Window;
using AndroidX.Activity;
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
			Microsoft.Maui.PlatformMauiAppCompatActivity.OnCreate(
				this,
				savedInstanceState,
				AllowFragmentRestore,
				Resource.Attribute.maui_splash,
				RuntimeFeature.IsMaterial3Enabled
				? Resource.Style.Maui_Material3_Theme_NoActionBar
				: Resource.Style.Maui_MainTheme_NoActionBar);

			base.OnCreate(savedInstanceState);

			// Pass transparent SystemBarStyles into EdgeToEdge so the navigation bar is fully
			// transparent on every API level. The default SystemBarStyle.auto() draws a
			// translucent contrast scrim behind the 3-button navigation bar on API 29+, which
			// previously forced us to clear Window.NavigationBarContrastEnforced manually for a
			// subset of versions. Dark/Light selects the system bar icon color so it stays
			// legible over the app content, and EdgeToEdge.Enable() also clears the status bar
			// contrast enforcement internally.
			// https://developer.android.com/develop/ui/views/layout/edge-to-edge
			var systemBarStyle = GetEdgeToEdgeSystemBarStyle();
			// without the systemBarStyle parameter, EdgeToEdge.Enable() will use SystemBarStyle.auto() which draws a translucent contrast scrim behind the navigation bar on API 29+.
			EdgeToEdge.Enable(this, systemBarStyle, systemBarStyle);

			if (IPlatformApplication.Current?.Application is not null)
			{
				this.CreatePlatformWindow(IPlatformApplication.Current.Application, savedInstanceState);
			}

			// Register predictive back callback (Android 13+/API 33+) if available.
			// This integrates MAUI lifecycle OnBackPressed events with the system back gesture animation.
			// Guidance: route custom back handling through AndroidX OnBackPressedDispatcher so
			// predictive back works correctly:
			// https://developer.android.com/guide/navigation/custom-back/predictive-back-gesture#update-custom
			if (OperatingSystem.IsAndroidVersionAtLeast(33) && _predictiveBackCallback is null)
			{
				_predictiveBackCallback = new PredictiveBackCallback(this);
				// Priority 0 = PRIORITY_DEFAULT: callback invoked only when no higher-priority callback handles the event
				OnBackInvokedDispatcher?.RegisterOnBackInvokedCallback(0, _predictiveBackCallback);
			}
		}

		// Builds a transparent SystemBarStyle for edge-to-edge, choosing Dark (light icons)
		// or Light (dark icons) based on the current UI night mode so the system bar icons
		// stay legible over the app content. Transparent scrims keep the bars fully
		// transparent and disable navigation bar contrast enforcement on API 29+.
		SystemBarStyle GetEdgeToEdgeSystemBarStyle()
		{
			var transparent = global::Android.Graphics.Color.Transparent.ToArgb();
			var configuration = Resources?.Configuration;
			var isDarkMode = configuration is not null &&
				(configuration.UiMode & global::Android.Content.Res.UiMode.NightMask) == global::Android.Content.Res.UiMode.NightYes;

			return isDarkMode
				? SystemBarStyle.Dark(transparent)
				: SystemBarStyle.Light(transparent, transparent);
		}

		protected override void OnDestroy()
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(33) && _predictiveBackCallback is not null)
			{
				OnBackInvokedDispatcher?.UnregisterOnBackInvokedCallback(_predictiveBackCallback);
				_predictiveBackCallback.Dispose();
				_predictiveBackCallback = null;
			}
			base.OnDestroy();
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

		PredictiveBackCallback? _predictiveBackCallback;

		sealed class PredictiveBackCallback : Java.Lang.Object, IOnBackInvokedCallback
		{
			readonly MauiAppCompatActivity _activity;
			public PredictiveBackCallback(MauiAppCompatActivity activity)
			{
				_activity = activity;
			}

			public void OnBackInvoked()
			{
				// Reuse unified handling (will invoke lifecycle events and conditionally propagate).
				_activity.HandleBackNavigation();
			}
		}
	}
}