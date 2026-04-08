#nullable enable
using System;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// Provides a public interface for firing pointer gesture events from custom platform backends.
	/// </summary>
	/// <remarks>
	/// Implement this interface or cast a <see cref="PointerGestureRecognizer"/> to it in order to
	/// programmatically send pointer events without resorting to reflection. This matches the existing
	/// pattern of <see cref="ISwipeGestureController"/>, <see cref="IPanGestureController"/>, and
	/// <see cref="IPinchGestureController"/>.
	/// </remarks>
	public interface IPointerGestureController
	{
		/// <summary>
		/// Sends a pointer-entered event to the recognizer.
		/// </summary>
		/// <param name="sender">The view over which the pointer entered.</param>
		/// <param name="getPosition">
		/// An optional delegate that returns the pointer position relative to a given element.
		/// Pass <see langword="null"/> if position information is not available.
		/// </param>
		void SendPointerEntered(View sender, Func<IElement?, Point?>? getPosition);

		/// <summary>
		/// Sends a pointer-exited event to the recognizer.
		/// </summary>
		/// <param name="sender">The view from which the pointer exited.</param>
		/// <param name="getPosition">
		/// An optional delegate that returns the pointer position relative to a given element.
		/// Pass <see langword="null"/> if position information is not available.
		/// </param>
		void SendPointerExited(View sender, Func<IElement?, Point?>? getPosition);

		/// <summary>
		/// Sends a pointer-moved event to the recognizer.
		/// </summary>
		/// <param name="sender">The view over which the pointer moved.</param>
		/// <param name="getPosition">
		/// An optional delegate that returns the pointer position relative to a given element.
		/// Pass <see langword="null"/> if position information is not available.
		/// </param>
		void SendPointerMoved(View sender, Func<IElement?, Point?>? getPosition);

		/// <summary>
		/// Sends a pointer-pressed event to the recognizer.
		/// </summary>
		/// <param name="sender">The view on which the pointer was pressed.</param>
		/// <param name="getPosition">
		/// An optional delegate that returns the pointer position relative to a given element.
		/// Pass <see langword="null"/> if position information is not available.
		/// </param>
		void SendPointerPressed(View sender, Func<IElement?, Point?>? getPosition);

		/// <summary>
		/// Sends a pointer-released event to the recognizer.
		/// </summary>
		/// <param name="sender">The view on which the pointer was released.</param>
		/// <param name="getPosition">
		/// An optional delegate that returns the pointer position relative to a given element.
		/// Pass <see langword="null"/> if position information is not available.
		/// </param>
		void SendPointerReleased(View sender, Func<IElement?, Point?>? getPosition);
	}
}
