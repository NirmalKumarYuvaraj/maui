# Android Navigation Architecture Improvements

## Scope and evidence

This document describes improvements to the Android Shell navigation architecture
on the `composable-android` branch. It covers compatibility with existing MAUI
navigation behavior, renderer customizations, and supported Android versions.
The current implementation uses handlers, AndroidX fragments, and navigation
components; it is not a Jetpack Compose implementation.

The findings below come from source inspection. Failure scenarios are proposed
regression cases, not device-reproduced failures. No application builds, device
tests, or performance measurements were run for this assessment. Source locations
are repository-relative; use member names because line numbers can change.

## Preserve the useful foundations

- Reuse `StackNavigationManager` and AndroidX navigation rather than introducing
  another native navigation engine.
- Preserve Shell routing, navigation cancellation, page lifecycle events,
  `BackButtonBehavior`, and existing public navigation APIs.
- Keep page creation lazy and retain managed page state independently of native
  view lifetime.
- Continue sharing drawer, toolbar, and tab infrastructure where ownership is
  explicit.
- Reuse existing renderer/handler variant tests to establish behavioral parity.

## 1. Align stack ownership with the managed contract

**Priority: high. Source finding with an unverified failure scenario.**

`ShellSection` owns a shared `_navStack`. Its
`IShellSectionController.PresentedPage` implementation returns the pushed top page
before considering the selected `ShellContent`. In contrast,
`ShellContentNavigationFragment` creates a native navigation manager per content
and maintains its own `_logicalNavigationStack`.

Independent bottom-tab stacks, where tabs are separate sections, should not be
confused with independent content stacks inside one section. Giving the latter
independent native histories without changing the managed contract creates
competing representations of navigation.

Scenario to verify: select content A, push a detail page, select content B in the
same section, then pop. Repeat with B never visited and with B already initialized.
Compare the displayed page, Shell.CurrentPage, managed stack, and native stack.

Recommendation:

- Keep the managed section stack authoritative for existing behavior.
- Reconcile the selected content's native presentation with that stack.
- Do not silently introduce per-content managed histories as part of a native
  modernization. Such behavior needs a separate API and compatibility decision.

Sources: `src/Controls/src/Core/Shell/ShellSection.cs`,
`src/Controls/src/Core/Handlers/Shell/ShellContentNavigationFragment.Android.cs`.

## 2. Associate completion with individual requests

**Priority: high. Source finding.**

`ShellContentNavigationFragment.OnNavigationRequested` assigns `e.Task` only when
the manager is not navigating. Requests received during another transition can
therefore return without an awaitable native completion. The fragment also uses
one completion source rather than a completion identity for every queued request.

`StackNavigationManager.NavigationFinished` clears `ActiveRequestedArgs`, calls
the consumer, and only then drains the queue. The fragment tests `IsNavigating`
inside that callback and can clear its logical pending stack even though queued
work remains. Completing its task can execute a continuation synchronously in
this window.

Recommendation:

- Give every accepted operation a completion identity.
- Preserve request ordering, including navigation triggered by completion or
  page lifecycle callbacks.
- Distinguish active-operation completion from queue exhaustion.
- Settle every operation exactly once: success, failure, or cancellation.
- Consider asynchronous task continuations where compatible with established
  lifecycle ordering; this does not replace correct queue ownership.
- Define how requests deferred before host attachment or during saved state
  complete, rather than relying on an absent task.

Sources: `ShellContentNavigationFragment.OnNavigationRequested`,
`ShellContentNavigationFragment.OnNavigationFinished`,
`src/Core/src/Platform/Android/Navigation/StackNavigationManager.cs`,
`ShellSection.OnPushAsync`.

## 3. Separate navigation success from lifecycle teardown

**Priority: high. Source finding; user-visible effects need reproduction.**

`StackNavigationManager.OnNavigationViewFragmentDestroyed` completes an active
request when a navigation fragment's view is destroyed. `Disconnect` clears
queued requests and calls the same completion path for active navigation.
Destruction can represent a normal transition, but also interruption or teardown.

Recommendation:

- Define completion as the expected destination and stack being applied, with
  presentation ready; document animation timing separately.
- Correlate lifecycle callbacks with the active operation and expected native
  destination rather than treating any relevant destruction as success.
- Explicitly settle interrupted operations during disconnect.
- Clear connection-specific deferred state and reject callbacks from previous
  connections.
- Never report an unapplied operation as successful merely to release an await.

## 4. Preserve destination-specific handler selection

**Priority: high for applications with custom handlers. Source finding.**

`ShellHandler.SwitchToItem` reuses the initial `ShellItemHandler`.
`ShellItemHandler.SwitchToShellItem` changes its virtual view without resolving a
handler for the new item's runtime type.

If different derived ShellItem types register different handlers, the second item
can continue using the first item's handler implementation.

Recommendation:

- Keep a stable native host if useful, but resolve destination handlers through
  the handler service.
- Reuse only when handler type, context, and ownership are compatible.
- Test two derived ShellItem types with different registered handlers.
- Preserve the legacy renderer path for existing renderer subclasses.
- Audit whether retained factory hooks are actually invoked by the new path.
  Keeping a method such as `CreateFragmentForPage` does not itself preserve its
  old customization behavior.

Sources: `src/Controls/src/Core/Handlers/Shell/ShellHandler.Android.cs`,
`src/Controls/src/Core/Handlers/Shell/ShellItemHandler.Android.cs`.

## 5. Coordinate back navigation before gesture commit

**Priority: high. Source finding; animation behavior needs device validation.**

`ShellItemWrapperFragment.ShellBackPressedCallback` starts enabled and forwards
unhandled back presses after temporarily disabling itself. The activity already
has state-aware callback registration through `IBackNavigationState`.

An enabled Shell callback at the root can intercept a gesture before the system
chooses its predictive back-to-home behavior. Forwarding at commit is too late to
choose that preview. Consumed Shell back presses also take a different route from
the activity lifecycle event chain.

Recommendation:

- Establish a coordinated ownership and priority policy across activity, Shell,
  modal, drawer, and native callbacks.
- Update callback enabled state before gestures based on actual consumable state.
- Preserve page overrides, commands, cancellation, and lifecycle interception.
- Distinguish back commit support from interactive predictive-back animations.
- Delegate to AndroidX compatibility APIs for supported older Android versions.

Sources: `ShellItemWrapperFragment.Android.cs`, `ShellItemHandler.OnBackButtonPressed`,
`src/Core/src/Platform/Android/MauiAppCompatActivity.cs`, and
`src/Controls/src/Core/Window/Window.Android.cs`.

## 6. Separate view lifetime, retained state, and restoration

**Priority: medium to high. Source findings and lifecycle risks.**

The implementation disables pager state saving, retains all section/content
fragments through offscreen limits, and uses parameterless restored fragments
that return empty views when managed dependencies are unavailable.

These choices avoid some restoration paths but do not prove process-death
restoration. Lazy page creation reduces startup work; it does not bound retained
native resources after visiting many tabs.

`ShellSectionWrapperFragment` also retains its view without an `OnDestroyView`
cleanup path. Handler disconnection and fragment view destruction need separate,
explicit ownership rules.

Recommendation:

- Separate managed page retention from view-scoped adapters, listeners, and views.
- Register and release view resources symmetrically on view creation/destruction.
- Retain the existing default activity recreation policy and separately validate
  `AllowFragmentRestore=true`; do not promise automatic route persistence.
- If restoration is supported, reconnect using stable identities and authoritative
  managed state rather than constructor-only references.
- Guard deferred tab-selection callbacks with connection generation and target
  identity, not only an index into a potentially changed collection.
- Measure retained memory after repeated tab visits, removal, and reconnection.

Sources: `ShellSectionWrapperFragment.Android.cs`,
`ShellSectionHandler.SetupViewPagerAdapter`,
`ShellItemHandler.SetupViewPagerAdapter`,
`ShellContentNavigationFragment.OnCreate`,
`ViewPagerPageChangeCallback.OnPageSelected`.

## Target responsibilities

```text
Managed Shell state and cancellation policy
                    |
Ordered operations and per-operation completion
                    |
AndroidX stack reconciliation
                    |
View-scoped fragments, toolbar, tabs, and drawer
```

These are responsibilities, not a requirement to add four new abstractions.
Prefer clarifying existing classes before introducing new coordinators.

The managed layer owns the intended stack and selected destination. The native
layer renders that state and reports operation outcomes. Fragments own native
view resources, not independent navigation history or completion policy.

## Compatibility-first implementation order

1. Establish section/content stack ownership and characterization tests.
2. Make queued and reentrant requests deterministic, with individual completion.
3. Separate teardown outcomes from normal navigation completion.
4. Correct destination-specific handler resolution and customization parity.
5. Coordinate back handling and predictive-back registration.
6. Harden view recreation, deferred callbacks, and resource release.
7. Provide an explicit legacy/new-handler selection for rollout, following the
   existing iOS feature-switch pattern where appropriate.

Android currently registers the new Shell handler unconditionally in
`src/Controls/src/Core/Hosting/AppHostBuilderExtensions.cs`. A rollout switch
should select a coherent set of Shell handlers and must preserve explicit
application registrations.

## Focused validation matrix

| Scenario | Required evidence |
| --- | --- |
| Top-content switch after push | Managed and native stacks agree for visited and unvisited content. |
| Rapid and reentrant push/pop | No lost pages; every returned task settles for its own operation. |
| Navigation while state is saved | Correct reconciliation on resume; no stranded completion. |
| Disconnect during a transition | No stale callbacks, hangs, or false successful completion. |
| Different derived ShellItem handlers | Correct registered handler is used for each item. |
| Root gesture back | System fallback and preview work when nothing consumes back. |
| Modal, flyout, command, and cancelled back | Correct priority; no duplicate pops or skipped interception. |
| Cancelled tab switch plus collection mutation | No stale-index selection or update of a replaced view. |
| View recreation and Shell reconnection | Correct toolbar, stack, observers, and lazy initialization. |
| Repeated tab visits and removal | Retained native resources stabilize and removed content is released. |
| Legacy renderer customizations | Supported hooks remain effective through the legacy path. |

Run relevant scenarios on the minimum supported Android version and on API
33-36, with gesture and button navigation where available. Extend to newer API
levels supported by the branch. Run parity cases with legacy renderers and new
handlers.

Existing starting points:

- `src/Controls/tests/DeviceTests/Elements/Shell/ShellTests.Android.cs`
- `src/Controls/tests/DeviceTests/Elements/Shell/ShellHandlerSubclasses.Android.cs`
- `src/Controls/tests/DeviceTests/Elements/Shell/ShellLazyStartupTests.Android.cs`
- `src/Controls/tests/DeviceTests/Elements/Shell/RendererHandlerVariant.cs`

Use focused managed tests for queue/state behavior where possible, device tests
for native lifecycle and handler behavior, and gesture-level validation for
predictive back. Existing tests are starting points, not proof that the proposed
failure cases already pass or fail.
