namespace Microsoft.Maui
{
	/// <summary>Provides details about a shared-element navigation transition to platform customizations.</summary>
	public sealed class SharedTransitionInfo
	{
		internal SharedTransitionInfo(string name, bool isPopping, uint duration, Easing easing, object? sourcePlatformView, object? destinationPlatformView)
		{
			Name = name;
			IsPopping = isPopping;
			Duration = duration;
			Easing = easing;
			SourcePlatformView = sourcePlatformView;
			DestinationPlatformView = destinationPlatformView;
		}

		/// <summary>Gets the shared transition name.</summary>
		public string Name { get; }

		/// <summary>Gets whether the navigation is popping a page.</summary>
		public bool IsPopping { get; }

		/// <summary>Gets the transition duration in milliseconds.</summary>
		public uint Duration { get; }

		/// <summary>Gets the transition easing.</summary>
		public Easing Easing { get; }

		/// <summary>Gets the native view on the outgoing page.</summary>
		/// <remarks>The returned object is a platform-specific view, such as an Android <c>View</c> or an iOS <c>UIView</c>.</remarks>
		public object? SourcePlatformView { get; }

		/// <summary>Gets the native view on the incoming page.</summary>
		/// <remarks>The returned object is a platform-specific view, such as an Android <c>View</c> or an iOS <c>UIView</c>.</remarks>
		public object? DestinationPlatformView { get; }
	}
}
