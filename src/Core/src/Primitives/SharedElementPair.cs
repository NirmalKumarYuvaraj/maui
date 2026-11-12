namespace Microsoft.Maui
{
	internal sealed class SharedElementPair
	{
		public SharedElementPair(IView source, IView destination, string name, string? group, uint duration, Easing easing)
		{
			Source = source;
			Destination = destination;
			Name = name;
			Group = group;
			Duration = duration;
			Easing = easing;
		}

		public IView Source { get; }

		public IView Destination { get; }

		public string Name { get; }

		public string? Group { get; }

		public string Key => CreateKey(Name, Group);

		internal static string CreateKey(string name, string? group) =>
			group is null ? $"{name.Length}:{name}" : $"{name.Length}:{name}{group.Length}:{group}";

		public uint Duration { get; }

		public Easing Easing { get; }
	}
}
