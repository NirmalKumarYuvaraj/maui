# Android Safe Area Review Tasks

**Review target:** `android-new-safeareaarchitecture`  
**Current verdict:** `NEEDS_CHANGES`

## Completed Review Work

- [x] Refresh `upstream/main` and materialize the exact compare range.
- [x] Read the full changed Android safe-area, navigation, handler, and test files.
- [x] Trace navigation root, Shell, FlyoutPage, toolbar, layout, ScrollView, and IME lifecycle paths.
- [x] Compare implementation behavior with `New Android SafeAreaArchitecture.md`.
- [x] Review partial inset consumption by source and edge.
- [x] Review explicit/implicit `SafeAreaEdges` ownership.
- [x] Review listener attach/detach and padding reset behavior.
- [x] Review the added device-test coverage.
- [x] Run whitespace validation.
- [x] Attempt a targeted Android Core build.
- [x] Record architecture analysis and review findings.

## Blocking Tasks

- [ ] Replace listener-per-view ownership with one listener and IME coordinator
  per logical navigation/content/modal/flyout region.
- [ ] Define lightweight registration/unregistration for explicit
  `SafeAreaEdges` participants.
- [ ] Resolve participant edge ownership from stable arranged bounds relative
  to the region, not `GetLocationOnScreen`.
- [ ] Define how a nested ownership region receives a scoped remaining typed
  inset snapshot.
- [ ] Define how top, bottom, left, and right ownership is resolved among
  explicit sibling layouts inside the content region.
- [ ] Prevent non-edge siblings from applying insets solely because they
  requested `SafeAreaEdges.Container` or `All`.
- [ ] Add an attached Android device test with vertically stacked explicit
  `Container` siblings and assert only edge-adjacent owners receive padding.
- [ ] Add horizontal sibling coverage for left/right cutout and system-bar
  insets.
- [ ] Add nested explicit parent/child/sibling coverage proving each inset
  source has one intended owner per content region.
- [ ] Add navigation ownership invalidation when AppBar/top-tab content appears,
  disappears, or changes visibility.
- [ ] Add navigation ownership invalidation when bottom-tab content appears,
  disappears, or changes visibility.
- [ ] Ensure invalidation requests insets for both the navigation root and the
  content host when their forwarded snapshots differ.
- [ ] Avoid invalidation loops by caching the last resolved owners or padding and
  requesting redispatch only when structural ownership changes.
- [ ] Add an attached Android device test: visible AppBar -> hidden AppBar after
  initial inset dispatch.
- [ ] Add an attached Android device test: hidden AppBar -> visible AppBar after
  initial inset dispatch.
- [ ] Add attached Android device tests for bottom tabs appearing and
  disappearing after initial inset dispatch.
- [ ] Add a fragment lifecycle test where an initially empty
  `FragmentContainerView` gains or loses visible content.

## Lifecycle Tasks

- [ ] Give `NavigationContentWindowInsetListener` an explicit detach path.
- [ ] Clear its `ViewCompat.SetOnApplyWindowInsetsListener` assignment when the
  navigation listener is removed or replaced.
- [ ] Dispose the Java listener and release its bottom-tabs reference.
- [ ] Verify repeated connect/disconnect and Shell tab switches do not accumulate
  listeners.
- [ ] Verify FlyoutPage and modal navigation teardown during an active IME
  animation restores original padding.

## IME Tasks

- [ ] Ignore non-IME `OnPrepare` callbacks without ending an active IME
  animation.
- [ ] Add a coordinator test with interleaved IME and system-bar animation
  callbacks.
- [ ] Replace per-frame `SetPadding` with the documented final-layout plus
  temporary-translation animation model, or document why that design changed.
- [ ] Test keyboard opening and closing with `Container`, `SoftInput`, `All`, and
  `None`.
- [ ] Test `AdjustPan`, `AdjustResize`, and `AdjustNothing`.
- [ ] Test focus changes while the keyboard remains visible.
- [ ] Test navigation and owner detach during an active keyboard animation.
- [ ] Test rotation and zero-inset cleanup while IME padding is applied.
- [ ] Verify no more than one view owns IME in each content region.

## Regression Matrix

- [ ] NavigationPage with toolbar visible/hidden.
- [ ] Shell with toolbar and top tabs.
- [ ] Shell bottom tabs with runtime visibility changes.
- [ ] FlyoutPage in flyout and locked modes.
- [ ] Modal pages.
- [ ] Nested layouts with explicit parent and explicit child `SafeAreaEdges`.
- [ ] ScrollView and CollectionView, including recycled item content.
- [ ] Display cutout in portrait and landscape.
- [ ] API 28-29 compat inset dispatch.
- [ ] API 30+ edge-to-edge dispatch.
- [ ] Gesture navigation and three-button navigation.

## Validation Tasks

- [ ] Build the required MAUI build tasks/native Android artifacts.
- [ ] Build `src/Core/src/Core.csproj` for `net10.0-android36.0`.
- [ ] Build the affected Controls and Core device-test projects.
- [ ] Run the focused Android device tests through the `run-device-tests` skill.
- [ ] Run the Sandbox scenarios with device-log and screenshot validation.
- [ ] Run `git diff --check` and remove `.vscode/launch.json` trailing whitespace.
- [ ] Remove `.vscode/launch.json` from the branch unless repository-wide IDE
  configuration is intended.
- [ ] Re-run the code review after all blocking tasks are complete.
