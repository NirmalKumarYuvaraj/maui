using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit;

namespace Microsoft.Maui.UnitTests
{
	[Collection(nameof(Material3ConfigurationTests))]
	public class Material3ConfigurationTests
	{
		const string Material3FeatureSwitch = "Microsoft.Maui.RuntimeFeature.IsMaterial3Enabled";

		[Fact]
		public void ConfigurationReflectsRuntimeFeatureSwitch()
		{
			AppContext.TryGetSwitch(Material3FeatureSwitch, out bool originalValue);

			try
			{
				AppContext.SetSwitch(Material3FeatureSwitch, false);

				Assert.False(Material3Configuration.Enabled);
				Assert.Equal(Material3ThemePolicy.Legacy, Material3Configuration.ThemePolicy);

				AppContext.SetSwitch(Material3FeatureSwitch, true);

				Assert.True(Material3Configuration.Enabled);
				Assert.Equal(Material3ThemePolicy.Material3, Material3Configuration.ThemePolicy);
			}
			finally
			{
				AppContext.SetSwitch(Material3FeatureSwitch, originalValue);
			}
		}

		[Fact]
		public void FuturePoliciesPreserveCurrentBehavior()
		{
			Assert.False(Material3Configuration.DynamicColorEnabled);
			Assert.Equal(
				Material3CompatibilityMode.PreserveLegacyBehavior,
				Material3Configuration.CompatibilityMode);
		}

#if NET10_0_OR_GREATER
		[Fact]
		public void EnabledRemainsLinkerFeatureSwitch()
		{
			var property = typeof(Material3Configuration).GetProperty(
				nameof(Material3Configuration.Enabled),
				BindingFlags.Public | BindingFlags.Static);
			var featureSwitch = property.GetCustomAttribute<FeatureSwitchDefinitionAttribute>();

			Assert.Equal(Material3FeatureSwitch, featureSwitch.SwitchName);
		}
#endif
	}

	[CollectionDefinition(nameof(Material3ConfigurationTests), DisableParallelization = true)]
	public class Material3ConfigurationTestCollection
	{
	}
}
