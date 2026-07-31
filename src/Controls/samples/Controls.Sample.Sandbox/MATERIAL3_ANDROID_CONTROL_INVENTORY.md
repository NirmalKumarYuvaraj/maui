# Android Material 3 Control Migration Inventory

## Purpose

This inventory is the Phase 1 baseline for the Android Material 3 migration.
It records the strategy, ownership, compatibility expectations and available
test evidence for every Android control registered by MAUI Controls.

The inventory describes the current implementation. It does not imply that a
control is complete merely because it uses a Material 3 native component.

## Status

- Baseline date: July 31, 2026
- Material 3 default: Disabled
- Activation property: `UseMaterial3`
- Runtime switch: `Microsoft.Maui.RuntimeFeature.IsMaterial3Enabled`
- Phase 1 status: Complete
- Phase 2 status: Complete
- Phase 3 status: Complete
- Phase 4 status: Complete - awaiting review
- Next phase: Migrate controls in controlled batches

## Ownership model

Named individual owners have not been assigned for this prototype. The
following provisional code-area owners are used so that no control is left
unowned:

| Owner | Responsibility |
| --- | --- |
| Core Android Handlers | Native controls, handlers, property mappings and lifecycle |
| Controls Android | Controls-specific mapper remapping and behavior |
| Controls Android Navigation | Shell, navigation, tabs, flyout and compatibility renderers |
| Controls Test Infrastructure | UI tests, device tests and Android screenshot baselines |

Named owners must replace these provisional owners before a control enters an
implementation batch.

`Controls Test Infrastructure` owns the test-status evidence for every
inventory row and is omitted from individual owner cells to avoid repetition.

## Classification and test status

| Value | Meaning |
| --- | --- |
| Replacement handler | Material 3 selects a separate `Handler2` implementation |
| Shared branch | Existing handler or platform helper branches at runtime |
| Theme/shared | No dedicated Material 3 handler; behavior may inherit the application theme |
| Legacy/compatibility | Compatibility renderer or legacy control path |
| Not migrated | No Material 3-specific behavior was identified |
| Dedicated M3 UI | Material 3 feature tests and Android screenshot baselines exist |
| Generic coverage | Existing tests exist, but no dedicated Material 3 suite was identified |
| Coverage gap | No relevant dedicated evidence was identified during the baseline |

## Replacement-handler inventory

| Control | Legacy implementation | Material 3 implementation | Property/state coverage | Test status | Known risks or gaps | Provisional owner |
| --- | --- | --- | --- | --- | --- | --- |
| Label | `LabelHandler` / AppCompat text view | `LabelHandler2` / `MauiMaterialTextView` based on `MaterialTextView` | Text, formatted text, font, color, alignment and line behavior are mapped; disabled state and set-then-clear theme restoration now have explicit coverage | Dedicated M3 UI | Public customization surface remains internal; typography is not backed by a MAUI token contract | Core Android Handlers |
| Editor | `EditorHandler` | `EditorHandler2` / `MauiMaterialEditText` | Text, placeholder, font, color, alignment, keyboard, focus and selection mappings exist | Dedicated M3 UI | `TextInputEditText` hierarchy can change measurement, placeholder and focus behavior | Core Android Handlers and Controls Android |
| Entry | `EntryHandler` | `EntryHandler2` / `MauiMaterialTextInputLayout` with `MauiMaterialEditText` | Text, placeholder, password, font, colors, alignment, keyboard, clear button, focus and selection mappings exist | Dedicated M3 UI | Native hierarchy differs from legacy; placeholder flow direction and character-spacing limitations are documented in tests | Core Android Handlers and Controls Android |
| Picker | `PickerHandler` | `PickerHandler2` / `MauiMaterialPicker` | Title, title color, selected item, font, text color, alignment and dialog mappings exist | Dedicated M3 UI | Dialog styling, selection synchronization and clearing values need lifecycle coverage | Core Android Handlers |
| DatePicker | `DatePickerHandler` | `DatePickerHandler2` / `MauiMaterialDatePicker` and Material date dialog | Date, min/max constraints, format, text color, font and focus mappings exist | Dedicated M3 UI | Material calendar constraints are immutable; constraint and recreation behavior require regression coverage | Core Android Handlers |
| TimePicker | `TimePickerHandler` | `TimePickerHandler2` / `MauiMaterialTimePicker` and Material time dialog | Time, format, text color, font, focus and dialog mappings exist | Dedicated M3 UI | Dialog mode, localization and native-to-virtual synchronization require broader evidence | Core Android Handlers |
| SearchBar | `SearchBarHandler` / search view | `SearchBarHandler2` / `TextInputLayout` with `TextInputEditText` | Text, placeholder, font, colors, alignment, clear action, keyboard, focus and search command mappings exist | Dedicated M3 UI | Material 3 emulates SearchBar semantics with a different native hierarchy; old search-view behavior must remain compatible | Core Android Handlers and Controls Android |
| RadioButton | `RadioButtonHandler` | `RadioButtonHandler2` / `MaterialRadioButton` | Checked state, content, color and enabled-state mappings exist | Dedicated M3 UI | State tint and reset-to-theme behavior need semantic-theme tests | Core Android Handlers |
| Switch | `SwitchHandler` | `SwitchHandler2` / `MaterialSwitch` | On state, thumb and track mappings exist | Dedicated M3 UI | Native Material 3 geometry and state colors differ; explicit color precedence must remain stable | Core Android Handlers |
| Slider | `SliderHandler` | `SliderHandler2` / Material `Slider` | Min, max, value, progress colors, enabled state and touch synchronization are mapped | Dedicated M3 UI | Listener implementation contains transitional work; event cleanup and drag behavior need device coverage | Core Android Handlers |
| ProgressBar | `ProgressBarHandler` | `ProgressBarHandler2` / `LinearProgressIndicator` | Progress, indicator-color mapping and explicit-color clearing on the existing handler now have dedicated coverage | Dedicated M3 UI | Runtime theme changes still require broader semantic-theme coverage | Core Android Handlers |
| ActivityIndicator | `ActivityIndicatorHandler` | `ActivityIndicatorHandler2` / `MaterialActivityIndicator` | Running and disabled states now have dedicated Material 3 coverage; color and visibility use the shared handler contract | Dedicated M3 UI | Size and indicator-style differences still require broader baselines | Core Android Handlers |
| Image | `ImageHandler` | `ImageHandler2` / Material shape-aware image view | Source, aspect, tint, loading, enabled state, measurement and replacement-instance bindings now have dedicated coverage | Dedicated M3 UI | Shape behavior and explicit background interactions require compatibility coverage | Core Android Handlers |

Primary registration source:
`src/Controls/src/Core/Hosting/AppHostBuilderExtensions.cs`.

## Shared-handler and runtime-branch inventory

| Control | Strategy | Current Material 3 behavior | Test status | Known risks or gaps | Provisional owner |
| --- | --- | --- | --- | --- | --- |
| Button | Shared branch and theme inheritance | `ButtonHandler` continues to create `MauiMaterialButton`; the context wrapper selects Material 3, XML selects `Widget.Material3.Button`, and ripple defaults branch for Material 3 | Dedicated M3 UI | MAUI removes native minimum size, padding and insets; explicit text/background values must restore native theme states when cleared | Core Android Handlers |
| ImageButton | Shared branch | `ImageButtonHandler` selects `MaterialShapeableImageView` when Material 3 is enabled and the legacy view otherwise | Dedicated M3 UI | The split is explicitly transitional; shape, clipping and measurement must converge before public customization is stabilized | Core Android Handlers |
| CheckBox | Shared branch | `CheckBoxHandler` creates a Material checkbox through the themed context; color reset uses the original theme `buttonTint` under Material 3 | Dedicated M3 UI plus issue coverage | Theme-tint capture and reset behavior must survive theme changes and handler recreation | Core Android Handlers and Controls Android |
| ScrollView | Shared platform branch | `MauiScrollView` changes Material 3-specific drawing and scrollbar-related behavior while registration remains unchanged | Generic coverage | Runtime checks inside the platform view make the migration boundary less visible; scrolling behavior must remain identical in legacy mode | Core Android Handlers |
| CollectionView | Shared platform branch | Android continues to use `CollectionViewHandler`; `MauiRecyclerView` manages the Material 3 AppBar lift target during attach, detach and visibility changes | Generic coverage; dedicated M3 gap | AppBar integration adds lifecycle state that must be cleaned up during recycling, navigation and handler teardown | Controls Android |
| CarouselView | Shared platform branch | Android continues to use `CarouselViewHandler` on the same `MauiRecyclerView` foundation and inherits the Material 3 AppBar lift-target behavior | Generic coverage; dedicated M3 gap | Carousel paging, recycling and visibility changes need Material 3-specific AppBar lifecycle coverage | Controls Android |

Relevant source:

- `src/Core/src/Handlers/Button/ButtonHandler.Android.cs`
- `src/Core/src/Handlers/ImageButton/ImageButtonHandler.Android.cs`
- `src/Core/src/Handlers/CheckBox/CheckBoxHandler.Android.cs`
- `src/Core/src/Platform/Android/CheckBoxExtensions.cs`
- `src/Core/src/Platform/Android/MauiScrollView.cs`
- `src/Core/src/Platform/Android/MauiRippleDrawableExtensions.cs`
- `src/Controls/src/Core/Handlers/Items/Android/MauiRecyclerView.cs`

## Theme/shared control inventory

These controls use the same registered handler in both modes. No dedicated
Material 3 handler replacement was identified.

| Control | Handler | Migration status | Test status | Material 3 action required | Provisional owner |
| --- | --- | --- | --- | --- | --- |
| Application | `ApplicationHandler` | Theme/shared | Generic coverage | Confirm application theme activation and resource precedence | Core Android Handlers |
| BoxView | `BoxViewHandler` | Theme/shared | Generic coverage | Confirm explicit colors and geometry remain theme-independent | Core Android Handlers |
| GraphicsView | `GraphicsViewHandler` | Theme/shared | Generic coverage | No Material widget migration expected; document as theme-neutral | Core Android Handlers |
| Layout | `LayoutHandler` | Theme/shared | Generic coverage | Confirm child measurement is unchanged between modes | Core Android Handlers |
| Page | `PageHandler` | Theme/shared | Generic coverage | Confirm page background resolves through application resources | Core Android Handlers |
| WebView | `WebViewHandler` | Theme/shared | Generic coverage | Document native content as outside Material component styling | Core Android Handlers |
| Border | `BorderHandler` | Theme/shared | Generic coverage | Confirm shape and stroke remain controlled by MAUI properties | Core Android Handlers |
| IContentView | `ContentViewHandler` | Theme/shared | Generic coverage | No dedicated Material component migration expected | Core Android Handlers |
| ContentView | `ContentViewHandler` | Theme/shared | Generic coverage | No dedicated Material component migration expected | Core Android Handlers |
| Ellipse | `ShapeViewHandler` | Theme/shared | Generic coverage | Document as MAUI-drawn shape rather than Material component | Core Android Handlers |
| Line | `LineHandler` | Theme/shared | Generic coverage | Document as MAUI-drawn shape rather than Material component | Core Android Handlers |
| Path | `PathHandler` | Theme/shared | Generic coverage | Document as MAUI-drawn shape rather than Material component | Core Android Handlers |
| Polygon | `PolygonHandler` | Theme/shared | Generic coverage | Document as MAUI-drawn shape rather than Material component | Core Android Handlers |
| Polyline | `PolylineHandler` | Theme/shared | Generic coverage | Document as MAUI-drawn shape rather than Material component | Core Android Handlers |
| Rectangle | `RectangleHandler` | Theme/shared | Generic coverage | Document as MAUI-drawn shape rather than Material component | Core Android Handlers |
| RoundRectangle | `RoundRectangleHandler` | Theme/shared | Generic coverage | Document as MAUI-drawn shape rather than Material component | Core Android Handlers |
| Window | `WindowHandler` | Theme/shared | Generic coverage | Confirm light/dark, system bars and edge-to-edge behavior | Core Android Handlers |
| IndicatorView | `IndicatorViewHandler` | Theme/shared | Generic coverage | Decide whether Material 3 page-indicator styling is in scope | Controls Android |
| RefreshView | `RefreshViewHandler` | Theme/shared | Generic coverage | Confirm progress indicator styling and explicit colors | Controls Android |
| SwipeItem | `SwipeItemMenuItemHandler` | Theme/shared | Generic coverage | Confirm action colors and touch states | Controls Android |
| SwipeView | `SwipeViewHandler` | Theme/shared | Generic coverage | Confirm reveal/action behavior and colors | Controls Android |
| SwipeItemView | `SwipeItemViewHandler` | Theme/shared | Generic coverage | Confirm custom swipe content remains unaffected | Controls Android |
| MenuBar | `MenuBarHandler` | Theme/shared | Generic coverage | Confirm menu surfaces inherit semantic theme values | Controls Android |
| MenuFlyoutSubItem | `MenuFlyoutSubItemHandler` | Theme/shared | Generic coverage | Confirm menu hierarchy and state colors | Controls Android |
| MenuFlyoutSeparator | `MenuFlyoutSeparatorHandler` | Theme/shared | Generic coverage | Confirm separator color and spacing | Controls Android |
| MenuFlyoutItem | `MenuFlyoutItemHandler` | Theme/shared | Generic coverage | Confirm enabled, disabled and selected states | Controls Android |
| MenuBarItem | `MenuBarItemHandler` | Theme/shared | Generic coverage | Confirm menu icon and text tint | Controls Android |

## Not-migrated inventory

| Control | Handler | Current status | Test status | Required decision | Provisional owner |
| --- | --- | --- | --- | --- | --- |
| Stepper | `StepperHandler` | No Material 3-specific behavior identified | Generic feature coverage | Determine whether a Material 3 component mapping exists or document the control as MAUI-specific | Core Android Handlers |
| HybridWebView | `HybridWebViewHandler` | Material 3 not applicable to hosted web content; only application chrome can inherit the theme | Generic coverage | Document the boundary between native chrome and web content | Core Android Handlers |

## Legacy and compatibility inventory

| Control | Android implementation | Current Material 3 status | Test status | Required migration decision | Provisional owner |
| --- | --- | --- | --- | --- | --- |
| ListView | Compatibility `ListViewRenderer` | Legacy/compatibility | Generic coverage | Preserve compatibility or explicitly exclude from new component work | Controls Android Navigation |
| Cell | Compatibility `CellRenderer` | Legacy/compatibility | Generic coverage | Define theme inheritance for cell surfaces and states | Controls Android Navigation |
| ImageCell | Compatibility `ImageCellRenderer` | Legacy/compatibility | Generic coverage | Define image, text and selection-state expectations | Controls Android Navigation |
| EntryCell | Compatibility `EntryCellRenderer` | Legacy/compatibility | Generic coverage | Verify embedded input remains functional under the Material 3 app theme | Controls Android Navigation |
| TextCell | Compatibility `TextCellRenderer` | Legacy/compatibility | Generic coverage | Define text and selection-state expectations | Controls Android Navigation |
| ViewCell | Compatibility `ViewCellRenderer` | Legacy/compatibility | Generic coverage | Verify custom content remains unaffected | Controls Android Navigation |
| SwitchCell | Compatibility `SwitchCellRenderer` | Legacy/compatibility | Generic coverage | Decide whether the embedded switch adopts Material 3 state styling | Controls Android Navigation |
| TableView | Compatibility `TableViewRenderer` | Legacy/compatibility | Generic coverage | Preserve behavior and define inherited theme expectations | Controls Android Navigation |
| Frame | Compatibility `FrameRenderer` | Legacy/compatibility | Generic coverage | Preserve legacy shadow, corner and background behavior | Controls Android Navigation |
| Shell | Compatibility `ShellRenderer` | Shared runtime branching across renderer and appearance trackers | Generic coverage; no consolidated M3 suite identified | Replace hard-coded colors with semantic roles and define navigation-state coverage | Controls Android Navigation |
| NavigationPage | `NavigationViewHandler` | Shared navigation handler | Generic coverage | Define Material 3 toolbar, system-bar and transition scope | Controls Android Navigation |
| Toolbar | `ToolbarHandler` | Shared navigation handler | Generic coverage | Define typography, icon tint, action mode and overflow behavior | Controls Android Navigation |
| FlyoutPage | `FlyoutViewHandler` | Shared navigation handler | Generic coverage | Define drawer surface, scrim and selected-item styling | Controls Android Navigation |
| TabbedPage | `TabbedViewHandler` | Shared runtime branching in tab manager and bottom navigation utilities | Generic coverage | Define navigation height, indicator, icon and label state behavior | Controls Android Navigation |

## Cross-cutting Material 3 implementation points

These areas are not represented by a single control registration but affect
multiple controls and navigation surfaces:

| Area | Source | Current behavior | Owner |
| --- | --- | --- | --- |
| Application theme | `src/Core/src/Platform/Android/MauiAppCompatActivity.cs` | Selects Material 3 or legacy no-action-bar theme | Core Android Handlers |
| Control theme wrapper | `src/Core/src/Platform/Android/MauiMaterialContextThemeWrapper.cs` | Selects Material 3 or legacy base theme for native controls | Core Android Handlers |
| XML styles | `src/Core/src/Platform/Android/Resources/values/styles-material3.xml` | Defines Material 3 app, action-mode, bottom-navigation and button styles | Core Android Handlers |
| Shell defaults | `src/Controls/src/Core/Shell/Shell.cs` | Uses separate hard-coded Material 3 and legacy colors | Controls Android Navigation |
| Shell renderer | `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/` | Branches for backgrounds, tint, tabs, flyout and toolbar behavior | Controls Android Navigation |
| Alerts | `src/Controls/src/Core/Platform/AlertManager/AlertManager.Android.cs` | Uses `MaterialAlertDialogBuilder` with a Material-themed context | Controls Android |
| Bottom navigation | `src/Controls/src/Core/Platform/Android/BottomNavigationViewUtils.cs` | Branches for Material 3 color and item behavior | Controls Android Navigation |
| Tab layout | `src/Controls/src/Core/Platform/Android/TabbedPageManager.cs` | Uses Material 3 bottom-navigation dimensions | Controls Android Navigation |
| Ripple behavior | `src/Core/src/Platform/Android/MauiRippleDrawableExtensions.cs` | Uses a Material 3 default corner radius | Core Android Handlers |
| Scroll behavior | `src/Core/src/Platform/Android/MauiScrollView.cs` | Contains Material 3-specific platform branches | Core Android Handlers |

## Legacy behavior contract

The following behavior must remain unchanged while Material 3 is disabled:

1. The legacy activity and control themes are selected.
2. Legacy handlers remain registered for every replacement-handler control.
3. Existing native control types and hierarchies remain unchanged.
4. Existing property and command mapper behavior remains unchanged.
5. Existing focus, keyboard, selection and event ordering remains unchanged.
6. Existing measurement, padding and minimum-size behavior remains unchanged.
7. Existing Shell, navigation, tab and flyout colors remain unchanged.
8. Existing custom handler registrations continue to override framework
   defaults.
9. Existing mapper modifications continue to run.
10. Existing automation identifiers and accessibility semantics remain
    available.
11. Existing application styles and explicit property values retain their
    precedence.
12. Material 3 resources or defaults do not leak into the legacy path.

Any violation of this contract is a backward-compatibility defect rather than
an accepted Material 3 visual difference.

## Accepted Material 3 visual differences

The following differences are acceptable only while Material 3 is enabled and
only when behavior and accessibility remain compatible:

- Material 3 native color roles and state colors.
- Material 3 control geometry, corner treatment and elevation.
- Material 3 typography defaults when an application has not assigned fonts.
- Material 3 ripple geometry and interaction visuals.
- Material 3 checkbox, radio button, switch and slider appearance.
- Material 3 progress indicator geometry.
- Material 3 button shape and icon layout.
- Material text-field container and placeholder presentation.
- Material date, time, picker and alert dialog presentation.
- Material 3 navigation bar height, indicator and item presentation.
- Material 3 Shell surface and navigation colors after they are resolved
  through semantic theme roles.

The following are not accepted as visual differences:

- Different event ordering.
- Lost or duplicated events.
- Changed binding behavior.
- Lost explicit colors, fonts or styles.
- Incorrect value synchronization.
- Broken keyboard or focus behavior.
- Changed control bounds that violate MAUI layout requests.
- Missing automation or accessibility information.
- Listener leaks or failures after handler recreation.

## Known implementation gaps

1. Material 3 checks are distributed across registration, handlers, Shell,
   compatibility renderers and platform helpers.
2. No centralized Material 3 configuration object exists.
3. No centralized semantic theme resolver exists.
4. Shell still contains hard-coded Material 3 palette values.
5. Dynamic color is not integrated.
6. Adaptive Material 3 layout behavior is outside the current migration.
7. Several `Handler2` and native wrapper types remain internal.
8. Navigation and compatibility controls lack a consolidated Material 3 test
   suite.
9. Generic test coverage does not prove semantic theme reset, accessibility
    or runtime theme-change behavior.

## Existing dedicated Material 3 test suites

- `ButtonMaterial3FeatureTests.cs`
- `DatePickerMaterial3FeatureTests.cs`
- `ImageButtonMaterial3FeatureTests.cs`
- `Material3ActivityIndicatorFeatureTests.cs`
- `Material3CheckBoxFeatureTests.cs`
- `Material3EditorFeatureTests.cs`
- `Material3EntryFeatureTests.cs`
- `Material3ImageFeatureTests.cs`
- `Material3LabelFeatureTests.cs`
- `Material3PickerFeatureTests.cs`
- `Material3ProgressBarFeatureTests.cs`
- `Material3RadioButtonFeatureTests.cs`
- `Material3SliderFeatureTests.cs`
- `Material3SwitchFeatureTests.cs`
- `Material3TimePickerFeatureTests.cs`
- `SearchBarMaterial3FeatureTests.cs`

These are located under:
`src/Controls/tests/TestCases.Shared.Tests/Tests/FeatureMatrix/`.

Android screenshot baselines are located under:
`src/Controls/tests/TestCases.Android.Tests/snapshots/`.

## Phase 1 exit-gate assessment

| Requirement | Status |
| --- | --- |
| Every registered Android control has a migration classification | Complete |
| Every control has a migration strategy or required decision | Complete |
| Every control has a compatibility-test status | Complete |
| Every control has provisional code-area ownership | Complete |
| Legacy behavior contract is documented | Complete |
| Accepted Material 3 visual differences are documented | Complete |
| Known gaps are documented for later phases | Complete |

Phase 1 is complete. Phase 2 must not begin until this baseline is reviewed and
the provisional ownership model is accepted or replaced.
