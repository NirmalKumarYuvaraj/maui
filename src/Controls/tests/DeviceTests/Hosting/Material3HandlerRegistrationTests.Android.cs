using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(nameof(Material3HandlerRegistrationTests))]
	public class Material3HandlerRegistrationTests
	{
		const string Material3FeatureSwitch = "Microsoft.Maui.RuntimeFeature.IsMaterial3Enabled";

		[Fact]
		[Category(TestCategory.Application)]
		public void RegistersReplacementHandlersAndPreservesUserOverrides()
		{
			AppContext.TryGetSwitch(Material3FeatureSwitch, out bool originalValue);

			try
			{
				AppContext.SetSwitch(Material3FeatureSwitch, true);

				var defaultApp = MauiApp.CreateBuilder()
					.UseMauiApp<ApplicationStub>()
					.Build();
				var defaultHandlers = defaultApp.Services.GetRequiredService<IMauiHandlersFactory>();

				Assert.IsType<LabelHandler2>(defaultHandlers.GetHandler(typeof(Label)));
				Assert.IsType<ImageHandler2>(defaultHandlers.GetHandler(typeof(Image)));
				Assert.IsType<ActivityIndicatorHandler2>(defaultHandlers.GetHandler(typeof(ActivityIndicator)));
				Assert.IsType<ProgressBarHandler2>(defaultHandlers.GetHandler(typeof(ProgressBar)));
				Assert.IsType<ButtonHandler>(defaultHandlers.GetHandler(typeof(Button)));
				Assert.Same(ImageHandler.Mapper, ImageHandler2.Mapper);

				var customizedApp = MauiApp.CreateBuilder()
					.UseMauiApp<ApplicationStub>()
					.ConfigureMauiHandlers(handlers => handlers.AddHandler<Label, LabelHandler>())
					.Build();
				var customizedHandlers = customizedApp.Services.GetRequiredService<IMauiHandlersFactory>();

				Assert.IsType<LabelHandler>(customizedHandlers.GetHandler(typeof(Label)));
			}
			finally
			{
				AppContext.SetSwitch(Material3FeatureSwitch, originalValue);
			}
		}
	}

	[CollectionDefinition(nameof(Material3HandlerRegistrationTests), DisableParallelization = true)]
	public class Material3HandlerRegistrationTestCollection
	{
	}
}
