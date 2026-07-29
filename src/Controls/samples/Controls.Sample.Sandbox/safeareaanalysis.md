# Android Safe Area Architecture Analysis

**Comparison:** `upstream/main...android-new-safeareaarchitecture`  
**Head:** `fa2423963b6daf559a54d02f8d46af5a338b9f6c`  
**Reviewed:** 2026-07-29

## Purpose

This branch replaces the previous Android safe-area implementation, which used
global registration and geometry-based ownership checks, with local
`WindowInsetsCompat` listeners and source-aware inset consumption.

The intended architecture is:

```text
Android window
    -> navigation-region listener
        -> AppBar / content / bottom-tabs ownership
            -> remaining typed insets
                -> MAUI layout listeners
                    -> SafeAreaEdges padding and consumption
```

## Main Components

| Component | Responsibility |
| --- | --- |
| `WindowInsetsManager` | Captures system-bar, display-cutout, and IME insets independently and rebuilds partially consumed snapshots. |
| `NavigationLayoutWindowInsetListener` | Resolves top and bottom ownership for AppBar, content, and bottom-tab regions. |
| `MauiWindowInsetListener` | Attaches a local apply-insets listener and IME animation callback to each eligible platform view. |
| `SafeAreaExtensions` | Maps `SafeAreaRegions` to padding and consumes only the owned inset sources. |
| `ImeWindowInsetsCoordinator` | Captures IME start/target padding and interpolates bottom padding during keyboard animation. |
| `ContentViewGroup`, `LayoutViewGroup`, `MauiScrollView` | Store original padding, apply local safe-area padding, and request redispatch when `SafeAreaEdges` changes. |

## Listener Topology: Previous vs Branch

The previous implementation did not create an independent listener object for
every MAUI layout. A listener was registered at a coordinator/host boundary,
such as a navigation root. Descendant views found that registered ancestor and
attached the same listener instance:

```text
CoordinatorLayout / host
    -> one registered MauiWindowInsetListener
        -> descendant LayoutViewGroup
        -> descendant ContentViewGroup
        -> descendant MauiScrollView
```

That shared listener kept a tracked-view set and used screen geometry to decide
which descendant overlapped each system edge.

The branch removes ancestor listener discovery and creates a new local listener
for each eligible view:

```text
CoordinatorLayout -> NavigationLayoutWindowInsetListener
LayoutViewGroup    -> MauiWindowInsetListener + ImeWindowInsetsCoordinator
ContentViewGroup   -> MauiWindowInsetListener + ImeWindowInsetsCoordinator
MauiScrollView     -> MauiWindowInsetListener + ImeWindowInsetsCoordinator
MaterialToolbar    -> MauiWindowInsetListener + ImeWindowInsetsCoordinator
```

This detail was implicit in the component table and findings, but it is
architecturally important: ownership state, animation callbacks, and lifecycle
cleanup are now distributed across the view tree rather than coordinated at a
logical host boundary.

### Benefits of the Branch Topology

- Removes the static registered-view list and ancestor listener lookup.
- Gives explicit descendants direct access to remaining typed insets.
- Makes attach/detach ownership local to most platform views.
- Allows Android's normal descendant inset propagation to carry partially
  consumed sources.

### Costs of the Branch Topology

- Every eligible view receives apply-insets and animation callbacks.
- Every listener owns a separate `ImeWindowInsetsCoordinator`.
- Siblings receive independent copies of the same parent snapshot, so returned
  consumption cannot enforce exclusive sibling ownership.
- Listener and Java-object lifecycle work scales with the number of views.
- Ownership decisions and invalidation are spread across navigation roots and
  individual views.
- Per-view IME padding animation can multiply layout work across nested owners.

## Container Inset Flow

1. `WindowHandler` enables edge-to-edge rendering on Android API 30 and later.
2. Navigation roots install `NavigationLayoutWindowInsetListener`.
3. The listener snapshots system bars, display cutouts, and IME separately.
4. If the AppBar has visible content, it receives the top safe-area padding and
   top container insets are removed before descendant dispatch.
5. When bottom tabs have visible content, the navigation content host receives
   bottom padding while a content-specific listener removes the bottom
   container inset from the content subtree.
6. Eligible MAUI layouts apply explicitly or implicitly requested remaining
   insets and rebuild the snapshot for descendants.

This is a substantial improvement over screen-coordinate overlap checks:
ownership is structural, inset sources remain typed, and consumption is
edge-specific.

## Explicit and Implicit Ownership

`SafeAreaExtensions.ShouldApplySafeAreaInsets` implements these rules:

- An explicitly configured view may consume remaining insets.
- An implicit view is blocked by an explicit safe-area ancestor.
- An explicit descendant may still consume sources left by an explicit
  ancestor.
- `SafeAreaRegions.None` establishes a boundary without consuming an inset.

RecyclerView, AppBar, and ScrollView suppression remains in
`MauiWindowInsetListener.ShouldSetMauiWindowInsetListener`; explicit
`SafeAreaEdges` can override most suppression paths.

## IME Architecture

IME is no longer collapsed into the generic bottom safe area:

- `WindowInsetsManager` tracks IME geometry and visibility separately.
- `SafeAreaExtensions` uses `Math.Max(containerBottom, imeBottom)` for `All`.
- `AdjustPan` disables MAUI IME padding while preserving container insets.
- `CanApplyImeInsets` selects the topmost explicit `SoftInput`/`All` owner.
- IME consumption removes only `WindowInsetsCompat.Type.Ime()`.

The current animation implementation captures final padding during
`OnApplyWindowInsets`, restores the start padding, and changes bottom padding on
each `OnProgress` frame. This produces the intended interpolation, but it also
causes layout work on every frame. The design proposal in
`New Android SafeAreaArchitecture.md` recommends final-layout plus temporary
translation instead; that remains unfinished.

The coordinator also treats every non-IME `OnPrepare` callback as a reason to
end the current IME animation. Android can run or report system-bar and IME
animations independently, so an unrelated inset animation can cancel IME state
before later IME progress callbacks arrive. Non-IME callbacks must leave an
active IME sequence unchanged.

## Lifecycle

General local-listener lifecycle is symmetric:

- attach with `SetupViewWithLocalListener`
- remove the apply-insets listener and animation callback
- reset the IME coordinator
- restore original padding through `IHandleWindowInsets`

The content-subtree listener created by
`NavigationLayoutWindowInsetListener`, however, is installed directly with
`ViewCompat.SetOnApplyWindowInsetsListener` and is not explicitly removed.
Future work should make this listener participate in the same lifecycle.

## Blocking Architecture Gap

### Sibling Ownership Is Not Exclusive

Removing geometry checks means an explicit view applies every remaining inset
source it requests, regardless of whether that view intersects the
corresponding window edge. Android dispatches the same parent inset snapshot to
each sibling; consumption returned by one sibling does not modify the snapshot
sent to another sibling.

For example, two vertically stacked explicit `SafeAreaEdges.Container` layouts
can both receive top and bottom container insets. The lower layout can add
status-bar padding even though it does not touch the top edge, and the upper
layout can add navigation-bar padding even though it does not touch the bottom
edge.

Navigation-region ownership solves this at the AppBar/content/bottom-tabs
boundary, but there is no equivalent ownership resolution among explicit
descendants inside content. The implementation therefore does not yet satisfy
the stated "exactly one owner per edge" rule.

### Dynamic Region Ownership Is Not Invalidated

Region ownership is recalculated only when Android dispatches window insets.
The owner inputs are dynamic:

- AppBar visibility and child content
- fragment/top-tab attachment
- bottom-tab visibility and child content

No layout, hierarchy, or visibility callback requests a new inset dispatch when
those inputs change. Therefore the architecture can retain the old owner and
old padding after a toolbar or tab region appears or disappears.

Examples:

- A hidden AppBar can retain top padding and continue removing the top inset
  from content.
- A newly visible AppBar can remain under the status bar because content still
  owns the top inset.
- Hidden bottom tabs can leave stale bottom padding on the content host and
  continue removing the bottom inset from descendants.
- Newly attached fragment content can change `HasVisibleContent` without
  causing the navigation listener to run again.

Ownership invalidation must be part of the navigation-region contract, not
dependent on an unrelated system inset change.

## Test Coverage Assessment

The branch adds useful tests for:

- owner selection
- visible-region detection
- source-aware consumption
- zero-inset reset
- explicit/implicit ancestor behavior
- IME interpolation
- listener tag cleanup

The tests do not exercise dynamic region changes after an initial inset
dispatch or multiple explicit sibling consumers. Required device coverage is
listed in `task.md`.

## Architecture Verdict

### Recommended Architecture: Region-Scoped Coordinator with Explicit Participants

The better architecture is a hybrid of the two designs:

1. Keep the branch's typed `WindowInsetsSnapshot` and source-aware partial
   consumption.
2. Install one listener and one IME coordinator per logical ownership region,
   not one per platform view.
3. Let `SafeAreaEdges` views register their intent with the nearest region
   coordinator as lightweight participants.
4. Resolve top/bottom/left/right owners after MAUI arrange using stable logical
   bounds relative to the region, not transient `GetLocationOnScreen` values.
5. Apply padding directly to selected participants and provide a scoped
   remaining snapshot to nested ownership regions.
6. Re-resolve only when participant bounds, visibility, hierarchy, or
   `SafeAreaEdges` change.
7. Apply final IME padding once and animate a temporary translation through the
   single region IME coordinator.

Suggested topology:

```text
Window
    -> Navigation/Shell/Flyout/Modal region coordinator
        -> typed inset snapshot
        -> structural AppBar/content/bottom-tabs ownership
        -> registered SafeAreaEdges participants
        -> optional nested region coordinator
```

This is preferable to both extremes:

- It avoids the old global registry, ancestor searches, and transient
  screen-coordinate heuristics.
- It avoids the branch's listener-per-view callback fan-out, competing sibling
  consumers, duplicated IME state, and distributed cleanup.
- It provides one place to invalidate dynamic ownership.
- It can preserve explicit nested safe-area behavior without assuming that
  every requesting view touches every system edge.
- It makes "one intended owner per edge per region" enforceable rather than
  relying on Android sibling dispatch order.

The branch's source-aware `WindowInsetsManager` should be retained, but the
per-view listener topology should be replaced with region-scoped coordination.
The architecture should not be considered complete until ownership among
content siblings is deterministic, dynamic invalidation and listener cleanup
are implemented, and attached-hierarchy tests pass.
