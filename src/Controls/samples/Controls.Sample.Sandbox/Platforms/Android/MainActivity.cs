using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Microsoft.Maui;

namespace Maui.Controls.Sample.Platform
{
	[Activity(
		Theme = "@style/Maui.SplashTheme",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode)]
	[IntentFilter(
		new[] { Microsoft.Maui.ApplicationModel.Platform.Intent.ActionAppAction },
		Categories = new[] { Android.Content.Intent.CategoryDefault })]
	[Register("com.microsoft.maui.sandbox.MainActivity")]
	public class MainActivity : MauiAppCompatActivity
	{
		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Window?.SetSoftInputMode(Android.Views.SoftInput.AdjustResize);
		}
	}
}