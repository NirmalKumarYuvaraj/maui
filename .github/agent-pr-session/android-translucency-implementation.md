# Android NavigationPage Translucency Implementation

**Date:** 2026-01-23 | **Branch:** main | **Type:** Feature Implementation

## ⏳ Status: IN PROGRESS

| Phase | Status | Time Estimate |
|-------|--------|---------------|
| Phase 1: Translucency Detection | ▶️ IN PROGRESS | 15 min |
| Phase 2: Inset Logic (CRITICAL) | ⏳ PENDING | 30 min |
| Phase 3: Elevation Control | ⏳ PENDING | 20 min |
| Phase 4: Handler Trigger | ⏳ PENDING | 10 min |
| Phase 5: Testing & Validation | ⏳ PENDING | 45 min |

**Total Estimated:** 120 minutes (2 hours)

---

## 📋 Implementation Objective

Implement automatic translucency for Android NavigationPage when `BarBackgroundColor.Alpha < 1f` to achieve edge-to-edge content rendering, aligning with iOS PR #30981.

**Key Behavior:**
- **Transparent toolbar** (alpha < 1f) → Content renders **edge-to-edge** (under toolbar)
- **Opaque toolbar** (alpha = 1f) → Content renders **below toolbar** (current behavior preserved)

**Reference:** `.github/instructions/android-navigationpage-translucency.instructions.md`

---

<details>
<summary><strong>📁 Files to Modify</strong></summary>

| Priority | File | Changes | Est. Lines | Status |
|----------|------|---------|------------|--------|
| **HIGH** | `src/Core/src/Handlers/Toolbar/ToolbarHandler.Android.cs` | Add helper method, MapBarBackground, update mapper | +46 | ⏳ PENDING |
| **HIGH** | `src/Core/src/Platform/Android/MauiWindowInsetListener.cs` | Add translucency check, modify inset consumption | +4, ~2 mods | ⏳ PENDING |
| **MEDIUM** | `src/Controls/src/Core/NavigationPage/NavigationPageToolbar.cs` | Add Android-specific handler trigger | +5 | ⏳ PENDING |

**Total:** 3 files, ~55 lines

</details>

---

<details>
<summary><strong>Phase 1: Translucency Detection Helper</strong></summary>

**Status**: ▶️ IN PROGRESS

**File:** `src/Core/src/Handlers/Toolbar/ToolbarHandler.Android.cs`

**Changes:**
- [ ] Add `IsTranslucentToolbar(MaterialToolbar? toolbar)` helper method
  - Check if toolbar.Handler is ToolbarHandler
  - Access VirtualView.BarBackground
  - Return true if SolidColorBrush.Color.Alpha < 1f
  - Handle null cases safely

**Code Location:** Before existing mappers (~line 30)

**Estimated Lines:** +15

**Completion Criteria:**
- [ ] Method compiles without errors
- [ ] Null safety checks in place
- [ ] Returns correct boolean based on alpha

</details>

<details>
<summary><strong>Phase 2: Inset Logic (CRITICAL)</strong></summary>

**Status**: ⏳ PENDING

**File:** `src/Core/src/Platform/Android/MauiWindowInsetListener.cs`

**Changes:**

### Change 2a: Add Translucency Check (Lines 253-265)
- [ ] After `bool appBarHasContent = ...` (line 239)
- [ ] Add toolbar lookup and translucency check
- [ ] Add explanatory comment

**Code:**
```csharp
var toolbar = appBarLayout.FindViewById<MaterialToolbar>(Resource.Id.maui_toolbar);
bool isTranslucent = toolbar != null && ToolbarHandler.IsTranslucentToolbar(toolbar);
```

### Change 2b: Modify Inset Consumption (Lines 279-297)
- [ ] Add `shouldConsumeTopInset` boolean
- [ ] Replace consumption logic for systemBars.Top
- [ ] Replace consumption logic for displayCutout.Top

**Code:**
```csharp
bool shouldConsumeTopInset = appBarHasContent && !isTranslucent;
```

**Critical Logic:**
- Opaque: `shouldConsumeTopInset = true` → Children start below toolbar
- Translucent: `shouldConsumeTopInset = false` → Children extend under toolbar

**Completion Criteria:**
- [ ] isTranslucent variable declared
- [ ] shouldConsumeTopInset logic implemented
- [ ] Both systemBars and displayCutout updated
- [ ] Code compiles without errors

</details>

<details>
<summary><strong>Phase 3: Elevation Control</strong></summary>

**Status**: ⏳ PENDING

**File:** `src/Core/src/Handlers/Toolbar/ToolbarHandler.Android.cs`

**Changes:**

### Change 3a: Add MapBarBackground Method
- [ ] Create static method `MapBarBackground(IToolbarHandler, IToolbar)`
- [ ] Get MaterialToolbar from handler
- [ ] Get parent AppBarLayout
- [ ] Detect translucency from BarBackground
- [ ] Set elevation to 0f when translucent
- [ ] Restore default elevation when opaque

**Estimated Lines:** +30

### Change 3b: Update Mapper
- [ ] Add `[nameof(IToolbar.BarBackground)] = MapBarBackground` to Mapper

**Estimated Lines:** +1

**Completion Criteria:**
- [ ] MapBarBackground method implemented
- [ ] Mapper registration added
- [ ] Elevation control working
- [ ] Code compiles without errors

</details>

<details>
<summary><strong>Phase 4: Handler Trigger</strong></summary>

**Status**: ⏳ PENDING

**File:** `src/Controls/src/Core/NavigationPage/NavigationPageToolbar.cs`

**Changes:**
- [ ] Find ApplyChanges method (~line 254)
- [ ] After BarBackground assignment block (lines 254-259)
- [ ] Add Android-specific UpdateValue trigger

**Code:**
```csharp
#if ANDROID
// Trigger handler update for translucency detection
Handler?.UpdateValue(nameof(IToolbar.BarBackground));
#endif
```

**Estimated Lines:** +5

**Completion Criteria:**
- [ ] Platform-specific block added
- [ ] UpdateValue called correctly
- [ ] Code compiles without errors

</details>

<details>
<summary><strong>Phase 5: Testing & Validation</strong></summary>

**Status**: ⏳ PENDING

### Test Scenarios

#### Test 1: Transparent Background (Alpha = 0)
- [ ] Create Sandbox test with `BarBackgroundColor = Colors.Transparent`
- [ ] Run: `pwsh .github/scripts/BuildAndRunSandbox.ps1 -Platform android`
- [ ] **Expected:** Content visible edge-to-edge, under toolbar area
- [ ] **Result:** [PENDING]

#### Test 2: Semi-Transparent Background (Alpha = 0.5)
- [ ] Set `BarBackgroundColor = Color.FromRgba(255, 0, 0, 128)`
- [ ] Run on Android
- [ ] **Expected:** Content visible under toolbar, red translucent overlay
- [ ] **Result:** [PENDING]

#### Test 3: Opaque Background (Alpha = 1.0)
- [ ] Set `BarBackgroundColor = Colors.Red`
- [ ] Run on Android
- [ ] **Expected:** Content starts below toolbar (current behavior preserved)
- [ ] **Result:** [PENDING]

#### Test 4: Elevation Visual Check
- [ ] Verify transparent toolbar has no shadow (elevation = 0)
- [ ] Verify opaque toolbar has Material Design shadow
- [ ] **Result:** [PENDING]

### Verification Checklist
- [ ] Transparent toolbar: Content renders edge-to-edge ✅
- [ ] Semi-transparent toolbar: Content visible through overlay ✅
- [ ] Opaque toolbar: Content below toolbar (existing behavior) ✅
- [ ] Elevation: Removed when translucent, present when opaque ✅
- [ ] Status bar: Toolbar respects status bar insets ✅
- [ ] No build errors or warnings
- [ ] No visual glitches during navigation transitions

</details>

---

## 📝 Progress Notes

### Session Started: 2026-01-23T04:16:54.786Z

**Next Step:** Phase 1 - Add IsTranslucentToolbar helper method to ToolbarHandler.Android.cs

