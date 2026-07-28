using System.Diagnostics;
using System.Globalization;
using AndroidX.Core.View;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform;

internal static class MauiWindowInsetDebug
{
	const string Tag = "[MauiWindowInsetListener]";

	[Conditional("DEBUG")]
	internal static void WriteInsets(
		string className,
		string methodName,
		string eventName,
		AView view,
		WindowInsetsCompat? insets)
	{
		var systemBars = insets?.GetInsets(WindowInsetsCompat.Type.SystemBars());
		var displayCutout = insets?.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
		var ime = insets?.GetInsets(WindowInsetsCompat.Type.Ime());

		Debug.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"{0} {1}.{2}: Event={3} View={4} Attached={5} LaidOut={6} Bounds=({7},{8},{9},{10}) Size={11}x{12} Padding=({13},{14},{15},{16}) TranslationY={17} SystemBars=({18},{19},{20},{21}) DisplayCutout=({22},{23},{24},{25}) Ime=({26},{27},{28},{29}) ImeVisible={30}",
			Tag,
			className,
			methodName,
			eventName,
			view.GetType().FullName,
			view.IsAttachedToWindow,
			view.IsLaidOut,
			view.Left,
			view.Top,
			view.Right,
			view.Bottom,
			view.Width,
			view.Height,
			view.PaddingLeft,
			view.PaddingTop,
			view.PaddingRight,
			view.PaddingBottom,
			view.TranslationY,
			systemBars?.Left ?? 0,
			systemBars?.Top ?? 0,
			systemBars?.Right ?? 0,
			systemBars?.Bottom ?? 0,
			displayCutout?.Left ?? 0,
			displayCutout?.Top ?? 0,
			displayCutout?.Right ?? 0,
			displayCutout?.Bottom ?? 0,
			ime?.Left ?? 0,
			ime?.Top ?? 0,
			ime?.Right ?? 0,
			ime?.Bottom ?? 0,
			insets?.IsVisible(WindowInsetsCompat.Type.Ime()) ?? false));
	}

	[Conditional("DEBUG")]
	internal static void WriteImeAnimation(
		string methodName,
		string eventName,
		int sequence,
		int frame,
		long uptimeMillis,
		long frameDeltaMillis,
		AView? view,
		bool eligible,
		float fraction,
		int imeBottom,
		int startPaddingBottom,
		int targetPaddingBottom,
		float translationBefore,
		float translationAfter)
	{
		if (eventName == "Frame" &&
			frame > 2 &&
			frame % 4 != 0 &&
			frameDeltaMillis <= 20 &&
			fraction < 1)
		{
			return;
		}

		Debug.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"{0} ImeWindowInsetsCoordinator.{1}: Event={2} Sequence={3} Frame={4} TimeMs={5} DeltaMs={6} Eligible={7} Fraction={8:F4} ImeBottom={9} StartPaddingBottom={10} CurrentPaddingBottom={11} TargetPaddingBottom={12} TranslationBefore={13:F2} TranslationAfter={14:F2} View={15} Attached={16} LaidOut={17} Bounds=({18},{19},{20},{21}) Size={22}x{23}",
			Tag,
			methodName,
			eventName,
			sequence,
			frame,
			uptimeMillis,
			frameDeltaMillis,
			eligible,
			fraction,
			imeBottom,
			startPaddingBottom,
			view?.PaddingBottom ?? 0,
			targetPaddingBottom,
			translationBefore,
			translationAfter,
			view?.GetType().FullName ?? "<null>",
			view?.IsAttachedToWindow ?? false,
			view?.IsLaidOut ?? false,
			view?.Left ?? 0,
			view?.Top ?? 0,
			view?.Right ?? 0,
			view?.Bottom ?? 0,
			view?.Width ?? 0,
			view?.Height ?? 0));
	}
}
