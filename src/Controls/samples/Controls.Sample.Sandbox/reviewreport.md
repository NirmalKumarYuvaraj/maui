# Android Safe Area Code Review

**Comparison:** <https://github.com/dotnet/maui/compare/main...NirmalKumarYuvaraj:maui:android-new-safeareaarchitecture>  
**Reviewed range:** `refs/remotes/upstream/main...refs/remotes/review/android-new-safeareaarchitecture`  
**Verdict:** `NEEDS_CHANGES`  
**Confidence:** Medium

## Independent Assessment

The change removes global inset registration and geometry-based safe-area
selection. It introduces local listeners, navigation-region ownership,
source-aware partial consumption, and a dedicated IME animation coordinator.
The architectural direction is appropriate for Android edge-to-edge layouts,
but dynamic ownership changes are not currently propagated.

## Listener Topology Assessment

The previous design registered a listener at coordinator/host boundaries and
allowed descendants to locate and share that listener. The branch instead
creates a separate `MauiWindowInsetListener` and
`ImeWindowInsetsCoordinator` for every eligible layout, ScrollView, and toolbar.

The report previously described this as "local listeners," but did not state
the topology difference explicitly enough. The per-view model removes the
static registry, but it is the direct cause of several review risks:

- sibling consumption is independent rather than exclusive
- IME animation state exists in many listeners
- callback and cleanup cost grows with the view tree
- ownership invalidation has no single authority

## Findings

### Error: Explicit siblings independently apply insets for edges they do not touch

**Location:** `src/Core/src/Platform/Android/SafeAreaExtensions.cs:37-40,67`

The previous overlap calculation has been removed. An eligible explicit view
now applies its requested remaining inset without determining whether it
intersects that window edge. Android sends the same parent snapshot to sibling
views independently, so one sibling's returned consumption does not establish
exclusive ownership for another sibling.

A vertically stacked pair of explicit `SafeAreaEdges.Container` layouts can
therefore both apply the status-bar and navigation-bar values. The lower layout
gets incorrect top padding and the upper layout gets incorrect bottom padding.

**Required change:** define deterministic edge ownership within the content
region, or constrain inset dispatch so only the structural edge owner receives
each source. Add attached-hierarchy tests with non-overlapping explicit
siblings.

### Error: Dynamic navigation-region ownership is never invalidated

**Location:** `src/Core/src/Platform/Android/Navigation/NavigationLayoutWindowInsetListener.cs:43-52,70-107,131-181`

`HasVisibleContent` is evaluated only from `OnApplyWindowInsets`. AppBar,
top-tab, fragment, and bottom-tab visibility can change without Android
dispatching new window insets. No hierarchy/layout/visibility listener requests
`ViewCompat.RequestApplyInsets` for the navigation root or content host.

After the first dispatch, hiding or showing a navigation region can therefore
leave stale padding and stale consumed insets. A common failure path is:

1. AppBar is visible, receives top padding, and consumes the top inset.
2. The toolbar/AppBar is hidden at runtime.
3. Normal layout runs, but the navigation inset listener is not invoked.
4. Content still receives a snapshot with the top inset removed.

The inverse transition can place a newly visible AppBar under the status bar.
Bottom-tab transitions have the equivalent stale-bottom behavior.

**Required change:** add a lifecycle-safe invalidation mechanism for structural
owner changes and device tests that toggle each region after an initial inset
dispatch.

### Warning: The content listener is installed outside the managed lifecycle

**Location:** `src/Core/src/Platform/Android/Navigation/NavigationLayoutWindowInsetListener.cs:29-40,161-182`

The root listener is tagged, reset, and removed through
`SetupViewWithLocalListener`/`RemoveViewWithLocalListener`. The nested content
listener is assigned directly with
`ViewCompat.SetOnApplyWindowInsetsListener` and is never cleared or disposed.
It also retains the bottom-tabs view.

**Required change:** explicitly detach and dispose the content listener when the
navigation listener is replaced or removed.

### Error: Unrelated inset animations cancel active IME animation state

**Location:** `src/Core/src/Platform/Android/ImeWindowInsetsCoordinator.cs:45-66`

`OnPrepare` calls `EndAnimation` whenever the callback is not for IME. Android
may deliver system-bar, immersive-mode, predictive-back, and IME animation
callbacks as separate sequences. A non-IME prepare callback can therefore clear
`_isAnimating` while the keyboard is still moving; subsequent IME progress is
ignored and padding can snap or remain stale.

**Required change:** ignore non-IME callbacks without mutating the active IME
sequence. Add a callback-ordering test with interleaved IME and non-IME
animations.

### Warning: IME animation performs layout-affecting padding changes per frame

**Location:** `src/Core/src/Platform/Android/ImeWindowInsetsCoordinator.cs:139-154,173-230`

`OnProgress` calls `SetPadding` throughout the animation. For
`LayoutViewGroup`, `ContentViewGroup`, and `MauiScrollView`, padding affects
measure and arrange, so this can trigger repeated layout work and keyboard
jank. It also differs from the branch's architecture document, which recommends
applying final padding once and animating a temporary translation.

**Required change:** implement the documented layout-then-translate model or
provide device performance evidence that per-frame layout is acceptable.

### Suggestion: Remove branch-only IDE configuration

**Location:** `.vscode/launch.json:7`

The comparison adds a personal launch configuration with trailing whitespace.
It is unrelated to the framework change and should be removed from the eventual
PR unless it is intentionally shared repository configuration.

## Blast Radius

- **Runs for all instances:** Yes. Edge-to-edge window setup and navigation-root
  listeners affect every Android window/navigation host.
- **Startup impact:** Yes. Listeners are installed while window and navigation
  roots are created.
- **Static/shared state:** No new global ownership registry; this is an
  improvement over the prior design.
- **Lifecycle sensitivity:** High. Navigation fragments, Shell, FlyoutPage,
  toolbar visibility, tab visibility, handler reconnect, and IME transitions
  all cross the changed paths.

## Failure-Mode Probing

| Scenario | Result |
| --- | --- |
| Two vertically stacked explicit `Container` siblings | Both receive the same parent snapshot and can apply top and bottom padding regardless of edge overlap. |
| Toolbar hidden after initial inset dispatch | Ownership is not recalculated; content can remain without the top inset. |
| Toolbar shown after initial inset dispatch | AppBar can remain unpadded until another system inset event. |
| Bottom tabs hidden or shown dynamically | Content padding and bottom consumption can remain stale. |
| Fragment child added to an initially empty region | `HasVisibleContent` changes, but the root listener is not guaranteed to rerun. |
| System-bar animation begins during IME animation | Non-IME `OnPrepare` ends the IME sequence, so later progress is ignored. |
| Owner detached during IME animation | General listener removal resets padding, but the nested content listener has no explicit cleanup path. |
| IME animation on a complex page | Padding changes can force measure/arrange on every animation frame. |

## External Output Contract

Not applicable. The change does not classify external tool output.

## Test and Build Status

- Added tests cover helper logic and source consumption.
- No test covers live ownership transfer after an initial inset dispatch.
- A targeted `Core.csproj` Android build reached compilation dependencies but
  could not complete because the generated Android native `maui.aar` artifact
  was absent. Build tasks/native artifacts must be prepared before validation.
- `git diff --check` reports trailing whitespace in `.vscode/launch.json`.

## Recommendation

Do not merge the architecture in its current form. Define exclusive ownership
inside content, fix navigation ownership invalidation and the IME callback-state
defect, then add attached-hierarchy device tests. Address the nested listener
lifecycle and validate IME animation performance before requesting final review.

The recommended replacement is **one listener and one IME coordinator per
logical region**, with explicit `SafeAreaEdges` views registered as lightweight
participants. Retain typed inset snapshots and source-aware consumption, but
resolve owners centrally from stable arranged bounds and structural regions.
This avoids both the previous global registry/geometry heuristics and the
branch's listener-per-view duplication.
