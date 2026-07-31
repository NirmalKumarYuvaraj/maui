using System;
using Android.Content;
using Android.Content.Res;
using AColor = Android.Graphics.Color;
using Color = Microsoft.Maui.Graphics.Color;

namespace Microsoft.Maui.Platform
{
	enum Material3ColorRole
	{
		Primary,
		Surface,
		SurfaceContainer,
		OnSurface,
		OnSurfaceVariant,
		OutlineVariant,
	}

	static class Material3ThemeResolver
	{
		public static int ResolveColor(Context context, Material3ColorRole role)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			if (context.TryGetThemeAttrColor(GetColorAttribute(role), out int color))
				return color;

			return ResolveFallbackColor(role, IsDarkTheme(context)).ToPlatform().ToArgb();
		}

		public static int ResolveColor(Context context, Material3ColorRole role, float alpha)
		{
			int color = ResolveColor(context, role);
			int originalAlpha = AColor.GetAlphaComponent(color);
			return (color & 0x00ffffff) | ((int)Math.Round(originalAlpha * alpha) << 24);
		}

		public static Color ResolveMauiColor(Context context, Material3ColorRole role) =>
			new AColor(ResolveColor(context, role)).ToColor();

		public static Color ResolveFallbackColor(Material3ColorRole role, bool isDark) =>
			role switch
			{
				Material3ColorRole.Primary => Color.FromArgb(isDark ? "#FFFFFF" : "#625B71"),
				Material3ColorRole.Surface => Color.FromArgb(isDark ? "#141218" : "#FEF7FF"),
				Material3ColorRole.SurfaceContainer => Color.FromArgb(isDark ? "#1D1B20" : "#F3EDF7"),
				Material3ColorRole.OnSurface => Color.FromArgb(isDark ? "#E6E0E9" : "#1D1B20"),
				Material3ColorRole.OnSurfaceVariant => Color.FromArgb(isDark ? "#CAC4D0" : "#49454F"),
				Material3ColorRole.OutlineVariant => Color.FromArgb(isDark ? "#49454F" : "#CAC4D0"),
				_ => throw new ArgumentOutOfRangeException(nameof(role)),
			};

		static int GetColorAttribute(Material3ColorRole role) =>
			role switch
			{
				Material3ColorRole.Primary => Resource.Attribute.colorPrimary,
				Material3ColorRole.Surface => Resource.Attribute.colorSurface,
				Material3ColorRole.SurfaceContainer => Resource.Attribute.colorSurfaceContainer,
				Material3ColorRole.OnSurface => Resource.Attribute.colorOnSurface,
				Material3ColorRole.OnSurfaceVariant => Resource.Attribute.colorOnSurfaceVariant,
				Material3ColorRole.OutlineVariant => Resource.Attribute.colorOutlineVariant,
				_ => throw new ArgumentOutOfRangeException(nameof(role)),
			};

		static bool IsDarkTheme(Context context)
		{
			var uiMode = context.Resources?.Configuration?.UiMode ?? UiMode.NightUndefined;
			return (uiMode & UiMode.NightMask) == UiMode.NightYes;
		}
	}
}
