using System;

namespace Microsoft.Maui.Controls
{
    /// <summary>Event arguments for toggle state changes before they are applied.</summary>
    public class TogglingEventArgs : EventArgs
    {
        /// <summary>Creates a new <see cref="TogglingEventArgs"/> for a pending state transition.</summary>
        /// <param name="oldValue">The current switch state before the toggle operation.</param>
        /// <param name="value">The new switch state being requested.</param>
        public TogglingEventArgs(bool oldValue, bool value)
        {
            OldValue = oldValue;
            Value = value;
        }

        /// <summary>Gets a value indicating whether to cancel the pending state change.</summary>
        public bool Cancel { get; set; }

        /// <summary>Gets the current switch state before the toggle operation.</summary>
        public bool OldValue { get; private set; }

        /// <summary>Gets the new switch state being requested.</summary>
        public bool Value { get; private set; }
    }
}