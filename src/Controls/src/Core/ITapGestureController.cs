#nullable enable
using System;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// Provides a public interface for firing tap gesture events from custom platform backends.
	/// </summary>
	/// <remarks>
	/// Implement this interface or cast a <see cref="TapGestureRecognizer"/> to it in order to
	/// programmatically send tap events without resorting to reflection. This matches the existing
	/// pattern of <see cref="ISwipeGestureController"/>, <see cref="IPanGestureController"/>, and
	/// <see cref="IPinchGestureController"/>.
	/// </remarks>
	public interface ITapGestureController
	{
		/// <summary>
		/// Sends a tapped event to the recognizer, firing any bound <see cref="TapGestureRecognizer.Tapped"/>
		/// handlers and executing the associated <see cref="TapGestureRecognizer.Command"/>.
		/// </summary>
		/// <param name="sender">The view on which the tap was recognized.</param>
		/// <param name="getPosition">
		/// An optional delegate that returns the tap position relative to a given element.
		/// Pass <see langword="null"/> if position information is not available.
		/// </param>
		void SendTapped(View sender, Func<IElement?, Point?>? getPosition = null);
	}
}
