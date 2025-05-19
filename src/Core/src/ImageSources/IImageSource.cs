#nullable enable
using System;
#pragma warning disable RS0016

namespace Microsoft.Maui
{
	public interface IImageSource
	{
		bool IsEmpty { get; }
		TimeSpan CacheValidity { get; }

		bool CachingEnabled { get; }
	}
}