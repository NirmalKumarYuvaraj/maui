using System.Diagnostics;
using System.Globalization;

namespace Maui.Controls.Sample;

// All state is accessed on the UI thread. No UI objects are retained by the logger.
internal static class FlyoutPerf
{
	static readonly long s_origin = Stopwatch.GetTimestamp();
	static int s_nextTransition;

	internal sealed record Transition(int Id, long Started);

	internal static Transition Current { get; private set; } = new(0, Stopwatch.GetTimestamp());
	internal static int DrawerCycle { get; set; }
	internal static bool NativeClosing { get; set; }

	internal static long Now => Stopwatch.GetTimestamp();
	internal static double Milliseconds(long start) => (Now - start) * 1000.0 / Stopwatch.Frequency;

	internal static void BeginNavigation(ShellNavigatingEventArgs args)
	{
		Current = new(++s_nextTransition, Now);
		Write("Navigating", "-", detail: $"from={args.Current?.Location} to={args.Target.Location} source={args.Source}");
	}

	[Conditional("DEBUG")]
	internal static void Write(string eventName, string page, Transition? transition = null,
		string visit = "-", string detail = "")
	{
		var context = transition ?? Current;
		var now = Now;
		Debug.WriteLine(string.Create(CultureInfo.InvariantCulture,
			$"[FlyoutPerf] event={eventName} page={page} transition={context.Id} visit={visit} timeMs={(now - s_origin) * 1000.0 / Stopwatch.Frequency:F3} elapsedMs={(now - context.Started) * 1000.0 / Stopwatch.Frequency:F3} drawerCycle={DrawerCycle} nativeClosing={NativeClosing} {detail}"));
	}
}

public abstract partial class FlyoutPerfPage : ContentPage
{
	FlyoutPerf.Transition _transition;
	int _visits;
	bool _loggedSize;
	bool _loggedLoaded;
	bool _awaitingPreDraw;
	string Visit => _visits <= 1 ? "cold" : "warm";

	protected FlyoutPerfPage(string title, Func<View> buildContent)
	{
		var started = FlyoutPerf.Now;
		_transition = FlyoutPerf.Current;
		Title = title;
		Log("ConstructionStart", "scope=page-constructor-body");
		BackgroundColor = Colors.Black;
		var buildStarted = FlyoutPerf.Now;
		var closingAtStart = FlyoutPerf.NativeClosing;
		Log("ContentBuildStart");
		Content = buildContent();
		Log("ContentBuildEnd", FormattableString.Invariant(
			$"durationMs={FlyoutPerf.Milliseconds(buildStarted):F3} nativeClosingAtBuildStart={closingAtStart}"));

		HandlerChanging += (_, _) => RemoveNativePreDraw();
		HandlerChanged += (_, _) =>
		{
			Log("PageHandlerChanged", $"connected={Handler is not null}");
			if (_awaitingPreDraw && Handler is not null)
				ArmNativePreDraw();
		};
		Content.HandlerChanging += (_, _) => RemoveNativePreDraw();
		Content.HandlerChanged += (_, _) =>
		{
			Log("ContentHandlerChanged", $"connected={Content.Handler is not null}");
			if (_awaitingPreDraw)
				ArmNativePreDraw();
		};
		Content.Unloaded += (_, _) => RemoveNativePreDraw();
		Content.Loaded += (_, _) =>
		{
			if (!_loggedLoaded)
			{
				_loggedLoaded = true;
				Log("ContentLoaded", "milestone=managed-not-rendered");
			}
			if (_awaitingPreDraw)
				ArmNativePreDraw();
		};
		Content.SizeChanged += (_, _) =>
		{
			if (!_loggedSize && Content.Width > 0 && Content.Height > 0)
			{
				_loggedSize = true;
				Log("FirstPositiveContentSize", FormattableString.Invariant(
					$"width={Content.Width:F1} height={Content.Height:F1} milestone=managed-layout-not-rendered"));
			}
		};
		Log("ConstructionEnd", FormattableString.Invariant($"durationMs={FlyoutPerf.Milliseconds(started):F3}"));
	}

	protected override void OnAppearing()
	{
		_transition = FlyoutPerf.Current;
		_visits++;
		_loggedSize = false;
		_loggedLoaded = false;
		_awaitingPreDraw = true;
		Log("Appearing");
		base.OnAppearing();
		ArmNativePreDraw();
	}

	protected override void OnDisappearing()
	{
		Log("Disappearing", $"triggerTransition={FlyoutPerf.Current.Id}");
		_awaitingPreDraw = false;
		RemoveNativePreDraw();
		base.OnDisappearing();
	}

	void Log(string eventName, string detail = "") =>
		FlyoutPerf.Write(eventName, Title, _transition, Visit, $"appearance={_visits} {detail}");

	partial void ArmNativePreDraw();
	partial void RemoveNativePreDraw();
}

public sealed class HomePage : FlyoutPerfPage
{
	public HomePage() : base("Home", () => FlyoutDashboard.Build("Home")) { }
}

public sealed class DrawsPage : FlyoutPerfPage
{
	public DrawsPage() : base("Draws", () => FlyoutDashboard.Build("Draws")) { }
}

public sealed class InvestmentsPage : FlyoutPerfPage
{
	public InvestmentsPage() : base("Investments", () => FlyoutDashboard.Build("Investments")) { }
}

public sealed class HoldersPage : FlyoutPerfPage
{
	public HoldersPage() : base("Holders", () => FlyoutDashboard.Build("Holders")) { }
}
