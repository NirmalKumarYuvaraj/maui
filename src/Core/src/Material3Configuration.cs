using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Maui
{
	enum Material3ThemePolicy
	{
		Legacy,
		Material3,
	}

	enum Material3CompatibilityMode
	{
		PreserveLegacyBehavior,
	}

	static class Material3Configuration
	{
#if NET10_0_OR_GREATER
		[FeatureSwitchDefinition("Microsoft.Maui.RuntimeFeature.IsMaterial3Enabled")]
#endif
		public static bool Enabled => RuntimeFeature.IsMaterial3Enabled;

		public static bool DynamicColorEnabled => false;

		public static Material3ThemePolicy ThemePolicy =>
			Enabled ? Material3ThemePolicy.Material3 : Material3ThemePolicy.Legacy;

		public static Material3CompatibilityMode CompatibilityMode =>
			Material3CompatibilityMode.PreserveLegacyBehavior;
	}
}
