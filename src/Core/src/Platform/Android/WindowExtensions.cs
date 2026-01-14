using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui
{
	public static partial class WindowExtensions
	{
		internal static void UpdateTitle(this Activity platformWindow, IWindow window)
		{
			if (string.IsNullOrEmpty(window.Title))
				platformWindow.Title = ApplicationModel.AppInfo.Current.Name;
			else
				platformWindow.Title = window.Title;
		}

		internal static DisplayOrientation GetOrientation(this IWindow? window)
		{
			if (window == null)
				return DeviceDisplay.Current.MainDisplayInfo.Orientation;

			return window.Handler?.MauiContext?.GetPlatformWindow()?.Resources?.Configuration?.Orientation switch
			{
				Orientation.Landscape => DisplayOrientation.Landscape,
				Orientation.Portrait => DisplayOrientation.Portrait,
				Orientation.Square => DisplayOrientation.Portrait,
				_ => DisplayOrientation.Unknown
			};
		}

		internal static void UpdateWindowSoftInputModeAdjust(this IWindow platformView, SoftInput inputMode)
		{
			var activity = platformView?.Handler?.PlatformView as Activity ??
							platformView?.Handler?.MauiContext?.GetPlatformWindow();

			activity?
				.Window?
				.SetSoftInputMode(inputMode);
		}

		/// <summary>
		/// Gets the current WindowSoftInputMode from the activity's window attributes.
		/// </summary>
		/// <param name="context">The Android context</param>
		/// <returns>The current SoftInput mode, or StateUnspecified if unable to determine</returns>
		internal static SoftInput GetCurrentSoftInputMode(this Context context)
		{
			return context.GetActivity()?.Window?.Attributes?.SoftInputMode
				?? SoftInput.StateUnspecified;
		}

		/// <summary>
		/// Checks if the SoftInput mode is AdjustResize.
		/// </summary>
		/// <param name="mode">The SoftInput mode to check</param>
		/// <returns>True if the adjust mode is AdjustResize</returns>
		internal static bool IsAdjustResize(this SoftInput mode)
		{
			// SoftInput adjust mask is 0xf0 (bits 4-7)
			return ((int)mode & 0xf0) == (int)SoftInput.AdjustResize;
		}

		/// <summary>
		/// Checks if the SoftInput mode is AdjustPan.
		/// </summary>
		/// <param name="mode">The SoftInput mode to check</param>
		/// <returns>True if the adjust mode is AdjustPan</returns>
		internal static bool IsAdjustPan(this SoftInput mode)
		{
			// SoftInput adjust mask is 0xf0 (bits 4-7)
			return ((int)mode & 0xf0) == (int)SoftInput.AdjustPan;
		}

		/// <summary>
		/// Checks if the SoftInput mode is AdjustNothing.
		/// </summary>
		/// <param name="mode">The SoftInput mode to check</param>
		/// <returns>True if the adjust mode is AdjustNothing</returns>
		internal static bool IsAdjustNothing(this SoftInput mode)
		{
			// SoftInput adjust mask is 0xf0 (bits 4-7)
			return ((int)mode & 0xf0) == (int)SoftInput.AdjustNothing;
		}

		/// <summary>
		/// Checks if the SoftInput mode is AdjustUnspecified.
		/// </summary>
		/// <param name="mode">The SoftInput mode to check</param>
		/// <returns>True if the adjust mode is AdjustUnspecified</returns>
		internal static bool IsAdjustUnspecified(this SoftInput mode)
		{
			// SoftInput adjust mask is 0xf0 (bits 4-7)
			return ((int)mode & 0xf0) == (int)SoftInput.AdjustUnspecified;
		}

		//TODO : Make it public in NET 11.
		internal static void ConfigureTranslucentSystemBars(this Window? window, Activity activity)
		{
			if (window is null)
			{
				return;
			}

			// Set appropriate system bar appearance for readability using API 30+ methods
			var windowInsetsController = WindowCompat.GetInsetsController(window, window.DecorView);
			if (windowInsetsController is not null)
			{
				// Automatically adjust icon/text colors based on app theme
				var configuration = activity.Resources?.Configuration;
				var isLightTheme = configuration is null ||
					(configuration.UiMode & UiMode.NightMask) != UiMode.NightYes;

				windowInsetsController.AppearanceLightStatusBars = isLightTheme;
				windowInsetsController.AppearanceLightNavigationBars = isLightTheme;
			}
		}
	}
}
