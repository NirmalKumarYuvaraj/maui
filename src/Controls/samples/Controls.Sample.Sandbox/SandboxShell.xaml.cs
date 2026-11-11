using System.Diagnostics;
#if ANDROID
using AView = Android.Views.View;
using ARect = Android.Graphics.Rect;
#endif

namespace Maui.Controls.Sample;

public partial class SandboxShell : Shell
{
	internal static readonly (string Id, string Title)[] Actions =
	{
		("NavSetup", "Setup fresh lazy roots"),
		("NavSelfTest", "Run deterministic self-tests"),
		("NavOwnership", "Top content: unvisited + visited; section isolation"),
		("NavRapid", "Rapid push/pop: track each completion"),
		("NavReentrant", "Push from completion and Appearing"),
		("NavCancel", "Cancel push, pop and top-content switch"),
		("NavHandlers", "Switch derived item handlers A → B → A"),
		("NavReconnect", "Detach and reconnect this Shell"),
		("NavInterrupt", "Disconnect during animated push"),
		("NavPush", "Push detail (hardware / toolbar back)"),
		("NavPop", "Pop detail"),
		("NavCancelBack", "Arm cancelled hardware back on detail"),
		("NavCommandBack", "Arm BackButtonBehavior command on detail"),
		("NavDrawer", "Open drawer (hardware back closes first)"),
		("NavRootBack", "Prepare root back (system should background app)"),
		("NavSnapshot", "Inspect current page, stack and handler"),
	};

	readonly List<MainPage> _pages = new();
	readonly HashSet<string> _created = new();
	ShellItem _itemA = null!;
	ShellItem _itemB = null!;
	ShellSection _shared = null!;
	ShellSection _independent = null!;
	ShellContent _a = null!;
	ShellContent _b = null!;
	string _status = "READY";
	bool _busy;
	bool _failed;
	int _generation;
	int _request;
	int _commandCount;
	ShellNavigationSource? _cancelNext;
	int _cancelCount;

	public SandboxShell()
	{
		InitializeComponent();
		Title = "Android navigation validation";
		FlyoutBehavior = FlyoutBehavior.Flyout;
		FlyoutHeader = new Label
		{
			AutomationId = "NavDrawerIdentity",
			Text = "Navigation harness drawer",
			Padding = 20
		};
		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(FlyoutIsPresented))
			{
				Record($"DRAWER presented={FlyoutIsPresented}");
				RefreshPages();
			}
		};
		Navigating += (_, e) =>
		{
			Record($"NAVIGATING {e.Source} {e.Target?.Location} canCancel={e.CanCancel}");
			if (_cancelNext == e.Source)
			{
				_cancelNext = null;
				if (e.Cancel())
				{
					_cancelCount++;
					Record($"CANCELLED {e.Source}");
					SetStatus($"CANCELLED {e.Source}");
				}
			}
		};
		Navigated += (_, e) => Record($"NAVIGATED {e.Source} {e.Current?.Location}");
		Setup();
	}

	void Setup()
	{
		_cancelNext = null;
		_failed = false;
		_generation++;
		_created.Clear();
		_pages.Clear();
		_a = LazyContent("A");
		_b = LazyContent("B");
		_shared = new ShellSection { Title = "Shared section", Route = $"shared{_generation}", Items = { _a, _b } };
		_independent = new ShellSection { Title = "Independent", Route = $"independent{_generation}", Items = { LazyContent("C") } };
		_itemA = new NavigationItemA { Title = "Item A", Route = $"itemA{_generation}", Items = { _shared, _independent } };
		_itemB = new NavigationItemB
		{
			Title = "Item B",
			Route = $"itemB{_generation}",
			Items = { new ShellSection { Title = "Other item", Items = { LazyContent("D") } } }
		};
		FlyoutIsPresented = false;
		Items.Clear();
		Items.Add(_itemA);
		Items.Add(_itemB);
		CurrentItem = _itemA;
		SetStatus("READY");
		Record($"SETUP generation={_generation}");
	}

	ShellContent LazyContent(string name) => new()
	{
		Title = name,
		Route = name + _generation,
		ContentTemplate = new DataTemplate(() =>
		{
			_created.Add(name);
			Record($"CREATED lazy root {name}");
			return Page(name);
		})
	};

	MainPage Page(string name)
	{
		var page = new MainPage(this, name);
		_pages.Add(page);
		page.Update(_status, Snapshot(), _failed);
		return page;
	}

	internal void Record(string message)
	{
		Console.WriteLine($"SANDBOX: {message}");
	}

	string Snapshot()
	{
		var section = CurrentItem?.CurrentItem;
		var stack = section?.Navigation.NavigationStack;
		return $"page={CurrentPage?.Title} | section={section?.Title} content={section?.CurrentItem?.Title} drawer={FlyoutIsPresented}\n" +
			$"stack=[{(stack is null ? "" : string.Join(", ", stack.Select(p => p?.Title ?? "<root>")))}] | " +
			$"itemHandler={CurrentItem?.Handler?.GetType().Name ?? "null"} | created={string.Join(",", _created.OrderBy(x => x))}";
	}

	void SetStatus(string status)
	{
		_status = status;
		RefreshPages();
		Record($"{status} | {Snapshot()}");
	}

	void RefreshPages()
	{
		var snapshot = Snapshot();
		foreach (var page in _pages)
			page.Update(_status, snapshot, _failed);
	}

	internal async Task RunAsync(string action)
	{
		if (_busy)
		{
			Record($"IGNORED busy {action}");
			return;
		}
		_busy = true;
		_failed = false;
		SetStatus($"RUNNING {action}");
		try
		{
			switch (action)
			{
				case "NavSetup": Setup(); await ExpectRoot(_a); break;
				case "NavSelfTest":
					Setup();
					await Ownership();
					await Rapid();
					await Reentrant();
					await Cancellation();
					await Handlers();
					await Reconnect(false);
					break;
				case "NavOwnership": Setup(); await Ownership(); break;
				case "NavRapid": await Home(); await Rapid(); break;
				case "NavReentrant": await Home(); await Reentrant(); break;
				case "NavCancel": await Home(); await Cancellation(); break;
				case "NavHandlers": await Home(); await Handlers(); break;
				case "NavReconnect": await Home(); await Reconnect(false); break;
				case "NavInterrupt": await Home(); await Reconnect(true); break;
				case "NavPush": await Track("manual push", Navigation.PushAsync(Page("Manual detail"), true)); break;
				case "NavPop": await Track("manual pop", Navigation.PopAsync(true)); break;
				case "NavCancelBack":
					await Home();
					await Track("back detail", Navigation.PushAsync(Page("Cancelled back detail"), false));
					_cancelNext = ShellNavigationSource.Pop;
					SetStatus("ARMED cancelled back: press hardware back; detail must remain");
					return;
				case "NavCommandBack":
					await Home();
					var detail = Page("Command back detail");
					_commandCount = 0;
					SetBackButtonBehavior(detail, new BackButtonBehavior
					{
						Command = new Command(() =>
						{
							_commandCount++;
							SetStatus($"BACK COMMAND {_commandCount}; detail must remain");
						})
					});
					await Track("command detail", Navigation.PushAsync(detail, false));
					SetStatus("ARMED command back: press hardware or toolbar back");
					return;
				case "NavDrawer":
					FlyoutIsPresented = true;
					SetStatus("ARMED drawer back: drawer must close without popping");
					return;
				case "NavRootBack":
					await Home();
					SetStatus("ARMED root back: system fallback; reopen app to continue");
					return;
				case "NavSnapshot": break;
				default: throw new InvalidOperationException($"Unknown action {action}");
			}
			SetStatus($"PASS {action}");
		}
		catch (Exception ex)
		{
			_failed = true;
			SetStatus($"FAIL {action}: {ex.Message}");
			Record($"ERROR {ex}");
		}
		finally
		{
			_busy = false;
		}
	}

	static void Require(bool condition, string message)
	{
		if (!condition)
			throw new InvalidOperationException(message);
	}

	static async Task Until(Func<bool> condition, string description)
	{
		var timer = Stopwatch.StartNew();
		while (!condition())
		{
			if (timer.Elapsed > TimeSpan.FromSeconds(10))
				throw new TimeoutException(description);
			await Task.Delay(50);
		}
	}

	static bool IsShown(Page page)
	{
#if ANDROID
		if ((page.Handler as IPlatformViewHandler)?.PlatformView is not AView view || !view.IsShown)
			return false;
		using var visible = new ARect();
		return view.GetGlobalVisibleRect(visible) && visible.Width() > 0 && visible.Height() > 0;
#else
		return page.Handler is not null;
#endif
	}

	async Task Expect(Page page, ShellSection section)
	{
		Require(ReferenceEquals(CurrentPage, page), $"CurrentPage must be {page.Title}");
		Require(ReferenceEquals(((IShellSectionController)section).PresentedPage, page), $"PresentedPage must be {page.Title}");
		await Until(() => IsShown(page), $"Native page not shown: {page.Title}");
		Record($"ASSERT presented={page.Title} nativeShown=true");
	}

	async Task ExpectRoot(ShellContent content)
	{
		var controller = (IShellContentController)content;
		await Until(() => controller.Page is not null, $"Root not created: {content.Title}");
		await Expect(controller.Page, (ShellSection)content.Parent);
	}

	async Task Track(string label, Task operation)
	{
		int id = ++_request;
		Record($"REQUEST {id} {label}");
		try
		{
			await operation.WaitAsync(TimeSpan.FromSeconds(10));
			Record($"COMPLETE {id} {label} {Snapshot()}");
		}
		catch (Exception ex)
		{
			Record($"OUTCOME {id} {label} {ex.GetType().Name}");
			throw;
		}
	}

	async Task Home()
	{
		_cancelNext = null;
		FlyoutIsPresented = false;
		CurrentItem = _itemA;
		_itemA.CurrentItem = _shared;
		await Track("home pop to root", _shared.Navigation.PopToRootAsync(false));
		_shared.CurrentItem = _a;
		await ExpectRoot(_a);
	}

	async Task Ownership()
	{
		await Home();
		Require(!_created.Contains("B"), "Fresh B was already created before selection");
		foreach (bool visited in new[] { false, true })
		{
			var detail = Page(visited ? "Visited detail" : "Unvisited detail");
			await Track("push before top-content switch", _shared.Navigation.PushAsync(detail, false));
			await Expect(detail, _shared);
			_shared.CurrentItem = _b;
			await Expect(detail, _shared);
			Require(_shared.Navigation.NavigationStack.Last() == detail, "Top-content switch lost section stack");
			await Track("pop after top-content switch", _shared.Navigation.PopAsync(false));
			await ExpectRoot(_b);
			Require(_created.Contains("B"), "B must be created after pop");
			_shared.CurrentItem = _a;
			await ExpectRoot(_a);
			Record($"ASSERT section ownership visited={visited}");
		}

		var retained = Page("Retained section detail");
		await Track("independent section setup", _shared.Navigation.PushAsync(retained, false));
		_itemA.CurrentItem = _independent;
		await ExpectRoot(_independent.CurrentItem);
		Require(_independent.Navigation.NavigationStack.Count == 1, "Independent section inherited detail");
		_itemA.CurrentItem = _shared;
		await Expect(retained, _shared);
		await Home();
	}

	async Task Rapid()
	{
		var pages = Enumerable.Range(1, 3).Select(i => Page($"Rapid {i}")).ToArray();
		// Submit before awaiting; each task gets its own request/completion marker.
		var pushes = pages.Select(p => Track("rapid push " + p.Title, _shared.Navigation.PushAsync(p, true))).ToArray();
		await Task.WhenAll(pushes);
		Require(_shared.Navigation.NavigationStack.Skip(1).SequenceEqual(pages), "Rapid push order differs");
		await Expect(pages[2], _shared);
		var pops = new List<Task>();
		for (int i = 0; i < pages.Length; i++)
			pops.Add(PopExpected(pages[pages.Length - 1 - i]));
		await Task.WhenAll(pops);
		await ExpectRoot(_a);
		Require(_shared.Navigation.NavigationStack.Count == 1, "Rapid pop left details");
	}

	async Task PopExpected(Page expected)
	{
		var operation = _shared.Navigation.PopAsync(true);
		await Track("rapid pop " + expected.Title, operation);
		Require(await operation == expected, $"Pop returned wrong page: {expected.Title}");
	}

	async Task Reentrant()
	{
		var first = Page("Completion first");
		var second = Page("Completion second");
		await Track("completion first", Navigation.PushAsync(first, true));
		await Expect(first, _shared);
		await Track("push from await continuation", Navigation.PushAsync(second, true));
		await Expect(second, _shared);
		await Track("completion pop second", Navigation.PopAsync(true));
		await Expect(first, _shared);
		await Track("pop from await continuation", Navigation.PopAsync(true));
		await ExpectRoot(_a);

		var appearing = Page("Appearing trigger");
		var target = Page("Appearing target");
		Task? nested = null;
		bool fired = false;
		appearing.Appearing += (_, _) =>
		{
			if (fired)
				return;
			fired = true;
			nested = Track("push from Appearing", Navigation.PushAsync(target, true));
		};
		await Track("appearing trigger", Navigation.PushAsync(appearing, true));
		Require(fired && nested is not null, "Appearing did not initiate navigation");
		await nested!;
		await Expect(target, _shared);
		Require(_shared.Navigation.NavigationStack.Skip(1).SequenceEqual(new[] { appearing, target }), "Reentrant stack order differs");
		await Home();

		var popBase = Page("Reentrant pop base");
		var popTop = Page("Reentrant pop top");
		await Track("reentrant pop setup base", Navigation.PushAsync(popBase, false));
		await Track("reentrant pop setup top", Navigation.PushAsync(popTop, false));
		Task? nestedPop = null;
		bool popped = false;
		popBase.Appearing += (_, _) =>
		{
			if (popped)
				return;
			popped = true;
			nestedPop = Track("pop from Appearing", Navigation.PopAsync(true));
		};
		await Track("reveal reentrant pop base", Navigation.PopAsync(true));
		Require(popped && nestedPop is not null, "Reappearing page did not initiate pop");
		await nestedPop!;
		await ExpectRoot(_a);
	}

	async Task Cancellation()
	{
		int before = _cancelCount;
		_cancelNext = ShellNavigationSource.Push;
		await Track("cancelled push", Navigation.PushAsync(Page("Must not appear"), false));
		Require(_cancelCount == before + 1, "Push was not cancelled");
		await ExpectRoot(_a);
		Require(_shared.Navigation.NavigationStack.Count == 1, "Cancelled push changed stack");

		var detail = Page("Cancel pop detail");
		await Track("cancel setup", Navigation.PushAsync(detail, false));
		_cancelNext = ShellNavigationSource.Pop;
		await Track("cancelled pop", Navigation.PopAsync(false));
		Require(_cancelCount == before + 2, "Pop was not cancelled");
		await Expect(detail, _shared);
		await Home();

		_cancelNext = ShellNavigationSource.ShellContentChanged;
		// Direct CurrentItem assignment is intentionally non-cancellable; routing is cancellable.
		await Track("cancelled top-content route",
			GoToAsync($"//{_itemA.Route}/{_shared.Route}/{_b.Route}", false));
		Require(_cancelCount == before + 3 && _shared.CurrentItem == _a, "Cancelled top-content selection changed content");
		await ExpectRoot(_a);
	}

	async Task Handlers()
	{
		foreach (var item in new[] { _itemA, _itemB, _itemA })
		{
			CurrentItem = item;
			await ExpectRoot(item.CurrentItem.CurrentItem);
#if ANDROID
			var expected = item is NavigationItemA ? typeof(NavigationItemHandlerA) : typeof(NavigationItemHandlerB);
			Require(item.Handler?.GetType() == expected, $"Destination {item.Title}: expected {expected.Name}, got {item.Handler?.GetType().Name}");
#endif
			Record($"ASSERT destination handler {item.Title}: {item.Handler?.GetType().Name ?? "null"}");
		}
	}

	async Task Reconnect(bool interrupt)
	{
		var window = Window ?? throw new InvalidOperationException("No Window");
		var oldHandler = Handler ?? throw new InvalidOperationException("No Shell handler");
		var detail = Page(interrupt ? "Interrupted detail" : "Reconnect retained detail");
		var pending = ObserveTransition(detail, Navigation.PushAsync(detail, interrupt), interrupt);
		if (!interrupt)
			await pending;
		window.Page = new ContentPage { Content = new Label { Text = "SANDBOX reconnecting Shell" } };
		oldHandler.DisconnectHandler();
		Handler = null;
		try
		{
			await Until(() => !IsLoaded, "Shell did not unload before reconnect");
			await pending.WaitAsync(TimeSpan.FromSeconds(10));
		}
		finally
		{
			window.Page = this;
		}
		await Until(() => Handler is not null && !ReferenceEquals(Handler, oldHandler), "Shell did not acquire a new handler");
		var expected = ((IShellSectionController)_shared).PresentedPage;
		await Expect(expected, _shared);
		if (!interrupt)
			Require(_shared.Navigation.NavigationStack.Contains(detail), "Reconnect lost managed detail");
		else
			Require(expected == detail || expected == ((IShellContentController)_a).Page,
				"Interrupted reconnect presented an unexpected managed destination");
		await Home();
	}

	async Task ObserveTransition(Page detail, Task operation, bool allowInterruption)
	{
		try
		{
			await Track("transition before disconnect", operation);
		}
		catch (Exception ex) when (allowInterruption && ex is not TimeoutException)
		{
			Record($"INTERRUPTED outcome={ex.GetType().Name}");
			return;
		}
		// Sample immediately at completion, not after reconnect, to reject false success.
		Require(CurrentPage == detail && IsShown(detail), "Transition completed successfully without showing its destination");
		Record("ASSERT transition success applied before teardown");
	}
}
