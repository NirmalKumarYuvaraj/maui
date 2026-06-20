# Plan

Goal: Refactor the Android window inset handling architecture so that a CoordinatorLayout owns exactly one MauiWindowInsetListener and child views do not register their own Android OnApplyWindowInsetsListener instances.

Current State:

- MauiWindowInsetListener can be attached to many child views.
- Child views locate a listener through:
    - RegisterView
    - UnregisterView
    - FindListenerForView
    - RegisterParentForChildViews
    - \_registeredViews static registry

- Each child ultimately receives Android inset callbacks even when the same listener instance is reused.
- This creates listener management complexity, hierarchy walking, and static state.

Target Architecture:

- One MauiWindowInsetListener per CoordinatorLayout.
- Listener attached only to CoordinatorLayout.
- LayoutViewGroup, ContentViewGroup, and NestedScrollView should not have Android inset listeners attached.
- CoordinatorLayout receives WindowInsets once and dispatches them internally to interested children.
- Remove the need for child listener discovery and listener registration lookup.
- Preserve existing IME animation support through WindowInsetsAnimationCompat.Callback.

Requirements:

1. Introduce an inset consumer abstraction:

```csharp
internal interface IInsetConsumer
{
    void ApplyInsets(WindowInsetsCompat insets);
    void ResetInsets();
}
```

2. MauiWindowInsetListener should maintain a collection of inset consumers.

Example:

```csharp
readonly HashSet<IInsetConsumer> _consumers;
```

3. When CoordinatorLayout receives OnApplyWindowInsets:
    - Store the latest WindowInsetsCompat.
    - Dispatch the insets to all registered consumers.
    - Continue existing CoordinatorLayout-specific inset logic.
    - Preserve IME animation behavior.

4. Support late registration:
    - If a consumer registers after insets were already received,
      immediately apply the last known insets.

Example:

```csharp
WindowInsetsCompat? _lastInsets;
```

5. Remove or deprecate:
    - \_registeredViews
    - ViewEntry
    - RegisterView
    - UnregisterView
    - FindListenerForView
    - RegisterParentForChildViews
    - SetMauiWindowInsetListenerForChildView
    - RemoveMauiWindowInsetListenerForChildView

6. Child view responsibilities:
    - LayoutViewGroup implements IInsetConsumer.
    - ContentViewGroup implements IInsetConsumer.
    - NestedScrollView (or Maui wrapper) implements IInsetConsumer.
    - Children apply padding/margin adjustments based on dispatched insets.
    - Children no longer receive Android OnApplyWindowInsets callbacks directly.

7. Preserve cleanup:
    - Existing ResetView / ResetAllViews behavior should be adapted for consumer reset.
    - Avoid memory leaks.
    - Consumers must unregister when disposed or detached.

8. Investigate inset propagation semantics:
   Determine whether current behavior depends on inset consumption ordering.

Questions:

- Does LayoutViewGroup consume top insets before ContentViewGroup sees them?
- Do all consumers need the original WindowInsetsCompat?
- Is current behavior effectively a broadcast model or a consumption pipeline?

If consumption ordering is required, propose a chained dispatcher:

```csharp
WindowInsetsCompat current = insets;

foreach (var consumer in consumers)
{
    current = consumer.ApplyInsets(current);
}
```

Otherwise use a broadcast dispatcher:

```csharp
foreach (var consumer in consumers)
{
    consumer.ApplyInsets(insets);
}
```

Deliverables:

1. Proposed class diagram.
2. Refactoring steps.
3. API changes.
4. Migration strategy.
5. Example implementation snippets.
6. Risks and compatibility concerns.
7. Recommendation on broadcast vs consumption pipeline based on current code.

---

# Analysis & Findings

> Status: analysis only — no code changed. File/line citations refer to the current `main` working tree.

## Key finding (drives every decision)

The current design is **already a hierarchical consumption pipeline** — but the chaining is performed by **Android's native top-down inset dispatch**, not by the listener itself. The same `MauiWindowInsetListener` instance is attached to the `CoordinatorLayout` **and** to each child via `ViewCompat.SetOnApplyWindowInsetsListener`. Android walks the view tree: the parent's listener fires → returns transformed insets → Android dispatches _those_ insets to the children → each child's listener fires.

- Attach points: `MauiWindowInsetListener.cs:146-147` (`SetupViewWithLocalListener`), `:467-468` (`SetMauiWindowInsetListenerForChildView`).
- Child attach call sites: `LayoutViewGroup.cs:63`, `ContentViewGroup.cs:65`, `MauiScrollView.cs:64`, `ToolbarHandler.Android.cs:52`.

Two hard ordering dependencies prove this is **not** a flat broadcast:

1. `ApplyDefaultWindowInsets` **consumes the top inset** when the AppBar has content, _before_ descendants see it. The inline comment states that otherwise `SafeAreaExtensions` will **double-apply** the top inset (`MauiWindowInsetListener.cs:289-308`).
2. `ApplyAdjustedSafeAreaInsetsPx` transforms insets **per view** based on each view's on-screen position, then returns the partially-consumed insets for that view's subtree (`SafeAreaExtensions.cs:248-308`).

A flat `HashSet<IInsetConsumer>` + broadcast (spec requirement #2 + the broadcast snippet) would **lose** parent-before-child ordering and per-subtree consumption, producing visible double-padding regressions.

## 7. Recommendation: pipeline vs broadcast

**Use a hierarchical (tree) consumption pipeline, dispatched by walking the live `CoordinatorLayout` subtree** — not a flat HashSet, and not a single linear chain.

- A **linear** chain (`current = consumer.ApplyInsets(current)`) is also incorrect: the view tree is a _tree_, so sibling subtrees must each independently receive their _parent's_ output. One linear list cannot model two sibling branches.
- On dispatch, **walk the CoordinatorLayout's descendants top-down**, threading each node's returned insets into _its_ children — exactly what Android does today. This keeps ordering correct regardless of registration order and avoids registration/unregister races.
- Consequence: `IInsetConsumer.ApplyInsets` **must return** `WindowInsetsCompat?`. The spec's `void` signature loses consumption and is insufficient.

Answers to the spec's open questions:

- **Does LayoutViewGroup consume top insets before ContentViewGroup sees them?** Not those two specifically. The real dependency is **CoordinatorLayout/AppBar consumes top → every descendant below it**. Ordering is general parent→descendant, enforced by tree position.
- **Do all consumers need the original WindowInsetsCompat?** **No.** Each needs the **ancestor-transformed** insets (top already removed by the AppBar). Passing everyone the original re-introduces double-application.
- **Is current behavior broadcast or consumption pipeline?** A **hierarchical consumption pipeline** realized through Android's native dispatch.

## 1. Proposed class diagram

```
WindowInsetsAnimationCompat.Callback, IOnApplyWindowInsetsListener
        ▲
        │ owns
MauiWindowInsetListener  (ONE per CoordinatorLayout)
  - _consumers   : ordered set / live subtree walk
  - _lastInsets  : WindowInsetsCompat?     // late registration (req. #4)
  - _trackedViews                          // keep (used by SafeAreaExtensions)
  + Register(IInsetConsumer) / Unregister(IInsetConsumer)
  + OnApplyWindowInsets(v, insets) → AppBar/BottomNav logic, then Dispatch()

IInsetConsumer  (internal)
  + WindowInsetsCompat? ApplyInsets(WindowInsetsCompat insets)   // returns, not void
  + void ResetInsets()
        ▲                 ▲                  ▲
  LayoutViewGroup   ContentViewGroup   MauiScrollView    (replace IHandleWindowInsets)
```

## 2. Refactoring steps

1. Add `IInsetConsumer` (returns `WindowInsetsCompat?`). Migrate the 3 `IHandleWindowInsets` implementers (`LayoutViewGroup.cs:16`, `ContentViewGroup.cs:15`, `MauiScrollView.cs:16`); their bodies are nearly identical and all delegate to `SafeAreaExtensions.ApplyAdjustedSafeAreaInsetsPx`.
2. In `MauiWindowInsetListener`, add `_consumers` + `_lastInsets`; add `Register`/`Unregister`; on register, immediately apply `_lastInsets` (req. #4).
3. In `OnApplyWindowInsets`, after the CoordinatorLayout-level `ApplyDefaultWindowInsets`, **dispatch** to consumers by walking the subtree top-down. Stop attaching listeners to children.
4. Children stop attaching listeners: remove `SetMauiWindowInsetListenerForChildView` calls in `OnAttachedToWindow` (`LayoutViewGroup.cs:63`, `ContentViewGroup.cs:65`, `MauiScrollView.cs:64`, `ToolbarHandler.Android.cs:52`); instead register as `IInsetConsumer` with the owning CoordinatorLayout's listener.
5. Remove `_registeredViews`, `ViewEntry`, `RegisterView`, `UnregisterView`, `FindListenerForView`, `RegisterParentForChildViews`, `SetMauiWindowInsetListenerForChildView`, `RemoveMauiWindowInsetListenerForChildView` (req. #5).
6. Update the 5 `SetupViewWithLocalListener` call sites and the reset path in `ViewHandler.Android.cs:271`.

## 3. API changes (all `internal` — no public surface)

- **Add:** `IInsetConsumer`; `MauiWindowInsetListener.Register`/`Unregister`.
- **Remove:** the 8 members in req. #5 + the `MauiWindowInsetListenerExtensions` child/root helpers.
- **Change:** consumer reset uses `IInsetConsumer.ResetInsets()` instead of `ResetView`/`ResetAppliedSafeAreas`.
- ✅ No `PublicAPI.Unshipped.txt` impact — everything here is `internal`.

## 4. Migration strategy

- The owning listener must be discoverable by children without `FindListenerForView`. Cleanest: child finds its ancestor `CoordinatorLayout` in `OnAttachedToWindow` and calls `listener.Register(this)`; `OnDetachedFromWindow` → `Unregister`.
- **Coupling to surface:** `SafeAreaExtensions.cs:65` itself calls `FindListenerForView(view)` to read `TrackView`/`HasTrackedView`/`IsViewTracked`. So either `ApplyInsets` receives the owning listener, or the consumer holds a reference to it.
- 5 CoordinatorLayouts create listeners and must be updated: `NavigationRootManager.cs:84`, `FlyoutViewHandler.Android.cs:302`, `ShellSectionRenderer.cs:108`, `ShellContentFragment.cs:139`, `ShellFlyoutTemplatedContentRenderer.cs:211`.

## 5. Example implementation snippets

```csharp
internal interface IInsetConsumer
{
    WindowInsetsCompat? ApplyInsets(WindowInsetsCompat insets); // returns transformed insets
    void ResetInsets();
}

// In MauiWindowInsetListener
WindowInsetsCompat? _lastInsets;

public void Register(IInsetConsumer c)
{
    if (_consumers.Add(c) && _lastInsets is not null)
        c.ApplyInsets(_lastInsets);     // req. #4 late registration
}

// dispatch = walk subtree top-down so each node receives its ANCESTORS' output
WindowInsetsCompat Dispatch(AView v, WindowInsetsCompat insets)
{
    if (v is IInsetConsumer c)
        insets = c.ApplyInsets(insets) ?? insets;

    if (v is ViewGroup g)
        for (int i = 0; i < g.ChildCount; i++)
            Dispatch(g.GetChildAt(i), insets); // each subtree gets parent's output

    return insets;
}
```

## 6. Risks & compatibility concerns

- 🔴 **MaterialToolbar is a 4th consumer the spec omits.** It is _not_ `IHandleWindowInsets` — it is handled inline in `ApplyDefaultWindowInsets` (`MauiWindowInsetListener.cs:220-224`) yet attaches the listener via `ToolbarHandler.Android.cs:52`. The new dispatch must still special-case it.
- 🟠 **Shared `_trackedViews` state** powers the "first view to pad" optimization (`SafeAreaExtensions.cs:107,294`). It must continue to live on the listener.
- 🟠 **API < 30 workaround** `ViewGroupCompat.InstallCompatInsetsDispatch` (`NavigationRootManager.cs:104`) exists _because_ Android dispatches to multiple child listeners. A single-listener + manual subtree walk may make it **unnecessary** (a potential simplification) — but must be verified on API 28–29.
- 🟠 **IME animation** uses an `OnProgress`/`_pendingView` single-view model (`MauiWindowInsetListener.cs:411-419`). It must be redirected to consumer dispatch.
- 🟠 **FlyoutView DrawerLayout** registers a non-CoordinatorLayout parent (`FlyoutViewHandler.Android.cs:298`) — special-case this during removal/migration.
- 🟢 **Low test coupling:** only 3 `IHandleWindowInsets` implementers; no unit tests bind to the registry API directly.
