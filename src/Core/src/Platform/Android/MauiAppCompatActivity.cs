using System;
using Android.OS;
using Android.Views;
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
			WindowCompat.SetDecorFitsSystemWindows(Window, false);

			if (IPlatformApplication.Current?.Application is not null)
			{
				this.CreatePlatformWindow(IPlatformApplication.Current.Application, savedInstanceState);
			}

			// Use AndroidX OnBackPressedDispatcher to handle back navigation.
			// This replaces the deprecated OnBackPressed() override and integrates with
			// the Android 13+ predictive back gesture system automatically.
			// https://developer.android.com/guide/navigation/custom-back/predictive-back-gesture
			OnBackPressedDispatcher.AddCallback(this, new MauiBackPressedCallback(this));
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

		sealed class MauiBackPressedCallback : OnBackPressedCallback
		{
			readonly MauiAppCompatActivity _activity;

			public MauiBackPressedCallback(MauiAppCompatActivity activity) : base(true)
			{
				_activity = activity;
			}

			public override void HandleOnBackPressed()
			{
				var preventBackPropagation = false;
				IPlatformApplication.Current?.Services?.InvokeLifecycleEvents<AndroidLifecycle.OnBackPressed>(del =>
				{
					preventBackPropagation = del(_activity) || preventBackPropagation;
				});

				if (!preventBackPropagation)
				{
					// Temporarily disable this callback and re-dispatch so the
					// default back behavior (or lower-priority callbacks) can run.
					Enabled = false;
					_activity.OnBackPressedDispatcher.OnBackPressed();
					Enabled = true;
				}
			}
		}
	}
}