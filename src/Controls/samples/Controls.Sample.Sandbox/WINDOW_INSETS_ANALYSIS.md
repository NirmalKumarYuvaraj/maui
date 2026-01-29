# Window Insets Animation Architecture Analysis

## Overview

This document analyzes the window insets animation approach used in the Android platform sample and provides recommendations for implementing similar functionality in a CoordinatorLayout-based navigation layout.

---

## Sample Architecture

The sample uses a **layered callback approach** with three main components working together to create smooth IME (keyboard) animations.

### Component Hierarchy

```
Root View (InsetsAnimationLinearLayout)
├── RootViewDeferringInsetsCallback (handles insets + animation)
│
├── Toolbar
│
├── RecyclerView
│   └── TranslateDeferringInsetsAnimationCallback (translates during animation)
│
└── Message Holder (EditText container)
    ├── TranslateDeferringInsetsAnimationCallback (translates during animation)
    │
    └── EditText
        └── ControlFocusInsetsAnimationCallback (manages focus)
```

---

## Core Components

### 1. RootViewDeferringInsetsCallback

**Location**: `RootViewDeferringInsetsCallback.kt`

**Purpose**: Acts as both `WindowInsetsAnimationCompat.Callback` and `OnApplyWindowInsetsListener` on the root view.

**Key Behavior**:

- Defers IME insets during animations to prevent layout "jumping"
- Applies system bar insets immediately as padding
- Delays IME insets until animation completes

```kotlin
class RootViewDeferringInsetsCallback(
    val persistentInsetTypes: Int,  // e.g., systemBars()
    val deferredInsetTypes: Int     // e.g., ime()
) : WindowInsetsAnimationCompat.Callback(DISPATCH_MODE_CONTINUE_ON_SUBTREE),
    OnApplyWindowInsetsListener {

    private var deferredInsets = false

    override fun onApplyWindowInsets(v: View, windowInsets: WindowInsetsCompat): WindowInsetsCompat {
        val types = when {
            deferredInsets -> persistentInsetTypes  // Only system bars during animation
            else -> persistentInsetTypes or deferredInsetTypes  // Both when not animating
        }
        val typeInsets = windowInsets.getInsets(types)
        v.setPadding(typeInsets.left, typeInsets.top, typeInsets.right, typeInsets.bottom)
        return WindowInsetsCompat.CONSUMED
    }

    override fun onPrepare(animation: WindowInsetsAnimationCompat) {
        if (animation.typeMask and deferredInsetTypes != 0) {
            deferredInsets = true  // Start deferring
        }
    }

    override fun onEnd(animation: WindowInsetsAnimationCompat) {
        if (deferredInsets && (animation.typeMask and deferredInsetTypes) != 0) {
            deferredInsets = false  // Stop deferring, re-apply insets
            // Trigger re-application of insets
        }
    }
}
```

### 2. TranslateDeferringInsetsAnimationCallback

**Location**: `TranslateDeferringInsetsAnimationCallback.kt`

**Purpose**: Smoothly translates views during IME animation by calculating the difference between deferred and persistent insets.

**Key Behavior**:

- Calculates difference between IME insets and system bar insets
- Applies `translationX` and `translationY` during animation progress
- Resets translation to 0 when animation ends

```kotlin
class TranslateDeferringInsetsAnimationCallback(
    private val view: View,
    val persistentInsetTypes: Int,
    val deferredInsetTypes: Int,
    dispatchMode: Int = DISPATCH_MODE_STOP
) : WindowInsetsAnimationCompat.Callback(dispatchMode) {

    override fun onProgress(
        insets: WindowInsetsCompat,
        runningAnimations: List<WindowInsetsAnimationCompat>
    ): WindowInsetsCompat {
        val typesInset = insets.getInsets(deferredInsetTypes)
        val otherInset = insets.getInsets(persistentInsetTypes)

        val diff = Insets.subtract(typesInset, otherInset).let {
            Insets.max(it, Insets.NONE)
        }

        view.translationX = (diff.left - diff.right).toFloat()
        view.translationY = (diff.top - diff.bottom).toFloat()

        return insets
    }

    override fun onEnd(animation: WindowInsetsAnimationCompat) {
        view.translationX = 0f
        view.translationY = 0f
    }
}
```

### 3. ControlFocusInsetsAnimationCallback

**Location**: `ControlFocusInsetsAnimationCallback.kt`

**Purpose**: Manages focus state based on IME visibility.

**Key Behavior**:

- Requests focus on the view when IME appears
- Clears focus when IME disappears

```kotlin
class ControlFocusInsetsAnimationCallback(
    private val view: View,
    dispatchMode: Int = DISPATCH_MODE_STOP
) : WindowInsetsAnimationCompat.Callback(dispatchMode) {

    override fun onEnd(animation: WindowInsetsAnimationCompat) {
        if (animation.typeMask and WindowInsetsCompat.Type.ime() != 0) {
            view.post { checkFocus() }
        }
    }

    private fun checkFocus() {
        val imeVisible = ViewCompat.getRootWindowInsets(view)
            ?.isVisible(WindowInsetsCompat.Type.ime()) == true

        if (imeVisible && view.rootView.findFocus() == null) {
            view.requestFocus()
        } else if (!imeVisible && view.isFocused) {
            view.clearFocus()
        }
    }
}
```

---

## Dispatch Modes

Understanding dispatch modes is critical for proper callback chaining:

| Mode                                | Behavior                                           |
| ----------------------------------- | -------------------------------------------------- |
| `DISPATCH_MODE_STOP`                | Callbacks on child views are NOT called            |
| `DISPATCH_MODE_CONTINUE_ON_SUBTREE` | Callbacks continue to be dispatched to child views |

**Usage Pattern**:

- Root callbacks: Use `DISPATCH_MODE_CONTINUE_ON_SUBTREE`
- Leaf/child callbacks: Use `DISPATCH_MODE_STOP` (default)

---

## Recommended Implementation for CoordinatorLayout

### Target Layout Structure

```xml
<CoordinatorLayout>
    <AppBarLayout>
        <FragmentContainerView id="toptabs" />
    </AppBarLayout>

    <FragmentContainerView
        id="content"
        app:layout_behavior="@string/appbar_scrolling_view_behavior" />

    <FragmentContainerView
        id="bottomtabs"
        android:layout_gravity="bottom" />
</CoordinatorLayout>
```

### Implementation

```kotlin
class NavigationInsetsHandler(
    private val coordinatorLayout: CoordinatorLayout,
    private val appBarLayout: View,
    private val contentView: View,
    private val bottomTabsView: View
) {

    fun setup() {
        // Step 1: Root deferring callback
        val deferringInsetsCallback = RootViewDeferringInsetsCallback(
            persistentInsetTypes = WindowInsetsCompat.Type.systemBars(),
            deferredInsetTypes = WindowInsetsCompat.Type.ime()
        )

        ViewCompat.setWindowInsetsAnimationCallback(coordinatorLayout, deferringInsetsCallback)
        ViewCompat.setOnApplyWindowInsetsListener(coordinatorLayout, deferringInsetsCallback)

        // Step 2: AppBarLayout - top system bar insets
        ViewCompat.setOnApplyWindowInsetsListener(appBarLayout) { view, insets ->
            val systemBars = insets.getInsets(WindowInsetsCompat.Type.systemBars())
            view.updatePadding(top = systemBars.top)
            insets
        }

        // Step 3: Bottom tabs - translate with IME
        ViewCompat.setWindowInsetsAnimationCallback(
            bottomTabsView,
            TranslateDeferringInsetsAnimationCallback(
                view = bottomTabsView,
                persistentInsetTypes = WindowInsetsCompat.Type.systemBars(),
                deferredInsetTypes = WindowInsetsCompat.Type.ime(),
                dispatchMode = WindowInsetsAnimationCompat.Callback.DISPATCH_MODE_CONTINUE_ON_SUBTREE
            )
        )

        ViewCompat.setOnApplyWindowInsetsListener(bottomTabsView) { view, insets ->
            val systemBars = insets.getInsets(WindowInsetsCompat.Type.systemBars())
            view.updatePadding(bottom = systemBars.bottom)
            insets
        }

        // Step 4: Content view - react to IME
        ViewCompat.setWindowInsetsAnimationCallback(
            contentView,
            TranslateDeferringInsetsAnimationCallback(
                view = contentView,
                persistentInsetTypes = WindowInsetsCompat.Type.systemBars(),
                deferredInsetTypes = WindowInsetsCompat.Type.ime()
            )
        )
    }
}
```

### Activity Setup

```kotlin
class NavigationActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        // CRITICAL: Enable edge-to-edge BEFORE setContentView
        WindowCompat.setDecorFitsSystemWindows(window, false)

        super.onCreate(savedInstanceState)
        setContentView(R.layout.navigation_layout)

        NavigationInsetsHandler(
            coordinatorLayout = findViewById(R.id.navigation_layout),
            appBarLayout = findViewById(R.id.navigationlayout_appbar),
            contentView = findViewById(R.id.navigationlayout_content),
            bottomTabsView = findViewById(R.id.navigationlayout_bottomtabs)
        ).setup()
    }
}
```

---

## Alternative Approaches

### Option A: Hide Bottom Tabs When IME Appears

```kotlin
ViewCompat.setOnApplyWindowInsetsListener(bottomTabsView) { view, insets ->
    val imeVisible = insets.isVisible(WindowInsetsCompat.Type.ime())
    view.visibility = if (imeVisible) View.GONE else View.VISIBLE

    val systemBars = insets.getInsets(WindowInsetsCompat.Type.systemBars())
    view.updatePadding(bottom = systemBars.bottom)
    insets
}
```

### Option B: Animate Bottom Tabs Visibility

```kotlin
ViewCompat.setWindowInsetsAnimationCallback(
    bottomTabsView,
    object : WindowInsetsAnimationCompat.Callback(DISPATCH_MODE_STOP) {
        override fun onProgress(
            insets: WindowInsetsCompat,
            runningAnimations: List<WindowInsetsAnimationCompat>
        ): WindowInsetsCompat {
            val imeAnimation = runningAnimations.find {
                it.typeMask and WindowInsetsCompat.Type.ime() != 0
            }
            imeAnimation?.let {
                // Fade out as IME comes in
                bottomTabsView.alpha = 1f - it.interpolatedFraction
            }
            return insets
        }

        override fun onEnd(animation: WindowInsetsAnimationCompat) {
            val imeVisible = ViewCompat.getRootWindowInsets(bottomTabsView)
                ?.isVisible(WindowInsetsCompat.Type.ime()) == true
            bottomTabsView.alpha = if (imeVisible) 0f else 1f
        }
    }
)
```

### Option C: Jetpack Compose Interop

If using Compose within fragments:

```kotlin
@Composable
fun ContentScreen() {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .systemBarsPadding()  // Handles system bars
            .imePadding()          // Handles IME automatically
    ) {
        // Content
    }
}
```

---

## Comparison: Sample vs CoordinatorLayout

| Aspect                  | Sample Approach                   | CoordinatorLayout Needs                        |
| ----------------------- | --------------------------------- | ---------------------------------------------- |
| **Root Container**      | LinearLayout                      | CoordinatorLayout (respects `layout_behavior`) |
| **Top Area**            | Simple Toolbar                    | AppBarLayout with collapsing behavior          |
| **Bottom Area**         | EditText holder (translates)      | Bottom tabs (translate or hide)                |
| **Content**             | RecyclerView                      | FragmentContainerView with scroll behavior     |
| **Scroll Coordination** | Manual via NestedScrollingParent3 | Built-in via CoordinatorLayout behaviors       |

---

## Key Considerations

### 1. Edge-to-Edge Setup

```kotlin
// Must be called BEFORE setContentView
WindowCompat.setDecorFitsSystemWindows(window, false)
```

### 2. Inset Types Reference

```kotlin
WindowInsetsCompat.Type.systemBars()      // Status + Navigation bars
WindowInsetsCompat.Type.statusBars()      // Status bar only
WindowInsetsCompat.Type.navigationBars()  // Navigation bar only
WindowInsetsCompat.Type.ime()             // Keyboard
WindowInsetsCompat.Type.displayCutout()   // Notch/cutout areas
WindowInsetsCompat.Type.systemGestures()  // System gesture areas
```

### 3. Callback Order

1. `onPrepare()` - Before animation starts
2. `onStart()` - Animation is starting
3. `onProgress()` - Called repeatedly during animation
4. `onEnd()` - Animation has finished

### 4. Thread Safety

- All callbacks run on the main thread
- Use `view.post {}` for operations that depend on updated insets state

---

## Troubleshooting

| Issue                                | Solution                                                            |
| ------------------------------------ | ------------------------------------------------------------------- |
| Views don't animate                  | Ensure `setDecorFitsSystemWindows(false)` is called                 |
| Child callbacks not triggered        | Use `DISPATCH_MODE_CONTINUE_ON_SUBTREE` on parent                   |
| Layout jumps at animation end        | Ensure `onEnd()` resets translation to 0                            |
| Insets not applied                   | Check callback return value (don't consume if children need insets) |
| CoordinatorLayout behavior conflicts | Apply translation instead of padding changes                        |

---

## References

- [WindowInsetsAnimation documentation](https://developer.android.com/reference/android/view/WindowInsetsAnimation)
- [WindowInsetsAnimationCompat (AndroidX)](https://developer.android.com/reference/androidx/core/view/WindowInsetsAnimationCompat)
- [Edge-to-edge guide](https://developer.android.com/develop/ui/views/layout/edge-to-edge)
- [IME animations guide](https://developer.android.com/develop/ui/views/layout/sw-keyboard)
