using Microsoft.Maui.Hosting;

namespace Maui.Controls.Sample;

internal static class NavigationHarnessRegistration
{
	public static void Configure(IMauiHandlersCollection handlers)
	{
#if ANDROID
		handlers.AddHandler<NavigationItemA, NavigationItemHandlerA>();
		handlers.AddHandler<NavigationItemB, NavigationItemHandlerB>();
#endif
	}
}

public sealed class NavigationItemA : FlyoutItem { }
public sealed class NavigationItemB : FlyoutItem { }

#if ANDROID
public sealed class NavigationItemHandlerA : Microsoft.Maui.Controls.Handlers.ShellItemHandler { }
public sealed class NavigationItemHandlerB : Microsoft.Maui.Controls.Handlers.ShellItemHandler { }
#endif
