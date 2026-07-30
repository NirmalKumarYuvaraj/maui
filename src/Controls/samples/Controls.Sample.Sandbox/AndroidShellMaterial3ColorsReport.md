# Android Shell Material 3 Color Handling Report

## Purpose

Android Shell supplied hardcoded fallback colors whenever Shell appearance
properties were unset. With Material 3 enabled, these values replaced the
colors resolved by the native Material theme, preventing custom themes and
Material component defaults from taking effect.

NavigationPage does not eagerly replace unset toolbar colors. Its Android
toolbar can therefore resolve defaults from `toolbarStyle` and the active
application theme.

## Intended Behavior

- Explicit Shell colors must continue to override native styling.
- When Material 3 is enabled and a Shell color is unset, the corresponding
  Android control should retain its style-derived color.
- Clearing an explicit Shell color should restore the native Material 3
  default.
- Material 2 should retain its existing fallback behavior unless a separate
  compatibility change is approved.

## Investigation Findings

The Android controls were already created with native style attributes:

- `MaterialToolbar` uses the active toolbar style.
- `BottomNavigationView` is created with `bottomNavigationViewStyle`.
- `TabLayout` is created with the active Material tab style.
- Shell app bars use `appBarLayoutStyle`.

The style values were subsequently replaced by Shell fallback logic:

- `Shell.cs` assigned concrete Android toolbar colors when appearance values
  were unset.
- Shell toolbar, tab, and bottom-navigation appearance trackers substituted
  hardcoded fallback colors.
- `PlatformInterop.createNavigationBar` explicitly set the bottom-navigation
  background to white.

## Implemented Changes

### Shell toolbar

- Unset Shell toolbar appearance values now flow as unset values instead of
  being replaced immediately.
- Native toolbar background, background tint, title color, navigation-icon
  color, and overflow-icon color are captured before an explicit override.
- Clearing an override restores the values resolved from Android
  `toolbarStyle`.
- Default values are stored per native toolbar and drawable rather than in
  process-wide static color fields.

### Bottom navigation

- Removed the hardcoded white background applied during native view creation.
- Captures the native background, item text colors, and item icon tint list.
- Restores the captured values when Shell tab-bar colors are cleared.
- Preserves a native `null` tint list, which is a valid style configuration.
- When Shell supplies only some colors, missing color states are derived from
  Android theme attributes such as `textColorSecondary`, `colorPrimary`, and
  `colorOnSurface`.

### Top tabs

- Captures the native `TabLayout` background, text color state list, selected
  indicator drawable, and indicator color.
- Restores those values after explicit Shell appearance values are cleared.
- Added Android interop for resolving `tabIndicatorColor` from the active
  `tabStyle`.

### Compatibility API

Existing public `ShellRenderer.Default*Color` members remain available for
binary and source compatibility. The native Shell controls no longer depend
on those members for style restoration.

## Files Updated

- `src/Controls/src/Core/Shell/Shell.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellRenderer.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellToolbarAppearanceTracker.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellTabLayoutAppearanceTracker.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellBottomNavViewAppearanceTracker.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellItemRenderer.cs`
- `src/Controls/src/Core/Platform/Android/Extensions/ToolbarExtensions.cs`
- `src/Core/AndroidNative/maui/src/main/java/com/microsoft/maui/PlatformInterop.java`
- `src/Controls/tests/DeviceTests/Elements/Shell/ShellTests.Android.cs`

## Test Coverage

An Android device test applies explicit toolbar and bottom-navigation colors,
clears them, and verifies that the original native style-derived backgrounds
and color state lists are restored.

The Android Shell device-test category completed with:

- 70 tests passed
- 0 tests failed

## Recommended Material 3 Scope

The implementation should be gated by `RuntimeFeature.IsMaterial3Enabled` to
match the intended compatibility boundary:

```text
Material 3 + unset Shell color -> use native Material style
Material 3 + explicit Shell color -> use explicit Shell color
Material 2 + unset Shell color -> preserve existing Shell fallback
```

This avoids changing established Material 2 visuals while allowing Material 3
dynamic colors, custom theme overlays, component state lists, and future
Material defaults to work without additional hardcoded color tables.

The gate should be applied consistently to toolbar, top-tab, and
bottom-navigation reset paths. Tests should cover both Material 2 compatibility
and Material 3 native-style restoration.
