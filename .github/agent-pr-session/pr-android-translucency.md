# PR Review: Android NavigationPage Translucency Implementation

**Date:** 2026-01-23 | **Issue:** Internal (from conversation) | **PR:** None (new implementation)

## ⏳ Status: IN PROGRESS

| Phase | Status |
|-------|--------|
| Pre-Flight | ✅ COMPLETE |
| 🧪 Tests | ✅ COMPLETE |
| 🚦 Gate | ▶️ IN PROGRESS |
| 🔧 Fix | ⏳ PENDING |
| 📋 Report | ⏳ PENDING |

---

<details>
<summary><strong>📋 Issue Summary</strong></summary>

**Description:**
Implement automatic translucency for Android NavigationPage when `BarBackgroundColor.Alpha < 1f` to achieve edge-to-edge content rendering, aligning with iOS PR #30981 behavior.

**Current Behavior:**
When `NavigationPage.BarBackgroundColor` is set to a transparent or semi-transparent color on Android:
- Content renders **below** the toolbar (opaque behavior)
- AppBarLayout consumes top window insets
- No edge-to-edge rendering

**Expected Behavior:**
When `BarBackgroundColor.Alpha < 1f`:
- Content should render **edge-to-edge** (under the transparent toolbar)
- AppBarLayout keeps status bar padding but doesn't consume top insets
- AppBarLayout elevation set to 0f (flat appearance)

When `BarBackgroundColor.Alpha == 1f` (opaque):
- Current behavior maintained (content below toolbar)

**Platforms Affected:**
- [ ] iOS
- [x] Android
- [ ] Windows
- [ ] MacCatalyst

**Is Regression:** No (new feature parity with iOS)

</details>

<details>
<summary><strong>📁 Files to Modify</strong></summary>

Based on instruction file `.github/instructions/android-navigationpage-translucency.instructions.md`:

| File | Type | Actual Changes |
|------|------|----------------|
| `src/Controls/src/Core/NavigationPage/NavigationPageToolbar.cs` | Fix | +11 lines - Call UpdateToolbarTranslucency |
| `src/Controls/src/Core/Platform/Android/Extensions/ToolbarExtensions.cs` | Fix | +43 lines - Tag management, elevation control |
| `src/Core/src/Platform/Android/MauiWindowInsetListener.cs` | Fix | +25, -2 lines - Read tag, modify inset consumption |

**Total:** 3 files, +77 lines

**Architecture:** Uses Android View tags to communicate translucency state from Controls layer to Core layer. Respects Core/Controls separation.

</details>

<details>
<summary><strong>💬 Discussion Summary</strong></summary>

**Key Context:**
- User provided iOS PR #30981 as reference for expected behavior
- Stack Overflow reference: https://stackoverflow.com/questions/26558705/material-design-transparent-actionbar confirms use of `Elevation` property
- Detailed instruction file created: `.github/instructions/android-navigationpage-translucency.instructions.md`

**Initial Implementation Attempt:**
- Encountered compilation errors due to architectural constraints
- Core project cannot reference Controls namespace (Controls.Toolbar, SolidColorBrush)
- Identified that detection logic must live in Controls layer, Core reads a flag

**Architectural Constraints:**
- Core project cannot reference Controls namespace
- Detection logic must live in Controls layer (`NavigationPageToolbar.cs`)
- Core layer reads a flag/tag set by Controls
- Use `Elevation` property (not `SetElevation()`)
- No `SetStateListAnimator()` - Android API difference

**Author Uncertainty:**
- Best approach for flag/tag mechanism between Core and Controls layers
- Whether to use toolbar tags or alternative communication method

</details>

<details>
<summary><strong>🧪 Tests</strong></summary>

**Status**: ✅ COMPLETE

Test scenarios defined in instruction file:
1. **Transparent** (`Colors.Transparent`) → Edge-to-edge rendering
2. **Semi-transparent** (`Color.FromRgba(255, 0, 0, 128)`) → Content visible under toolbar
3. **Opaque** (`Colors.Red`) → Content below toolbar (current behavior preserved)

- [x] Tests exist (Issue17022 extended to Android)
- [x] Tests compile successfully
- [ ] Tests verified to reproduce issue (pending Gate phase)

**Test Files:**
- HostApp: `src/Controls/tests/TestCases.HostApp/Issues/Issue17022.xaml.cs` (updated to include Android)
- NUnit: `src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue17022.cs` (updated to include Android)

**Test Cases:**
- `NewNavigationPageGridTransparentButton` - Tests transparent toolbar → expects boxview at top (edge-to-edge)
- `NewNavigationPageGridTransparentTranslucentButton` - Tests transparent + translucent → expects boxview at top
- Multiple variants for NavigationPage and FlyoutPage with different transparency configurations

</details>

<details>
<summary><strong>🚦 Gate - Test Verification</strong></summary>

**Status**: ⏳ PENDING

- [ ] Tests FAIL without fix (bug reproduced)
- [ ] Tests PASS with fix (fix validated)

**Result:** [PENDING]

</details>

<details>
<summary><strong>🔧 Fix Candidates</strong></summary>

**Status**: ⏳ PENDING

| # | Source | Approach | Test Result | Files Changed | Notes |
|---|--------|----------|-------------|---------------|-------|
| - | Instruction File | Use toolbar tag mechanism for translucency flag | ⏳ PENDING | 3 files | Documented approach |

**Exhausted:** No
**Selected Fix:** [PENDING]

</details>

---

**Next Step:** Complete Pre-Flight, then create tests in Phase 2.

