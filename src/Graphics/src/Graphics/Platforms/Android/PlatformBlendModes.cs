using System;
using System.Runtime.Versioning;
using Android.Graphics;
using APaint = Android.Graphics.Paint;
using ABlendMode = Android.Graphics.BlendMode;

namespace Microsoft.Maui.Graphics.Platform
{
	internal static class PlatformBlendModes
	{
		internal static bool RequiresSoftware(BlendMode mode) =>
			mode == BlendMode.PlusDarker ||
			(!OperatingSystem.IsAndroidVersionAtLeast(29) && LegacyMode(mode) == null);

		internal static void Apply(APaint paint, BlendMode mode)
		{
			paint.SetXfermode(null);
			if (OperatingSystem.IsAndroidVersionAtLeast(29))
			{
				paint.BlendMode = NativeMode(mode);
			}
			else
			{
				var legacy = LegacyMode(mode);
				if (legacy != null && mode != BlendMode.Normal)
				{
					using var transfer = new PorterDuffXfermode(legacy);
					paint.SetXfermode(transfer);
				}
			}
		}

		[SupportedOSPlatform("android29.0")]
		static ABlendMode NativeMode(BlendMode mode) => mode switch
		{
			BlendMode.Normal => ABlendMode.SrcOver,
			BlendMode.Multiply => ABlendMode.Multiply,
			BlendMode.Screen => ABlendMode.Screen,
			BlendMode.Overlay => ABlendMode.Overlay,
			BlendMode.Darken => ABlendMode.Darken,
			BlendMode.Lighten => ABlendMode.Lighten,
			BlendMode.ColorDodge => ABlendMode.ColorDodge,
			BlendMode.ColorBurn => ABlendMode.ColorBurn,
			BlendMode.SoftLight => ABlendMode.SoftLight,
			BlendMode.HardLight => ABlendMode.HardLight,
			BlendMode.Difference => ABlendMode.Difference,
			BlendMode.Exclusion => ABlendMode.Exclusion,
			BlendMode.Hue => ABlendMode.Hue,
			BlendMode.Saturation => ABlendMode.Saturation,
			BlendMode.Color => ABlendMode.Color,
			BlendMode.Luminosity => ABlendMode.Luminosity,
			BlendMode.Clear => ABlendMode.Clear,
			BlendMode.Copy => ABlendMode.Src,
			BlendMode.SourceIn => ABlendMode.SrcIn,
			BlendMode.SourceOut => ABlendMode.SrcOut,
			BlendMode.SourceAtop => ABlendMode.SrcAtop,
			BlendMode.DestinationOver => ABlendMode.DstOver,
			BlendMode.DestinationIn => ABlendMode.DstIn,
			BlendMode.DestinationOut => ABlendMode.DstOut,
			BlendMode.DestinationAtop => ABlendMode.DstAtop,
			BlendMode.Xor => ABlendMode.Xor,
			BlendMode.PlusLighter => ABlendMode.Plus,
			BlendMode.PlusDarker => ABlendMode.SrcOver, // Rendered into a source buffer before custom composition.
			_ => throw new ArgumentOutOfRangeException(nameof(mode))
		};

		static PorterDuff.Mode LegacyMode(BlendMode mode) => mode switch
		{
			BlendMode.Normal => PorterDuff.Mode.SrcOver,
			BlendMode.Screen => PorterDuff.Mode.Screen,
			BlendMode.Overlay => PorterDuff.Mode.Overlay,
			BlendMode.Darken => PorterDuff.Mode.Darken,
			BlendMode.Lighten => PorterDuff.Mode.Lighten,
			BlendMode.Clear => PorterDuff.Mode.Clear,
			BlendMode.Copy => PorterDuff.Mode.Src,
			BlendMode.SourceIn => PorterDuff.Mode.SrcIn,
			BlendMode.SourceOut => PorterDuff.Mode.SrcOut,
			BlendMode.SourceAtop => PorterDuff.Mode.SrcAtop,
			BlendMode.DestinationOver => PorterDuff.Mode.DstOver,
			BlendMode.DestinationIn => PorterDuff.Mode.DstIn,
			BlendMode.DestinationOut => PorterDuff.Mode.DstOut,
			BlendMode.DestinationAtop => PorterDuff.Mode.DstAtop,
			BlendMode.Xor => PorterDuff.Mode.Xor,
			BlendMode.PlusLighter => PorterDuff.Mode.Add,
			_ => null
		};
	}
}
