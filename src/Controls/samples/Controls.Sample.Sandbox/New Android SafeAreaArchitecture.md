# Android Safe Area Redesign Proposal

## Region-Based Window Insets Ownership for .NET MAUI

**Status:** Proposal

**Audience:** .NET MAUI Platform Team

**Platform:** Android

---

# Overview

This document proposes a redesign of Android safe area handling within .NET MAUI.

The current implementation resolves safe area ownership at the individual view level by allowing each view that receives `WindowInsets` to independently determine whether it should consume a portion of those insets.

Although this approach supports many scenarios, it has become increasingly difficult to maintain due to the amount of state tracking, geometry calculations, and special-case handling required.

This proposal introduces a **region-based ownership model**, where safe area ownership is determined once by the navigation layout and propagated downward through the layout hierarchy.

The goal is to simplify the architecture while remaining aligned with Android's intended `WindowInsets` model.

---

# Background

Android's edge-to-edge APIs expose `WindowInsets` as information describing the parts of the window occupied by:

- Status Bar
- Navigation Bar
- Display Cutout
- IME (Keyboard)
- Gesture Areas

Importantly, **WindowInsets do not specify which view should receive those insets.**

Instead, they simply describe unavailable portions of the window.

The responsibility of deciding **which UI region owns those insets** belongs to the application.

---

# Current Architecture

Today, inset handling is distributed across many views.

```
WindowInsets
        │
        ▼
Every View
        │
        ▼
Should I consume these insets?
```

Each eligible view performs its own evaluation.

Typical processing includes:

- receiving WindowInsets
- determining SafeAreaEdges
- computing screen coordinates
- calculating overlap with system bars
- consuming portions of the insets
- forwarding remaining insets to children

Because multiple views perform this independently, the implementation requires additional infrastructure.

---

# Current Implementation Responsibilities

The existing implementation currently includes:

- Global and local WindowInset listeners
- View registration
- Listener discovery
- Ancestor hierarchy traversal
- Tracked view bookkeeping
- Padding reset logic
- Safe area overlap calculations
- Geometry inspection
- Animation detection
- RecyclerView exceptions
- ScrollView exceptions
- AppBar exceptions
- Toolbar exceptions
- Keyboard handling

Most of this complexity exists solely to answer one question:

> Should this view consume this safe area?

---

# Problems With the Current Design

## Geometry-Based Decisions

Many decisions rely on:

```
GetLocationOnScreen()
```

followed by calculations such as:

- viewTop
- viewBottom
- screenHeight
- overlap with status bar
- overlap with navigation bar

This works for static layouts but becomes increasingly fragile when views are animated.

Examples include:

- Fragment transitions
- Shell navigation animations
- TranslationX
- TranslationY
- CoordinatorLayout behaviors
- Collapsing AppBars
- Predictive Back
- MotionLayout

Geometry is no longer a reliable indicator of ownership.

---

## Multiple Consumers

Every view independently determines whether it should consume an inset.

This creates situations where:

- multiple views consume the same edge
- parent and child compete
- duplicate padding occurs
- bookkeeping becomes necessary

---

## Complex State Tracking

To compensate for duplicate consumption, the current implementation maintains:

- tracked views
- registered listeners
- reset logic
- previously consumed state

These mechanisms are implementation artifacts rather than actual layout requirements.

---

# Design Goals

The redesigned architecture should:

- Resolve ownership once.
- Eliminate geometry-based overlap calculations.
- Avoid screen-coordinate inspection.
- Support dynamic page layouts.
- Support nested MAUI layouts.
- Preserve Android's WindowInsets propagation model.
- Continue supporting partial consumption.
- Provide deterministic IME ownership and animation.
- Keep system bars, display cutouts, and IME consumption independent.
- Reduce implementation complexity.

---

# Core Principle

Instead of asking:

> Should this view consume this inset?

The system should instead ask:

> Which layout region owns this edge?

Ownership should be resolved exactly once.

---

# Region-Based Ownership

The Android navigation layout already divides the page into logical regions.

```
CoordinatorLayout
│
├── AppBarLayout
│     └── Top Tabs
│
├── Content
│
└── Bottom Tabs
```

These regions naturally represent ownership boundaries.

Instead of allowing every descendant view to consume insets, ownership is assigned to these regions.

---

# Ownership Model

Each edge has exactly one owner.

```
Top
Bottom
Left
Right
```

Ownership is exclusive.

For example:

```
Top
 ├── AppBar (if present)
 └── otherwise Content

Bottom
 ├── BottomTabs (if present)
 └── otherwise Content
```

No other layout consumes those edges.

---

# Dynamic Ownership Resolution

Ownership is determined during layout.

Pseudo-code:

```csharp
TopOwner =
    AppBar.HasVisibleContent
        ? AppBar
        : Content;

BottomOwner =
    BottomTabs.HasVisibleContent
        ? BottomTabs
        : Content;
```

The NavigationLayout then applies:

```csharp
TopOwner.PaddingTop = topInset;

BottomOwner.PaddingBottom = bottomInset;
```

Every other region receives zero padding for those edges.

---

# Example Layouts

## Page With AppBar

```
Status Bar

AppBar

Content

Bottom Tabs
```

Result:

```
AppBar
    PaddingTop = StatusBarInset

Content
    PaddingTop = 0

BottomTabs
    PaddingBottom = NavigationInset
```

---

## Page Without AppBar

```
Status Bar

Content
```

Result:

```
Content
    PaddingTop = StatusBarInset
```

Ownership automatically transfers.

---

## Page Without Bottom Tabs

```
Status Bar

AppBar

Content
```

Result:

```
Content
    PaddingBottom = NavigationInset
```

Again, ownership transfers automatically.

---

# Partial WindowInsets Consumption

Android already supports partial consumption.

Example:

Original:

```
Top = 63
Bottom = 48
```

If AppBar owns the top edge:

```
Top = 0
Bottom = 48
```

The rebuilt WindowInsets are forwarded to children.

Children never receive the already-consumed top inset.

Bottom remains available.

This aligns naturally with Android's WindowInsets design.

---

# SafeAreaEdges

Existing APIs such as:

```
SafeAreaEdges.Top

SafeAreaEdges.Bottom

SafeAreaEdges.None
```

can remain.

However, they should no longer determine ownership through geometry inspection.

Instead:

- they express intent
- ownership is resolved by the parent region
- children receive already-adjusted WindowInsets

Explicit and implicit values have different ownership semantics:

- An explicit `SafeAreaEdges` value establishes an ownership boundary.
- Implicit descendants do not claim insets beneath an explicit ancestor.
- Explicit descendants may consume any inset sources that remain available.
- `None` does not consume an inset source; it prevents implicit descendants from
  becoming accidental owners while still allowing an explicit child override.

---

# Nested Layouts

Nested layouts should not need to know where they are on the screen.

Example:

```
NavigationLayout
    Content
        Grid
            Border
                ScrollView
```

The Grid should never calculate:

```
GetLocationOnScreen()
```

Neither should:

- Border
- ScrollView
- StackLayout

They simply receive WindowInsets that already represent the remaining available edges.

---

# WindowInsets Flow

The proposed architecture becomes:

```
System
        │
        ▼
Activity
        │
        ▼
WindowInsetsManager
        │
        ▼
NavigationLayout
        │
        ├── AppBar
        ├── Content
        └── BottomTabs
                │
                ▼
Remaining WindowInsets
                │
                ▼
Descendants
```

Only NavigationLayout decides ownership between navigation regions.

Within a content region, explicitly configured descendants may consume the
remaining inset sources. Implicit descendants must respect the nearest explicit
ownership boundary.

---

# WindowInsetsManager

The WindowInsetsManager becomes responsible for:

- receiving WindowInsets
- caching current values
- rebuilding partially consumed WindowInsets
- notifying NavigationLayout

It does **not** determine ownership.

---

# NavigationLayout Responsibilities

NavigationLayout becomes responsible for:

- determining edge owners
- applying padding
- consuming owned insets
- rebuilding WindowInsets
- forwarding remaining insets

NavigationLayout becomes the single authority for ownership.

---

# Child Layout Responsibilities

Child layouts should only:

- receive remaining WindowInsets
- apply explicitly requested SafeAreaEdges
- perform normal layout

They should **not**:

- inspect screen coordinates
- detect overlap
- determine ownership
- inspect animation state

---

# Animation Support

One of the biggest benefits of the proposed design is animation robustness.

Current implementation:

```
Fragment Animation

↓

View position changes

↓

Overlap changes

↓

Safe Area changes
```

Proposed implementation:

```
Fragment Animation

↓

Owner unchanged

↓

Insets unchanged
```

Ownership depends on layout structure rather than transient geometry.

This eliminates numerous animation-specific heuristics.

---

# Keyboard and IME Insets

Keyboard handling follows the same ownership model as container safe areas, but
IME must remain a distinct inset source.

The architecture must never infer IME ownership from:

- view coordinates
- overlap calculations
- whether bottom padding is non-zero
- a navigation-bar inset
- a layout animation

IME visibility should come from:

```csharp
WindowInsetsCompat.IsVisible(WindowInsetsCompat.Type.Ime())
```

IME geometry should come from:

```csharp
WindowInsetsCompat.GetInsets(WindowInsetsCompat.Type.Ime())
```

---

## Keyboard Behavior Contract

The bottom `SafeAreaRegions` value defines keyboard behavior:

| Bottom region | Keyboard hidden                     | Keyboard visible             |
| ------------- | ----------------------------------- | ---------------------------- |
| `None`        | No padding                          | No keyboard padding          |
| `Container`   | System bar and cutout               | System bar and cutout only   |
| `SoftInput`   | No padding                          | IME inset                    |
| `All`         | System bar and cutout               | Maximum of container and IME |
| `Default`     | Control-specific container behavior | Does not implicitly own IME  |

`All` must not add the navigation-bar and IME values together. These sources
normally overlap, so the effective bottom padding is:

```csharp
Math.Max(containerBottom, imeBottom)
```

---

## IME Ownership

Exactly one owner should handle IME within each content region.

Ownership resolution follows these rules:

1. An explicitly configured `SoftInput` or `All` view may own IME.
2. An explicit ancestor blocks implicit descendants from claiming IME.
3. An explicit descendant may override an explicit ancestor when IME remains
   unconsumed.
4. A default nested `ScrollView` must not become an accidental IME owner.
5. Navigation regions resolve IME independently. Flyout, modal, Shell, and tab
   content must not consume one another's IME state.

An explicit `None` or `Container` ancestor does not consume IME. It establishes
that implicit descendants should not claim it. An explicit child using
`SoftInput` or `All` may still consume the forwarded IME source.

---

## Typed Inset Snapshot

`WindowInsetsManager` should cache each inset source independently:

```text
WindowInsetsSnapshot
    SystemBars
    DisplayCutout
    Ime
    SystemBarsVisible
    DisplayCutoutVisible
    ImeVisible
```

Animation state belongs to a dedicated IME coordinator:

```text
ImeAnimationState
    IsRunning
    StartInsets
    EndInsets
    CurrentInsets
```

Container and IME values must not be collapsed into one generic bottom value
before ownership is resolved.

---

## Source-Aware Consumption

Consumption must identify both the edge and the source:

```text
Consumed Container Edges
    Left
    Top
    Right
    Bottom

Consumed Sources
    SystemBars
    DisplayCutout
    Ime
```

Examples:

- Consuming IME must not remove the bottom system-bar inset.
- Consuming the bottom navigation bar must not remove IME.
- AppBar ownership consumes top system-bar and cutout values, not IME.
- BottomTabs consume bottom container insets only for the Content subtree.
- BottomNavigationView still receives the original bottom inset so its
  background can extend edge-to-edge.

Different sibling regions may therefore receive different rebuilt snapshots:

```text
AppBar      -> top container consumed
Content     -> top and/or bottom region-owned sources consumed
BottomTabs  -> original bottom container inset preserved
```

---

## IME Insets Flow

The keyboard flow becomes:

```text
Window
    │
    ▼
WindowInsetsManager
    │
    ├── Container snapshot
    └── IME snapshot
            │
            ▼
ImeWindowInsetsCoordinator
            │
            ▼
Resolved IME owner
            │
            ▼
Apply final padding
            │
            ▼
Forward remaining typed sources
```

`MauiWindowInsetListener` remains responsible for receiving and forwarding
snapshots. `SafeAreaExtensions` resolves final padding from an already resolved
snapshot. `ImeWindowInsetsCoordinator` owns animation state and final IME
redispatch.

---

## IME Animation

The listener must not ignore all inset updates until `OnEnd`. That produces a
jump between the closed and open keyboard states.

It must also avoid changing layout padding on every animation frame because
that can trigger repeated measure and arrange operations.

The recommended sequence is:

### `OnPrepare`

- Capture the owner's current bounds.
- Capture the current applied padding.
- Mark the IME transition as running.

### `OnApplyWindowInsets`

- Resolve the final IME snapshot.
- Apply final padding once.
- Allow Android to perform the final layout.

### `OnStart`

- Compare the pre-layout and final owner bounds.
- Calculate the visual offset introduced by the final layout.
- Apply a temporary translation that visually restores the starting position.

### `OnProgress`

- Interpolate the temporary translation toward zero using the current IME
  animation fraction.
- Do not repeatedly replace final padding.

### `OnEnd`

- Clear the temporary translation.
- Clear animation state.
- Request one final inset application if ownership or focus changed during the
  transition.

This follows Android's recommended layout-then-translate animation model and
avoids screen-overlap heuristics.

Animation callbacks must continue through the subtree until the resolved IME
owner is reached. A parent callback must not prevent an eligible explicit child
from receiving animation state.

---

## Soft Input Modes

The architecture must define behavior for each Android soft-input mode.

### `AdjustResize`

- Android may resize the available content area.
- IME padding must not duplicate the resize adjustment.
- The typed IME snapshot remains available for explicit `SoftInput` and `All`
  ownership.

### `AdjustPan`

- Android owns panning of the focused control.
- MAUI must not consume the complete `WindowInsetsCompat` snapshot.
- System-bar and display-cutout insets continue to descendants.
- Explicit IME padding is not added on top of Android's pan.

### `AdjustNothing`

- Android does not resize or pan the content.
- The resolved MAUI IME owner applies keyboard avoidance.
- This is the primary mode for fully controlled edge-to-edge animation.

Edge-to-edge window configuration must use dispatched IME insets as the
authoritative keyboard geometry rather than visible display-frame calculations.

---

## Focus Visibility

After final IME layout, the focused editor may need to be revealed by its
scrolling container.

The implementation should:

- request that the nearest active scrolling container reveal the focused view
- use Android focus-rectangle and scrolling APIs
- perform the request after final padding has been applied
- avoid manual screen-coordinate overlap calculations
- avoid custom panning when `AdjustPan` already owns that behavior

Focus changes while the keyboard remains visible must transfer reveal behavior
without transferring IME ownership unnecessarily.

---

## IME Lifecycle

The coordinator must handle:

- keyboard opening
- keyboard closing
- focus changes while the keyboard remains visible
- orientation and window-size changes
- app background and foreground transitions
- navigation during an active animation
- owner detach or replacement
- hardware keyboard connection
- floating and split keyboards
- zero-inset snapshots

When an owner detaches during animation, the coordinator must:

1. clear temporary translation
2. restore original platform padding
3. discard pending animation state
4. resolve a new owner on the next inset dispatch

Zero-inset snapshots must always be processed so padding from a previous
keyboard or system-bar state cannot remain stale.

---

## IME Component Responsibilities

### `WindowInsetsManager`

- Cache system bars, display cutout, and IME independently.
- Rebuild snapshots by source and edge.
- Preserve visibility state.
- Never decide ownership.

### `ImeWindowInsetsCoordinator`

- Resolve one IME owner per content region.
- Track the IME animation lifecycle.
- Coordinate final layout and temporary translation.
- Coalesce final `RequestApplyInsets` calls.
- Clear state when the owner detaches.

### `MauiWindowInsetListener`

- Receive all snapshots, including zero snapshots.
- Forward animation callbacks through the subtree.
- Route snapshots to custom handlers or region coordinators.
- Avoid maintaining independent competing IME state for every nested view.

### `SafeAreaExtensions`

- Convert resolved source values into final edge padding.
- Apply the `SafeAreaRegions` behavior contract.
- Consume only the sources actually owned by the current view.
- Reset original padding when the resolved value becomes zero.

---

## Keyboard Implementation Phases

### Phase 7

Add typed system-bar, display-cutout, and IME snapshots to
`WindowInsetsManager`.

### Phase 8

Introduce `ImeWindowInsetsCoordinator` and resolve one owner per content
region.

### Phase 9

Replace animation suppression with the layout-then-translate animation
lifecycle.

### Phase 10

Implement explicit behavior for `AdjustResize`, `AdjustPan`, and
`AdjustNothing`.

### Phase 11

Add focus-reveal coordination and lifecycle cleanup.

### Phase 12

Remove obsolete per-view IME animation state and any remaining keyboard
geometry heuristics.

---

## Keyboard Test Matrix

Device tests should cover:

- every bottom `SafeAreaRegions` value with IME hidden and visible
- explicit parent and explicit child IME ownership
- implicit nested `ScrollView`
- opening and closing reset behavior
- system-bar preservation while IME is visible
- `AdjustResize`
- `AdjustPan`
- `AdjustNothing`
- navigation during animation
- focus changes between editors
- BottomTabs and BottomNavigationView
- Shell content
- flyout and modal content
- rotation with IME visible
- hardware keyboard transitions
- owner detach during animation
- zero-inset cleanup

Tests should assert:

- final platform padding
- remaining system-bar, cutout, and IME snapshots
- exactly one resolved IME owner
- no duplicate final inset requests
- temporary translation is cleared after animation

---

# Benefits

## Simpler Architecture

Ownership is determined once.

Not hundreds of times.

---

## Deterministic

Exactly one owner exists for every edge.

No duplicate padding.

---

## Better Performance

Removes:

- repeated geometry calculations
- overlap detection
- ancestor searches
- tracked-view lookups

---

## Easier Debugging

Developers can immediately identify which region owns an edge.

No hidden consumption.

No competing listeners.

---

## Better Android Alignment

Android's WindowInsets APIs are fundamentally region-oriented.

This proposal follows the intended platform design rather than working around it.

---

# Migration Strategy

The redesign can be introduced incrementally.

### Phase 1

Introduce ownership resolution into NavigationLayout.

---

### Phase 2

Continue rebuilding partially consumed WindowInsets.

Forward remaining insets to descendants.

---

### Phase 3

Remove geometry-based overlap calculations.

---

### Phase 4

Remove tracked-view bookkeeping.

---

### Phase 5

Remove ancestor listener discovery.

---

### Phase 6

Simplify SafeAreaExtensions to operate only on remaining WindowInsets.

---

# Future Extensions

The ownership model naturally supports additional layout regions.

Examples:

```
Overlay Host

Drawer

Floating Toolbar

Bottom Sheet

Modal Host

Floating Action Container
```

Each region can register ownership for one or more edges without affecting descendant layouts.

---

# Non-Goals

This proposal intentionally avoids:

- Per-view overlap detection
- Screen coordinate inspection
- GetLocationOnScreen()
- Animation heuristics
- View tracking
- Padding reset bookkeeping
- Descendant ownership discovery

These concerns become unnecessary when ownership is resolved at the region level.

---

# Conclusion

The current implementation has evolved into a sophisticated system for determining which individual view should consume WindowInsets. While functional, it requires substantial bookkeeping, geometry calculations, and special-case handling.

By shifting responsibility from individual views to logical layout regions, the system becomes significantly simpler, more deterministic, and better aligned with Android's WindowInsets architecture.

The proposed region-based ownership model resolves inset ownership exactly once, allows dynamic layouts to naturally transfer ownership when regions appear or disappear, eliminates geometry-dependent calculations, and provides a foundation that is easier to maintain as .NET MAUI continues to evolve.

Rather than asking every view:

> "Should I consume these insets?"

the system instead answers a single question:

> **"Which layout region owns this edge?"**

Once that question is answered, the remaining layout hierarchy can operate with already-resolved WindowInsets, resulting in a cleaner, more predictable, and more maintainable implementation.
