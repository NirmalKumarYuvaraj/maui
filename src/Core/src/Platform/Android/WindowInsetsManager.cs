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

internal readonly record struct WindowInsetConsumption(
	WindowInsetEdges SystemBars,
	WindowInsetEdges DisplayCutout,
	WindowInsetEdges Ime)
{
	internal static WindowInsetConsumption None { get; } =
		new(WindowInsetEdges.None, WindowInsetEdges.None, WindowInsetEdges.None);

	internal static WindowInsetConsumption Container(WindowInsetEdges edges) =>
		new(edges, edges, WindowInsetEdges.None);
}

internal readonly record struct WindowInsetsSnapshot(
	Insets SystemBars,
	Insets DisplayCutout,
	Insets Ime,
	bool SystemBarsVisible,
	bool DisplayCutoutVisible,
	bool ImeVisible)
{
	internal static WindowInsetsSnapshot Empty { get; } =
		new(Insets.None!, Insets.None!, Insets.None!, false, false, false);
}

internal sealed class WindowInsetsManager
{
	WindowInsetsCompat? _currentInsets;

	internal WindowInsetsSnapshot Current { get; private set; } = WindowInsetsSnapshot.Empty;

	internal Insets SystemBars => Current.SystemBars;

	internal Insets DisplayCutout => Current.DisplayCutout;

	internal Insets Ime => Current.Ime;

	internal void Update(WindowInsetsCompat insets)
	{
		_currentInsets = insets;
		Current = CreateSnapshot(insets);
	}

	internal int GetSafeAreaInset(WindowInsetEdges edge)
	{
		return edge switch
		{
			WindowInsetEdges.Left => Math.Max(SystemBars.Left, DisplayCutout.Left),
			WindowInsetEdges.Top => Math.Max(SystemBars.Top, DisplayCutout.Top),
			WindowInsetEdges.Right => Math.Max(SystemBars.Right, DisplayCutout.Right),
			WindowInsetEdges.Bottom => Math.Max(SystemBars.Bottom, DisplayCutout.Bottom),
			_ => 0,
		};
	}

	internal WindowInsetsCompat BuildRemaining(WindowInsetEdges consumedEdges)
	{
		return BuildRemaining(WindowInsetConsumption.Container(consumedEdges));
	}

	internal WindowInsetsCompat BuildRemaining(WindowInsetConsumption consumption)
	{
		if (_currentInsets is null)
		{
			throw new InvalidOperationException("Window insets must be updated before remaining insets are built.");
		}

		return BuildRemaining(_currentInsets, Current, consumption);
	}

	internal static WindowInsetsCompat BuildRemaining(
		WindowInsetsCompat insets,
		WindowInsetConsumption consumption)
	{
		return BuildRemaining(insets, CreateSnapshot(insets), consumption);
	}

	static WindowInsetsCompat BuildRemaining(
		WindowInsetsCompat insets,
		WindowInsetsSnapshot snapshot,
		WindowInsetConsumption consumption)
	{
		var systemBars = Consume(snapshot.SystemBars, consumption.SystemBars);
		var displayCutout = Consume(snapshot.DisplayCutout, consumption.DisplayCutout);
		var ime = Consume(snapshot.Ime, consumption.Ime);
		var builder = new WindowInsetsCompat.Builder(insets);

		builder.SetInsets(WindowInsetsCompat.Type.SystemBars(), systemBars);
		builder.SetInsets(WindowInsetsCompat.Type.DisplayCutout(), displayCutout);
		builder.SetInsets(WindowInsetsCompat.Type.Ime(), ime);
		builder.SetVisible(
			WindowInsetsCompat.Type.SystemBars(),
			GetRemainingVisibility(
				snapshot.SystemBarsVisible,
				systemBars,
				consumption.SystemBars));
		builder.SetVisible(
			WindowInsetsCompat.Type.DisplayCutout(),
			GetRemainingVisibility(
				snapshot.DisplayCutoutVisible,
				displayCutout,
				consumption.DisplayCutout));
		builder.SetVisible(
			WindowInsetsCompat.Type.Ime(),
			GetRemainingVisibility(
				snapshot.ImeVisible,
				ime,
				consumption.Ime));

		return builder.Build() ?? insets;
	}

	static WindowInsetsSnapshot CreateSnapshot(WindowInsetsCompat insets)
	{
		return new(
			GetInsets(insets, WindowInsetsCompat.Type.SystemBars()),
			GetInsets(insets, WindowInsetsCompat.Type.DisplayCutout()),
			GetInsets(insets, WindowInsetsCompat.Type.Ime()),
			insets.IsVisible(WindowInsetsCompat.Type.SystemBars()),
			insets.IsVisible(WindowInsetsCompat.Type.DisplayCutout()),
			insets.IsVisible(WindowInsetsCompat.Type.Ime()));
	}

	static Insets GetInsets(WindowInsetsCompat insets, int typeMask) =>
		insets.GetInsets(typeMask) ?? Insets.None!;

	static Insets Consume(Insets insets, WindowInsetEdges consumedEdges)
	{
		return Insets.Of(
			IsConsumed(consumedEdges, WindowInsetEdges.Left) ? 0 : insets.Left,
			IsConsumed(consumedEdges, WindowInsetEdges.Top) ? 0 : insets.Top,
			IsConsumed(consumedEdges, WindowInsetEdges.Right) ? 0 : insets.Right,
			IsConsumed(consumedEdges, WindowInsetEdges.Bottom) ? 0 : insets.Bottom) ?? Insets.None!;
	}

	static bool IsConsumed(WindowInsetEdges consumedEdges, WindowInsetEdges edge) =>
		(consumedEdges & edge) != 0;

	static bool IsEmpty(Insets insets) =>
		insets.Left == 0 &&
		insets.Top == 0 &&
		insets.Right == 0 &&
		insets.Bottom == 0;

	static bool GetRemainingVisibility(
		bool wasVisible,
		Insets remainingInsets,
		WindowInsetEdges consumedEdges) =>
		wasVisible &&
		(consumedEdges == WindowInsetEdges.None || !IsEmpty(remainingInsets));
}
