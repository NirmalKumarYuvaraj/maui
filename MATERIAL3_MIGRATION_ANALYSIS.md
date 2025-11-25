# Material Design 3 Migration Analysis for .NET MAUI

**Date:** November 25, 2025  
**Scope:** Android platform implementation in src/Controls and src/Core  
**Current State:** Material Components (Material Design 2)  
**Target State:** Material 3 (Material You)

---

## Executive Summary

This document provides a comprehensive analysis of the current Material Design implementation in .NET MAUI's Android platform code and outlines the migration path to Material Design 3 (Material You). The migration will modernize the visual design system, improve theming capabilities, and align with Google's latest design guidelines.

**Key Findings:**

-   Currently using Material Components (Material Design 2) via `Xamarin.Google.Android.Material` version 1.12.0.5
-   Base theme: `Theme.MaterialComponents.DayNight`
-   No Material 3 components currently in use
-   Custom wrappers and extensions built around Material Components APIs
-   Estimated 100+ files requiring updates across src/Core and src/Controls

---

## Table of Contents

1. [Current Material Design Implementation](#current-material-design-implementation)
2. [Material 3 Overview](#material-3-overview)
3. [Dependency Analysis](#dependency-analysis)
4. [Theme and Style Analysis](#theme-and-style-analysis)
5. [Component Inventory](#component-inventory)
6. [Migration Strategy](#migration-strategy)
7. [Implementation Plan](#implementation-plan)
8. [Breaking Changes and Considerations](#breaking-changes-and-considerations)
9. [Testing Requirements](#testing-requirements)
10. [References](#references)

---

## 1. Current Material Design Implementation

### 1.1 Package Dependencies

**Primary Material Design Package:**

```xml
<!-- eng/AndroidX.targets -->
<PackageReference Update="Xamarin.Google.Android.Material" Version="1.12.0.5" />
```

**Related AndroidX Dependencies:**

```xml
<PackageReference Update="Xamarin.AndroidX.Activity" Version="1.10.1.3" />
<PackageReference Update="Xamarin.AndroidX.Browser" Version="1.8.0.11" />
<PackageReference Update="Xamarin.AndroidX.DynamicAnimation" Version="1.1.0.3" />
<PackageReference Update="Xamarin.AndroidX.Lifecycle.LiveData" Version="2.9.2.1" />
<PackageReference Update="Xamarin.AndroidX.Navigation.Common" Version="2.9.2.1" />
<PackageReference Update="Xamarin.AndroidX.Navigation.Fragment" Version="2.9.2.1" />
<PackageReference Update="Xamarin.AndroidX.Navigation.Runtime" Version="2.9.2.1" />
<PackageReference Update="Xamarin.AndroidX.Navigation.UI" Version="2.9.2.1" />
<PackageReference Update="Xamarin.AndroidX.SwipeRefreshLayout" Version="1.1.0.29" />
<PackageReference Update="Xamarin.AndroidX.Transition" Version="1.6.0.1" />
```

### 1.2 Base Themes

**Location:** `src/Core/src/Platform/Android/Resources/values/styles.xml`

**Current Theme Hierarchy:**

```xml
<style name="Maui.MainTheme.Base" parent="Theme.MaterialComponents.DayNight">
    <item name="colorPrimary">@color/colorPrimary</item>
    <item name="colorPrimaryDark">@color/colorPrimaryDark</item>
    <item name="colorAccent">@color/colorAccent</item>
    <item name="android:actionMenuTextColor">@color/colorActionMenuTextColor</item>
    <item name="appBarLayoutStyle">@style/MauiAppBarLayout</item>
    <item name="bottomNavigationViewStyle">@style/Widget.Design.BottomNavigationView</item>
    <item name="materialButtonStyle">@style/MauiMaterialButton</item>
    <item name="checkboxStyle">@style/MauiCheckBox</item>
</style>
```

**Current Color Scheme:**

```xml
<!-- src/Core/src/Platform/Android/Resources/values/colors.xml -->
<color name="colorPrimary">#2c3e50</color>
<color name="colorPrimaryDark">#1B3147</color>
<color name="colorAccent">#3498db</color>
<color name="colorActionMenuTextColor">#FFFFFF</color>
```

### 1.3 Custom Material Components

#### MauiMaterialButton

-   **File:** `src/Core/src/Platform/Android/MauiMaterialButton.cs`
-   **Extends:** `Google.Android.Material.Button.MaterialButton`
-   **Custom Features:**
    -   Bottom icon gravity support (custom feature not in standard Material)
    -   Dynamic icon sizing with MauiResizableDrawable
    -   Custom padding management
    -   Theme wrapper integration via `MauiMaterialContextThemeWrapper`

#### MauiMaterialContextThemeWrapper

-   **File:** `src/Core/src/Platform/Android/MauiMaterialContextThemeWrapper.cs`
-   **Purpose:** Wraps Android Context with Material theme
-   **Base Style:** `Resource.Style.Maui_MainTheme_Base`

### 1.4 Material Component Styles

**Button Style:**

```xml
<style name="MauiMaterialButton" parent="Widget.MaterialComponents.Button.UnelevatedButton">
    <item name="android:minWidth">0dp</item>
    <item name="android:minHeight">0dp</item>
    <item name="android:paddingLeft">0dp</item>
    <item name="android:paddingRight">0dp</item>
    <item name="android:paddingTop">0dp</item>
    <item name="android:paddingBottom">0dp</item>
    <item name="android:insetTop">0dp</item>
    <item name="android:insetBottom">0dp</item>
    <item name="android:insetLeft">0dp</item>
    <item name="android:insetRight">0dp</item>
</style>
```

**CheckBox Style:**

```xml
<style name="MauiCheckBox" parent="Widget.Material3.CompoundButton.CheckBox">
    <item name="android:minWidth">0dp</item>
    <item name="android:minHeight">0dp</item>
    <item name="buttonTint">?attr/colorPrimary</item>
</style>
```

**Note:** CheckBox already references Material 3 style, showing partial migration has occurred.

### 1.5 AppCompat Usage

**Base Activity:**

-   `MauiAppCompatActivity` extends `AndroidX.AppCompat.App.AppCompatActivity`
-   Uses `AppCompatDelegate` for theme management

**AppCompat Widgets in Use:**

```csharp
// Text inputs
AppCompatEditText
AppCompatTextView
AppCompatCheckBox
AppCompatRadioButton

// Toolbar
AndroidX.AppCompat.Widget.Toolbar
AppCompatImageView

// Other
SwitchCompat
SearchView (AppCompat version)
```

### 1.6 Material Design Files Inventory

**Core Platform Files (src/Core/src/Platform/Android/):**

_Material-Dependent Classes:_

1. `MauiMaterialButton.cs` - Custom MaterialButton implementation
2. `MauiMaterialContextThemeWrapper.cs` - Theme wrapper
3. `MauiShapeableImageView.cs` - Uses Material ShapeableImageView
4. `MauiAppCompatActivity.cs` - Base activity with Material theme
5. `MauiAppCompatEditText.cs` - Text input with AppCompat
6. `MauiTimePicker.cs` - Date/Time pickers with AppCompat
7. `MauiDatePicker.cs`
8. `MauiPicker.cs`
9. `MauiTextView.cs` - Text views with AppCompat
10. `MauiSearchView.cs` - Search with AppCompat

_Extension Classes:_ 11. `ButtonExtensions.cs` - MaterialButton extensions 12. `CheckBoxExtensions.cs` - AppCompatCheckBox extensions 13. `RadioButtonExtensions.cs` - AppCompatRadioButton extensions 14. `SwitchExtensions.cs` - SwitchCompat extensions 15. `ToolbarExtensions.cs` - AppCompat Toolbar extensions 16. `SearchViewExtensions.cs` - SearchView extensions 17. `PickerExtensions.cs` - AlertDialog usage 18. `ViewExtensions.cs` - Material background handling

_Navigation & Layout:_ 19. `Navigation/NavigationRootManager.cs` - Uses MaterialToolbar 20. `Navigation/StackNavigationManager.cs` - Toolbar management 21. `MauiWindowInsetListener.cs` - MaterialToolbar awareness 22. `SafeAreaExtensions.cs` - AppBarLayout handling

_Visual Effects:_ 23. `MauiRippleDrawableExtensions.cs` - Material ripple effects 24. `SliderExtensions.cs` - Material seekbar theme

**Controls Platform Files (src/Controls/src/Core/Platform/Android/):**

25. `Extensions/ButtonExtensions.cs` - MaterialButton text updates
26. `Extensions/ToolbarExtensions.cs` - AppCompat toolbar integration
27. `Extensions/SearchViewExtensions.cs`
28. `AutomationPropertiesProvider.cs` - Toolbar automation
29. `DragAndDropGestureHandler.cs` - References AppCompat renderer

**Compatibility Layer (src/Compatibility/Material/):**

30. `Compatibility.Material.Android.csproj` - Legacy Material renderers
31. `Resources/values/styles.xml` - Xamarin.Forms Material styles

**Resource Files:**

-   `src/Core/src/Platform/Android/Resources/values/styles.xml` - Main theme definitions
-   `src/Core/src/Platform/Android/Resources/values/colors.xml` - Color palette
-   `src/Core/src/Platform/Android/Resources/values/attr.xml` - Custom attributes

---

## 2. Material 3 Overview

### 2.1 What is Material 3?

Material 3 (also known as Material You) is Google's latest design system that emphasizes:

**Key Principles:**

-   **Dynamic Color:** System-wide theming based on user wallpaper (Android 12+)
-   **Increased Accessibility:** Better contrast ratios and larger touch targets
-   **Expressive Personalization:** User-driven customization
-   **Adaptive Design:** Better responsiveness across form factors
-   **Updated Components:** Refined visual hierarchy and interaction patterns

### 2.2 Material 3 vs Material Components (Material 2)

| Aspect           | Material 2                    | Material 3                                |
| ---------------- | ----------------------------- | ----------------------------------------- |
| **Theme Base**   | `Theme.MaterialComponents.*`  | `Theme.Material3.*`                       |
| **Color System** | Primary, PrimaryDark, Accent  | Dynamic color roles (40+ semantic colors) |
| **Typography**   | Material Type Scale           | Material 3 Type Scale (updated)           |
| **Shapes**       | Small, Medium, Large          | Extra Small through Extra Large           |
| **Components**   | `Widget.MaterialComponents.*` | `Widget.Material3.*`                      |
| **Elevation**    | Shadow-based                  | Surface tint-based                        |
| **States**       | Standard ripple               | Enhanced state layer system               |

### 2.3 Material 3 Color System

Material 3 introduces a comprehensive color system with semantic naming:

**Primary Colors:**

-   `colorPrimary`, `colorOnPrimary`
-   `colorPrimaryContainer`, `colorOnPrimaryContainer`

**Secondary Colors:**

-   `colorSecondary`, `colorOnSecondary`
-   `colorSecondaryContainer`, `colorOnSecondaryContainer`

**Tertiary Colors:**

-   `colorTertiary`, `colorOnTertiary`
-   `colorTertiaryContainer`, `colorOnTertiaryContainer`

**Surface Colors:**

-   `colorSurface`, `colorOnSurface`
-   `colorSurfaceVariant`, `colorOnSurfaceVariant`
-   `colorSurfaceDim`, `colorSurfaceBright`
-   `colorSurfaceContainerLowest` through `colorSurfaceContainerHighest`

**Other:**

-   `colorError`, `colorOnError`, `colorErrorContainer`, `colorOnErrorContainer`
-   `colorOutline`, `colorOutlineVariant`
-   `colorBackground`, `colorOnBackground` (deprecated in favor of surface colors)

---

## 3. Dependency Analysis

### 3.1 Required Package Updates

**Current:**

```xml
<PackageReference Update="Xamarin.Google.Android.Material" Version="1.12.0.5" />
```

**Target for Material 3:**

```xml
<PackageReference Update="Xamarin.Google.Android.Material" Version="1.13.0+" />
```

**Note:** Material 3 support was added in Google Material Components 1.5.0+, with full stability in 1.8.0+. Version 1.12.0.5 already includes Material 3 components, but the library must be used with Material 3 themes to activate M3 styling.

### 3.2 AndroidX Compatibility

Material 3 requires:

-   **Minimum Android API Level:** 21 (Android 5.0)
-   **Target API Level:** 34+ recommended for full dynamic color support
-   **AndroidX Core:** 1.9.0+
-   **AppCompat:** 1.6.0+ (current usage is compatible)

Current .NET MAUI AndroidX versions are compatible with Material 3.

### 3.3 Compatibility Layer Impact

**Xamarin.Forms Compatibility:**

-   Legacy Material renderers in `src/Compatibility/Material/` use Material 2 styles
-   These will need parallel Material 3 styles or deprecation plan
-   Xamarin.Forms apps embedding .NET MAUI controls may see visual changes

---

## 4. Theme and Style Analysis

### 4.1 Current Theme Structure

```
Maui.MainTheme.Base (Theme.MaterialComponents.DayNight)
├── Maui.MainTheme
│   └── Maui.MainTheme.NoActionBar
├── Maui.SplashTheme
└── Backward Compatibility Aliases
    ├── MainTheme
    ├── AppTheme
    └── Legacy Xamarin.Forms themes
```

### 4.2 Required Theme Changes

**Primary Theme Update:**

```xml
<!-- BEFORE -->
<style name="Maui.MainTheme.Base" parent="Theme.MaterialComponents.DayNight">

<!-- AFTER -->
<style name="Maui.MainTheme.Base" parent="Theme.Material3.DayNight">
```

**Color Attribute Migration:**

```xml
<!-- BEFORE -->
<item name="colorPrimary">@color/colorPrimary</item>
<item name="colorPrimaryDark">@color/colorPrimaryDark</item>
<item name="colorAccent">@color/colorAccent</item>

<!-- AFTER -->
<item name="colorPrimary">@color/md_theme_primary</item>
<item name="colorOnPrimary">@color/md_theme_on_primary</item>
<item name="colorPrimaryContainer">@color/md_theme_primary_container</item>
<item name="colorOnPrimaryContainer">@color/md_theme_on_primary_container</item>
<item name="colorSecondary">@color/md_theme_secondary</item>
<item name="colorOnSecondary">@color/md_theme_on_secondary</item>
<!-- ... additional M3 color attributes -->
```

**Component Style Updates:**

```xml
<!-- BEFORE -->
<item name="materialButtonStyle">@style/MauiMaterialButton</item>
<style name="MauiMaterialButton" parent="Widget.MaterialComponents.Button.UnelevatedButton">

<!-- AFTER -->
<item name="materialButtonStyle">@style/MauiMaterialButton</item>
<style name="MauiMaterialButton" parent="Widget.Material3.Button">
```

### 4.3 Dynamic Color Support (Android 12+)

Material 3's dynamic color system extracts colors from the user's wallpaper on Android 12+:

```xml
<!-- Enable dynamic color when available -->
<item name="dynamicColorThemeOverlay">@style/ThemeOverlay.Material3.DynamicColors.DayNight</item>
```

**C# Integration:**

```csharp
// In MauiAppCompatActivity or theme setup
if (OperatingSystem.IsAndroidVersionAtLeast(31)) // Android 12+
{
    DynamicColors.ApplyToActivityIfAvailable(this);
}
```

---

## 5. Component Inventory

### 5.1 Material Components Currently Used

| Component        | Current Implementation              | Material 3 Equivalent                        | Migration Complexity        |
| ---------------- | ----------------------------------- | -------------------------------------------- | --------------------------- |
| **Button**       | `MaterialButton` (M2)               | `MaterialButton` (M3 styled)                 | Medium - Style updates      |
| **Checkbox**     | `AppCompatCheckBox`                 | Material 3 CheckBox                          | Low - Already uses M3 style |
| **Radio Button** | `AppCompatRadioButton`              | Material 3 RadioButton                       | Low                         |
| **Switch**       | `SwitchCompat`                      | Material 3 Switch                            | Medium                      |
| **Toolbar**      | `Toolbar` (AppCompat)               | `TopAppBar` (M3)                             | Medium                      |
| **Text Fields**  | `AppCompatEditText`                 | `TextInputLayout` + `TextInputEditText` (M3) | High                        |
| **Pickers**      | `AppCompatEditText` + `AlertDialog` | Material 3 Dialogs                           | Medium                      |
| **Search**       | `SearchView` (AppCompat)            | Material 3 SearchBar/SearchView              | Medium                      |
| **Bottom Nav**   | `BottomNavigationView`              | Material 3 NavigationBar                     | Low                         |
| **AppBar**       | `AppBarLayout`                      | Material 3 TopAppBar                         | Medium                      |
| **Card**         | Custom                              | Material 3 Card                              | N/A (not used)              |
| **FAB**          | Not currently used                  | Material 3 FAB                               | N/A                         |
| **Chip**         | Not currently used                  | Material 3 Chip                              | N/A                         |

### 5.2 Custom Material Widgets

#### MauiMaterialButton

-   **Current Parent:** `MaterialButton`
-   **Changes Required:**
    -   Update internal style references to Material 3
    -   Verify icon gravity implementation with M3
    -   Test ripple effects with M3 state layers
    -   Update elevation handling (shadow → surface tint)

#### MauiShapeableImageView

-   **Current:** `Google.Android.Material.ImageView.ShapeableImageView`
-   **M3 Changes:** Component is compatible, but shape token system updated
-   **Action:** Verify shape definitions align with M3 shape scale

### 5.3 Component-Specific Changes

#### Buttons

**Material 2:**

-   `Widget.MaterialComponents.Button.UnelevatedButton` (Filled)
-   `Widget.MaterialComponents.Button.OutlinedButton` (Outlined)
-   `Widget.MaterialComponents.Button.TextButton` (Text)

**Material 3:**

-   `Widget.Material3.Button` (Filled)
-   `Widget.Material3.Button.Outlined`
-   `Widget.Material3.Button.TextButton`
-   `Widget.Material3.Button.ElevatedButton` (NEW)
-   `Widget.Material3.Button.TonalButton` (NEW)

#### Text Fields

**Material 2:**

-   `TextInputLayout` with `Widget.MaterialComponents.TextInputLayout.*`
-   Variants: Filled, Outlined

**Material 3:**

-   `TextInputLayout` with `Widget.Material3.TextInputLayout.*`
-   Same variants but updated visual design
-   Better error state handling

#### Elevation System

**Material 2:**

-   Uses shadows for elevation
-   `android:elevation` attribute

**Material 3:**

-   Uses surface tint overlay for elevation
-   `app:elevation` with automatic tint calculation
-   Provides better dark mode appearance

---

## 6. Migration Strategy

### 6.1 Migration Approach

**Strategy:** Incremental migration with theme-based rollout

**Phases:**

1. **Phase 1:** Update base theme and core infrastructure
2. **Phase 2:** Migrate individual components
3. **Phase 3:** Update resource files and documentation
4. **Phase 4:** Testing and refinement

**Compatibility:** No backwards compatibility maintained (as per requirements)

### 6.2 Theme Migration Strategy

**Option A: Clean Break (RECOMMENDED)**

-   Update base theme to Material 3 in one commit
-   Update all component styles simultaneously
-   Provides consistent visual language
-   Cleaner codebase

**Option B: Parallel Themes**

-   Maintain both M2 and M3 themes temporarily
-   Allow gradual component migration
-   More complex to maintain
-   **Not recommended per requirements**

### 6.3 Color System Migration

**Steps:**

1. **Generate Material 3 Color Scheme:**

    - Use Material Theme Builder (https://m3.material.io/theme-builder)
    - Create color schemes for light and dark modes
    - Export as XML resource files

2. **Create Color Resource Files:**

    ```
    src/Core/src/Platform/Android/Resources/values/
    ├── colors_m3.xml (light mode colors)
    └── colors_m3_night.xml (dark mode colors in values-night/)
    ```

3. **Map Existing Colors to M3 Tokens:**

    ```
    colorPrimary (#2c3e50) → md_theme_primary
    colorPrimaryDark (deprecated) → removed
    colorAccent (#3498db) → md_theme_secondary
    ```

4. **Add M3-Specific Colors:**
    - Surface variants
    - Container colors
    - Outline colors
    - Error colors

### 6.4 Component Migration Priority

**High Priority (Core UI Elements):**

1. MauiMaterialButton
2. Toolbar/TopAppBar
3. Text inputs (Entry, Editor, SearchBar)
4. Dialogs (Alerts, Pickers)

**Medium Priority:** 5. CheckBox, RadioButton, Switch 6. Bottom Navigation 7. AppBarLayout

**Low Priority:** 8. Ripple effects and state layers 9. Shape definitions 10. Elevation system refinement

---

## 7. Implementation Plan

### 7.1 Phase 1: Infrastructure Updates (Week 1)

**Tasks:**

1. **Update Package Dependencies**

    - File: `eng/AndroidX.targets`
    - Verify Material package version supports M3
    - Test dependency conflicts

2. **Create Material 3 Base Theme**

    - File: `src/Core/src/Platform/Android/Resources/values/styles.xml`
    - Change parent to `Theme.Material3.DayNight`
    - Update theme attributes to M3 naming

3. **Generate Material 3 Color Schemes**

    - Create `colors_m3.xml` (light)
    - Create `colors_m3_night.xml` (dark, in values-night/)
    - Map current brand colors to M3 tokens

4. **Update MauiMaterialContextThemeWrapper**

    - File: `src/Core/src/Platform/Android/MauiMaterialContextThemeWrapper.cs`
    - Verify theme resource references
    - Test theme application

5. **Update MauiAppCompatActivity**
    - File: `src/Core/src/Platform/Android/MauiAppCompatActivity.cs`
    - Apply M3 theme
    - Add dynamic color support for Android 12+

**Deliverables:**

-   Updated theme infrastructure
-   Material 3 color system
-   Base theme functioning with M3

### 7.2 Phase 2: Component Migration (Week 2-3)

#### Week 2: Core Components

**Day 1-2: Buttons**

-   File: `src/Core/src/Platform/Android/MauiMaterialButton.cs`
-   Update styles to Material 3 button variants
-   File: `src/Core/src/Platform/Android/ButtonExtensions.cs`
-   File: `src/Controls/src/Core/Platform/Android/Extensions/ButtonExtensions.cs`
-   Test button states (enabled, disabled, pressed)
-   Verify custom icon gravity feature

**Day 3-4: Text Inputs**

-   Files: `MauiAppCompatEditText.cs`, `EntryExtensions.cs`, `EditorExtensions.cs`
-   Migrate to Material 3 TextInputLayout patterns
-   Update hint/label behavior
-   Test error states

**Day 5: Toolbar/AppBar**

-   File: `src/Core/src/Platform/Android/Navigation/NavigationRootManager.cs`
-   Update MaterialToolbar to M3 styling
-   File: `src/Core/src/Platform/Android/ToolbarExtensions.cs`
-   File: `src/Controls/src/Core/Platform/Android/Extensions/ToolbarExtensions.cs`
-   Test navigation integration

#### Week 3: Additional Components

**Day 1: Dialogs**

-   File: `src/Core/src/Platform/Android/PickerExtensions.cs`
-   Update AlertDialog theming to Material 3
-   Test picker dialogs
-   Update date/time picker styles

**Day 2: Checkable Components**

-   Files: `CheckBoxExtensions.cs`, `RadioButtonExtensions.cs`, `SwitchExtensions.cs`
-   Update component styles to Material 3
-   Verify state colors and animations

**Day 3: Search**

-   File: `src/Core/src/Platform/Android/MauiSearchView.cs`
-   File: `src/Core/src/Platform/Android/SearchViewExtensions.cs`
-   Migrate to Material 3 SearchBar/SearchView

**Day 4: Bottom Navigation**

-   Update navigation component styles
-   Test with Shell navigation

**Day 5: Ripple Effects**

-   File: `src/Core/src/Platform/Android/MauiRippleDrawableExtensions.cs`
-   Update ripple implementation for M3 state layers
-   Update default ripple color to use M3 color tokens

### 7.3 Phase 3: Visual Refinement (Week 4)

**Tasks:**

1. **Elevation System**

    - Update components using elevation to use surface tint
    - Test in light and dark modes
    - Verify visual hierarchy

2. **Shape System**

    - Review shape definitions
    - Update to Material 3 shape scale (if needed)
    - Test rounded corners and shape appearances

3. **Typography**

    - Review text appearance styles
    - Align with Material 3 type scale
    - Update font weights and sizes if needed

4. **State Layers**

    - Verify pressed, focused, hovered states
    - Test state layer opacity values
    - Ensure accessibility compliance

5. **Dynamic Color Integration**

    - Implement dynamic color support
    - Test on Android 12+ devices
    - Verify fallback behavior on older versions

6. **Dark Mode**
    - Test all components in dark mode
    - Verify color contrast ratios
    - Adjust surface colors if needed

### 7.4 Phase 4: Cleanup and Documentation (Week 5)

**Tasks:**

1. **Remove Deprecated References**

    - Search for Material 2 style references
    - Remove `colorPrimaryDark` usage
    - Clean up compatibility code

2. **Update Code Comments**

    - Document Material 3 migration decisions
    - Update component documentation
    - Add Material 3 references

3. **Update Compatibility Layer**

    - Evaluate `src/Compatibility/Material/` - update or deprecate
    - Document breaking changes for Xamarin.Forms users

4. **Resource Cleanup**

    - Remove unused Material 2 styles
    - Organize color resources
    - Consolidate theme definitions

5. **Documentation**
    - Update developer documentation
    - Document new Material 3 features
    - Provide migration guide for MAUI developers

---

## 8. Breaking Changes and Considerations

### 8.1 Visual Changes

**Expected Visual Differences:**

-   Button shapes (more rounded in M3)
-   Elevation appearance (tint vs shadow)
-   Text field outlines (thinner, more subtle)
-   Ripple effects (updated animation)
-   Color palette (semantic naming)
-   Component sizing (updated touch targets)

**Impact:** Users will notice visual changes. All MAUI Android apps will adopt Material 3 appearance.

### 8.2 API Changes

**Potential Breaking Changes:**

1. **Color Attributes**

    - `colorPrimaryDark` is deprecated in M3
    - Apps referencing this attribute may need updates
    - Replacement: Use `colorPrimary` with automatic elevation tinting

2. **Theme Attributes**

    - Some Material 2 theme attributes removed
    - Component style names changed
    - Custom themes may need updates

3. **Component Styles**
    - Button style variants updated
    - New button types available (Tonal, Elevated)
    - Default button appearance changed

### 8.3 Minimum SDK Considerations

**Current MAUI Minimum:**

-   API Level 21 (Android 5.0)

**Material 3 Support:**

-   Full support on API 21+
-   Dynamic color requires API 31+ (Android 12)
-   Some advanced features may require newer APIs

**Strategy:**

-   Maintain API 21 minimum
-   Feature-detect for dynamic color
-   Graceful fallback for older devices

### 8.4 Performance Considerations

**Material 3 Performance:**

-   Generally equal or better than Material 2
-   Surface tint elevation may be more performant than shadows
-   State layer animations optimized

**Testing Required:**

-   Measure app startup time
-   Profile rendering performance
-   Test on low-end devices

---

## 9. Testing Requirements

### 9.1 Visual Testing

**Scope:**

-   All MAUI controls on Android
-   Light and dark modes
-   Different screen densities
-   Different Android versions

**Test Matrix:**
| Control | Light | Dark | API 21 | API 31 | API 34 |
|---------|-------|------|--------|--------|--------|
| Button | ✓ | ✓ | ✓ | ✓ | ✓ |
| Entry | ✓ | ✓ | ✓ | ✓ | ✓ |
| CheckBox | ✓ | ✓ | ✓ | ✓ | ✓ |
| ... | ... | ... | ... | ... | ... |

### 9.2 Functional Testing

**Critical Paths:**

-   Navigation (Shell, NavigationPage)
-   Input controls (Entry, Editor, Picker)
-   Button interactions
-   Dialog display and interaction
-   Theme switching
-   Dynamic color activation (API 31+)

### 9.3 Regression Testing

**Areas of Focus:**

-   Custom button icon gravity (MauiMaterialButton)
-   Ripple effects and boundaries
-   Text field validation and error states
-   Picker dialogs
-   Search functionality
-   Toolbar navigation
-   SafeArea and window insets

### 9.4 Accessibility Testing

**Requirements:**

-   Minimum touch target size (48dp)
-   Color contrast ratios (WCAG AA)
-   Screen reader compatibility
-   Keyboard navigation
-   Focus indicators

### 9.5 Performance Testing

**Metrics:**

-   App startup time
-   Frame rate during animations
-   Memory usage
-   Layout inflation time
-   Rendering performance

---

## 10. References

### 10.1 Material Design Documentation

-   **Material 3 Guidelines:** https://m3.material.io/
-   **Material 3 Components:** https://m3.material.io/components
-   **Material Theme Builder:** https://m3.material.io/theme-builder
-   **Material Design 3 GitHub:** https://github.com/material-components/material-components-android

### 10.2 Android Documentation

-   **Material Components for Android:** https://github.com/material-components/material-components-android
-   **Migrating to Material 3:** https://material.io/blog/migrating-material-3
-   **AndroidX Material Package:** https://developer.android.com/reference/com/google/android/material/package-summary

### 10.3 Code References

**Material 3 Migration Guides:**

-   https://github.com/material-components/material-components-android/blob/master/docs/theming/Material3.md
-   https://github.com/material-components/material-components-android/blob/master/docs/theming/Color.md
-   https://github.com/material-components/material-components-android/blob/master/docs/theming/Shape.md

**Material 3 Component Documentation:**

-   Buttons: https://m3.material.io/components/buttons/overview
-   Text Fields: https://m3.material.io/components/text-fields/overview
-   Navigation Bar: https://m3.material.io/components/navigation-bar/overview
-   Top App Bar: https://m3.material.io/components/top-app-bar/overview

### 10.4 Xamarin AndroidX Bindings

-   **Xamarin.Google.Android.Material:** https://www.nuget.org/packages/Xamarin.Google.Android.Material
-   **AndroidX Bindings:** https://github.com/xamarin/AndroidX

---

## Appendix A: File Change Checklist

### Core Android Platform Files (src/Core/src/Platform/Android/)

**Theme & Resources:**

-   [ ] `Resources/values/styles.xml` - Update base theme to Material 3
-   [ ] `Resources/values/colors.xml` - Add Material 3 color scheme
-   [ ] `Resources/values-night/colors_m3.xml` - Add dark mode colors (NEW FILE)
-   [ ] `Resources/values/attr.xml` - Update custom attributes if needed

**Custom Material Components:**

-   [ ] `MauiMaterialButton.cs` - Update for M3 styling
-   [ ] `MauiMaterialContextThemeWrapper.cs` - Verify M3 theme application
-   [ ] `MauiShapeableImageView.cs` - Verify shape system compatibility

**Activity & Application:**

-   [ ] `MauiAppCompatActivity.cs` - Add dynamic color support

**Extensions:**

-   [ ] `ButtonExtensions.cs` - Update MaterialButton extensions
-   [ ] `CheckBoxExtensions.cs` - Update for M3 styles
-   [ ] `RadioButtonExtensions.cs` - Update for M3 styles
-   [ ] `SwitchExtensions.cs` - Update SwitchCompat for M3
-   [ ] `ToolbarExtensions.cs` - Update Toolbar for M3
-   [ ] `SearchViewExtensions.cs` - Update SearchView for M3
-   [ ] `PickerExtensions.cs` - Update AlertDialog theme
-   [ ] `ViewExtensions.cs` - Update Material background handling
-   [ ] `TextViewExtensions.cs` - Verify M3 compatibility
-   [ ] `EditTextExtensions.cs` - Update text input styling

**Navigation:**

-   [ ] `Navigation/NavigationRootManager.cs` - Update MaterialToolbar
-   [ ] `Navigation/StackNavigationManager.cs` - Update toolbar handling
-   [ ] `NavigationViewExtensions.cs` - Update navigation styles

**Visual Effects:**

-   [ ] `MauiRippleDrawableExtensions.cs` - Update ripple for M3 state layers
-   [ ] `SliderExtensions.cs` - Update seekbar theme
-   [ ] `BorderDrawable.cs` - Verify shape compatibility

**Platform Integration:**

-   [ ] `MauiWindowInsetListener.cs` - Verify MaterialToolbar handling
-   [ ] `SafeAreaExtensions.cs` - Verify AppBarLayout compatibility
-   [ ] `ThemeExtensions.cs` - Update theme detection

### Controls Platform Files (src/Controls/src/Core/Platform/Android/)

-   [ ] `Extensions/ButtonExtensions.cs` - Update MaterialButton integration
-   [ ] `Extensions/ToolbarExtensions.cs` - Update AppCompat toolbar
-   [ ] `Extensions/SearchViewExtensions.cs` - Update search integration
-   [ ] `AutomationPropertiesProvider.cs` - Verify toolbar automation

### Configuration Files:

-   [ ] `eng/AndroidX.targets` - Verify package versions
-   [ ] `src/Core/src/Core.csproj` - Check Material package reference
-   [ ] `src/Controls/src/Core/Controls.Core.csproj` - Check package reference

### Compatibility Layer (Optional):

-   [ ] `src/Compatibility/Material/src/Android/` - Update or deprecate
-   [ ] `src/Compatibility/Material/src/Android/Resources/values/styles.xml` - M3 styles

---

## Appendix B: Material 3 Color Generation Example

**Using Material Theme Builder:**

1. Visit https://m3.material.io/theme-builder
2. Input source color (e.g., #2c3e50 - current colorPrimary)
3. Generate theme
4. Export as Android XML

**Generated colors_m3.xml (example):**

```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <!-- Light theme colors -->
    <color name="md_theme_primary">#2c3e50</color>
    <color name="md_theme_on_primary">#FFFFFF</color>
    <color name="md_theme_primary_container">#D6E4F0</color>
    <color name="md_theme_on_primary_container">#001D32</color>

    <color name="md_theme_secondary">#3498db</color>
    <color name="md_theme_on_secondary">#FFFFFF</color>
    <color name="md_theme_secondary_container">#D8E4FF</color>
    <color name="md_theme_on_secondary_container">#001A41</color>

    <color name="md_theme_tertiary">#6B5B8F</color>
    <color name="md_theme_on_tertiary">#FFFFFF</color>
    <color name="md_theme_tertiary_container">#F0DBFF</color>
    <color name="md_theme_on_tertiary_container">#250046</color>

    <color name="md_theme_error">#BA1A1A</color>
    <color name="md_theme_on_error">#FFFFFF</color>
    <color name="md_theme_error_container">#FFDAD6</color>
    <color name="md_theme_on_error_container">#410002</color>

    <color name="md_theme_background">#FCFCFF</color>
    <color name="md_theme_on_background">#1A1C1E</color>

    <color name="md_theme_surface">#FCFCFF</color>
    <color name="md_theme_on_surface">#1A1C1E</color>
    <color name="md_theme_surface_variant">#DDE3EA</color>
    <color name="md_theme_on_surface_variant">#41474D</color>

    <color name="md_theme_outline">#71787E</color>
    <color name="md_theme_outline_variant">#C1C7CE</color>

    <color name="md_theme_surface_dim">#D9D9DC</color>
    <color name="md_theme_surface_bright">#FCFCFF</color>
    <color name="md_theme_surface_container_lowest">#FFFFFF</color>
    <color name="md_theme_surface_container_low">#F3F3F6</color>
    <color name="md_theme_surface_container">#EDEEF1</color>
    <color name="md_theme_surface_container_high">#E8E8EB</color>
    <color name="md_theme_surface_container_highest">#E2E2E5</color>
</resources>
```

**Generated colors_m3.xml in values-night/ (example):**

```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <!-- Dark theme colors -->
    <color name="md_theme_primary">#A8C7E7</color>
    <color name="md_theme_on_primary">#00344F</color>
    <color name="md_theme_primary_container">#004B6E</color>
    <color name="md_theme_on_primary_container">#D6E4F0</color>

    <color name="md_theme_secondary">#ADC6FF</color>
    <color name="md_theme_on_secondary">#002E69</color>
    <color name="md_theme_secondary_container">#004494</color>
    <color name="md_theme_on_secondary_container">#D8E4FF</color>

    <!-- ... additional dark theme colors ... -->

    <color name="md_theme_surface">#1A1C1E</color>
    <color name="md_theme_on_surface">#E2E2E5</color>

    <!-- ... -->
</resources>
```

---

## Appendix C: Component Migration Examples

### Example 1: Button Style Migration

**Before (Material 2):**

```xml
<style name="MauiMaterialButton" parent="Widget.MaterialComponents.Button.UnelevatedButton">
    <item name="android:minWidth">0dp</item>
    <item name="android:minHeight">0dp</item>
    <item name="android:paddingLeft">0dp</item>
    <item name="android:paddingRight">0dp</item>
    <item name="android:paddingTop">0dp</item>
    <item name="android:paddingBottom">0dp</item>
    <item name="android:insetTop">0dp</item>
    <item name="android:insetBottom">0dp</item>
    <item name="android:insetLeft">0dp</item>
    <item name="android:insetRight">0dp</item>
</style>
```

**After (Material 3):**

```xml
<style name="MauiMaterialButton" parent="Widget.Material3.Button">
    <!-- M3 buttons have updated defaults, but we maintain MAUI's custom padding -->
    <item name="android:minWidth">0dp</item>
    <item name="android:minHeight">0dp</item>
    <item name="android:paddingLeft">0dp</item>
    <item name="android:paddingRight">0dp</item>
    <item name="android:paddingTop">0dp</item>
    <item name="android:paddingBottom">0dp</item>
    <item name="android:insetTop">0dp</item>
    <item name="android:insetBottom">0dp</item>
    <item name="android:insetLeft">0dp</item>
    <item name="android:insetRight">0dp</item>
    <!-- M3 specific: Use container color instead of backgroundTint -->
    <item name="backgroundTint">?attr/colorPrimary</item>
</style>
```

### Example 2: Theme Migration

**Before (Material 2):**

```xml
<style name="Maui.MainTheme.Base" parent="Theme.MaterialComponents.DayNight">
    <item name="colorPrimary">@color/colorPrimary</item>
    <item name="colorPrimaryDark">@color/colorPrimaryDark</item>
    <item name="colorAccent">@color/colorAccent</item>
    <item name="android:actionMenuTextColor">@color/colorActionMenuTextColor</item>
</style>
```

**After (Material 3):**

```xml
<style name="Maui.MainTheme.Base" parent="Theme.Material3.DayNight">
    <!-- Material 3 semantic colors -->
    <item name="colorPrimary">@color/md_theme_primary</item>
    <item name="colorOnPrimary">@color/md_theme_on_primary</item>
    <item name="colorPrimaryContainer">@color/md_theme_primary_container</item>
    <item name="colorOnPrimaryContainer">@color/md_theme_on_primary_container</item>

    <item name="colorSecondary">@color/md_theme_secondary</item>
    <item name="colorOnSecondary">@color/md_theme_on_secondary</item>
    <item name="colorSecondaryContainer">@color/md_theme_secondary_container</item>
    <item name="colorOnSecondaryContainer">@color/md_theme_on_secondary_container</item>

    <item name="colorTertiary">@color/md_theme_tertiary</item>
    <item name="colorOnTertiary">@color/md_theme_on_tertiary</item>
    <item name="colorTertiaryContainer">@color/md_theme_tertiary_container</item>
    <item name="colorOnTertiaryContainer">@color/md_theme_on_tertiary_container</item>

    <item name="colorError">@color/md_theme_error</item>
    <item name="colorOnError">@color/md_theme_on_error</item>
    <item name="colorErrorContainer">@color/md_theme_error_container</item>
    <item name="colorOnErrorContainer">@color/md_theme_on_error_container</item>

    <item name="android:colorBackground">@color/md_theme_background</item>
    <item name="colorOnBackground">@color/md_theme_on_background</item>

    <item name="colorSurface">@color/md_theme_surface</item>
    <item name="colorOnSurface">@color/md_theme_on_surface</item>
    <item name="colorSurfaceVariant">@color/md_theme_surface_variant</item>
    <item name="colorOnSurfaceVariant">@color/md_theme_on_surface_variant</item>

    <item name="colorOutline">@color/md_theme_outline</item>
    <item name="colorOutlineVariant">@color/md_theme_outline_variant</item>

    <!-- colorPrimaryDark is deprecated in M3 - removed -->
    <!-- colorAccent is replaced by colorSecondary -->

    <item name="android:actionMenuTextColor">?attr/colorOnSurface</item>
</style>
```

### Example 3: Dynamic Color Support

**Add to MauiAppCompatActivity.cs:**

```csharp
using Google.Android.Material.Color;

protected override void OnCreate(Bundle? savedInstanceState)
{
    // Apply dynamic color if available (Android 12+)
    if (OperatingSystem.IsAndroidVersionAtLeast(31))
    {
        DynamicColors.ApplyToActivityIfAvailable(this);
    }

    // existing code...
    base.OnCreate(savedInstanceState);
}
```

---

## Conclusion

This analysis provides a comprehensive roadmap for migrating .NET MAUI's Android implementation from Material Design 2 (Material Components) to Material Design 3 (Material You). The migration will modernize the visual design system, improve theming capabilities, and align with Google's latest design guidelines.

**Key Takeaways:**

-   Migration is feasible with existing package versions (1.12.0.5 already contains M3 components)
-   Primary change is theme-based: `Theme.MaterialComponents.*` → `Theme.Material3.*`
-   Approximately 100+ files will require updates
-   Estimated timeline: 5 weeks for complete migration
-   No backwards compatibility to maintain per requirements

**Next Steps:**

1. Review and approve this analysis
2. Begin Phase 1: Infrastructure updates
3. Proceed with incremental component migration
4. Comprehensive testing on multiple Android versions
5. Documentation updates

The migration will result in a modern, accessible, and visually consistent Android experience for all .NET MAUI applications.
