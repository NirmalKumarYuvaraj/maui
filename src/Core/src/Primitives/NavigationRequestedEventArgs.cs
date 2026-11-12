using System;
using System.Collections.Generic;

namespace Microsoft.Maui
{
	public class NavigationRequest
	{
		public IReadOnlyList<IView> NavigationStack { get; }

		public NavigationRequest(IReadOnlyList<IView> newNavigationStack, bool animated)
		{
			NavigationStack = newNavigationStack;
			Animated = animated;
		}

		public bool Animated { get; }

		internal IReadOnlyList<SharedElementPair> SharedElementPairs { get; set; } = Array.Empty<SharedElementPair>();
	}
}