using Microsoft.Maui.Graphics;

namespace Microsoft.Maui
{
	/// <summary>
	/// Represents a Shadow that can be applied to a View.
	/// </summary>
	public interface IShadow
	{
		/// <summary>
		/// The radius of the blur used to generate the shadow
		/// </summary>
		float Radius { get; }

		/// <summary>
		/// The opacity of the shadow.
		/// </summary>
		float Opacity { get; }

		/// <summary>
		/// The Paint used to colorize the Shadow.
		/// SolidPaint is fully supported on all platforms.
		/// LinearGradientPaint and RadialGradientPaint are supported on Android and iOS/MacCatalyst.
		/// On Windows, gradient paints fall back to a blended solid color approximation.
		/// </summary>
		Paint Paint { get; }

		/// <summary>
		/// Offset of the shadow relative to the View
		/// </summary>
		Point Offset { get; }
	}
}
