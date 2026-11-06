using Microsoft.Maui.Hosting;

namespace Maui.Controls.Sample;

internal static class NavigationHarnessRegistration
{
#if SANDBOX_LEGACY_SHELL
	public static bool UsesLegacy => true;
#else
	public static bool UsesLegacy => false;
#endif

	public static void Configure(IMauiHandlersCollection handlers)
	{
#if ANDROID
#if !SANDBOX_LEGACY_SHELL
		handlers.AddHandler<NavigationItemA, NavigationItemHandlerA>();
		handlers.AddHandler<NavigationItemB, NavigationItemHandlerB>();
#endif
#endif
	}
}

public sealed class NavigationItemA : FlyoutItem { }
public sealed class NavigationItemB : FlyoutItem { }

#if ANDROID
public sealed class NavigationItemHandlerA : Microsoft.Maui.Controls.Handlers.ShellItemHandler { }
public sealed class NavigationItemHandlerB : Microsoft.Maui.Controls.Handlers.ShellItemHandler { }
#endif
