# Android Material 3 Architecture Analysis

## Scope

This document compares the official Android Material 3 architecture with the
Material 3 implementation currently present in .NET MAUI for Android.

The analysis is based on the repository source as of July 31, 2026. It focuses
on architecture rather than visual correctness for individual controls.

## Executive summary

Official Android Material 3 is a token-driven design system:

```text
Brand or system input
        |
Reference tokens
        |
Semantic system tokens
        |
Component tokens
        |
Theme-aware components
        |
Rendered interaction state
```

The current .NET MAUI Android implementation is primarily a runtime-selectable
native-control architecture:

```text
UseMaterial3 MSBuild property
        |
Runtime feature switch
        |
Application and control themes
        |
Material 3 handler selection
        |
Native Material control wrappers
        |
MAUI property mappers and events
```

The implementation successfully introduces Material 3 native controls without
removing the existing Material Components implementation. However, it does not
yet constitute a complete Material 3 design-system layer. It is best described
as a transitional, control-by-control Material 3 backend for MAUI handlers.

## Reference Android Material 3 architecture

Android Material 3 separates visual intent from concrete values through three
levels of design tokens:

1. Reference tokens contain source palettes, font families and dimensions.
2. System tokens assign semantic roles such as `primary`, `surface`,
   `onSurface` and `bodyLarge`.
3. Component tokens map those semantic roles to parts and states of a
   component.

In Jetpack Compose, `MaterialTheme` distributes `ColorScheme`, `Typography` and
`Shapes` through composition. Material components read those values and apply
component defaults for enabled, disabled, focused, pressed and selected states.
Dynamic color can replace the static color scheme with a scheme derived from
the Android system on supported devices.

Material 3 also includes adaptive architecture. Window size, posture and
navigation requirements can select different canonical layouts rather than
only resizing the same controls.

## Current .NET MAUI Android architecture

### Activation path

Material 3 is opt-in and disabled by default.

```text
<UseMaterial3>true</UseMaterial3>
        |
Microsoft.Maui.Controls.targets
        |
Microsoft.Maui.RuntimeFeature.IsMaterial3Enabled
        |
+-----------------------------+
|                             |
Application theme             Handler registration
MauiAppCompatActivity         AppHostBuilderExtensions
|                             |
Maui.Material3.Theme          Handler2 implementations
```

The Sandbox exposes the same switch in
`Maui.Controls.Sample.Sandbox.csproj`. Changing it requires rebuilding the
relevant artifacts because the value is emitted as a runtime host
configuration option.

Relevant source:

- `src/Controls/src/Build.Tasks/nuget/buildTransitive/netstandard2.0/Microsoft.Maui.Controls.targets`
- `src/Core/src/RuntimeFeature.cs`
- `src/Core/src/Platform/Android/MauiAppCompatActivity.cs`
- `src/Core/src/Platform/Android/MauiMaterialContextThemeWrapper.cs`

### Theme layer

`MauiAppCompatActivity` selects either the Material 3 or legacy application
theme. `MauiMaterialContextThemeWrapper` performs the same selection for
controls that require a themed context.

The Material 3 XML theme:

- Inherits from `Theme.Material3.DayNight`.
- Provides a no-action-bar variant.
- Selects Material 3 bottom navigation and button styles.
- Removes native button minimum sizes, padding and insets because MAUI owns
  layout measurement.

This gives MAUI access to Android Material 3 defaults while preserving MAUI's
cross-platform sizing contract.

Relevant source:

- `src/Core/src/Platform/Android/Resources/values/styles-material3.xml`
- `src/Core/src/Platform/Android/Resources/values/styles.xml`

### Handler selection layer

`AppHostBuilderExtensions.AddControlsHandlers` conditionally registers
Material 3 `Handler2` implementations when the feature switch is enabled.

The conditional set includes:

- `Label`
- `Editor`
- `Entry`
- `Picker`
- `DatePicker`
- `TimePicker`
- `SearchBar`
- `RadioButton`
- `Switch`
- `Slider`
- `ProgressBar`
- `ActivityIndicator`
- `Image`

Other controls use a shared handler that changes its native view or styling
internally. Examples include `Button`, `CheckBox` and `ImageButton`.

This produces two Material 3 adoption patterns:

1. Registration-time replacement with a `Handler2` implementation.
2. Runtime branching inside an existing handler or platform helper.

The first pattern creates a clean compatibility boundary. The second reduces
duplication but spreads Material 3 conditions through existing code.

Relevant source:

- `src/Controls/src/Core/Hosting/AppHostBuilderExtensions.cs`
- `src/Core/src/Handlers/*/*Handler2.Android.cs`
- `src/Core/src/Handlers/Button/ButtonHandler.Android.cs`
- `src/Core/src/Handlers/CheckBox/CheckBoxHandler.Android.cs`
- `src/Core/src/Handlers/ImageButton/ImageButtonHandler.Android.cs`

### Handler and native-control layers

The `Handler2` implementations follow the normal MAUI handler lifecycle:

```text
MAUI virtual view property
        |
PropertyMapper or CommandMapper
        |
Handler2 mapping method
        |
Android extension method
        |
Native Material component
```

Each handler generally:

1. Creates a Material-themed platform view.
2. Maps MAUI properties and commands to Android APIs.
3. Connects native listeners.
4. Propagates native state changes back to the virtual view.
5. Disconnects and disposes listeners during teardown.

Thin wrapper controls adapt Android Material components to MAUI behavior. For
example, Entry and SearchBar use `TextInputLayout` with
`TextInputEditText`, while Label uses `MaterialTextView`.

Relevant source:

- `src/Core/src/Handlers/Entry/EntryHandler2.Android.cs`
- `src/Core/src/Handlers/Editor/EditorHandler2.Android.cs`
- `src/Core/src/Handlers/SearchBar/SearchBarHandler2.Android.cs`
- `src/Core/src/Handlers/Picker/PickerHandler2.Android.cs`
- `src/Core/src/Handlers/DatePicker/DatePickerHandler2.Android.cs`
- `src/Core/src/Platform/Android/Material3Controls/`
- `src/Core/src/Platform/Android/MauiMaterialSearchBarTextInputLayout.cs`

### Controls integration layer

Controls-specific mapper extensions remain in the Controls assembly. Entry,
Editor and SearchBar conditionally append mappings for their Material 3
handlers.

This maintains an important boundary:

```text
Microsoft.Maui.Core
    Native views, handlers and Android property updates

Microsoft.Maui.Controls
    Controls behavior, handler registration and mapper remapping
```

The separation is consistent with existing MAUI architecture, although
conditional Material 3 mapping is currently implemented independently for
several controls.

Relevant source:

- `src/Controls/src/Core/Entry/Entry.Mapper.cs`
- `src/Controls/src/Core/Editor/Editor.Mapper.cs`
- `src/Controls/src/Core/SearchBar/SearchBar.Mapper.cs`

## Architecture comparison

| Area | Official Android Material 3 | Current .NET MAUI implementation | Assessment |
| --- | --- | --- | --- |
| Activation | Material theme selected as part of the application design system | MSBuild property converted to a runtime feature switch | Strong migration and compatibility mechanism |
| Theme propagation | `MaterialTheme` or Android theme attributes provide semantic values | Activity and per-control `ContextThemeWrapper` select Material 3 XML themes | Correct for native Views, but narrower than a complete design system |
| Color architecture | Tonal palettes mapped to semantic color roles | Primarily inherited theme attributes, with some hard-coded fallback colors in Controls and Shell | Partial |
| Typography | Semantic type scale and component typography tokens | Native theme defaults plus MAUI font property mappings | Partial |
| Shape | Shape scheme and component shape tokens | Native widget defaults plus control-specific MAUI shape handling | Partial |
| Component model | Components consume component tokens and expose supported customization | MAUI handlers create Material widgets and map MAUI properties | Strong platform integration |
| State model | Standard state layers and tokenized colors for pressed, focused, selected and disabled states | Mostly delegated to native widgets; MAUI mappings override selected properties | Good baseline, but override consistency must be tested |
| Dynamic color | System-derived schemes on supported Android versions | No centralized dynamic-color pipeline was identified | Missing |
| Adaptive design | Window size, posture and canonical adaptive layouts | Existing MAUI layout and Foldable APIs are separate from Material 3 activation | Outside the current Material 3 implementation |
| Extensibility | Custom tokens and components extend the design system | Internal `Handler2` and wrapper classes, with several planned for later public exposure | Transitional |
| Compatibility | Migration generally occurs at theme and component boundaries | Legacy handlers remain available and are selected when the switch is disabled | Strong |
| Testing | Theme, component, accessibility and adaptive behavior | Broad Android feature and screenshot coverage for multiple controls | Strong visual coverage; limited design-system coverage |

## Key findings

### 1. The implementation is a platform backend, not a cross-platform token system

The repository currently uses Android Material 3 themes and native widgets to
modernize the Android rendering backend. There is no central MAUI abstraction
equivalent to Material 3 reference, system and component tokens.

This is not inherently incorrect. A MAUI `Color`, `Font`, `Style` or
`ResourceDictionary` remains the application-facing design layer. The
architectural limitation is that MAUI resources and Android Material semantic
roles are not connected by a clearly defined token contract.

### 2. The runtime switch provides a safe migration boundary

Keeping the existing handlers and registering Material 3 handlers only when
enabled reduces regression risk and supports direct A/B testing. It also allows
the Material 3 implementation to mature without immediately changing every
application.

The cost is that both rendering paths must remain functional until Material 3
becomes the default or the legacy path is removed.

### 3. Material 3 adoption is intentionally uneven

Some controls use dedicated `Handler2` classes, while other controls branch
inside existing handlers or helpers. Shell, scrolling, ripple behavior and
compatibility renderers also contain feature-switch checks.

This reflects incremental adoption, but it makes the full Material 3 surface
harder to discover and reason about. A control inventory or shared strategy for
choosing between replacement handlers and internal branching would reduce this
complexity.

### 4. Native defaults are doing most of the token work

Inheriting from `Theme.Material3.DayNight` and using `Widget.Material3.*`
provides Android's component defaults, state handling and theme attributes.
This is an effective way to obtain Material 3 behavior with limited duplicated
code.

It also means that behavior may vary when MAUI overrides native background,
padding, size, color or shape properties. Each override must preserve the
native component's Material 3 state behavior.

### 5. The implementation is still transitional

Several `Handler2`, extension and platform types are internal and include
comments indicating intended public exposure in a later .NET release. That
reduces the compatibility commitment during development, but applications
cannot yet depend on these types as a stable customization surface.

### 6. Dynamic color is the largest design-system gap

No centralized mechanism was identified for:

- Obtaining Android dynamic color palettes.
- Translating those palettes into MAUI resources.
- Applying the result consistently to Shell and controls.
- Falling back to an application brand scheme on unsupported devices.

Some Controls and Shell defaults use explicit Material-like color values.
Those values provide a predictable appearance but do not form a semantic,
theme-derived color system.

### 7. Adaptive architecture is separate

The Material 3 switch does not select adaptive navigation or content patterns.
MAUI layout, Shell and Foldable features continue to own those concerns.

This separation is reasonable for a cross-platform framework, but the current
implementation should not be described as complete Material 3 adaptive
architecture.

## Strengths

- Preserves compatibility through an opt-in switch.
- Uses official Android Material 3 themes and widgets.
- Keeps MAUI's virtual-view and property-mapper architecture.
- Isolates complex native behavior in thin platform wrappers.
- Retains Controls/Core assembly responsibilities.
- Delegates standard interaction states to native Material widgets.
- Includes broad Android feature tests and screenshot baselines.
- Allows legacy and Material 3 behavior to be compared in the same codebase.

## Risks and limitations

- Material 3 conditions are distributed across handler registration, handlers,
  Controls mappers, Shell, compatibility renderers and platform helpers.
- There is no centralized semantic token model shared by MAUI resources and
  Android themes.
- Hard-coded Material color values can diverge from application themes and
  future Material revisions.
- Dynamic color is not exposed as an integrated application capability.
- Shared handlers and replacement handlers use different migration patterns.
- Internal `Handler2` APIs limit application-level extensibility.
- Native component behavior can be unintentionally lost when MAUI replaces
  backgrounds, shapes, padding or colors.
- Visual screenshot coverage does not by itself validate contrast, font scale,
  touch targets, screen-reader semantics or dynamic themes.

## Recommended architectural direction

### Priority 1: Define the supported Material 3 surface

Maintain a single inventory of:

- Controls using a replacement handler.
- Controls using an existing handler with Material 3 branching.
- Controls inheriting Material 3 behavior only from the application theme.
- Controls that are not yet Material 3 compliant.

This would make migration status explicit and help prevent accidental gaps.

### Priority 2: Centralize semantic theme resolution

Introduce an internal Android Material 3 theme service or resolver that owns
semantic colors and other theme-derived values. Platform code should query
semantic roles rather than repeat fixed Material color values.

The resolver could initially remain Android-specific without introducing a
new public cross-platform design-system API.

### Priority 3: Add dynamic color as an explicit policy

Dynamic color should be opt-in and should define:

- Android version support.
- Light and dark scheme generation.
- Brand fallback behavior.
- Mapping into MAUI application resources.
- Interaction with explicit control colors.

### Priority 4: Standardize the handler migration pattern

Document when a control requires `Handler2` and when an existing handler should
branch internally. Prefer registration-time replacement when the native
control hierarchy or event model changes significantly.

### Priority 5: Expand non-visual validation

Add focused coverage for:

- Light and dark theme attribute resolution.
- Resetting explicit colors back to theme defaults.
- Disabled, focused, pressed and selected states.
- Font scaling and minimum touch targets.
- Accessibility semantics.
- Handler connect/disconnect lifecycle.
- Future dynamic-color behavior.

### Priority 6: Stabilize customization points deliberately

Before making `Handler2` types public, identify the smallest stable public
surface required for custom handlers. Avoid exposing temporary native wrapper
details that would constrain later architecture changes.

## Maturity assessment

| Dimension | Assessment |
| --- | --- |
| Native Material 3 component adoption | Medium to high for the currently targeted controls |
| Compatibility strategy | High |
| Handler architecture integration | High |
| Theme and token abstraction | Low to medium |
| Dynamic color | Low |
| Adaptive Material 3 architecture | Not part of the current implementation |
| Public extensibility | Low while `Handler2` remains internal |
| Visual regression coverage | High for covered controls |
| Accessibility and semantic-theme coverage | Requires further evidence |

## Conclusion

The current .NET MAUI Android Material 3 work has a sound incremental
architecture. It preserves the established MAUI handler pipeline and replaces
the Android rendering layer at controlled boundaries. This is a practical and
low-risk route for adopting Material 3 native components.

Its present scope is narrower than official Android Material 3 architecture.
The source implements theme selection, native component substitution and
property mapping, but not a complete token, dynamic-color or adaptive-design
system. The next architectural step should be central semantic theme
resolution rather than adding more isolated feature-switch checks or
hard-coded Material defaults.

## Primary source map

- `src/Core/src/RuntimeFeature.cs`
- `src/Controls/src/Build.Tasks/nuget/buildTransitive/netstandard2.0/Microsoft.Maui.Controls.targets`
- `src/Controls/src/Core/Hosting/AppHostBuilderExtensions.cs`
- `src/Core/src/Platform/Android/MauiAppCompatActivity.cs`
- `src/Core/src/Platform/Android/MauiMaterialContextThemeWrapper.cs`
- `src/Core/src/Platform/Android/Resources/values/styles-material3.xml`
- `src/Core/src/Platform/Android/Material3Controls/`
- `src/Core/src/Handlers/Entry/EntryHandler2.Android.cs`
- `src/Core/src/Handlers/Editor/EditorHandler2.Android.cs`
- `src/Core/src/Handlers/SearchBar/SearchBarHandler2.Android.cs`
- `src/Core/src/Handlers/Picker/PickerHandler2.Android.cs`
- `src/Core/src/Handlers/DatePicker/DatePickerHandler2.Android.cs`
- `src/Controls/src/Core/Entry/Entry.Mapper.cs`
- `src/Controls/src/Core/Editor/Editor.Mapper.cs`
- `src/Controls/src/Core/SearchBar/SearchBar.Mapper.cs`
- `src/Controls/src/Core/Shell/Shell.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/`
- `src/Controls/tests/TestCases.Shared.Tests/Tests/FeatureMatrix/`
- `src/Controls/tests/TestCases.Android.Tests/snapshots/`

## External references

- [Material 3 design tokens](https://m3.material.io/foundations/design-tokens)
- [Material 3 in Jetpack Compose](https://developer.android.com/develop/ui/compose/designsystems/material3)
- [Build adaptive apps with Jetpack Compose](https://developer.android.com/develop/ui/compose/build-adaptive-apps)
- [Migrate from Material 2 to Material 3](https://developer.android.com/develop/ui/compose/designsystems/material2-material3)
