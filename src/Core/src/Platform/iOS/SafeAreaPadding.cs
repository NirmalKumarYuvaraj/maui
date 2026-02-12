using System;
using CoreGraphics;
using UIKit;

namespace Microsoft.Maui.Platform;

internal readonly record struct SafeAreaPadding(double Left, double Right, double Top, double Bottom)
{
	// Tolerance for comparing safe area values to prevent layout loops from floating-point precision differences.
	// iOS can report values like 14.495513916015625 vs 14.495514869689941 which differ by ~1e-6.
	// We use 0.001 (1/1000th of a point) as the tolerance - differences smaller than this are imperceptible.
	internal const double ComparisonTolerance = 0.001;

	public static SafeAreaPadding Empty { get; } = new(0, 0, 0, 0);

	public bool IsEmpty { get; } = Left == 0 && Right == 0 && Top == 0 && Bottom == 0;
	public double HorizontalThickness { get; } = Left + Right;
	public double VerticalThickness { get; } = Top + Bottom;

	/// <summary>
	/// Compares two SafeAreaPadding values with a tolerance to account for floating-point precision differences.
	/// This prevents layout loops caused by iOS reporting slightly different values between calls.
	/// </summary>
	public bool EqualsWithTolerance(SafeAreaPadding other)
	{
		return Math.Abs(Left - other.Left) < ComparisonTolerance &&
			   Math.Abs(Right - other.Right) < ComparisonTolerance &&
			   Math.Abs(Top - other.Top) < ComparisonTolerance &&
			   Math.Abs(Bottom - other.Bottom) < ComparisonTolerance;
	}

	public CGRect InsetRect(CGRect bounds)
	{
		if (IsEmpty)
		{
			return bounds;
		}

		return new CGRect(
			bounds.Left + Left,
			bounds.Top + Top,
			bounds.Width - HorizontalThickness,
			bounds.Height - VerticalThickness);
	}

	public CGRect ToCGRect() =>
		new((nfloat)Top, (nfloat)Left, (nfloat)Bottom, (nfloat)Right);
}

internal static class SafeAreaInsetsExtensions
{
	public static SafeAreaPadding ToSafeAreaInsets(this UIEdgeInsets insets)
	{
		// Filters out negligible floating-point values from UIKit that may cause layout issues (e.g., 3.5527136788005009e-15).
		const double tolerance = 1e-14;

		static double ApplyTolerance(double value) => Math.Abs(value) < tolerance ? 0 : value;

		return new(
			ApplyTolerance(insets.Left),
			ApplyTolerance(insets.Right),
			ApplyTolerance(insets.Top),
			ApplyTolerance(insets.Bottom)
		);
	}
}