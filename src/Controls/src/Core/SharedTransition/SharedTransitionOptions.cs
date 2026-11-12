namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// App-wide defaults for shared-element transitions. Configure with
	/// <see cref="Hosting.SharedTransitionAppHostBuilderExtensions.ConfigureSharedTransitions"/>.
	/// </summary>
	public sealed class SharedTransitionOptions
	{
		internal const uint DefaultDurationValue = 300;

		/// <summary>Gets or sets whether shared transitions run in the app. The default is <see langword="true"/>.</summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>Gets or sets the default transition duration in milliseconds. The default is 300.</summary>
		public uint DefaultDuration { get; set; } = DefaultDurationValue;

		/// <summary>Gets or sets the default transition easing. The default is <see cref="Easing.CubicInOut"/>.</summary>
		public Easing DefaultEasing { get; set; } = Easing.CubicInOut;
	}
}
