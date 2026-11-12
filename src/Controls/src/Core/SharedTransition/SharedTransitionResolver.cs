using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Maui.Controls
{
	internal static class SharedTransitionResolver
	{
		static readonly SharedTransitionOptions s_defaultOptions = new();

		/// <summary>
		/// Matches elements tagged with <see cref="SharedTransition.NameProperty"/> and
		/// <see cref="SharedTransition.GroupProperty"/> on <paramref name="outgoing"/> and <paramref name="incoming"/>.
		/// </summary>
		/// <param name="outgoing">The page currently shown.</param>
		/// <param name="incoming">The page that will be shown.</param>
		/// <param name="isPopping">
		/// Whether the navigation removes <paramref name="outgoing"/>. Page-level settings come from the page
		/// being pushed or popped, so a detail page configures both directions.
		/// </param>
		/// <param name="options">App options; resolved from the page's services when <see langword="null"/>.</param>
		public static IReadOnlyList<SharedElementPair> Resolve(Page? outgoing, Page? incoming, bool isPopping, SharedTransitionOptions? options = null)
		{
			if (outgoing is null || incoming is null || ReferenceEquals(outgoing, incoming))
				return Array.Empty<SharedElementPair>();

			options ??= GetOptions(outgoing) ?? GetOptions(incoming) ?? s_defaultOptions;

			if (!options.IsEnabled || !SharedTransition.GetIsEnabled(outgoing) || !SharedTransition.GetIsEnabled(incoming))
				return Array.Empty<SharedElementPair>();

			var sources = Collect(outgoing);
			if (sources.Count == 0)
				return Array.Empty<SharedElementPair>();

			var destinations = Collect(incoming);
			if (destinations.Count == 0)
				return Array.Empty<SharedElementPair>();

			var settingsPage = isPopping ? outgoing : incoming;
			var pairs = new List<SharedElementPair>();

			foreach (var (key, source) in sources)
			{
				if (!destinations.TryGetValue(key, out var destination))
					continue;

				var settingsElement = isPopping ? source : destination;
				var otherElement = isPopping ? destination : source;

				pairs.Add(new SharedElementPair(
					source,
					destination,
					key.Name,
					key.Group,
					GetDuration(settingsElement, otherElement, settingsPage, options),
					GetEasing(settingsElement, otherElement, settingsPage, options)));
			}

			return pairs;
		}

		internal static string CreateKey(string name, string? group) =>
			SharedElementPair.CreateKey(name, NormalizeGroup(group));

		static SharedTransitionOptions? GetOptions(Page page) =>
			page.FindMauiContext()?.Services.GetService<SharedTransitionOptions>();

		static OrderedElements Collect(Page page)
		{
			var elements = new OrderedElements();

			foreach (var descendant in page.GetVisualTreeDescendants())
			{
				if (descendant is not VisualElement element || element is Page)
					continue;

				var name = SharedTransition.GetName(element);
				if (string.IsNullOrEmpty(name) || !element.IsVisible || !SharedTransition.GetIsEnabled(element))
					continue;

				var key = (name!, NormalizeGroup(SharedTransition.GetGroup(element)));
				if (!elements.TryAdd(key, element))
					Debug.WriteLine($"SharedTransition: duplicate name '{CreateKey(key.Item1, key.Item2)}' on {page.GetType().Name}; using the first visible element.");
			}

			return elements;
		}

		static string? NormalizeGroup(string? group) => string.IsNullOrEmpty(group) ? null : group;

		static uint GetDuration(VisualElement settingsElement, VisualElement otherElement, Page settingsPage, SharedTransitionOptions options)
		{
			if (settingsElement.IsSet(SharedTransition.DurationProperty))
				return SharedTransition.GetDuration(settingsElement);
			if (otherElement.IsSet(SharedTransition.DurationProperty))
				return SharedTransition.GetDuration(otherElement);
			if (settingsPage.IsSet(SharedTransition.DurationProperty))
				return SharedTransition.GetDuration(settingsPage);
			return options.DefaultDuration;
		}

		static Easing GetEasing(VisualElement settingsElement, VisualElement otherElement, Page settingsPage, SharedTransitionOptions options) =>
			SharedTransition.GetEasing(settingsElement)
			?? SharedTransition.GetEasing(otherElement)
			?? SharedTransition.GetEasing(settingsPage)
			?? options.DefaultEasing
			?? Easing.Linear;

		sealed class OrderedElements : IEnumerable<((string Name, string? Group) Key, VisualElement Element)>
		{
			readonly List<((string Name, string? Group) Key, VisualElement Element)> _items = new();
			readonly Dictionary<(string Name, string? Group), VisualElement> _lookup = new();

			public int Count => _items.Count;

			public bool TryAdd((string Name, string? Group) key, VisualElement element)
			{
				if (_lookup.ContainsKey(key))
					return false;

				_lookup.Add(key, element);
				_items.Add((key, element));
				return true;
			}

			public bool TryGetValue((string Name, string? Group) key, out VisualElement element) =>
				_lookup.TryGetValue(key, out element!);

			public List<((string Name, string? Group) Key, VisualElement Element)>.Enumerator GetEnumerator() => _items.GetEnumerator();

			IEnumerator<((string Name, string? Group) Key, VisualElement Element)> IEnumerable<((string Name, string? Group) Key, VisualElement Element)>.GetEnumerator() => GetEnumerator();

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
