using Microsoft.Extensions.Logging;

namespace MauiSafeAreaEdgesDelay;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

#if ANDROID
		if (OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			builder.Services.AddMauiBlazorWebView();
		}
#else
		builder.Services.AddMauiBlazorWebView();
#endif

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
