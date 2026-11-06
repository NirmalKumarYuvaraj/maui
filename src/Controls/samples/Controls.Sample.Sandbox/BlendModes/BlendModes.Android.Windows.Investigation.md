# Blend modes: Android and Windows investigation

Issue: [dotnet/maui#14945](https://github.com/dotnet/maui/issues/14945)

Investigation date: 2026-11-06

## Scope and status

Investigate whether all 28 existing `Microsoft.Maui.Graphics.BlendMode` values
can be implemented on Android and Windows, and track the work required to move
from the current no-op setters to working platform implementations.

This is a source/API investigation, not an implemented fix. No build, automated
rendering test, or device validation has been run. Native API correspondence
does not establish exact pixel parity, particularly for transparency,
antialiasing, color spaces, and non-separable modes.

All 28 modes are technically implementable with native operations and custom
composition where necessary. There is no single native enum mapping covering
every mode on either platform.

### Public API and compatibility

`ICanvas.BlendMode` and all 28 enum members already exist. Implementation can
use private/internal mappings, state, and rendering helpers without adding
public or protected members. No `PublicAPI.Unshipped.txt` entries are required
if that boundary is preserved.

This still changes behavior: assignments currently ignored on Android and
Windows will start affecting drawing. Preserve the default `Normal` behavior,
existing minimum supported OS versions, and save/restore/reset semantics.
Performance and resource usage may change when offscreen composition is used.

Shared source:

- `src/Graphics/src/Graphics/BlendMode.cs`
- `src/Graphics/src/Graphics/ICanvas.cs`
- `src/Graphics/src/Graphics/AbstractCanvas.cs`
- `src/Graphics/src/Graphics/ScalingCanvas.cs`

## Android

### Findings

The `BlendMode` setter in
`src/Graphics/src/Graphics/Platforms/Android/PlatformCanvas.cs:101`
contains only commented-out code. That code refers to Apple's `CGBlendMode`;
uncommenting it is not an Android implementation.

Android 10 / API 29 introduced `android.graphics.BlendMode` and
`Paint.setBlendMode`. Its enum provides corresponding operations for 27 of
MAUI's 28 values. `PlusDarker` has no direct equivalent.

For MAUI 10, Android API 21 remains supported. MAUI 11 raises the minimum to
API 24, which is still below API 29. A modern-only mapping therefore does not
cover the full supported Android range.

### Mode mapping

Names in the second column are native Android enum constants, not C# binding
member spellings.

| MAUI mode | Android `BlendMode`, API 29+ | Legacy `PorterDuff.Mode` |
|---|---|---|
| `Normal` | `SRC_OVER` | `SRC_OVER` |
| `Multiply` | `MULTIPLY` | No equivalent with the same transparency semantics |
| `Screen` | `SCREEN` | `SCREEN` |
| `Overlay` | `OVERLAY` | `OVERLAY` |
| `Darken` | `DARKEN` | `DARKEN` |
| `Lighten` | `LIGHTEN` | `LIGHTEN` |
| `ColorDodge` | `COLOR_DODGE` | No direct equivalent |
| `ColorBurn` | `COLOR_BURN` | No direct equivalent |
| `SoftLight` | `SOFT_LIGHT` | No direct equivalent |
| `HardLight` | `HARD_LIGHT` | No direct equivalent |
| `Difference` | `DIFFERENCE` | No direct equivalent |
| `Exclusion` | `EXCLUSION` | No direct equivalent |
| `Hue` | `HUE` | No direct equivalent |
| `Saturation` | `SATURATION` | No direct equivalent |
| `Color` | `COLOR` | No direct equivalent |
| `Luminosity` | `LUMINOSITY` | No direct equivalent |
| `Clear` | `CLEAR` | `CLEAR` |
| `Copy` | `SRC` | `SRC` |
| `SourceIn` | `SRC_IN` | `SRC_IN` |
| `SourceOut` | `SRC_OUT` | `SRC_OUT` |
| `SourceAtop` | `SRC_ATOP` | `SRC_ATOP` |
| `DestinationOver` | `DST_OVER` | `DST_OVER` |
| `DestinationIn` | `DST_IN` | `DST_IN` |
| `DestinationOut` | `DST_OUT` | `DST_OUT` |
| `DestinationAtop` | `DST_ATOP` | `DST_ATOP` |
| `Xor` | `XOR` | `XOR` |
| `PlusDarker` | No direct equivalent | No direct equivalent |
| `PlusLighter` | `PLUS` | `ADD` |

Legacy `PorterDuff.Mode.MULTIPLY` multiplies both color and alpha. It corresponds
to modern `BlendMode.MODULATE`, not modern `BlendMode.MULTIPLY`. An opaque-only
test can miss this difference.

### Rendering considerations

Blend state must reach fill, stroke, text, and image paints, including paints
created lazily or cloned during state changes. `DrawImage` obtains a separate
paint through `CurrentState.GetImagePaint`, so changing only fill/stroke paints
will leave image drawing incomplete.

Relevant source:

- `src/Graphics/src/Graphics/Platforms/Android/PlatformCanvasState.cs`
- `src/Graphics/src/Graphics/Platforms/Android/PlatformCanvasStateService.cs`
- `src/Graphics/src/Graphics/Platforms/Android/PlatformCanvas.cs:752`
- `src/Graphics/src/Graphics/Platforms/Android/PlatformGraphicsView.cs`

Destination-sensitive operations require the correct drawing destination.
Use an isolated layer/offscreen target where needed; do not assume the view's
background or an opaque window surface is a transparent compositing target.
Do not default to disabling hardware acceleration for the entire view.

Older-API gaps and `PlusDarker` require custom composition, for example a
software pixel implementation or a shader with explicit source/destination
inputs. The fallback architecture and its performance are not yet validated.

### Android migration tasks

- [ ] Define expected blend, alpha, and drawing-coverage semantics using the existing MAUI/CoreGraphics behavior as a reference.
- [ ] Add private/internal mappings for all 27 API 29+ native equivalents, with runtime API-level guards.
- [ ] Implement the legacy `PorterDuffXfermode` subset without treating legacy `MULTIPLY` as equivalent.
- [ ] Select and implement a fallback for all remaining modes below API 29; do not silently substitute `Normal`.
- [ ] Implement `PlusDarker` for every supported Android API level and verify partially transparent inputs, not just opaque colors.
- [ ] Propagate mode changes to fill, stroke, plain/attributed text, image, and temporary/effect paints.
- [ ] Preserve blend state through `SaveState`, `RestoreState`, reset, paint cloning, and reuse across draw callbacks.
- [ ] Preserve clipping, transforms, `Alpha`, gradients, image transparency, shadows, and blur.
- [ ] Add layer isolation only where required and dispose native paints, transfer modes, and offscreen resources correctly.
- [ ] Add focused rendering regression tests that fail with the current no-op setter, including transparent `Multiply` and destination-sensitive modes.
- [ ] Validate all 28 modes in Sandbox on an API below 29 and an API 29+ device/emulator, using opaque and partially transparent inputs.
- [ ] Validate hardware and software canvas behavior, drawing coverage, resize, redraw, and resource reuse; retain logs and visual/pixel evidence.
- [ ] Measure fallback allocation, memory, and redraw cost; preserve the normal drawing fast path.
- [ ] Run the relevant build/tests and confirm no unintended public/protected API additions or minimum-OS changes.
- [ ] Update Android support documentation only after the supported API ranges and fallback behavior are verified.

### Android references

- [Android BlendMode](https://developer.android.com/reference/android/graphics/BlendMode)
- [Android Paint.setBlendMode](https://developer.android.com/reference/android/graphics/Paint#setBlendMode(android.graphics.BlendMode))
- [Android PorterDuff.Mode](https://developer.android.com/reference/android/graphics/PorterDuff.Mode)
- [MAUI supported platforms](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms?view=net-maui-10.0)

## Windows

### Findings

The `BlendMode` setter in
`src/Graphics/src/Graphics/Platforms/Windows/PlatformCanvas.cs:138` is empty.
The Windows implementation already uses Win2D. The additional Graphics.Win2D
project compiles these same platform sources, so changes must cover both build
configurations.

`CanvasDrawingSession.Blend` uses `CanvasBlend`, which exposes only
`SourceOver`, `Copy`, `Min`, and `Add`. It cannot implement all MAUI modes.
`Min` is not a general substitute for `Darken` with transparency.

Win2D exposes the needed separable/non-separable modes through `BlendEffect`
and Porter-Duff operations through `CanvasComposite` / `CompositeEffect`.
These Direct2D facilities predate MAUI's Windows 10 version 1809 minimum;
the documented OS baseline is not the blocker.

The `NoComposition` annotations on Win2D HSL blend modes concern Windows
Composition integration, not use of those effects through Win2D drawing.

### Mode mapping

| MAUI mode | Windows / Win2D route |
|---|---|
| `Normal` | `CanvasBlend.SourceOver` |
| `Multiply` | `BlendEffectMode.Multiply` |
| `Screen` | `BlendEffectMode.Screen` |
| `Overlay` | `BlendEffectMode.Overlay` |
| `Darken` | `BlendEffectMode.Darken` |
| `Lighten` | `BlendEffectMode.Lighten` |
| `ColorDodge` | `BlendEffectMode.ColorDodge` |
| `ColorBurn` | `BlendEffectMode.ColorBurn` |
| `SoftLight` | `BlendEffectMode.SoftLight` |
| `HardLight` | `BlendEffectMode.HardLight` |
| `Difference` | `BlendEffectMode.Difference` |
| `Exclusion` | `BlendEffectMode.Exclusion` |
| `Hue` | `BlendEffectMode.Hue` |
| `Saturation` | `BlendEffectMode.Saturation` |
| `Color` | `BlendEffectMode.Color` |
| `Luminosity` | `BlendEffectMode.Luminosity` |
| `Clear` | Coverage-aware transparent replacement; no dedicated `CanvasComposite.Clear` |
| `Copy` | `CanvasBlend.Copy` / `CanvasComposite.Copy`, with correct bounds and coverage |
| `SourceIn` | `CanvasComposite.SourceIn` |
| `SourceOut` | `CanvasComposite.SourceOut` |
| `SourceAtop` | `CanvasComposite.SourceAtop` |
| `DestinationOver` | `CanvasComposite.DestinationOver` |
| `DestinationIn` | `CanvasComposite.DestinationIn` |
| `DestinationOut` | `CanvasComposite.DestinationOut` |
| `DestinationAtop` | `CanvasComposite.DestinationAtop` |
| `Xor` | `CanvasComposite.Xor` |
| `PlusDarker` | Arithmetic/custom effect; transparent-input equivalence remains unverified |
| `PlusLighter` | `CanvasBlend.Add` / `CanvasComposite.Add` |

### Rendering considerations

`BlendEffect` takes explicit foreground and background images. Blending
arbitrary shapes/text therefore requires an accumulated destination surface
and a source surface for the current operation, followed by composition.
Do not read and write the same render target simultaneously; use separate
buffers and replace the destination with the composed output rather than
source-over drawing it a second time.

The existing `Draw(Action<CanvasDrawingSession>)` helper at
`PlatformCanvas.cs:730` centralizes many draw operations and already supports
shadow/blur intermediates. It does not currently maintain the accumulated
destination required by blend effects.

Copying a full transparent source bitmap or clearing the entire target is
not equivalent to clearing/copying only the requested geometry. Preserve
operation coverage and clipping, including antialiased edges and transparent
pixels inside image bounds. `Copy` and `BoundedCopy` have different bounds
semantics and must not be selected interchangeably.

Relevant source:

- `src/Graphics/src/Graphics/Platforms/Windows/PlatformCanvas.cs`
- `src/Graphics/src/Graphics/Platforms/Windows/PlatformCanvasState.cs`
- `src/Graphics/src/Graphics/Platforms/Windows/PlatformGraphicsView.cs`
- `src/Graphics/src/Graphics.Win2D/Graphics.Win2D.csproj`

### PlusDarker investigation

MAUI and CoreGraphics document the color operation as:

```text
max(0, source + destination - 1)
```

`ArithmeticCompositeEffect` can express that arithmetic with multiplication
amount `0`, source amounts `1` and `1`, offset `-1`, and output clamping.
This is a candidate for opaque-color inputs, not proof of full equivalence.
The arithmetic effect applies its formula to RGBA; premultiplied alpha,
transparent regions, and geometry coverage must be checked against the
reference behavior. An additional effect graph or custom shader may be needed.

`LinearBurn` has a related opaque-color function but uses the blend effect's
alpha composition. Do not assume it is an exact `PlusDarker` replacement.
Likewise, `PlusLighter` should use additive composition, not an unchecked
substitution with `LinearDodge`.

### Windows migration tasks

- [ ] Define expected blend, alpha, and drawing-coverage semantics using the existing MAUI/CoreGraphics behavior as a reference.
- [ ] Keep direct primitive operations for modes that can use them safely; preserve the normal drawing fast path.
- [ ] Introduce private/internal blend state and preserve it through save/restore/reset and session changes.
- [ ] Design accumulated-destination/source buffers for effect modes, with no render-target read/write feedback.
- [ ] Rasterize each operation and compose it in draw order; do not blend all operations as one group.
- [ ] Map all 15 non-normal separable/non-separable modes through `BlendEffect`, with foreground/background ordering verified.
- [ ] Implement Porter-Duff operations through `CanvasComposite` / `CompositeEffect` where appropriate.
- [ ] Implement coverage-aware `Clear` and `Copy`, preserving pixels outside the operation and current clip.
- [ ] Implement `PlusLighter` with additive composition and verify color/alpha clamping.
- [ ] Evaluate the arithmetic `PlusDarker` candidate against opaque and partially transparent reference pixels; implement a custom effect if needed.
- [ ] Cover fills, strokes, images, plain/attributed text, gradients, transforms, clipping, `Alpha`, shadows, and blur.
- [ ] Integrate blending with the existing draw/effect helpers and audit draw paths that bypass them.
- [ ] Manage DPI, resize, redraw, device loss, session lifetime, buffer reuse, and disposal.
- [ ] Add focused rendering regression tests that fail with the current no-op setter, including source/destination order and untouched-region checks.
- [ ] Validate all 28 modes in Windows Sandbox, retaining visual/pixel evidence and testing partially transparent inputs.
- [ ] Measure effect allocation, memory, and redraw cost; avoid unnecessary full-surface composition.
- [ ] Build/test both the main Graphics Windows implementation and the `MAUI_GRAPHICS_WIN2D` configuration; confirm no unintended API or OS-baseline changes.
- [ ] Update Windows support documentation only after the actual mode coverage and limitations are verified.

### Windows references

- [Win2D CanvasBlend](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_CanvasBlend.htm)
- [Win2D CanvasComposite](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_CanvasComposite.htm)
- [Win2D BlendEffectMode](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Effects_BlendEffectMode.htm)
- [Win2D BlendEffect](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Effects_BlendEffect.htm)
- [Direct2D blend effect and alpha equations](https://learn.microsoft.com/en-us/windows/win32/direct2d/blend)
- [Direct2D composite effect](https://learn.microsoft.com/en-us/windows/win32/direct2d/composite)
- [Direct2D arithmetic composite effect](https://learn.microsoft.com/en-us/windows/win32/direct2d/arithmetic-composite)
- [Win2D custom effects](https://learn.microsoft.com/en-us/windows/apps/develop/win2d/custom-effects)
- [CoreGraphics PlusDarker](https://developer.apple.com/documentation/coregraphics/cgblendmode/plusdarker)

## Completion criteria

Do not mark either platform complete based on enum mapping alone. Completion
requires all 28 modes to produce verified output across that platform's
supported OS range, including transparency and coverage edge cases, while
preserving state management and default rendering behavior.

Record the exact validation commands, their results, OS/device details, and
artifact locations when implementation and testing take place. None have been
run for this document.

Shared documentation:
[MAUI blend modes](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/blendmodes?view=net-maui-10.0).
