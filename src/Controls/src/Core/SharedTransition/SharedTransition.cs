namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// Attached properties that opt elements into shared-element transitions between pages.
	/// </summary>
	/// <remarks>
	/// When navigating between two pages, elements that share the same <see cref="NameProperty"/> and
	/// <see cref="GroupProperty"/> are animated from their position on the outgoing page to their position
	/// on the incoming page using the platform's native transition support.
	/// </remarks>
	public static class SharedTransition
	{
		/// <summary>Bindable property for attached property <c>Name</c>.</summary>
		public static readonly BindableProperty NameProperty =
			BindableProperty.CreateAttached("Name", typeof(string), typeof(SharedTransition), null);

		/// <summary>Bindable property for attached property <c>Group</c>.</summary>
		public static readonly BindableProperty GroupProperty =
			BindableProperty.CreateAttached("Group", typeof(string), typeof(SharedTransition), null);

		/// <summary>Bindable property for attached property <c>Duration</c>, in milliseconds.</summary>
		public static readonly BindableProperty DurationProperty =
			BindableProperty.CreateAttached("Duration", typeof(uint), typeof(SharedTransition), SharedTransitionOptions.DefaultDurationValue);

		/// <summary>Bindable property for attached property <c>Easing</c>.</summary>
		public static readonly BindableProperty EasingProperty =
			BindableProperty.CreateAttached("Easing", typeof(Easing), typeof(SharedTransition), null);

		/// <summary>Bindable property for attached property <c>IsEnabled</c>.</summary>
		public static readonly BindableProperty IsEnabledProperty =
			BindableProperty.CreateAttached("IsEnabled", typeof(bool), typeof(SharedTransition), true);

		/// <summary>Gets the shared transition name of <paramref name="bindable"/>.</summary>
		public static string? GetName(BindableObject bindable) => (string?)bindable.GetValue(NameProperty);

		/// <summary>Sets the shared transition name of <paramref name="bindable"/>.</summary>
		public static void SetName(BindableObject bindable, string? value) => bindable.SetValue(NameProperty, value);

		/// <summary>Gets the group that scopes the shared transition name, for example an item identifier in a list.</summary>
		public static string? GetGroup(BindableObject bindable) => (string?)bindable.GetValue(GroupProperty);

		/// <summary>Sets the group that scopes the shared transition name, for example an item identifier in a list.</summary>
		public static void SetGroup(BindableObject bindable, string? value) => bindable.SetValue(GroupProperty, value);

		/// <summary>Gets the transition duration, in milliseconds, for an element or page.</summary>
		public static uint GetDuration(BindableObject bindable) => (uint)bindable.GetValue(DurationProperty);

		/// <summary>Sets the transition duration, in milliseconds, for an element or page.</summary>
		public static void SetDuration(BindableObject bindable, uint value) => bindable.SetValue(DurationProperty, value);

		/// <summary>Gets the transition easing for an element or page.</summary>
		public static Easing? GetEasing(BindableObject bindable) => (Easing?)bindable.GetValue(EasingProperty);

		/// <summary>Sets the transition easing for an element or page.</summary>
		public static void SetEasing(BindableObject bindable, Easing? value) => bindable.SetValue(EasingProperty, value);

		/// <summary>Gets whether shared transitions are enabled for an element or page.</summary>
		public static bool GetIsEnabled(BindableObject bindable) => (bool)bindable.GetValue(IsEnabledProperty);

		/// <summary>Sets whether shared transitions are enabled for an element or page.</summary>
		public static void SetIsEnabled(BindableObject bindable, bool value) => bindable.SetValue(IsEnabledProperty, value);
	}
}
