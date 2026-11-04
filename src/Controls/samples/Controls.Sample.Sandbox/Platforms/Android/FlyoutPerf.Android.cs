#if ANDROID
using Android.Views;
using AndroidX.DrawerLayout.Widget;
using AView = Android.Views.View;

namespace Maui.Controls.Sample;

public partial class SandboxShell
{
	DrawerLayout? _timingDrawer;
	float _lastOffset;
	int _slideDirection;
	long _closeStarted;

	partial void ConnectDrawerTiming()
	{
		if (Handler?.PlatformView is not DrawerLayout drawer)
		{
			FlyoutPerf.Write("NativeDrawerUnavailable", "-", detail: "reason=handler-platform-view-not-DrawerLayout");
			return;
		}

		_timingDrawer = drawer;
		_lastOffset = drawer.IsDrawerOpen((int)GravityFlags.Start) ? 1 : 0;
		drawer.DrawerSlide += OnTimingDrawerSlide;
		drawer.DrawerStateChanged += OnTimingDrawerStateChanged;
		drawer.DrawerOpened += OnTimingDrawerOpened;
		drawer.DrawerClosed += OnTimingDrawerClosed;
		FlyoutPerf.Write("NativeDrawerAttached", "-");
	}

	partial void DisconnectDrawerTiming()
	{
		if (_timingDrawer is not { } drawer)
			return;

		drawer.DrawerSlide -= OnTimingDrawerSlide;
		drawer.DrawerStateChanged -= OnTimingDrawerStateChanged;
		drawer.DrawerOpened -= OnTimingDrawerOpened;
		drawer.DrawerClosed -= OnTimingDrawerClosed;
		_timingDrawer = null;
		_closeStarted = 0;
		_slideDirection = 0;
		FlyoutPerf.NativeClosing = false;
		FlyoutPerf.Write("NativeDrawerDetached", "-");
	}

	void OnTimingDrawerSlide(object? sender, DrawerLayout.DrawerSlideEventArgs args)
	{
		var direction = args.SlideOffset.CompareTo(_lastOffset);
		_lastOffset = args.SlideOffset;
		if (direction == 0 || direction == _slideDirection)
			return;

		// Only direction changes are logged, never individual animation frames.
		_slideDirection = direction;
		if (direction < 0)
		{
			_closeStarted = FlyoutPerf.Now;
			FlyoutPerf.NativeClosing = true;
			FlyoutPerf.Write("NativeDrawerCloseStart", CurrentPage?.Title ?? "Main",
				detail: "milestone=first-decreasing-slide-offset");
		}
		else
		{
			FlyoutPerf.DrawerCycle++;
			FlyoutPerf.NativeClosing = false;
			_closeStarted = 0;
			FlyoutPerf.Write("NativeDrawerOpenStart", CurrentPage?.Title ?? "Main");
		}
	}

	void OnTimingDrawerStateChanged(object? sender, DrawerLayout.DrawerStateChangedEventArgs args) =>
		FlyoutPerf.Write("NativeDrawerState", CurrentPage?.Title ?? "Main", detail: $"state={args.NewState}");

	void OnTimingDrawerOpened(object? sender, DrawerLayout.DrawerOpenedEventArgs args)
	{
		_lastOffset = 1;
		_slideDirection = 0;
		FlyoutPerf.NativeClosing = false;
		FlyoutPerf.Write("NativeDrawerOpened", CurrentPage?.Title ?? "Main");
	}

	void OnTimingDrawerClosed(object? sender, DrawerLayout.DrawerClosedEventArgs args)
	{
		var detail = _closeStarted == 0 ? "observedSlide=false" :
			FormattableString.Invariant($"observedSlide=true durationMs={FlyoutPerf.Milliseconds(_closeStarted):F3}");
		_lastOffset = 0;
		_slideDirection = 0;
		_closeStarted = 0;
		FlyoutPerf.NativeClosing = false;
		FlyoutPerf.Write("NativeDrawerClosed", CurrentPage?.Title ?? "Main", detail: detail);
	}
}

public abstract partial class FlyoutPerfPage
{
	FirstContentPreDraw? _preDraw;

	partial void ArmNativePreDraw()
	{
		RemoveNativePreDraw();
		if (Content.Handler?.PlatformView is AView view && view.ViewTreeObserver is { IsAlive: true } observer)
		{
			_preDraw = new FirstContentPreDraw(this, view, observer);
			observer.AddOnPreDrawListener(_preDraw);
		}
	}

	partial void RemoveNativePreDraw()
	{
		if (_preDraw is not { } listener)
			return;

		_preDraw = null;
		listener.Remove();
		listener.Dispose();
	}

	sealed class FirstContentPreDraw(FlyoutPerfPage page, AView view, ViewTreeObserver observer)
		: Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
	{
		public bool OnPreDraw()
		{
			if (!view.IsAttachedToWindow || !view.IsShown || view.Width <= 0 || view.Height <= 0)
				return true;

			page._awaitingPreDraw = false;
			page.Log("ContentNativePreDraw", $"width={view.Width} height={view.Height} milestone=pre-draw-not-frame-presented");
			page.RemoveNativePreDraw();
			return true;
		}

		internal void Remove()
		{
			if (observer.IsAlive)
				observer.RemoveOnPreDrawListener(this);
			// Android may replace the observer on attachment.
			if (view.ViewTreeObserver is { IsAlive: true } current && current != observer)
				current.RemoveOnPreDrawListener(this);
		}
	}
}
#endif
