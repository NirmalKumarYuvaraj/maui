using System;
using System.Numerics;

namespace Microsoft.Maui.Graphics.Platform
{
	// Pixels enter and leave this helper as unpremultiplied Android ARGB values.
	internal static class BlendModeCompositor
	{
		internal static int Blend(int source, int destination, BlendMode mode, float coverage = 1)
		{
			float sa = (uint)source >> 24;
			sa /= 255;
			if (sa == 0 || coverage == 0)
				return destination;

			float da = ((uint)destination >> 24) / 255f;
			var s = Unpack(source);
			var d = Unpack(destination);
			float alpha;
			Vector3 result;

			if (mode == BlendMode.PlusDarker)
			{
				// PlusDarker uses premultiplied components and additive alpha, not source-over alpha.
				sa = Math.Min(1, sa / coverage);
				alpha = Math.Min(1, sa + da);
				result = Vector3.Max(Vector3.Zero, s * sa + d * da - new Vector3(Math.Max(0, sa + da - 1)));
				result = result * coverage + d * da * (1 - coverage);
				alpha = alpha * coverage + da * (1 - coverage);
			}
			else
			{
				alpha = sa + da * (1 - sa);
				var blended = BlendColor(s, d, mode);
				result = (1 - da) * sa * s + (1 - sa) * da * d + sa * da * blended;
			}

			result /= alpha;
			return (Byte(alpha) << 24) | (Byte(result.X) << 16) | (Byte(result.Y) << 8) | Byte(result.Z);
		}

		static Vector3 Unpack(int pixel) =>
			new Vector3((pixel >> 16) & 255, (pixel >> 8) & 255, pixel & 255) / 255;

		static int Byte(float value) => (int)(Math.Clamp(value, 0, 1) * 255 + 0.5f);

		static Vector3 BlendColor(Vector3 s, Vector3 d, BlendMode mode)
		{
			switch (mode)
			{
				case BlendMode.Hue:
					return SetLuminosity(SetSaturation(s, Saturation(d)), Luminosity(d));
				case BlendMode.Saturation:
					return SetLuminosity(SetSaturation(d, Saturation(s)), Luminosity(d));
				case BlendMode.Color:
					return SetLuminosity(s, Luminosity(d));
				case BlendMode.Luminosity:
					return SetLuminosity(d, Luminosity(s));
				default:
					return new Vector3(Channel(s.X, d.X, mode), Channel(s.Y, d.Y, mode), Channel(s.Z, d.Z, mode));
			}
		}

		static float Channel(float s, float d, BlendMode mode)
		{
			switch (mode)
			{
				case BlendMode.Multiply:
					return s * d;
				case BlendMode.ColorDodge:
					return d == 0 ? 0 : s == 1 ? 1 : Math.Min(1, d / (1 - s));
				case BlendMode.ColorBurn:
					return d == 1 ? 1 : s == 0 ? 0 : 1 - Math.Min(1, (1 - d) / s);
				case BlendMode.HardLight:
					return s <= 0.5f ? 2 * s * d : 1 - 2 * (1 - s) * (1 - d);
				case BlendMode.SoftLight:
					if (s <= 0.5f)
						return d - (1 - 2 * s) * d * (1 - d);
					float curve = d <= 0.25f ? ((16 * d - 12) * d + 4) * d : (float)Math.Sqrt(d);
					return d + (2 * s - 1) * (curve - d);
				case BlendMode.Difference:
					return Math.Abs(d - s);
				case BlendMode.Exclusion:
					return s + d - 2 * s * d;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, "This mode has a native Android implementation.");
			}
		}

		static float Luminosity(Vector3 color) => 0.3f * color.X + 0.59f * color.Y + 0.11f * color.Z;

		static float Saturation(Vector3 color) => Max(color) - Min(color);

		static float Min(Vector3 color) => Math.Min(color.X, Math.Min(color.Y, color.Z));

		static float Max(Vector3 color) => Math.Max(color.X, Math.Max(color.Y, color.Z));

		static Vector3 SetSaturation(Vector3 color, float saturation)
		{
			float min = Min(color);
			float range = Max(color) - min;
			return range > 0 ? (color - new Vector3(min)) * (saturation / range) : Vector3.Zero;
		}

		static Vector3 SetLuminosity(Vector3 color, float luminosity)
		{
			color += new Vector3(luminosity - Luminosity(color));
			float min = Min(color);
			float max = Max(color);
			if (min < 0)
				color = new Vector3(luminosity) + (color - new Vector3(luminosity)) * (luminosity / (luminosity - min));
			if (max > 1)
				color = new Vector3(luminosity) + (color - new Vector3(luminosity)) * ((1 - luminosity) / (max - luminosity));
			return color;
		}
	}
}
