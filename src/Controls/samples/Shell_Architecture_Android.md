# Shell Architecture on Android - Comprehensive Analysis

## Executive Summary

This document provides a comprehensive analysis of the .NET MAUI Shell implementation on Android, identifying the key architectural differences between Shell and NavigationPage, specifically focusing on why Shell uses hardcoded values for colors and styling instead of a layout-based approach with theme attributes like NavigationPage.

### Key Finding

**NavigationPage** uses XML layout files (`navigationlayout.axml`) with theme attributes (`@style/MauiAppBarLayout`) that resolve colors at runtime from the theme.

**Shell** builds its UI components **programmatically in Java/C#** without XML layouts for the main Shell structure (except for `flyoutcontent.axml` and `shellcontent.axml` for AppBarLayout), and uses **hardcoded color values** in C# code that are resolved at runtime based on `RuntimeFeature.IsMaterial3Enabled`.

---

## 1. Architecture Comparison

### 1.1 NavigationPage Approach (Layout-Based)

**File:** `src/Core/src/Platform/Android/Resources/Layout/navigationlayout.axml`

```xml
<androidx.coordinatorlayout.widget.CoordinatorLayout ...>
    <com.google.android.material.appbar.AppBarLayout
        android:id="@+id/navigationlayout_appbar"
        android:theme="@style/MauiAppBarLayout">  <!-- THEME ATTRIBUTE -->
        
        <androidx.fragment.app.FragmentContainerView
            android:id="@+id/navigationlayout_toptabs" />
    </com.google.android.material.appbar.AppBarLayout>

    <androidx.fragment.app.FragmentContainerView
        android:id="@+id/navigationlayout_content"
        app:layout_behavior="@string/appbar_scrolling_view_behavior" />
        
    <androidx.fragment.app.FragmentContainerView
        android:id="@+id/navigationlayout_bottomtabs" />
</androidx.coordinatorlayout.widget.CoordinatorLayout>
```

**Style Definition:** `src/Core/src/Platform/Android/Resources/values/styles.xml`

```xml
<style name="MauiAppBarLayout" parent="ThemeOverlay.AppCompat.Dark.ActionBar">
    <item name="android:background">?attr/colorPrimary</item>  <!-- RESOLVES FROM THEME -->
</style>
```

**Benefits:**
- Colors are resolved from the current theme at runtime
- Automatically adapts to Material 3 theme colors
- No code changes needed for theme updates
- Standard Android theming behavior

### 1.2 Shell Approach (Programmatic with Hardcoded Values)

**Shell creates UI components programmatically via:**

1. **`PlatformInterop.java`** - Creates core layout views
2. **`ShellSectionRenderer.cs`** - Orchestrates UI creation
3. **`ShellItemRenderer.cs`** - Creates BottomNavigationView
4. **`ShellRenderer.cs`** - Defines hardcoded default colors

**Shell Layout Files (Limited):**

| File | Purpose | Theme Attribute? |
|------|---------|-----------------|
| `flyoutcontent.axml` | Flyout content wrapper | ✅ Uses `@style/MauiAppBarLayout` |
| `shellcontent.axml` | Shell content wrapper | ✅ Uses `@style/MauiAppBarLayout` |

**But these only wrap AppBarLayout - the rest is programmatic!**

---

## 2. Hardcoded Color Values in Shell

### 2.1 Location: `ShellRenderer.cs`

**File:** `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellRenderer.cs`

```csharp
// Lines 95-99 - THE PROBLEMATIC HARDCODED VALUES
public static Color DefaultBackgroundColor => ResolveThemeColor(
    RuntimeFeature.IsMaterial3Enabled ? Color.FromArgb("#FEF7FF") : Color.FromArgb("#2c3e50"),  // HARDCODED
    RuntimeFeature.IsMaterial3Enabled ? Color.FromArgb("#141218") : Color.FromArgb("#1B3147")); // HARDCODED

public static Color DefaultForegroundColor => ResolveThemeColor(
    RuntimeFeature.IsMaterial3Enabled ? Color.FromArgb("#1D1B20") : Colors.White,  // HARDCODED
    RuntimeFeature.IsMaterial3Enabled ? Color.FromArgb("#E6E0E9") : Colors.White); // HARDCODED

public static Color DefaultTitleColor => ResolveThemeColor(
    RuntimeFeature.IsMaterial3Enabled ? Color.FromArgb("#1D1B20") : Colors.White,  // HARDCODED
    RuntimeFeature.IsMaterial3Enabled ? Color.FromArgb("#E6E0E9") : Colors.White); // HARDCODED

public static readonly Color DefaultUnselectedColor = Color.FromRgba(255, 255, 255, 180);

internal static Color DefaultBottomNavigationViewBackgroundColor => ResolveThemeColor(
    RuntimeFeature.IsMaterial3Enabled ? Color.FromArgb("#F3EDF7") : Colors.White,  // HARDCODED
    RuntimeFeature.IsMaterial3Enabled ? Color.FromArgb("#1D1B20") : Color.FromArgb("#1B3147")); // HARDCODED

static Color ResolveThemeColor(Color light, Color dark)
{
    if (IsDarkTheme)
        return dark;
    return light;
}
```

### 2.2 Where These Colors Are Used

| Component | Class | Method | Colors Used |
|-----------|-------|--------|-------------|
| Toolbar | `ShellToolbarAppearanceTracker` | `ResetAppearance()` | `DefaultForegroundColor`, `DefaultBackgroundColor`, `DefaultTitleColor` |
| TabLayout | `ShellTabLayoutAppearanceTracker` | `ResetAppearance()` | `DefaultForegroundColor`, `DefaultBackgroundColor`, `DefaultTitleColor`, `DefaultUnselectedColor` |
| BottomNavView | `ShellBottomNavViewAppearanceTracker` | `SetBackgroundColor()` | `DefaultBottomNavigationViewBackgroundColor` |
| BottomNavView | `ShellBottomNavViewAppearanceTracker` | `MakeDefaultColorStateList()` | `DefaultBackgroundColor` (for unselected icons) |

### 2.3 Additional Hardcoded Values

**`ShellBottomNavViewAppearanceTracker.cs` Line 134:**
```csharp
var colorPrimary = (ShellRenderer.IsDarkTheme) 
    ? AColor.White 
    : RuntimeFeature.IsMaterial3Enabled 
        ? Color.FromArgb("#625B71").ToPlatform()  // HARDCODED M3 color
        : ShellRenderer.DefaultBackgroundColor.ToPlatform();
```

**`PlatformInterop.java` Line 200:**
```java
public static BottomNavigationView createNavigationBar(...) {
    BottomNavigationView navigationView = new BottomNavigationView(context, null, styleAttribute);
    navigationView.setBackgroundColor(Color.WHITE);  // HARDCODED!
    ...
}
```

---

## 3. Shell Component Creation Flow

### 3.1 Main View Hierarchy

```
ShellRenderer (IShellContext)
    ├── ShellFlyoutRenderer (DrawerLayout)
    │       ├── Content Area (FrameLayout)
    │       │       └── ShellItemRenderer (Fragment)
    │       │               ├── LinearLayout (outer)
    │       │               │   ├── FrameLayout (navigation area)
    │       │               │   │   └── ShellSectionRenderer (Fragment)
    │       │               │   │           ├── CoordinatorLayout
    │       │               │   │           │   ├── AppBarLayout
    │       │               │   │           │   │   ├── MaterialToolbar
    │       │               │   │           │   │   └── TabLayout
    │       │               │   │           │   └── ViewPager2
    │       │               │   └── BottomNavigationView
    │       └── Flyout Content (ShellFlyoutTemplatedContentRenderer)
    │               └── ShellFlyoutLayout (CoordinatorLayout)
    │                       ├── AppBarLayout (from flyoutcontent.axml)
    │                       │   └── HeaderContainer
    │                       └── RecyclerView (flyout items)
```

### 3.2 Component Creation Methods

| Component | Created In | Method | Layout Source |
|-----------|-----------|--------|---------------|
| **BottomNavigationView** | `ShellItemRenderer.cs` | `OnCreateView()` | `PlatformInterop.createNavigationBar()` - **PROGRAMMATIC** |
| **TabLayout** | `ShellSectionRenderer.cs` | `OnCreateView()` | `PlatformInterop.createShellTabLayout()` - **PROGRAMMATIC** |
| **Toolbar** | `ShellSectionRenderer.cs` | `OnCreateView()` | `PlatformInterop.createToolbar()` via `Toolbar.ToPlatform()` |
| **CoordinatorLayout** | `ShellSectionRenderer.cs` | `OnCreateView()` | `PlatformInterop.createShellCoordinatorLayout()` - **PROGRAMMATIC** |
| **AppBarLayout** | `ShellSectionRenderer.cs` | `OnCreateView()` | `PlatformInterop.createShellAppBar()` - **PROGRAMMATIC** |
| **ViewPager2** | `ShellSectionRenderer.cs` | `OnCreateView()` | `PlatformInterop.createShellViewPager()` - **PROGRAMMATIC** |
| **Flyout Content** | `ShellFlyoutTemplatedContentRenderer.cs` | `LoadView()` | `flyoutcontent.axml` - **XML LAYOUT** ✅ |

---

## 4. PlatformInterop Methods Analysis

### 4.1 Shell-Specific Methods in `PlatformInterop.java`

```java
// Creates CoordinatorLayout without any theme/style
public static CoordinatorLayout createShellCoordinatorLayout(Context context) {
    CoordinatorLayout layout = new CoordinatorLayout(context);
    layout.setLayoutParams(new ViewGroup.LayoutParams(MATCH_PARENT, MATCH_PARENT));
    return layout;  // NO THEME, NO STYLE
}

// Creates AppBarLayout with style attribute but NO theme colors
public static AppBarLayout createShellAppBar(Context context, int appBarStyleAttribute, CoordinatorLayout layout) {
    AppBarLayout appbar = new AppBarLayout(context, null, appBarStyleAttribute);
    appbar.setLayoutParams(new ViewGroup.LayoutParams(MATCH_PARENT, WRAP_CONTENT));
    layout.addView(appbar);
    return appbar;  // Style from attribute, but background not set
}

// Creates TabLayout - NO background color from theme
public static TabLayout createShellTabLayout(Context context, AppBarLayout appbar, int actionBarHeight) {
    TabLayout layout = new TabLayout(context);
    AppBarLayout.LayoutParams layoutParams = new AppBarLayout.LayoutParams(MATCH_PARENT, actionBarHeight);
    layoutParams.gravity = Gravity.BOTTOM;
    layout.setLayoutParams(layoutParams);
    layout.setTabMode(TabLayout.MODE_SCROLLABLE);
    appbar.addView(layout);
    return layout;  // NO BACKGROUND COLOR SET!
}

// Creates BottomNavigationView - HARDCODED WHITE BACKGROUND!
public static BottomNavigationView createNavigationBar(...) {
    BottomNavigationView navigationView = new BottomNavigationView(context, null, styleAttribute);
    navigationView.setBackgroundColor(Color.WHITE);  // ❌ HARDCODED
    navigationView.setOnItemSelectedListener(listener);
    linearLayout.addView(navigationView);
    return navigationView;
}
```

---

## 5. Appearance Tracker System

Shell uses "Appearance Trackers" to apply styling after components are created:

### 5.1 Tracker Classes

| Tracker | Responsibility | Uses Hardcoded Defaults? |
|---------|---------------|-------------------------|
| `ShellToolbarAppearanceTracker` | Toolbar colors | ✅ Yes - `DefaultForegroundColor`, `DefaultBackgroundColor`, `DefaultTitleColor` |
| `ShellTabLayoutAppearanceTracker` | TabLayout colors | ✅ Yes - Same defaults |
| `ShellBottomNavViewAppearanceTracker` | BottomNavigationView colors | ✅ Yes - `DefaultBottomNavigationViewBackgroundColor` |

### 5.2 Example: `ShellTabLayoutAppearanceTracker.cs`

```csharp
public virtual void ResetAppearance(TabLayout tabLayout)
{
    // ALL using hardcoded defaults from ShellRenderer
    SetColors(tabLayout, 
        ShellRenderer.DefaultForegroundColor,
        ShellRenderer.DefaultBackgroundColor,
        ShellRenderer.DefaultTitleColor,
        ShellRenderer.DefaultUnselectedColor);
}

protected virtual void SetColors(TabLayout tabLayout, Color foreground, Color background, Color title, Color unselected)
{
    var titleArgb = title.ToPlatform(ShellRenderer.DefaultTitleColor).ToArgb();
    var unselectedArgb = unselected.ToPlatform(ShellRenderer.DefaultUnselectedColor).ToArgb();

    tabLayout.SetTabTextColors(unselectedArgb, titleArgb);
    tabLayout.SetBackground(new ColorDrawable(background.ToPlatform(ShellRenderer.DefaultBackgroundColor)));
    tabLayout.SetSelectedTabIndicatorColor(foreground.ToPlatform(ShellRenderer.DefaultForegroundColor));
}
```

---

## 6. Problems with Current Approach

### 6.1 Issues

1. **No Theme Integration**: Colors don't resolve from `?attr/colorPrimary`, `?attr/colorSurface`, etc.
2. **Duplicate Color Definitions**: Material 3 colors are hardcoded in C# AND in `styles-material3.xml`
3. **No Dynamic Theme Support**: Dynamic Color (Material You) won't work
4. **Maintenance Burden**: Every theme update requires C# code changes
5. **Inconsistency**: Flyout uses XML layout with theme, but Shell content doesn't
6. **Dark Theme Issues**: Manual `IsDarkTheme` check instead of using theme system

### 6.2 Why NavigationPage Works Better

NavigationPage's `navigationlayout.axml`:
```xml
<AppBarLayout android:theme="@style/MauiAppBarLayout">
```

Where `MauiAppBarLayout` uses:
```xml
<item name="android:background">?attr/colorPrimary</item>
```

This `?attr/colorPrimary` is resolved at runtime from the current theme, whether it's:
- `Maui.MainTheme.Base` (Material Components)
- `Maui.Material3.Theme.Base` (Material 3)
- A custom theme

---

## 7. Proposed Solution: Layout-Based Shell Architecture

### 7.1 Create New Shell Layout Files

**New File: `shelllayout.axml`** (similar to `navigationlayout.axml`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<androidx.coordinatorlayout.widget.CoordinatorLayout
    xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:app="http://schemas.android.com/apk/res-auto"
    android:id="@+id/shell_coordinator"
    android:layout_width="match_parent"
    android:layout_height="match_parent">

    <com.google.android.material.appbar.AppBarLayout
        android:id="@+id/shell_appbar"
        android:layout_width="match_parent"
        android:layout_height="wrap_content"
        android:theme="@style/MauiShellAppBarLayout">

        <com.google.android.material.appbar.MaterialToolbar
            android:id="@+id/shell_toolbar"
            android:layout_width="match_parent"
            android:layout_height="?attr/actionBarSize"
            app:layout_scrollFlags="noScroll" />

        <com.google.android.material.tabs.TabLayout
            android:id="@+id/shell_tablayout"
            android:layout_width="match_parent"
            android:layout_height="wrap_content"
            android:visibility="gone"
            style="@style/MauiShellTabLayout" />

    </com.google.android.material.appbar.AppBarLayout>

    <androidx.viewpager2.widget.ViewPager2
        android:id="@+id/shell_viewpager"
        android:layout_width="match_parent"
        android:layout_height="match_parent"
        app:layout_behavior="@string/appbar_scrolling_view_behavior" />

</androidx.coordinatorlayout.widget.CoordinatorLayout>
```

**New File: `shellitemlayout.axml`** (for BottomNavigationView)

```xml
<?xml version="1.0" encoding="utf-8"?>
<LinearLayout
    xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:app="http://schemas.android.com/apk/res-auto"
    android:id="@+id/shell_item_container"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    android:orientation="vertical">

    <FrameLayout
        android:id="@+id/shell_navigation_area"
        android:layout_width="match_parent"
        android:layout_height="0dp"
        android:layout_weight="1" />

    <com.google.android.material.bottomnavigation.BottomNavigationView
        android:id="@+id/shell_bottom_navigation"
        android:layout_width="match_parent"
        android:layout_height="wrap_content"
        style="@style/MauiShellBottomNavigationView" />

</LinearLayout>
```

### 7.2 Add Shell-Specific Styles

**Add to `styles.xml`:**

```xml
<!-- Shell AppBar Style -->
<style name="MauiShellAppBarLayout" parent="ThemeOverlay.AppCompat.Dark.ActionBar">
    <item name="android:background">?attr/colorPrimary</item>
</style>

<!-- Shell TabLayout Style -->
<style name="MauiShellTabLayout" parent="Widget.MaterialComponents.TabLayout">
    <item name="tabBackground">?attr/colorPrimary</item>
    <item name="tabTextColor">?attr/colorOnPrimary</item>
    <item name="tabSelectedTextColor">?attr/colorOnPrimary</item>
    <item name="tabIndicatorColor">?attr/colorOnPrimary</item>
</style>

<!-- Shell BottomNavigationView Style -->
<style name="MauiShellBottomNavigationView" parent="Widget.MaterialComponents.BottomNavigationView">
    <item name="backgroundTint">?attr/colorSurface</item>
    <item name="itemIconTint">@color/maui_shell_bottom_nav_icon_color</item>
    <item name="itemTextColor">@color/maui_shell_bottom_nav_text_color</item>
</style>
```

**Add to `styles-material3.xml`:**

```xml
<!-- Material 3 Shell Styles -->
<style name="MauiShellAppBarLayout" parent="ThemeOverlay.Material3.ActionBar">
    <item name="android:background">?attr/colorSurface</item>
</style>

<style name="MauiShellTabLayout" parent="Widget.Material3.TabLayout">
    <item name="tabBackground">?attr/colorSurface</item>
    <item name="tabTextColor">?attr/colorOnSurfaceVariant</item>
    <item name="tabSelectedTextColor">?attr/colorPrimary</item>
    <item name="tabIndicatorColor">?attr/colorPrimary</item>
</style>

<style name="MauiShellBottomNavigationView" parent="Widget.Material3.BottomNavigationView">
    <!-- Material 3 BottomNavigationView inherits correct defaults -->
</style>
```

### 7.3 Update C# Code to Use Layouts

**Modify `ShellSectionRenderer.cs`:**

```csharp
public override AView OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
{
    // INSTEAD OF:
    // var root = PlatformInterop.CreateShellCoordinatorLayout(context);
    // var appbar = PlatformInterop.CreateShellAppBar(context, ...);
    // _tablayout = PlatformInterop.CreateShellTabLayout(context, appbar, actionBarHeight);
    // _viewPager = PlatformInterop.CreateShellViewPager(...);

    // USE XML LAYOUT:
    var root = (CoordinatorLayout)inflater.Inflate(Resource.Layout.shelllayout, container, false);
    
    _appBar = root.FindViewById<AppBarLayout>(Resource.Id.shell_appbar);
    _toolbar = root.FindViewById<MaterialToolbar>(Resource.Id.shell_toolbar);
    _tablayout = root.FindViewById<TabLayout>(Resource.Id.shell_tablayout);
    _viewPager = root.FindViewById<ViewPager2>(Resource.Id.shell_viewpager);
    
    // Continue with setup...
}
```

**Modify `ShellItemRenderer.cs`:**

```csharp
public override AView OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
{
    // INSTEAD OF:
    // _outerLayout = PlatformInterop.CreateNavigationBarOuterLayout(context);
    // _navigationArea = PlatformInterop.CreateNavigationBarArea(context, _outerLayout);
    // _bottomView = PlatformInterop.CreateNavigationBar(context, ...);

    // USE XML LAYOUT:
    _outerLayout = (LinearLayout)inflater.Inflate(Resource.Layout.shellitemlayout, container, false);
    
    _navigationArea = _outerLayout.FindViewById<FrameLayout>(Resource.Id.shell_navigation_area);
    _bottomView = _outerLayout.FindViewById<BottomNavigationView>(Resource.Id.shell_bottom_navigation);
    
    _bottomView.SetOnItemSelectedListener(this);
    
    // Continue with setup...
}
```

### 7.4 Remove Hardcoded Colors from Appearance Trackers

**Modify `ShellToolbarAppearanceTracker.cs`:**

```csharp
public virtual void ResetAppearance(AToolbar toolbar, IShellToolbarTracker toolbarTracker)
{
    // INSTEAD OF hardcoded colors:
    // SetColors(toolbar, toolbarTracker, 
    //     ShellRenderer.DefaultForegroundColor, 
    //     ShellRenderer.DefaultBackgroundColor, 
    //     ShellRenderer.DefaultTitleColor);
    
    // Let the XML style handle defaults - only apply user-set colors
    if (_shellContext?.Shell?.Toolbar is Toolbar shellToolbar)
    {
        // Only apply colors if explicitly set by user
        if (shellToolbar.BarTextColor != null)
            shellToolbar.BarTextColor = shellToolbar.BarTextColor;
        // etc.
    }
}
```

---

## 8. Implementation Roadmap

### Phase 1: Create Layout Files
1. Create `shelllayout.axml` for ShellSection content area
2. Create `shellitemlayout.axml` for ShellItem with BottomNavigationView
3. Add resource IDs to `Resource.designer.cs`

### Phase 2: Add Theme Styles
1. Add Shell-specific styles to `styles.xml` for Material Components
2. Add Shell-specific styles to `styles-material3.xml` for Material 3
3. Create color state lists for icon/text tinting

### Phase 3: Modify Renderers
1. Update `ShellSectionRenderer.cs` to inflate from XML
2. Update `ShellItemRenderer.cs` to inflate from XML
3. Remove programmatic creation from `PlatformInterop.java`

### Phase 4: Update Appearance Trackers
1. Modify trackers to respect XML defaults
2. Only apply colors when user explicitly sets them
3. Remove hardcoded default colors from `ShellRenderer.cs`

### Phase 5: Testing
1. Test with Material Components theme
2. Test with Material 3 theme
3. Test dark mode transitions
4. Test custom user colors still work

---

## 9. Files to Modify

| File | Changes Required |
|------|-----------------|
| `PlatformInterop.java` | Remove Shell-specific creation methods (or mark deprecated) |
| `ShellSectionRenderer.cs` | Use layout inflation instead of programmatic creation |
| `ShellItemRenderer.cs` | Use layout inflation for BottomNavigationView |
| `ShellRenderer.cs` | Remove/deprecate hardcoded color properties |
| `ShellToolbarAppearanceTracker.cs` | Don't apply hardcoded defaults |
| `ShellTabLayoutAppearanceTracker.cs` | Don't apply hardcoded defaults |
| `ShellBottomNavViewAppearanceTracker.cs` | Don't apply hardcoded defaults |
| `styles.xml` | Add Shell-specific styles |
| `styles-material3.xml` | Add Material 3 Shell styles |
| **NEW:** `shelllayout.axml` | Main Shell section layout |
| **NEW:** `shellitemlayout.axml` | Shell item with BottomNavigationView |

---

## 10. Expected Benefits

1. **Theme Consistency**: Shell will use the same theming approach as NavigationPage
2. **Material 3 Support**: Automatic support for Material 3 colors via theme attributes
3. **Dynamic Color Support**: Material You will work automatically
4. **Dark Theme**: Proper dark theme handling via theme system
5. **Maintainability**: No code changes needed for theme updates
6. **Reduced Code**: Remove hardcoded color definitions
7. **Customization**: Users can override styles in their app's `styles.xml`

---

## 11. Appendix: Current File Locations

### Layout Files
- `src/Core/src/Platform/Android/Resources/Layout/navigationlayout.axml` - NavigationPage layout ✅
- `src/Controls/src/Core/Compatibility/Android/Resources/layout/flyoutcontent.axml` - Shell flyout ✅
- `src/Controls/src/Core/Compatibility/Android/Resources/layout/shellcontent.axml` - Shell content wrapper ✅

### Style Files
- `src/Core/src/Platform/Android/Resources/values/styles.xml` - Main styles
- `src/Core/src/Platform/Android/Resources/values/styles-material3.xml` - Material 3 styles

### Shell Android Classes
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellRenderer.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellFlyoutRenderer.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellItemRenderer.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellSectionRenderer.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellToolbarTracker.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellToolbarAppearanceTracker.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellTabLayoutAppearanceTracker.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellBottomNavViewAppearanceTracker.cs`
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellFlyoutTemplatedContentRenderer.cs`

### Java Interop
- `src/Core/AndroidNative/maui/src/main/java/com/microsoft/maui/PlatformInterop.java`

---

## 12. Implementation Status

### ✅ Completed Changes

The following changes have been implemented:

#### Phase 1: Layout Files Created
- [x] `src/Controls/src/Core/Compatibility/Android/Resources/layout/shellsectionlayout.axml` - NEW
- [x] `src/Controls/src/Core/Compatibility/Android/Resources/layout/shellitemlayout.axml` - NEW
- [x] Added to `Controls.Core.csproj` for build inclusion

#### Phase 2: Theme Styles Added
- [x] `src/Core/src/Platform/Android/Resources/values/styles.xml` - Added `MauiShellAppBarLayout`, `MauiShellTabLayout`, `MauiShellBottomNavigationView`
- [x] Styles use theme attributes (`?attr/colorPrimary`, `?attr/colorSurface`) that work automatically for both Material Components AND Material 3

#### Phase 3: Renderers Modified
- [x] `ShellSectionRenderer.cs` - Uses layout inflation from `shellsectionlayout.axml`
- [x] `ShellItemRenderer.cs` - Uses layout inflation from `shellitemlayout.axml`

#### Phase 4: Appearance Trackers Updated
- [x] `ShellToolbarAppearanceTracker.cs` - No longer applies hardcoded defaults, lets XML theme take effect
- [x] `ShellTabLayoutAppearanceTracker.cs` - No longer applies hardcoded defaults
- [x] `ShellBottomNavViewAppearanceTracker.cs` - No longer applies hardcoded defaults, resolves colors from theme

#### Phase 5: Deprecated Hardcoded Colors
- [x] `ShellRenderer.cs` - All `Default*Color` properties marked `[Obsolete]`
- [x] `ShellBottomNavViewAppearanceTracker.cs` - `MakeDefaultColorStateList` now resolves `colorPrimary` from theme instead of hardcoded values
- [x] All appearance trackers no longer reference deprecated defaults

### 🔄 Pending (Testing)
- [ ] Test with Material Components theme
- [ ] Test with Material 3 theme
- [ ] Test dark mode transitions
- [ ] Test custom user colors still work

---

## 13. Conclusion

The current Shell architecture on Android uses programmatic UI creation with hardcoded color values, making it incompatible with the standard Android theming system and causing issues with Material 3 migration. By adopting a layout-based approach similar to NavigationPage, Shell can leverage Android's theme attributes for automatic color resolution, proper Material 3 support, and reduced maintenance burden.

The proposed solution maintains backward compatibility by:
1. Keeping the appearance tracker system for user-set colors
2. Only removing the hardcoded defaults
3. Using XML layouts that can be themed appropriately

This approach aligns Shell with both Android's theming best practices and MAUI's existing NavigationPage implementation.
