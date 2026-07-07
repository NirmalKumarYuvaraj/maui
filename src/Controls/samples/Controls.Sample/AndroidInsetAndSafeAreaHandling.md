# Android Window‑Inset Handling & `SafeAreaEdges` — Architecture Summary

> Scope: a deep walk‑through of how .NET MAUI turns Android **window insets**
> (status bar, navigation bar, display cutout/notch, IME/keyboard) into view
> padding, and how the cross‑platform **`SafeAreaEdges`** API drives that behavior.
> File/line references point at `main` at the time of writing.

---

## 1. The big picture

Since Android 15 (API 35) forces **edge‑to‑edge**, MAUI opts every window into
edge‑to‑edge itself and then re‑applies the insets it wants, per view, per edge.
The flow has three layers:

```
 ┌────────────────────────────────────────────────────────────────────┐
 │  Cross-platform API  (Microsoft.Maui / Microsoft.Maui.Controls)     │
 │    SafeAreaRegions (enum)  ─►  SafeAreaEdges (struct, 4 edges)       │
 │    ISafeAreaElement / ISafeAreaView2 / ISafeAreaView (legacy)        │
 │    Controls: ContentPage, Layout, ScrollView, Border, ContentView   │
 └───────────────▲──────────────────────────────────┬─────────────────┘
                 │ property mapper                   │ GetSafeAreaRegionsForEdge(edge)
 ┌───────────────┴──────────────────────────────────▼─────────────────┐
 │  Handler layer                                                      │
 │    ViewHandler.MapSafeAreaEdges (Android/iOS)                       │
 └───────────────▲──────────────────────────────────┬─────────────────┘
                 │ MarkSafeAreaEdgeConfigurationChanged                │
 ┌───────────────┴──────────────────────────────────▼─────────────────┐
 │  Android platform layer                                             │
 │    MauiAppCompatActivity → SetDecorFitsSystemWindows(false)         │
 │    MauiWindowInsetListener (IOnApplyWindowInsetsListener + IME anim) │
 │    IHandleWindowInsets  →  ContentViewGroup / LayoutViewGroup /     │
 │                            MauiScrollView                           │
 │    SafeAreaExtensions.ApplyAdjustedSafeAreaInsetsPx (core algorithm) │
 └────────────────────────────────────────────────────────────────────┘
```

---

## 2. Cross‑platform API surface

### 2.1 `SafeAreaRegions` (flags enum)
`src/Core/src/Primitives/SafeAreaRegions.cs`

| Member | Value | Meaning |
|--------|-------|---------|
| `None` | `0` | Edge‑to‑edge; **no** safe‑area padding. |
| `SoftInput` | `1 << 0` | Pad so content stays above the keyboard/IME. |
| `Container` | `1 << 1` | Content flows under the keyboard but stays out of bars/notch. |
| `Default` | `-1` | Apply platform default behavior (all bits set). |
| `All` | `1 << 15` | Obey **all** insets — bars, notch **and** keyboard. |

### 2.2 `SafeAreaEdges` (readonly struct)
`src/Core/src/Primitives/SafeAreaEdges.cs`

- Holds one `SafeAreaRegions` per edge: `Left`, `Top`, `Right`, `Bottom`.
- Constructors mirror `Thickness`: uniform, `(horizontal, vertical)`, or per‑edge.
- Static presets: `Default`, `None`, `All`, and internal `Container`.
- Internal helpers used by the platform math:
  - `IsSoftInput(region)` / `IsOnlySoftInput(region)` — `Default` returns **false**, `All` returns **true**, otherwise a flag test.
  - `IsContainer(region)` — same special‑casing for `Default`/`All`.
  - `GetEdge(int edge)` — `0=Left, 1=Top, 2=Right, 3=Bottom`.

### 2.3 `SafeAreaEdgesTypeConverter` (XAML)
`src/Core/src/Converters/SafeAreaEdgesTypeConverter.cs`

Parses `1`, `2`, or `4` comma‑separated tokens (`All`, `None`, `Container`,
`SoftInput`, `Default`) → `SafeAreaEdges`. `2` values = horizontal/vertical,
`4` = left/top/right/bottom. Uses `string.Equals` instead of `Enum.TryParse`
for perf. Design‑time twin: `Controls.Core.Design/SafeAreaEdgesTypeDesignConverter.cs`.

### 2.4 Interfaces
- **`ISafeAreaElement`** (`src/Core/src/Core/ISafeAreaElement.cs`, internal) — the bindable `SafeAreaEdges` property + `SafeAreaEdgesDefaultValueCreator()`.
- **`ISafeAreaView2`** (`src/Core/src/Core/ISafeAreaView2.cs`, internal) — the contract the platform actually consumes:
  - `bool HasExplicitSafeAreaEdges` — has the developer explicitly set the value?
  - `Thickness SafeAreaInsets { set; }` — report resolved insets back (used by iOS page).
  - `SafeAreaRegions GetSafeAreaRegionsForEdge(int edge)` — resolved region per edge.
- **`ISafeAreaView`** (`src/Core/src/Core/ISafeAreaView.cs`, public, **legacy**) — the old boolean `IgnoreSafeArea`. Historically iOS/Mac‑only; retained for back‑compat.

### 2.5 `SafeAreaElement` helper (Controls)
`src/Controls/src/Core/SafeAreaElement.cs`

Owns the shared `SafeAreaEdgesProperty` `BindableProperty` (default
`SafeAreaEdges.Default`, with a `defaultValueCreator` so each control can pick
its own default). Provides `GetEdgeValue` and `ShouldObeySafeAreaForEdge`.

---

## 3. Per‑control defaults & semantics

Each control implements `ISafeAreaView2` and supplies its own default via
`SafeAreaEdgesDefaultValueCreator()`:

| Control | Default | `GetSafeAreaRegionsForEdge` behavior | File |
|---------|---------|--------------------------------------|------|
| `Page` (base) | n/a | `HasExplicitSafeAreaEdges => false`; maps legacy `IgnoreSafeArea` → `None`/`Container`. | `Page/Page.cs:257‑281` |
| `ContentPage` | `None` | Explicit value wins; else on **iOS** falls back to legacy `IgnoreSafeArea`, on **Android** returns `None` (edge‑to‑edge). | `ContentPage/ContentPage.cs:173‑211` |
| `Layout` | `Container` | Explicit value wins; `Default` edge → `Container` (or `None` if obsolete `IgnoreSafeArea`). | `Layout/Layout.cs:372‑442` |
| `ScrollView` | `Default` | Explicit value wins; keyboard‑aware. | `ScrollView/ScrollView.cs:554‑` |
| `Border` | `None` | Explicit value wins. | `Border/Border.cs:469‑497` |
| `ContentView` | `None` | Explicit value wins. | `ContentView/ContentView.cs:80‑` |

`HasExplicitSafeAreaEdges` is almost always `IsSetExplicitly(SafeAreaEdgesProperty)`
— it distinguishes "developer set a value" from "default value creator ran". This
flag gates several platform decisions (e.g. whether a RecyclerView item is even
allowed to install an inset listener).

---

## 4. Android entry point — edge‑to‑edge

`src/Core/src/Platform/Android/MauiAppCompatActivity.cs:30`

```csharp
WindowCompat.SetDecorFitsSystemWindows(Window, false);
```

This disables the platform's automatic fitting of system windows, so the app
draws behind the status/navigation bars and MAUI becomes responsible for all
inset application. (`MauiWebChromeClient` toggles the same flag around fullscreen
video.)

---

## 5. `MauiWindowInsetListener` — the dispatcher

`src/Core/src/Platform/Android/MauiWindowInsetListener.cs`

A single class that is both an `IOnApplyWindowInsetsListener` **and** a
`WindowInsetsAnimationCompat.Callback`. It can act as one listener per activity
or, more commonly now, as **local listeners attached to specific view subtrees**
for isolation in complex navigation.

### 5.1 Local‑listener registry
- Static `List<ViewEntry> _registeredViews` where `ViewEntry(WeakReference<object> View, MauiWindowInsetListener Listener)` — weak refs avoid leaks.
- `RegisterView` / `UnregisterView` — add/remove, pruning dead refs on the way.
- `FindRegisteredListenerForView(view)` — walks **up** the parent chain to find the owning listener.
- `SetupViewWithLocalListener(view, listener?)` — creates a listener, wires `ViewCompat.SetOnApplyWindowInsetsListener` + `SetWindowInsetsAnimationCallback`, registers the view.
- `RegisterParentForChildViews(parent, listener?)` — registers a parent so *children* can find a listener without the parent itself consuming insets.

### 5.2 Where local listeners are installed
| Location | Purpose |
|----------|---------|
| `NavigationRootManager.cs:84` | `navigationlayout` `CoordinatorLayout` (the default content root). |
| Shell: `ShellSectionRenderer.cs:108`, `ShellContentFragment.cs:139`, `ShellFlyoutTemplatedContentRenderer.cs:212` | Shell content roots + flyout. |
| `FlyoutViewHandler.Android.cs:305,309` | Registers parent + local listener for FlyoutPage. |

### 5.3 Listener‑suppression rules — `ShouldSetMauiWindowInsetListener`
Walking up the tree, a view is **denied** its own listener (returns `false`) when
it lives inside an `AppBarLayout`, a `MauiScrollView`, or an `IMauiRecyclerView`
(unless it's a recycler *empty* view or has an **explicit** `SafeAreaEdges`).
`MaterialToolbar` is always exempt because it manages its own cutout padding.
This prevents double‑application and keeps scroll/recycler children edge‑to‑edge.

### 5.4 `OnApplyWindowInsets`
`MauiWindowInsetListener.cs:~230`
1. Bail out (return insets unchanged) if there are no insets, no view, or an IME
   animation is in flight (remembering the view as `_pendingView`).
2. If the view implements **`IHandleWindowInsets`**, delegate to it.
3. Otherwise run `ApplyDefaultWindowInsets`.

### 5.5 `ApplyDefaultWindowInsets` (navigation chrome)
Handles the standard NavigationPage/Shell chrome:
- **`MaterialToolbar`** → left/right **display‑cutout** padding only, then `Consumed`.
- Finds the `AppBarLayout` (by id or first/second child). If it has content, pads it with `max(systemBar.Top, cutout.Top)` (status bar + notch).
- Finds the bottom‑tab container; if present, pads the **content view's bottom** by `max(systemBar.Bottom, cutout.Bottom)` so content doesn't slide under the `BottomNavigationView` + nav bar.
- **Consumes the top inset** when the AppBar is visible (it already pads itself) so downstream `SafeAreaExtensions` won't double‑apply. **Bottom is passed through unconsumed** so the BottomNavigationView can extend its background into the nav‑bar region (issue #33344).

---

## 6. `IHandleWindowInsets` + the platform view groups

`src/Core/src/Platform/Android/IHandleWindowInsets.cs`

Views that want to run the *per‑edge safe‑area* algorithm implement this:
`WindowInsetsCompat? HandleWindowInsets(view, insets)` and `ResetWindowInsets(view)`.

Three view groups implement it, all following the same template:

| View group | Backs | File |
|------------|-------|------|
| `ContentViewGroup` | `IContentView` (ContentPage, Border, ContentView…) | `Platform/Android/ContentViewGroup.cs` |
| `LayoutViewGroup` | `Layout` | `Platform/Android/LayoutViewGroup.cs` |
| `MauiScrollView` | `ScrollView` | `Platform/Android/MauiScrollView.cs` |

Common template:
- On **attach** → `TrySetMauiWindowInsetListener` (a `ContentViewGroup` skips this if it's inside a `MauiScrollView`, which owns insets).
- On **detach** → `RemoveMauiWindowInsetListener`, reset flags.
- `HandleWindowInsets` → stores the **original padding** once, then calls `SafeAreaExtensions.ApplyAdjustedSafeAreaInsetsPx(insets, CrossPlatformLayout, context, view)`.
- `ResetWindowInsets` → restore the original padding.
- **`OnLayout`** → if the safe‑area config changed and a listener is set, call `ViewCompat.RequestApplyInsets(this)` and clear the flag. This is how a new inset pass is scheduled after layout.
- **`OnConfigurationChanged`** (rotation) → reset the view on its listener and re‑mark config changed.
- **`MarkSafeAreaEdgeConfigurationChanged`** → `RefreshMauiWindowInsetListener` (re‑evaluates eligibility, may detach + reset), set the dirty flag, `RequestLayout()`.

Measure/arrange in these groups account for the padding the algorithm applies
(`OnMeasure`/`OnLayout` subtract `PaddingLeft/Top/Right/Bottom`), so safe‑area
padding correctly shrinks the content area.

---

## 7. The core algorithm — `ApplyAdjustedSafeAreaInsetsPx`

`src/Core/src/Platform/Android/SafeAreaExtensions.cs`

This is where per‑edge `SafeAreaRegions` becomes actual pixel padding. Steps:

1. **Gather insets**
   - `baseSafeArea = max(systemBars, displayCutout)` per edge (`WindowInsetsExtensions.ToSafeAreaInsetsPx`).
   - `keyboardInsets` from the IME type; `isKeyboardShowing = !keyboardInsets.IsEmpty`.
   - `margins` from the virtual view (safe area and margins are made **additive**, not overlapping).

2. **Resolve per‑edge desired inset** via `GetSafeAreaForEdge(GetSafeAreaRegionForEdge(edge, layout), …)` for L/T/R/B. `GetSafeAreaRegionForEdge` calls `ISafeAreaView2.GetSafeAreaRegionsForEdge` (or the legacy `ISafeAreaView` fallback → `Container`/`None`).

3. **Pass‑through optimization** — if all four desired insets are `0` **and** the view isn't already tracked, return the insets **unconsumed** so child views with their own `SafeAreaEdges` can handle them.

4. **AdjustPan guard** — if the keyboard is showing and the window's soft‑input mode (masked with `SoftInput.MaskAdjust`) is `AdjustPan`, the window pans instead of resizing; the method consumes the insets and returns without changing padding.

5. **Overlap detection (the clever part)** — using `GetLocationOnScreen` and the real display metrics, it computes how much the view **actually** extends into each unsafe region and pads only that much. This avoids over‑padding a view that is already partly clear of a bar/notch. Margins are subtracted from the view rect first (only after a real layout pass, i.e. when `Width/Height > 0`).
   - **Animation heuristics**: during Shell fragment/tab transitions the view slides in from off‑screen. Horizontal (`viewLeft < 0 || viewRight > screenWidth`) and vertical (`viewTop > top && viewTop > 0 && viewBottom > screenHeight`) checks are evaluated **before** clamping so the full inset is applied (the view will settle at origin) instead of a wrong partial overlap.

6. **Rebuild insets & apply** — a `WindowInsetsCompat.Builder` zeroes only the edges that were consumed for `SystemBars`, `DisplayCutout`, and (when handled) `Ime`. Then `view.SetPadding((int)left,(int)top,(int)right,(int)bottom)`. If any padding was applied, the view is registered via `listener.TrackView(view)` so it can later be reset.

### 7.1 Region → inset mapping — `GetSafeAreaForEdge`
- `None` → `0`.
- **Bottom edge (`edge == 3`)**:
  - `IsOnlySoftInput` → keyboard height when showing, else `0`.
  - Keyboard showing **and** region includes `SoftInput` → keyboard height.
- Everything else (`Default`, `All`, `Container`, combos) → the original system inset. So `Container` keeps content out of bars/notch **but** lets it flow under the keyboard (its bottom uses the *bar* inset, not the keyboard).

### 7.2 `SafeAreaPadding` + `WindowInsetsExtensions`
`src/Core/src/Platform/Android/SafeAreaPadding.cs`
- `SafeAreaPadding(Left,Right,Top,Bottom)` readonly struct + `Empty`.
- `ToSafeAreaInsetsPx` → `max(systemBars, displayCutout)` per edge (cutout requires API 28+).
- `GetKeyboardInsetsPx` → IME bottom (API 30+).

---

## 8. IME / keyboard handling

Two paths cooperate:
- **Static insets** — the bottom edge of the algorithm above returns the keyboard height for `SoftInput`/`All` regions.
- **Animated insets** — `MauiWindowInsetListener` overrides `OnPrepare`/`OnStart`/`OnProgress`/`OnEnd`. While `IsImeAnimating`, `OnApplyWindowInsets` is short‑circuited and `OnProgress` drives `ApplyImeInsets`, which pads the **root view's bottom** frame‑by‑frame. On `OnEnd` it re‑posts `RequestApplyInsets` on the pending view so the final resting state is recomputed. `ViewExtensions.IsSoftInputShowing` reads `WindowInsetsCompat.Type.Ime()` visibility for callers that need a simple boolean.

> ⚠️ **This path has several known flaws** — see **§16 “Keyboard / IME handling — deep dive & known flaws.”**

---

## 9. Property‑change flow (round trip)

```
XAML / code sets SomeControl.SafeAreaEdges
      │  BindableProperty change
      ▼
ViewHandler mapper table  (ViewHandler.cs:82, ANDROID || IOS)
      │  nameof(ISafeAreaElement.SafeAreaEdges) → MapSafeAreaEdges
      ▼
ViewHandler.Android.MapSafeAreaEdges  (ViewHandler.Android.cs:258)
      │  find listener; ResetAppliedSafeAreas(platformView)
      │  platformView.MarkSafeAreaEdgeConfigurationChanged()
      │  view.InvalidateMeasure()
      ▼
ContentViewGroup / LayoutViewGroup / MauiScrollView
      │  RefreshMauiWindowInsetListener + dirty flag + RequestLayout
      ▼
OnLayout → ViewCompat.RequestApplyInsets(this)
      ▼
MauiWindowInsetListener.OnApplyWindowInsets → IHandleWindowInsets.HandleWindowInsets
      ▼
SafeAreaExtensions.ApplyAdjustedSafeAreaInsetsPx  → SetPadding
```

`MapSafeAreaEdges` early‑returns while the handler is still connecting
(`IsConnectingHandler()`), and is a no‑op if there's no `MauiContext`/platform view.

---

## 10. Shell & FlyoutPage integration

- **Shell content**: `ShellSectionRenderer` / `ShellContentFragment` install a local listener on their roots so each shell section handles insets independently.
- **Shell flyout**: `ShellFlyoutTemplatedContentRenderer` uses a **subclass** — `ShellFlyoutWindowInsetListener` — that overrides `OnApplyWindowInsets`. The flyout deliberately overlaps the status bar; the subclass pads the `AppBarLayout` top and the footer/flyout bottom specially (holding weak refs to the bg image, flyout, and footer views).
- **FlyoutPage**: `FlyoutViewHandler` registers the parent for child views and sets up a local listener on the content `CoordinatorLayout`.

This subclassing pattern is the intended extensibility seam: override
`OnApplyWindowInsets` for bespoke chrome, reuse the registry/animation plumbing.

---

## 11. Tracking, reset & lifecycle

- The listener keeps a `HashSet<AView> _trackedViews` of every view it padded.
- `ResetView` restores a view (via `IHandleWindowInsets.ResetWindowInsets`) and drops it from the set.
- `ResetAppliedSafeAreas(parent)` resets the parent and all tracked **descendants** — called before re‑applying when config changes or on detach.
- `Dispose` resets everything. Rotation (`OnConfigurationChanged`) resets the view and marks config dirty so the next layout recomputes.

---

## 12. Legacy compatibility (`IgnoreSafeArea`)

The old boolean `ISafeAreaView.IgnoreSafeArea` still works:
- `Page`/`ContentPage` map it: `IgnoreSafeArea == true` → `SafeAreaRegions.None`, `false` → `Container` (iOS path).
- `Layout.IgnoreSafeArea` is `[Obsolete]`; when the edge region resolves to `Default` it becomes `Container`, or `None` if the obsolete flag is set.
- `SafeAreaElement.ShouldObeySafeAreaForEdge` inverts legacy semantics ("ignore" → "don't obey") for callers that need a boolean.

Explicitly setting `SafeAreaEdges` always overrides the legacy path.

---

## 13. API‑level workaround (< 30)

`NavigationRootManager.cs:~90` calls `ViewGroupCompat.InstallCompatInsetsDispatch(_rootView)`
on API < 30. This is Google's fix for the API 28–29 bug where one child
consuming insets prevents siblings from receiving them
(android‑review 3310617). On API 30+ the framework dispatches correctly.

---

## 14. Key files reference

| Area | File |
|------|------|
| Enum | `src/Core/src/Primitives/SafeAreaRegions.cs` |
| Struct | `src/Core/src/Primitives/SafeAreaEdges.cs` |
| XAML converter | `src/Core/src/Converters/SafeAreaEdgesTypeConverter.cs` |
| Interfaces | `src/Core/src/Core/ISafeAreaView2.cs`, `ISafeAreaView.cs`, `ISafeAreaElement.cs` |
| Controls helper | `src/Controls/src/Core/SafeAreaElement.cs` |
| Controls | `ContentPage.cs`, `Layout.cs`, `ScrollView.cs`, `Border.cs`, `ContentView.cs`, `Page/Page.cs` |
| Handler mapper | `src/Core/src/Handlers/View/ViewHandler.cs` (registration), `ViewHandler.Android.cs` (`MapSafeAreaEdges`) |
| Edge‑to‑edge | `src/Core/src/Platform/Android/MauiAppCompatActivity.cs` |
| Dispatcher | `src/Core/src/Platform/Android/MauiWindowInsetListener.cs` |
| Self‑handling contract | `src/Core/src/Platform/Android/IHandleWindowInsets.cs` |
| Core algorithm | `src/Core/src/Platform/Android/SafeAreaExtensions.cs` |
| Inset math helpers | `src/Core/src/Platform/Android/SafeAreaPadding.cs` |
| View groups | `ContentViewGroup.cs`, `LayoutViewGroup.cs`, `MauiScrollView.cs` |
| Nav root wiring | `src/Core/src/Platform/Android/Navigation/NavigationRootManager.cs` |
| Shell | `ShellSectionRenderer.cs`, `ShellContentFragment.cs`, `ShellFlyoutTemplatedContentRenderer.cs` |
| Flyout | `src/Core/src/Handlers/FlyoutView/FlyoutViewHandler.Android.cs` |

---

## 15. Behavioral notes & gotchas

- **Insets are consumed selectively.** The algorithm zeroes only the edges it uses, so nested `SafeAreaEdges` views down the tree can still act on the rest. A view with no needed padding *passes insets through* untracked.
- **Safe area + margin are additive.** A 20 px margin plus a 30 px inset yields a 50 px offset, because margins are subtracted from the view rect before overlap math (only once `Width/Height > 0`).
- **Overlap‑based padding.** A view only gets the portion of an inset it actually overlaps — a view already clear of the notch gets `0` for that edge.
- **Animation heuristics are position‑based.** Off‑screen detection must happen before `Math.Max(0, …)` clamping, or the Shell slide‑in signal is destroyed. This is deliberately commented in `SafeAreaExtensions`.
- **`AdjustPan` short‑circuits.** With `windowSoftInputMode=adjustPan`, MAUI does not add keyboard padding — the window itself pans.
- **`Container` vs `All` at the bottom.** `Container` uses the *bar* inset (content flows under the keyboard); `All`/`SoftInput` use the *keyboard* inset.
- **RecyclerView/AppBar/ScrollView children are suppressed** from owning listeners unless they set an explicit `SafeAreaEdges`, to avoid double application.
- **Config changes (rotation) reset then recompute** via the dirty‑flag + `RequestApplyInsets` path; don't cache absolute inset pixels across rotations.

---

## 16. Keyboard / IME handling — deep dive & known flaws

### 16.1 Reconciling the Android guidance (`adjustResize`)

- `SOFT_INPUT_ADJUST_RESIZE` as an *auto‑resize window flag* is **deprecated (API 30+)**. The documented replacement is `setDecorFitsSystemWindows(false)` + consuming `WindowInsetsCompat.Type.ime()` yourself — preferably with the window still in **resize** (not `adjustPan`/`adjustNothing`) so the `ime()` inset is dispatched and the content area reflows.
- **MAUI already follows this**: edge‑to‑edge via `MauiAppCompatActivity.cs:30`, and the Android `WindowSoftInputModeAdjust` default is **`Resize` → `SoftInput.AdjustResize`** (`AndroidSpecific/Application.cs:23` → `ApplicationExtensions.ToPlatform` → applied in `Window.Android.cs:32`). The IME animation uses a `WindowInsetsAnimationCompat.Callback` with `DISPATCH_MODE_STOP` — the recommended pattern.

So the **design intent is correct**. The issues below are in **how** the IME inset is applied, not in the choice of `adjustResize`.

### 16.2 What actually happens when the keyboard opens

| Step | Code | Effect |
|------|------|--------|
| 1 | `OnPrepare` / `OnStart` (`MauiWindowInsetListener.cs:453,461`) | `IsImeAnimating = true` |
| 2 | `OnApplyWindowInsets` (`:238‑243`) | **short‑circuited** for every view; stashes the *last* view as `_pendingView` |
| 3 | `OnProgress` → `ApplyImeInsets` (`:476,365`) | `_rootView.SetPadding(0, 0, 0, ime.Bottom)` each frame |
| 4 | `OnEnd` (`:486‑495`) | `view.Post(() => { IsImeAnimating = false; RequestApplyInsets(_pendingView); })` |

The resting keyboard padding is therefore a **side effect of the final `OnProgress` frame** — the steady‑state path (`ApplyDefaultWindowInsets`, `:258‑355`) never reads `Type.Ime()`.

Critically, the per‑edge algorithm only produces keyboard padding for views whose `SafeAreaEdges` include `SoftInput` or `All` (`GetSafeAreaForEdge`). The **defaults** — `ContentPage`=`None`, `ScrollView`=`Default`, `Layout`=`Container` — all return `0` / the *bar* inset for the bottom edge, **not** the keyboard. So for the common case, keyboard avoidance comes **only** from the root `ApplyImeInsets`.

### 16.3 Known flaws

> Severity is a rough guide to user impact. These are observations from a static read of `main`.

1. **(High) Keyboard avoidance depends entirely on the animation callback.** For a default `ContentPage` / `ScrollView`, `ApplyImeInsets` in `OnProgress` is the *only* thing lifting content above the keyboard. If `WindowInsetsAnimationCompat` doesn't deliver progress frames (reduced‑motion / animator‑duration‑scale = 0, some OEM IMEs, split‑screen / multi‑window, parts of the API 21–29 compat shim), the keyboard **overlaps content** — there is no resting `OnApplyWindowInsets` fallback that applies `ime()` for the root.

2. **(High) `IsImeAnimating` can get permanently stuck.** Set `true` in `OnPrepare`; cleared only inside `OnEnd`, and the clear is **deferred via `view.Post(...)`** (`:490`). If `OnEnd` never fires (cancelled animation, view detached mid‑animation) or the view is detached before the posted runnable runs, `IsImeAnimating` stays `true` and `OnApplyWindowInsets` short‑circuits **all** future inset work (rotation, safe‑area changes) for that listener. Nothing in `ResetView` / detach / `Dispose` resets it, and there is no timeout failsafe.

3. **(Medium) `ApplyImeInsets` clobbers L/T/R padding.** `SetPadding(0, 0, 0, ime.Bottom)` (`:365`) hard‑zeros the other three edges. Any horizontal / top safe‑area padding on `_rootView` (landscape cutout, status bar) is wiped for the animation duration → content can shift under the notch while the keyboard animates.

4. **(Medium) `OnProgress` doesn't confirm the animation is the IME.** It only null‑checks `runningAnimations` (`:471‑476`), never scanning for an `ime()` type. A system‑bar show/hide animation also runs `ApplyImeInsets`, resetting root padding to `(0,0,0,0)` mid‑animation.

5. **(Medium) Single `_rootView` / `_pendingView` per shared listener.** One listener backs a whole subtree (multiple `RegisterView` entries), but IME padding always lands on the one `_rootView` from `SetupViewWithLocalListener` (`:191`), and only the *last* `_pendingView` is refreshed at `OnEnd` (last‑writer‑wins). With nested content roots, or a focused field inside a nested `MauiScrollView`, padding is applied at the wrong level and siblings aren't re‑applied.

6. **(Medium) Root‑level padding defeats scroll‑to‑reveal & risks double‑apply.** Padding the whole root by the keyboard height prevents a nested scrollable from scrolling its own content above the keyboard (the entire page shifts). If an inner view *does* obey `SoftInput` / `All`, the safe‑area pass adds keyboard padding too; only the geometry‑based overlap detection in `ApplyAdjustedSafeAreaInsetsPx` coincidentally cancels it — a fragile coupling between two independent mechanisms.

7. **(Low) Resting state is never reconciled.** Because the steady‑state path ignores `ime()`, the root's resting keyboard padding is whatever the last `OnProgress` frame left. If that value ≠ the true final `ime()` inset (interrupted / janky animation), stale padding persists until the next keyboard toggle. `_rootView` padding is also never reset on detach.

### 16.4 Suggested direction

- **Apply the resting `ime()` inset in the normal path** (`OnApplyWindowInsets` / `ApplyDefaultWindowInsets` reading `Type.Ime()`), and use `OnProgress` **only** for animation smoothing — so behavior is correct even without animation frames (addresses #1, #7).
- **Make `ApplyImeInsets` additive** with the existing L/T/R padding instead of `(0,0,0,x)`, and **gate `OnProgress`** on an actual IME animation being present in `runningAnimations` (addresses #3, #4).
- **Add a failsafe reset of `IsImeAnimating`** — clear it on detach / `ResetView`, and don't rely solely on a posted runnable (addresses #2).
- Consider **targeting the focused field's scroll container** instead of the single `_rootView`, so nested scrollables can reveal content (addresses #5, #6).

> Status: **analysis only** — flaws identified by static reading of the code on `main`; not yet reproduced with device tests or fixed.
