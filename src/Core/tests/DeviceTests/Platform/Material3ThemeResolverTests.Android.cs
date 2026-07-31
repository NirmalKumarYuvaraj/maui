using System;
using System.Threading.Tasks;
using Android.Views;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using Xunit;
using AColor = Android.Graphics.Color;

namespace Microsoft.Maui.DeviceTests
{
	public class Material3ThemeResolverTests : CoreHandlerTestBase
	{
		[Fact]
		[Category(TestCategory.MauiContext)]
		public Task SemanticColorsResolveFromMaterial3Theme() =>
			InvokeOnMainThreadAsync(() =>
			{
				using var context = new ContextThemeWrapper(
					MauiContext.Context!,
					Resource.Style.Maui_Material3_Theme_Base);

				Assert.Equal(
					context.GetThemeAttrColor(Resource.Attribute.colorPrimary),
					Material3ThemeResolver.ResolveColor(context, Material3ColorRole.Primary));
				Assert.Equal(
					context.GetThemeAttrColor(Resource.Attribute.colorSurface),
					Material3ThemeResolver.ResolveColor(context, Material3ColorRole.Surface));
				Assert.Equal(
					context.GetThemeAttrColor(Resource.Attribute.colorSurfaceContainer),
					Material3ThemeResolver.ResolveColor(context, Material3ColorRole.SurfaceContainer));
				Assert.Equal(
					context.GetThemeAttrColor(Resource.Attribute.colorOnSurface),
					Material3ThemeResolver.ResolveColor(context, Material3ColorRole.OnSurface));
				Assert.Equal(
					context.GetThemeAttrColor(Resource.Attribute.colorOnSurfaceVariant),
					Material3ThemeResolver.ResolveColor(context, Material3ColorRole.OnSurfaceVariant));
			});

		[Fact]
		[Category(TestCategory.MauiContext)]
		public void FallbackColorsPreservePreviousDefaults()
		{
			Assert.Equal(
				Color.FromArgb("#FEF7FF"),
				Material3ThemeResolver.ResolveFallbackColor(Material3ColorRole.Surface, isDark: false));
			Assert.Equal(
				Color.FromArgb("#141218"),
				Material3ThemeResolver.ResolveFallbackColor(Material3ColorRole.Surface, isDark: true));
			Assert.Equal(
				Color.FromArgb("#1D1B20"),
				Material3ThemeResolver.ResolveFallbackColor(Material3ColorRole.OnSurface, isDark: false));
			Assert.Equal(
				Color.FromArgb("#E6E0E9"),
				Material3ThemeResolver.ResolveFallbackColor(Material3ColorRole.OnSurface, isDark: true));
		}

		[Fact]
		[Category(TestCategory.MauiContext)]
		public Task AlphaIsAppliedToResolvedColor() =>
			InvokeOnMainThreadAsync(() =>
			{
				using var context = new ContextThemeWrapper(
					MauiContext.Context!,
					Resource.Style.Maui_Material3_Theme_Base);

				int color = Material3ThemeResolver.ResolveColor(context, Material3ColorRole.OnSurface);
				int expected = (color & 0x00ffffff) |
					((int)Math.Round(AColor.GetAlphaComponent(color) * 0.10f) << 24);

				Assert.Equal(
					expected,
					Material3ThemeResolver.ResolveColor(context, Material3ColorRole.OnSurface, 0.10f));
			});
	}
}
