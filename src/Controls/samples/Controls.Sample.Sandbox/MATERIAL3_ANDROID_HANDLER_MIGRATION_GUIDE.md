# Android Material 3 Handler Migration Guide

## Purpose

This guide defines when an Android control should use a Material 3 replacement
handler, when it must remain on a shared handler, and which compatibility rules
every migration must satisfy.

It is the Phase 4 implementation contract for future control migration work.

## Handler selection decision

Use a replacement handler only when Material 3 changes at least one of:

- Native view hierarchy
- Native event model
- Dialog or controller behavior
- Focus or keyboard behavior
- Measurement contract
- Native-to-virtual state synchronization

Keep the existing handler when Material 3 only changes:

- Theme attributes
- Native style selection
- Drawable or ripple defaults
- Semantic colors
- Shape defaults
- Behavior implemented by a shared platform view

```text
Does Material 3 change the native control contract?
        |
   +----+----+
   |         |
  Yes        No
   |         |
Replacement Shared handler
handler     or platform branch
```

Visual differences alone are not sufficient justification for creating a
replacement handler.

## Current replacement handlers

The following controls currently use registration-time replacement handlers:

| Control | Material 3 handler | Replacement reason |
| --- | --- | --- |
| Label | `LabelHandler2` | Native Material text view |
| Editor | `EditorHandler2` | Material edit-text behavior and mappings |
| Entry | `EntryHandler2` | `TextInputLayout` with child edit text |
| Picker | `PickerHandler2` | Material text input and dialog behavior |
| DatePicker | `DatePickerHandler2` | Material input and date-dialog lifecycle |
| TimePicker | `TimePickerHandler2` | Material input and time-dialog lifecycle |
| SearchBar | `SearchBarHandler2` | `TextInputLayout` hierarchy replaces search view |
| RadioButton | `RadioButtonHandler2` | Material native component |
| Switch | `SwitchHandler2` | Material native component |
| Slider | `SliderHandler2` | Material slider event and state behavior |
| ProgressBar | `ProgressBarHandler2` | Material progress component |
| ActivityIndicator | `ActivityIndicatorHandler2` | Material progress component |
| Image | `ImageHandler2` | Material shape-aware image component |

This list is owned by:

`src/Controls/src/Core/Hosting/Material3HandlerRegistration.Android.cs`

Adding a control to that file requires documenting which native contract
changed.

## Current shared handlers

The following controls must remain shared unless their native contract changes:

| Control | Shared implementation |
| --- | --- |
| Button | Existing handler with themed `MauiMaterialButton` |
| CheckBox | Existing handler with themed Material checkbox |
| ImageButton | Existing handler selecting the native image view |
| ScrollView | Existing handler with shared platform behavior |
| CollectionView | Existing Android items handler and `MauiRecyclerView` |
| CarouselView | Existing Android items handler and `MauiRecyclerView` |

Creating `Handler2` implementations for these controls solely to apply colors,
styles or shapes would duplicate behavior and increase compatibility risk.

## Central registration contract

Framework defaults are registered in this order:

1. `UseMauiApp` configures MAUI and Controls defaults.
2. Android registration selects Material 3 or legacy replacement handlers.
3. Shared handlers are registered normally.
4. User `ConfigureMauiHandlers` registrations run afterward.

The last registration for a virtual-view type wins. Therefore an application
registration such as:

```csharp
builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler<Label, CustomLabelHandler>();
});
```

must override both `LabelHandler` and `LabelHandler2`.

Material 3 activation must never re-register framework defaults after user
configuration has run.

## Required lifecycle order

Replacement handlers must follow this order:

```text
CreatePlatformView
        |
Capture native defaults
        |
base.ConnectHandler
        |
Register native listeners
        |
Apply and update mappings
        |
Remove native listeners
        |
Dispose owned native resources
        |
Clear references and cached defaults
        |
base.DisconnectHandler
```

### Connect requirements

- Call `base.ConnectHandler(platformView)` before registering listeners.
- Capture native defaults before explicit MAUI properties replace them.
- Register every native listener once.
- Avoid registering listeners from property mappers.
- Validate that callbacks can tolerate `VirtualView` becoming null.

### Disconnect requirements

- Remove every listener registered during connect or virtual-view setup.
- Clear native delegates such as dialog show/hide callbacks.
- Dismiss and detach owned dialogs.
- Dispose owned `Java.Lang.Object` listeners.
- Clear cached native defaults and handler references.
- Call `base.DisconnectHandler(platformView)` last.

## Mapper compatibility contract

Every replacement handler must preserve:

- The existing MAUI property names.
- The existing command names.
- Base `ViewMapper` and `ViewCommandMapper` chains.
- `AppendToMapping`, `PrependToMapping` and `ModifyMapping` customization.
- Controls-specific remapping.
- Property dependency ordering.

Controls remapping must target the selected handler without registering a
second handler.

Property updates must continue through:

```csharp
handler.UpdateValue(nameof(Property));
```

Calling mapper methods directly is not allowed because it bypasses user mapper
customizations.

## Native-default contract

When a MAUI property is unset or cleared:

1. Restore the captured native or semantic theme default.
2. Do not substitute a fixed color or drawable in the handler.
3. Re-resolve theme-dependent values when the themed context changes.
4. Preserve disabled, focused, pressed and selected native state lists.

Default capture and restoration are validated per control during Phase 5.

## Public extensibility contract

Before replacement handlers become public:

- User registrations must continue to take precedence.
- Stable property and command mappers must be available.
- Native platform views must be public or replaceable through protected
  factories.
- Applications must be able to target legacy, Material 3, or both.
- Public handlers must not expose temporary wrapper details.
- Registration and replacement behavior must be documented.

Making a `Handler2` class public does not by itself satisfy this contract.

## Per-handler review checklist

- [ ] The native contract requires a replacement handler.
- [ ] Legacy registration remains available when Material 3 is disabled.
- [ ] Shared controls are not duplicated.
- [ ] Base mapper chains are preserved.
- [ ] User mapper extensions still execute.
- [ ] Native defaults are captured before MAUI overrides.
- [ ] `base.ConnectHandler` is called before listener registration.
- [ ] Every listener is removed during disconnect.
- [ ] Owned Java listeners and dialogs are disposed.
- [ ] `base.DisconnectHandler` is called last.
- [ ] Reconnection does not duplicate callbacks.
- [ ] User custom-handler registration wins.
- [ ] Legacy and Material 3 behavior have focused tests.

## Phase 4 exit-gate assessment

| Requirement | Status |
| --- | --- |
| Material 3 and legacy replacement registrations have one owner | Complete |
| Replacement and shared-handler rules are documented | Complete |
| Existing registration order is preserved | Complete |
| User custom-handler precedence is covered | Complete |
| Replacement-handler base lifecycle ordering is normalized | Complete |
| Mapper customization contract is documented | Complete |
| Per-control default capture is deferred explicitly to Phase 5 | Complete |

Phase 4 is complete. Phase 5 must migrate controls in the documented batches
and apply this checklist to each control.
