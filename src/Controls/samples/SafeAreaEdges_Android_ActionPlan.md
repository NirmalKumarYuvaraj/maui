# SafeAreaEdges Android Implementation - Action Plan

## Executive Summary

This document outlines the implementation plan for the `SafeAreaEdges` feature on the Android platform. The feature enables granular control over how views respect safe area insets (system bars, notches, and soft input/keyboard) on a per-edge basis.

---

## Current State Analysis

### What Already Exists

1. **Core Primitives** (✅ Complete)
    - `SafeAreaRegions` enum with flags: `None`, `SoftInput`, `Container`, `Default`, `All`
    - `SafeAreaEdges` struct with `Left`, `Top`, `Right`, `Bottom` properties
    - `SafeAreaEdgesTypeConverter` for XAML parsing
    - `ISafeAreaView2` interface with `GetSafeAreaRegionsForEdge()` method

2. **Controls Layer** (✅ Partially Complete)
    - `SafeAreaElement` class with `SafeAreaEdgesProperty` bindable property
    - `ContentPage`, `Layout`, `Border` implement `ISafeAreaView2` and `ISafeAreaElement`
    - `SafeAreaEdges` property exposed on these controls

3. **Android Platform** (🔶 Partial - Needs Integration)
    - `MauiWindowInsetListener` - Handles window insets and IME animations
    - `SafeAreaExtensions` - Helper methods for getting safe area regions
    - `SafeAreaPadding` - Struct for safe area padding values
    - `IHandleWindowInsets` - Interface for custom insets handling
    - `NavigationRootManager` - Sets up coordinator layout with inset listener
    - View groups (`ContentViewGroup`, `LayoutViewGroup`, `MauiScrollView`) have padding infrastructure

4. **Handler Layer** (🔴 Missing Implementation)
    - `ViewHandler.Android.cs` has empty `MapSafeAreaEdges` method
    - No actual integration of `SafeAreaEdges` values into padding calculations

---

## Gap Analysis

### Missing Functionality

| Component                      | Gap                                                            | Priority |
| ------------------------------ | -------------------------------------------------------------- | -------- |
| `MauiWindowInsetListener`      | Doesn't read `SafeAreaRegions` from `ICrossPlatformLayout`     | High     |
| View Groups                    | Don't apply per-edge safe area filtering                       | High     |
| `ViewHandler.MapSafeAreaEdges` | Empty implementation                                           | Medium   |
| Keyboard (IME) tracking        | Need to track `isKeyboardShowing` state for `SoftInput` region | High     |
| Tests                          | No Android-specific SafeAreaEdges tests                        | Medium   |

---

## Implementation Plan

### Phase 1: Enhanced Window Inset Listener (High Priority)

**Goal**: Update `MauiWindowInsetListener` to apply safe area padding based on `SafeAreaRegions` configuration.

#### 1.1 Update `MauiWindowInsetListener.cs`

**File**: `src/Core/src/Platform/Android/MauiWindowInsetListener.cs`

**Changes**:

- Add `ICrossPlatformLayoutBacking` reference to get the virtual view's safe area settings
- Track keyboard visibility state (`isKeyboardShowing`)
- Apply per-edge safe area filtering using `SafeAreaExtensions.GetSafeAreaForEdge()`

```csharp
// Key changes:
public virtual WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
{
    // 1. Get the ICrossPlatformLayout from the view
    // 2. For each edge, call SafeAreaExtensions.GetSafeAreaRegionForEdge()
    // 3. Calculate filtered padding using SafeAreaExtensions.GetSafeAreaForEdge()
    // 4. Apply filtered padding via v.SetPadding()
}
```

#### 1.2 Track IME Visibility

**Current State**: `IsImeAnimating` flag exists but doesn't track final keyboard state.

**Changes**:

- Add `IsKeyboardVisible` property
- Update in `OnEnd()` when IME animation completes
- Pass to `GetSafeAreaForEdge()` for `SoftInput` region handling

### Phase 2: View Group Integration (High Priority)

**Goal**: Ensure view groups properly respect safe area settings through their `ICrossPlatformLayout`.

#### 2.1 ContentViewGroup / LayoutViewGroup

**Files**:

- `src/Core/src/Platform/Android/ContentViewGroup.cs`
- `src/Core/src/Platform/Android/LayoutViewGroup.cs`

**Analysis**: These view groups already:

- ✅ Implement `ICrossPlatformLayoutBacking`
- ✅ Account for padding in `OnMeasure()` and `OnLayout()`
- ✅ Have `CrossPlatformLayout` property

**No changes needed** - The inset listener sets padding, and these view groups already respect padding.

#### 2.2 MauiScrollView

**File**: `src/Core/src/Platform/Android/MauiScrollView.cs`

**Analysis**: Already implements `ICrossPlatformLayoutBacking` but may need special handling for scroll content insets vs container insets.

**Potential Changes**: Consider if scroll content should have different safe area behavior than the scroll container.

### Phase 3: Handler Integration (Medium Priority)

**Goal**: Implement `MapSafeAreaEdges` to trigger inset re-application when property changes.

#### 3.1 ViewHandler.Android.cs

**File**: `src/Core/src/Handlers/View/ViewHandler.Android.cs`

**Changes**:

```csharp
internal static void MapSafeAreaEdges(IViewHandler handler, IView view)
{
    if (handler.PlatformView is not View platformView)
        return;

    // Request insets re-application when SafeAreaEdges changes
    platformView.RequestApplyInsets();
}
```

### Phase 4: Testing (Medium Priority)

#### 4.1 Unit Tests

**File**: `src/Controls/tests/Core.UnitTests/SafeAreaTests.cs` (extend existing)

**Tests**:

- `SafeAreaEdges_None_ReturnsZeroPadding`
- `SafeAreaEdges_All_ReturnsFullSafeArea`
- `SafeAreaEdges_SoftInput_OnlyAppliesWhenKeyboardVisible`
- `SafeAreaEdges_Container_DoesNotIncludeKeyboard`
- `SafeAreaEdges_PerEdge_AppliesCorrectly`

#### 4.2 Device Tests

**File**: `src/Core/tests/DeviceTests/Handlers/Layout/SafeAreaEdgesTests.Android.cs` (new)

**Tests**:

- Verify actual padding values on physical device/emulator
- Test keyboard show/hide scenarios
- Test with different device configurations (notch, nav bar styles)

### Phase 5: Documentation & Samples (Low Priority)

#### 5.1 Sandbox Sample

**File**: `src/Controls/samples/Controls.Sample.Sandbox/MainPage.xaml`

**Sample demonstrating**:

- Different `SafeAreaEdges` configurations
- Keyboard interaction with `SoftInput` region
- Visual comparison of `None`, `Container`, `All`

---

## Detailed Implementation Steps

### Step 1: Update MauiWindowInsetListener

```csharp
// src/Core/src/Platform/Android/MauiWindowInsetListener.cs

internal class MauiWindowInsetListener : WindowInsetsAnimationCompat.Callback, IOnApplyWindowInsetsListener
{
    bool IsImeAnimating { get; set; }
    bool IsKeyboardVisible { get; set; }  // NEW
    AView? _view;
    WindowInsetsCompat? _windowInsetsCompat;

    public virtual WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
    {
        if (insets is null || !insets.HasInsets || v is null)
            return insets;

        _view = v;
        _windowInsetsCompat = insets;

        // Get safe area padding from system bars and display cutout
        var safeAreaPadding = insets.ToSafeAreaInsetsPx(v.Context);
        var keyboardInsets = insets.GetKeyboardInsetsPx(v.Context);

        // Track keyboard visibility
        IsKeyboardVisible = keyboardInsets.Bottom > 0;

        // Get the ICrossPlatformLayout if available
        var crossPlatformLayout = (v as ICrossPlatformLayoutBacking)?.CrossPlatformLayout;

        double left = safeAreaPadding.Left;
        double top = safeAreaPadding.Top;
        double right = safeAreaPadding.Right;
        double bottom = safeAreaPadding.Bottom;

        if (crossPlatformLayout is not null)
        {
            // Apply per-edge filtering based on SafeAreaRegions
            left = SafeAreaExtensions.GetSafeAreaForEdge(
                SafeAreaExtensions.GetSafeAreaRegionForEdge(0, crossPlatformLayout),
                safeAreaPadding.Left, 0, IsKeyboardVisible, keyboardInsets);

            top = SafeAreaExtensions.GetSafeAreaForEdge(
                SafeAreaExtensions.GetSafeAreaRegionForEdge(1, crossPlatformLayout),
                safeAreaPadding.Top, 1, IsKeyboardVisible, keyboardInsets);

            right = SafeAreaExtensions.GetSafeAreaForEdge(
                SafeAreaExtensions.GetSafeAreaRegionForEdge(2, crossPlatformLayout),
                safeAreaPadding.Right, 2, IsKeyboardVisible, keyboardInsets);

            bottom = SafeAreaExtensions.GetSafeAreaForEdge(
                SafeAreaExtensions.GetSafeAreaRegionForEdge(3, crossPlatformLayout),
                safeAreaPadding.Bottom, 3, IsKeyboardVisible, keyboardInsets);
        }
        else if (!IsImeAnimating)
        {
            // Fallback: include keyboard in bottom if not animating
            bottom = Math.Max(safeAreaPadding.Bottom, keyboardInsets.Bottom);
        }

        v.SetPadding((int)left, (int)top, (int)right, (int)bottom);
        return WindowInsetsCompat.Consumed;
    }
}
```

### Step 2: Wire Up Handler

```csharp
// src/Core/src/Handlers/View/ViewHandler.Android.cs

internal static void MapSafeAreaEdges(IViewHandler handler, IView view)
{
    if (handler.IsConnectingHandler())
        return;

    if (handler.MauiContext?.Context is null || handler.PlatformView is not View platformView)
        return;

    // Trigger re-application of window insets
    ViewCompat.RequestApplyInsets(platformView);
}
```

---

## Risk Assessment

| Risk                                                 | Impact | Mitigation                                                                |
| ---------------------------------------------------- | ------ | ------------------------------------------------------------------------- |
| Breaking existing apps that rely on current behavior | High   | Default to `SafeAreaRegions.Default` which preserves current behavior     |
| Keyboard animation jitter                            | Medium | IME animation deferring logic already exists in `MauiWindowInsetListener` |
| Performance overhead from per-edge calculations      | Low    | Calculations are simple flag checks; only during inset changes            |
| Navigation scenarios (Shell, NavigationPage)         | Medium | Need to test navigation stack scenarios carefully                         |

---

## Open Questions

1. **Should `MauiScrollView` have separate safe area handling?**
    - Scroll content vs scroll container padding may need different treatment

2. **How should nested layouts with different SafeAreaEdges interact?**
    - E.g., Page with `All` containing Layout with `None`

3. **Should we support `SafeAreaEdges` on individual controls (Entry, Button)?**
    - Current implementation focuses on layout containers

4. **API 28-29 compatibility**
    - `ViewGroupCompat.InstallCompatInsetsDispatch` is already used - verify it works with new logic

---

## Success Criteria

1. ✅ `SafeAreaEdges.None` results in edge-to-edge content (no padding)
2. ✅ `SafeAreaEdges.All` applies full safe area padding including keyboard
3. ✅ `SafeAreaEdges.Container` applies safe area but not keyboard
4. ✅ `SafeAreaEdges.SoftInput` only adds padding when keyboard is visible
5. ✅ Per-edge configuration works (e.g., `SafeAreaEdges="All,None,All,SoftInput"`)
6. ✅ Property change triggers re-layout
7. ✅ All existing tests pass
8. ✅ New tests cover SafeAreaEdges scenarios

---

## Estimated Effort

| Phase                   | Effort   | Dependencies |
| ----------------------- | -------- | ------------ |
| Phase 1: Inset Listener | 2-3 days | None         |
| Phase 2: View Groups    | 0.5 day  | Phase 1      |
| Phase 3: Handler        | 0.5 day  | Phase 1      |
| Phase 4: Tests          | 2 days   | Phases 1-3   |
| Phase 5: Docs/Samples   | 1 day    | Phases 1-3   |

**Total**: ~6-7 days

---

## Next Steps

1. **Review this plan** - Get feedback on approach
2. **Start Phase 1** - Update `MauiWindowInsetListener`
3. **Test incrementally** - Use Sandbox app to verify each phase
4. **Submit PR** - With tests and documentation
