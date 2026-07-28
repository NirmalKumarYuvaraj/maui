using System;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;

namespace Microsoft.Maui.Platform;

[Flags]
internal enum WindowInsetEdges
{
	None = 0,
	Left = 1,
	Top = 2,
	Right = 4,
	Bottom = 8,
}

internal sealed class WindowInsetsManager
{
	WindowInsetsCompat? _currentInsets;

	internal Insets? SystemBars { get; private set; }

	internal Insets? DisplayCutout { get; private set; }

	internal void Update(WindowInsetsCompat insets)
	{
		_currentInsets = insets;
		SystemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
		DisplayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
	}

	internal int GetSafeAreaInset(WindowInsetEdges edge)
	{
		return edge switch
		{
			WindowInsetEdges.Left => Math.Max(SystemBars?.Left ?? 0, DisplayCutout?.Left ?? 0),
			WindowInsetEdges.Top => Math.Max(SystemBars?.Top ?? 0, DisplayCutout?.Top ?? 0),
			WindowInsetEdges.Right => Math.Max(SystemBars?.Right ?? 0, DisplayCutout?.Right ?? 0),
			WindowInsetEdges.Bottom => Math.Max(SystemBars?.Bottom ?? 0, DisplayCutout?.Bottom ?? 0),
			_ => 0,
		};
	}

	internal WindowInsetsCompat BuildRemaining(WindowInsetEdges consumedEdges)
	{
		if (_currentInsets is null)
		{
			throw new InvalidOperationException("Window insets must be updated before remaining insets are built.");
		}

		var systemBars = Consume(SystemBars, consumedEdges);
		var displayCutout = Consume(DisplayCutout, consumedEdges);

		return new WindowInsetsCompat.Builder(_currentInsets)
			?.SetInsets(WindowInsetsCompat.Type.SystemBars(), systemBars)
			?.SetInsets(WindowInsetsCompat.Type.DisplayCutout(), displayCutout)
			?.Build() ?? _currentInsets;
	}

	static Insets Consume(Insets? insets, WindowInsetEdges consumedEdges)
	{
		return Insets.Of(
			IsConsumed(consumedEdges, WindowInsetEdges.Left) ? 0 : insets?.Left ?? 0,
			IsConsumed(consumedEdges, WindowInsetEdges.Top) ? 0 : insets?.Top ?? 0,
			IsConsumed(consumedEdges, WindowInsetEdges.Right) ? 0 : insets?.Right ?? 0,
			IsConsumed(consumedEdges, WindowInsetEdges.Bottom) ? 0 : insets?.Bottom ?? 0) ?? Insets.None!;
	}

	static bool IsConsumed(WindowInsetEdges consumedEdges, WindowInsetEdges edge) =>
		(consumedEdges & edge) != 0;
}
