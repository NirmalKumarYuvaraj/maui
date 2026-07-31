# Android Material 3 Migration Plan

## Objective

Migrate .NET MAUI Android controls to Material 3 while preserving existing
application behavior, customization points and legacy rendering.

The migration must remain opt-in until the Material 3 implementation has
complete control coverage and satisfies defined compatibility, accessibility
and quality gates.

This plan complements
[`MATERIAL3_ANDROID_ARCHITECTURE_ANALYSIS.md`](MATERIAL3_ANDROID_ARCHITECTURE_ANALYSIS.md).

## Non-negotiable compatibility principles

1. `<UseMaterial3>false</UseMaterial3>` must preserve the existing handlers,
   themes, measurements and behavior.
2. Existing MAUI properties, events, mapper modifications and custom handlers
   must continue to work in both rendering modes.
3. Explicit application values must override Material defaults.
4. Clearing an explicit value must restore the active theme value.
5. Material 3 must not become the default until all rollout gates pass.
6. Legacy handlers must not be removed without a documented deprecation cycle.
7. Transitional `Handler2` APIs must remain internal until their architecture
   and customization model are stable.
8. Dynamic color must be introduced independently from basic Material 3
   adoption.

## Target architecture

```text
Application configuration
        |
Material3Configuration
        |
Android semantic theme resolver
        |
+----------------------+----------------------+
|                                             |
Material 3 handlers                         Legacy handlers
|                                             |
Material 3 native controls                  Existing native controls
|                                             |
+----------------------+----------------------+
                       |
            MAUI property and event contract
```

The feature switch should remain the public activation mechanism during the
migration, but implementation code should consume centralized configuration
and theme services rather than directly branching on
`RuntimeFeature.IsMaterial3Enabled`.

## Phase 1: Establish the migration baseline

**Status:** Complete.

The baseline is recorded in
[`MATERIAL3_ANDROID_CONTROL_INVENTORY.md`](MATERIAL3_ANDROID_CONTROL_INVENTORY.md).

### Work

Create and maintain a control inventory containing:

| Field | Purpose |
| --- | --- |
| Control | MAUI control being migrated |
| Current strategy | Replacement handler, shared handler or theme-only |
| Legacy implementation | Handler and native control used when disabled |
| Material 3 implementation | Handler, wrapper and Material component |
| Property coverage | Supported and missing mappings |
| State coverage | Enabled, disabled, pressed, focused and selected |
| Test coverage | Unit, device, UI and screenshot coverage |
| Known differences | Intentional visual or behavioral differences |
| Owner | Person or team responsible for completion |

Classify every Android control as:

- Replacement `Handler2`
- Existing handler with Material 3 branching
- Material 3 behavior inherited from the application theme
- Not migrated

### Deliverables

- Version-controlled control migration inventory.
- Documented owner and migration strategy for every Android control.
- List of legacy behavior that must remain unchanged.
- List of accepted Material 3 visual differences.

### Exit gate

Every Android control has an explicit migration status, implementation strategy
and compatibility-test status.

## Phase 2: Centralize Material 3 configuration

**Status:** Complete.

The implementation introduces
`src/Core/src/Material3Configuration.cs` as the single internal configuration
owner. Existing Core and Controls call sites now consume
`Material3Configuration.Enabled`; only that owner reads
`RuntimeFeature.IsMaterial3Enabled`.

The configuration currently preserves existing behavior:

- `Enabled` forwards the existing runtime feature switch.
- `Enabled` retains `FeatureSwitchDefinition` metadata for trimming and AOT.
- `DynamicColorEnabled` remains `false`.
- `ThemePolicy` selects `Legacy` or `Material3` from `Enabled`.
- `CompatibilityMode` remains `PreserveLegacyBehavior`.
- `UseMaterial3` and its default value are unchanged.
- Existing framework handler registration order is unchanged.

### Work

Introduce one internal configuration model:

```text
Material3Configuration
    Enabled
    DynamicColorEnabled
    ThemePolicy
    CompatibilityMode
```

Initially, `Enabled` should continue to be populated from `UseMaterial3`.

Consolidate feature checks currently distributed across:

- Handler registration
- Platform handlers
- Controls mappers
- Shell and compatibility renderers
- Platform helpers
- Ripple, scrolling and navigation behavior

Feature checks should occur at clear architectural boundaries rather than
inside individual property updates whenever possible.

### Compatibility requirements

- Preserve the current `UseMaterial3` MSBuild property.
- Preserve the generated runtime host configuration switch.
- Do not change default behavior.
- Continue supporting application-provided handler registration.

### Exit gate

Material 3 activation has one configuration owner, and new platform code does
not introduce direct feature-switch checks without an architectural reason.

**Exit-gate assessment:** Complete. The only direct
`RuntimeFeature.IsMaterial3Enabled` read is inside
`Material3Configuration.Enabled`.

## Phase 3: Centralize semantic theme resolution

**Status:** Complete - awaiting review.

The implementation introduces
`src/Core/src/Platform/Android/Material3ThemeResolver.cs` as the Android
semantic color owner.

The resolver:

- Maps Material 3 roles to Android theme attributes.
- Resolves `colorPrimary`, `colorSurface`, `colorSurfaceContainer`,
  `colorOnSurface` and `colorOnSurfaceVariant` from the active themed context.
- Applies alpha after resolving a semantic color.
- Uses the previous hard-coded Material 3 palette only when a required theme
  attribute cannot resolve.
- Reads colors on demand so runtime theme changes are not hidden by static
  caches.

Shell, flyout, toolbar, tabs, bottom navigation, page containers and more-sheet
content now consume semantic roles when a themed Android `Context` is
available. Context-free public fallback properties remain compatible, while
platform call sites use actual theme attributes.

The default Material 3 `FlyoutBackgroundColor` binding was removed so that an
unset value can resolve through `colorSurface`. Explicit application values
continue to take precedence.

### Work

Create an internal Android Material 3 theme resolver responsible for:

- Application and control theme selection
- Semantic color attributes
- Light and dark mode
- Typography defaults
- Shape and component styles
- State colors
- Restoring theme defaults after explicit values are cleared
- Future dynamic-color integration

Replace hard-coded Material-like colors in Shell, compatibility renderers and
platform helpers with semantic theme resolution.

Use Android theme attributes such as `colorSurface` and `colorOnSurface`
instead of copying fixed palette values into platform code.

### Property precedence

All controls must implement this precedence:

```text
Explicit MAUI property
        |
Application resource or style
        |
MAUI Material 3 semantic default
        |
Native Android Material 3 default
```

### Exit gate

Material 3 colors and component defaults are resolved semantically, and
clearing an explicit property reliably restores the active theme value.

**Exit-gate assessment:** Complete for the Phase 3 color scope. Material 3
platform defaults resolve from semantic Android attributes, legacy defaults
remain unchanged, and unresolved attributes use the previous Material 3
palette as an explicit fallback. Typography and shape standardization remain
part of later control-migration phases.

## Phase 4: Standardize handler migration

### Handler selection rule

Use a replacement handler when Material 3 changes:

- Native control hierarchy
- Event model
- Measurement behavior
- Focus or keyboard behavior
- Property mapping semantics

Use the existing handler with internal strategy selection only when the native
control structure and lifecycle remain equivalent.

### Required lifecycle

```text
Create themed native control
        |
Apply initial MAUI mappings
        |
Connect native events
        |
Synchronize native and virtual state
        |
Disconnect and dispose native events
```

### Handler requirements

Every migrated handler must preserve:

- Existing `PropertyMapper` behavior
- Existing `CommandMapper` behavior
- Custom mapper modifications
- Custom handler registration
- Focus and keyboard behavior
- Automation identifiers
- Accessibility semantics
- Native-to-virtual state propagation
- Handler reconnection
- Listener disposal

### Exit gate

New migrations follow one documented handler-selection strategy and satisfy a
shared lifecycle checklist.

## Phase 5: Migrate controls in batches

Controls should be migrated in increasing order of behavioral and navigation
complexity.

### Batch 1: Foundation controls

- Label
- Image
- ActivityIndicator
- ProgressBar

Focus on theme defaults, measurement, explicit colors and enabled state.

### Batch 2: Selection controls

- CheckBox
- RadioButton
- Switch
- Slider

Focus on checked, selected, disabled and interaction-state colors.

### Batch 3: Text input controls

- Entry
- Editor
- SearchBar

Focus on native hierarchy, placeholder behavior, keyboard, focus, clear
buttons, validation states and text-selection behavior.

### Batch 4: Picker controls

- Picker
- DatePicker
- TimePicker

Focus on dialogs, constraints, localization, formatting and state propagation.

### Batch 5: Action controls

- Button
- ImageButton

Focus on shape, ripple, icon sizing, content layout and minimum touch targets.

### Batch 6: Navigation and containers

- Shell
- Bottom navigation
- Tabs
- Flyout
- Toolbars
- Alerts and dialogs

Focus on semantic theme roles, navigation state, system bars and compatibility
renderers.

### Batch 7: Collections and complex controls

- CollectionView
- CarouselView
- ListView compatibility paths
- Cell renderers

Focus on recycling, selection, scrolling, item states and performance.

### Batch completion rule

Do not begin the next batch until the current batch satisfies its behavioral,
visual, accessibility and lifecycle gates.

## Phase 6: Preserve property compatibility

### Required transition tests

For every theme-sensitive property:

1. Render with the theme default.
2. Assign an explicit MAUI value.
3. Change light or dark mode.
4. Clear the explicit value.
5. Disable and re-enable the control.
6. Disconnect and recreate the handler.
7. Change relevant application resources at runtime.

### Expected behavior

- Explicit values remain authoritative while assigned.
- Theme changes update values that are not explicitly assigned.
- Clearing a value restores the current theme, not a cached earlier value.
- Recreated handlers produce the same result as existing handlers.
- The legacy rendering path remains unchanged.

## Phase 7: Build the compatibility test matrix

Every migrated control must be evaluated across:

| Dimension | Required cases |
| --- | --- |
| Rendering mode | Legacy and Material 3 |
| Theme | Light and dark |
| Property source | Default, style, resource and explicit value |
| State | Normal, pressed, focused, selected and disabled |
| Text | Default font scale, large font scale and RTL |
| Lifecycle | Created, reparented, disconnected and recreated |
| Android version | Minimum, representative and latest supported API |
| Navigation | Standalone, navigation page and Shell |
| Customization | Mapper modification and custom handler |

### Test layers

- Unit tests for mapper and semantic theme resolution.
- Device tests for native property and lifecycle behavior.
- UI tests for interaction and accessibility behavior.
- Screenshot tests for intentional visual output.

Screenshot tests must not be the only evidence because they cannot reliably
validate event behavior, semantics, focus, keyboard interaction or listener
cleanup.

## Phase 8: Validate accessibility and system integration

### Required coverage

- Screen-reader names, roles and states
- Focus order
- Minimum touch targets
- Large font scaling
- High-contrast behavior where supported
- RTL layout and text
- System dark-mode changes
- Edge-to-edge content
- System bars
- Predictive back behavior

### Exit gate

Migrated controls meet the same accessibility contract as legacy controls and
do not depend solely on native visual defaults.

## Phase 9: Introduce dynamic color separately

Dynamic color should be an independent opt-in option after static Material 3
themes stabilize.

### Required policy

- Supported Android versions
- Light and dark scheme generation
- Brand fallback scheme
- Mapping into MAUI resources
- Interaction with explicit control values
- Runtime wallpaper or theme changes
- Application-level opt-out

### Compatibility rule

Enabling Material 3 must not automatically enable dynamic color. Applications
must be able to adopt Material 3 components while retaining a fixed brand
scheme.

## Phase 10: Stabilize customization APIs

Before exposing `Handler2` implementations publicly:

1. Confirm the native control hierarchy is final.
2. Confirm mapper names and responsibilities are stable.
3. Identify required subclassing and customization scenarios.
4. Expose the smallest sustainable public surface.
5. Document migration from custom legacy handlers.
6. Avoid exposing temporary native wrapper details.

Public API stabilization should follow implementation maturity rather than
being used as a mechanism to complete the migration.

## Phase 11: Preview rollout

### Rollout sequence

1. Keep Material 3 opt-in.
2. Publish supported controls and known differences.
3. Label incomplete controls explicitly.
4. Collect visual and behavioral regressions separately.
5. Provide a minimal migration guide for application developers.
6. Maintain the legacy escape path.

### Preview promotion gate

- No unresolved critical regressions.
- Primary control batches are complete.
- Shell and navigation behavior is stable.
- Custom handlers and mapper modifications are supported.
- Accessibility coverage is complete.
- Performance is not materially worse than legacy rendering.

## Phase 12: Default-on rollout

Change Material 3 to the default only at a planned release boundary.

Retain an explicit escape hatch:

```xml
<UseMaterial3>false</UseMaterial3>
```

Support the opt-out for at least one complete release cycle. Document
intentional visual differences separately from compatibility defects.

### Default-on gate

- All supported controls have a documented Material 3 path.
- Legacy and Material 3 compatibility suites pass.
- No critical accessibility gaps remain.
- Dynamic color remains separately configurable.
- Public customization guidance is available.
- Clean and incremental builds produce consistent switch behavior.

## Phase 13: Legacy deprecation and removal

Legacy handlers should be deprecated only after:

- Material 3 has been the default for at least one release cycle.
- Regression volume is acceptable.
- Custom-handler migration guidance is complete.
- The remaining opt-out usage is understood.
- Removal is announced as a breaking change.

Legacy implementation removal should occur in a major release. Until removal,
legacy paths must continue receiving compatibility fixes rather than being
allowed to decay.

## Per-control definition of done

A control is fully migrated only when it:

- Uses the intended Material 3 native component or style.
- Preserves all existing MAUI properties, commands and events.
- Preserves custom handler and mapper customization.
- Supports light and dark themes.
- Correctly applies application styles and resources.
- Restores theme defaults when explicit values are cleared.
- Handles enabled, disabled, focused, pressed and selected states.
- Supports large fonts, RTL and accessibility semantics.
- Correctly connects and disconnects native listeners.
- Passes legacy and Material 3 behavioral tests.
- Has separate visual baselines for both rendering modes.
- Introduces no Material 3 behavior when the feature is disabled.

## Program-level success criteria

The migration is complete when:

1. Material 3 has complete documented control coverage.
2. Semantic theme resolution replaces hard-coded platform defaults.
3. Handler migration follows one documented strategy.
4. Legacy behavior remains covered by automated tests.
5. Material 3 satisfies behavioral, visual, lifecycle and accessibility gates.
6. Dynamic color is available as an independent policy.
7. Customization APIs are stable and documented.
8. Material 3 can become the default without removing the legacy escape path.

## Recommended immediate actions

1. Create the control migration inventory.
2. Introduce centralized internal Material 3 configuration.
3. Design the semantic Android theme resolver.
4. Replace hard-coded Shell Material colors with semantic theme attributes.
5. Standardize the `Handler2` migration checklist.
6. Complete the compatibility matrix for the existing Entry, Editor and
   SearchBar implementations before migrating additional complex controls.
