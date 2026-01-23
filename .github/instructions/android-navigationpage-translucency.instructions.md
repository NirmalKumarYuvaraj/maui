# Android NavigationPage Translucency Implementation

## Context

iOS PR #30981 implements automatic NavigationBar translucency based on `BarBackgroundColor.Alpha < 1f`. This instruction provides guidance for implementing the equivalent behavior on Android platform.

## Key Difference: iOS vs Android

| Platform | Mechanism |
|----------|-----------|
| **iOS** | Single property: `NavigationBar.Translucent = true` |
| **Android** | Three-part coordination: AppBarLayout elevation + window insets + content layout |

## Problem Statement

**Current Behavior (Lines 256-264 in MauiWindowInsetListener.cs):**
```csharp
if (appBarHasContent)
{
    var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
    appBarLayout.SetPadding(systemBars?.Left ?? 0, topInset, systemBars?.Right ?? 0, 0);
}
```

**Issue:** When AppBarLayout has content (toolbar), padding is applied to push content **below** the AppBarLayout. This prevents edge-to-edge rendering even when BarBackground is transparent.

**Goal:** When `BarBackgroundColor.Alpha < 1f`, content should render **edge-to-edge** (under the transparent toolbar), not below it.

---

## Implementation Strategy

### Step 1: Detect Transparent Toolbar

Add method to check if toolbar should be translucent:

```csharp
// Location: ToolbarHandler.Android.cs or new ToolbarExtensions.Android.cs

internal static bool IsTranslucentToolbar(MaterialToolbar? toolbar)
{
    if (toolbar?.Handler is not ToolbarHandler handler)
        return false;
    
    var virtualToolbar = handler.VirtualView;
    if (virtualToolbar is null)
        return false;
    
    // Check BarBackground brush for alpha
    if (virtualToolbar.BarBackground is SolidColorBrush solidBrush)
    {
        return solidBrush.Color.Alpha < 1f;
    }
    
    return false;
}
```

### Step 2: Modify Window Inset Logic

**File:** `src/Core/src/Platform/Android/MauiWindowInsetListener.cs`

**Location:** Lines 253-265 (ApplyDefaultWindowInsets method)

**Current Code:**
```csharp
// Apply padding to AppBarLayout based on content and system insets
if (appBarLayout is not null)
{
    if (appBarHasContent)
    {
        var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
        appBarLayout.SetPadding(systemBars?.Left ?? 0, topInset, systemBars?.Right ?? 0, 0);
    }
    else
    {
        appBarLayout.SetPadding(0, 0, 0, 0);
    }
}
```

**Modified Code:**
```csharp
// Apply padding to AppBarLayout based on content, translucency, and system insets
if (appBarLayout is not null)
{
    // Check if toolbar inside AppBarLayout is transparent
    var toolbar = appBarLayout.FindViewById<MaterialToolbar>(Resource.Id.maui_toolbar);
    bool isTranslucent = toolbar != null && IsTranslucentToolbar(toolbar);
    
    if (appBarHasContent)
    {
        if (isTranslucent)
        {
            // Transparent toolbar: Apply padding to AppBarLayout itself (for status bar)
            // but let content render under it (edge-to-edge)
            var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
            appBarLayout.SetPadding(systemBars?.Left ?? 0, topInset, systemBars?.Right ?? 0, 0);
            
            // DO NOT consume top insets - pass through to content
            // This allows content to extend to screen top
        }
        else
        {
            // Opaque toolbar: Apply padding to AppBarLayout normally
            var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
            appBarLayout.SetPadding(systemBars?.Left ?? 0, topInset, systemBars?.Right ?? 0, 0);
        }
    }
    else
    {
        appBarLayout.SetPadding(0, 0, 0, 0);
    }
}
```

### Step 3: Modify Inset Consumption (Critical)

**Location:** Lines 279-297 (same file)

**Current Code:**
```csharp
// Create new insets with consumed values
var newSystemBars = Insets.Of(
    systemBars?.Left ?? 0,
    appBarHasContent ? 0 : systemBars?.Top ?? 0,  // ← Consumes top if appbar has content
    systemBars?.Right ?? 0,
    hasBottomNav ? 0 : systemBars?.Bottom ?? 0
) ?? Insets.None;
```

**Modified Code:**
```csharp
// Determine if we should consume top insets
// - If no appbar content: pass through (0 consumed, full inset to children)
// - If opaque appbar: consume (0 passed, appbar handles it)
// - If translucent appbar: pass through (0 consumed, content extends under toolbar)
bool shouldConsumeTopInset = appBarHasContent && !isTranslucent;

var newSystemBars = Insets.Of(
    systemBars?.Left ?? 0,
    shouldConsumeTopInset ? 0 : systemBars?.Top ?? 0,
    systemBars?.Right ?? 0,
    hasBottomNav ? 0 : systemBars?.Bottom ?? 0
) ?? Insets.None;

var newDisplayCutout = Insets.Of(
    displayCutout?.Left ?? 0,
    shouldConsumeTopInset ? 0 : displayCutout?.Top ?? 0,
    displayCutout?.Right ?? 0,
    hasBottomNav ? 0 : displayCutout?.Bottom ?? 0
) ?? Insets.None;
```

**Key Logic:**
- **Opaque toolbar** (`!isTranslucent`): Consume top insets (children start below toolbar)
- **Translucent toolbar** (`isTranslucent`): Pass top insets to children (content extends to screen top, rendering under toolbar)

---

## Step 4: Elevation Control (Visual Polish)

**File:** `src/Core/src/Handlers/Toolbar/ToolbarHandler.Android.cs`

Add elevation management when BarBackground changes:

```csharp
public static void MapBarBackground(IToolbarHandler handler, IToolbar toolbar)
{
    if (handler is not ToolbarHandler androidHandler || 
        androidHandler.PlatformView is not MaterialToolbar materialToolbar)
        return;
    
    // Get parent AppBarLayout
    var appBarLayout = materialToolbar.Parent as AppBarLayout;
    if (appBarLayout == null)
        return;
    
    // Detect if translucent
    bool isTranslucent = false;
    if (toolbar.BarBackground is SolidColorBrush solidBrush)
    {
        isTranslucent = solidBrush.Color.Alpha < 1f;
    }
    
    if (isTranslucent)
    {
        // Remove elevation for flat, translucent appearance
        appBarLayout.SetElevation(0f);
        appBarLayout.SetStateListAnimator(null); // Disable elevation animations
    }
    else
    {
        // Restore default Material Design elevation
        var context = materialToolbar.Context;
        if (context != null)
        {
            var defaultElevation = context.Resources.GetDimension(
                Resource.Dimension.design_appbar_elevation);
            appBarLayout.SetElevation(defaultElevation);
        }
    }
}
```

**Add to Mapper:**
```csharp
public static IPropertyMapper<IToolbar, IToolbarHandler> Mapper =
    new PropertyMapper<IToolbar, IToolbarHandler>(ElementMapper)
    {
        [nameof(IToolbar.Title)] = MapTitle,
        [nameof(IToolbar.BarBackground)] = MapBarBackground,  // ← Add this
    };
```

---

## Step 5: Trigger Update from NavigationPageToolbar

**File:** `src/Controls/src/Core/NavigationPage/NavigationPageToolbar.cs`

**Location:** Line 254 (ApplyChanges method)

**Current Code:**
```csharp
BarBackground = navigationPage.BarBackground;
if (Brush.IsNullOrEmpty(BarBackground) &&
    navigationPage.BarBackgroundColor != null)
{
    BarBackground = new SolidColorBrush(navigationPage.BarBackgroundColor);
}
```

**Add after this:**
```csharp
#if ANDROID
// Trigger handler update for translucency detection
if (Handler?.PlatformView is Google.Android.Material.AppBar.MaterialToolbar)
{
    Handler.UpdateValue(nameof(IToolbar.BarBackground));
}
#endif
```

---

## Testing Scenarios

### Test 1: Transparent Background (Alpha = 0)
```csharp
new NavigationPage(new ContentPage())
{
    BarBackgroundColor = Colors.Transparent
};
```
**Expected:** Content renders edge-to-edge, visible under toolbar area.

### Test 2: Semi-Transparent Background (0 < Alpha < 1)
```csharp
new NavigationPage(new ContentPage())
{
    BarBackgroundColor = Color.FromRgba(255, 0, 0, 128) // 50% red
};
```
**Expected:** Content visible under toolbar, toolbar has translucent red overlay.

### Test 3: Opaque Background (Alpha = 1)
```csharp
new NavigationPage(new ContentPage())
{
    BarBackgroundColor = Colors.Red
};
```
**Expected:** Content starts below toolbar (current behavior maintained).

### Test 4: With SafeAreaEdges
```csharp
new ContentPage 
{ 
    SafeAreaEdges = SafeAreaEdges.Top 
}
```
**Expected:** Even with transparent toolbar, content respects status bar safe area.

---

## Edge Cases

### 1. FlyoutPage with Translucent Navigation
When `IFlyoutView` uses translucent toolbar:
- Drawer content should also respect translucency
- Window insets apply to both drawer and detail pages

### 2. Keyboard Interaction
When keyboard shows with translucent toolbar:
- Toolbar remains translucent
- Content insets adjust for keyboard, not affected by toolbar translucency

### 3. AppBarLayout Height Changes
If toolbar visibility changes (`IsVisible` property):
- `appBarHasContent` recalculates based on `MeasuredHeight`
- Inset logic re-applies correctly

---

## Files to Modify

| Priority | File | Change |
|----------|------|--------|
| **HIGH** | `MauiWindowInsetListener.cs` (lines 253-297) | Detect translucent toolbar, modify inset consumption |
| **HIGH** | `ToolbarHandler.Android.cs` | Add BarBackground mapper, elevation control |
| **MEDIUM** | `NavigationPageToolbar.cs` (line 254+) | Trigger handler update on Android |
| **LOW** | Create `ToolbarExtensions.Android.cs` | Helper method `IsTranslucentToolbar()` |

---

## Architecture Notes

### Why This Differs from iOS

iOS has a unified translucency system: setting `Translucent = true` automatically:
1. Adjusts content layout to extend under bar
2. Applies blur/vibrancy effects
3. Handles status bar integration

Android requires manual coordination:
1. **Elevation** controls visual depth (shadow)
2. **Window insets** control content positioning
3. **CoordinatorLayout behavior** controls overlap

### Integration with Edge-to-Edge Architecture

MAUI Android enables edge-to-edge globally:
```csharp
// MauiAppCompatActivity.cs line 31
WindowCompat.SetDecorFitsSystemWindows(Window, false);
```

This means **all content can potentially draw under system bars**. The `MauiWindowInsetListener` manages which views get padding and which extend edge-to-edge.

**Current logic:** If AppBarLayout has content → consume top insets → children render below toolbar

**New logic:** If AppBarLayout has **translucent** content → pass top insets → children render under toolbar

---

## Common Mistakes to Avoid

❌ **Setting `appBarLayout.SetPadding(0, 0, 0, 0)` when translucent**
- This removes status bar padding from the toolbar itself
- Toolbar content (title, buttons) would render under status bar icons
- Correct: Keep padding on AppBarLayout, just don't consume insets for children

❌ **Only changing elevation without inset logic**
- Elevation is visual only
- Content positioning is controlled by window insets consumption
- Both must change together

❌ **Checking toolbar background in `appBarHasContent` check**
- `appBarHasContent` should remain height-based
- Translucency check is separate condition
- They work together but measure different things

---

## Performance Considerations

### Inset Listener Overhead
- `IsTranslucentToolbar()` checks happen during inset application
- Insets apply on layout pass, orientation change, keyboard show/hide
- Keep check lightweight (avoid heavy reflection or traversal)

### Recommended Optimization
Cache translucent state on the toolbar view:
```csharp
// When BarBackground changes
toolbar.SetTag(Resource.Id.toolbar_is_translucent, isTranslucent);

// In inset listener
bool isTranslucent = toolbar.GetTag(Resource.Id.toolbar_is_translucent) as bool? ?? false;
```

Define tag ID in `Resource.Id` (attrs.xml):
```xml
<item name="toolbar_is_translucent" type="id"/>
```

---

## Verification Checklist

Before committing implementation:

- [ ] Transparent toolbar: Content renders edge-to-edge (visible under toolbar)
- [ ] Opaque toolbar: Content starts below toolbar (existing behavior)
- [ ] Semi-transparent toolbar: Content visible through toolbar overlay
- [ ] Status bar: Toolbar respects status bar insets (padding on AppBarLayout)
- [ ] Navigation bar: Bottom content respects nav bar insets (unchanged)
- [ ] Keyboard: Shows without breaking translucent toolbar
- [ ] FlyoutPage: Drawer respects translucent navigation
- [ ] SafeAreaEdges: Still respected when set explicitly
- [ ] Elevation: Removed when translucent, restored when opaque
- [ ] No visual glitches during transitions

---

## Related Files

Understanding the full architecture:

- **`MauiAppCompatActivity.cs`** - Enables edge-to-edge globally (line 31)
- **`NavigationRootManager.cs`** - Sets up CoordinatorLayout with local listener (line 84)
- **`SafeAreaExtensions.cs`** - SafeArea/SafeAreaEdges handling
- **`navigationlayout.axml`** - CoordinatorLayout + AppBarLayout + content structure
- **`IToolbar.cs`** - Toolbar interface (would add IsTranslucent property if needed)

## Future Enhancements

### Optional: Explicit Translucent Property

Similar to iOS deprecating platform-specific `IsNavigationBarTranslucent`, could add:

```csharp
// Platform-specific for Android
NavigationPage.SetIsTranslucent(navPage, true);

// Priority: Explicit setting > Automatic (alpha-based)
```

This would override automatic detection, matching iOS behavior from PR #30981.
