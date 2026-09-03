using System.Globalization;
using UIKit;

namespace Maui.Controls.Sample;

public partial class Issue37892View
{
	partial void LogPlatformState(long changeCount)
	{
		if (ReproRoot.Handler?.PlatformView is not UIView outerView ||
			ReproScrollView.Handler?.PlatformView is not UIScrollView scrollView)
		{
			return;
		}

		Console.WriteLine(
			$"SANDBOX: ISSUE37892 NATIVE #{changeCount} " +
			$"outerFrame={FormatRect(outerView.Frame)} " +
			$"outerBounds={FormatRect(outerView.Bounds)} " +
			$"scrollFrame={FormatRect(scrollView.Frame)} " +
			$"scrollBounds={FormatRect(scrollView.Bounds)} " +
			$"contentSize={FormatSize(scrollView.ContentSize)} " +
			$"adjustedInset={FormatInsets(scrollView.AdjustedContentInset)} " +
			$"safeArea={FormatInsets(scrollView.SafeAreaInsets)} " +
			$"behavior={scrollView.ContentInsetAdjustmentBehavior}");
	}

	static string FormatRect(CoreGraphics.CGRect rect) =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"{(double)rect.X:R},{(double)rect.Y:R},{(double)rect.Width:R},{(double)rect.Height:R}");

	static string FormatSize(CoreGraphics.CGSize size) =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"{(double)size.Width:R}x{(double)size.Height:R}");

	static string FormatInsets(UIEdgeInsets insets) =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"{(double)insets.Top:R},{(double)insets.Left:R},{(double)insets.Bottom:R},{(double)insets.Right:R}");
}
