using Microsoft.Maui.ManualTests.Views;

namespace Microsoft.Maui.ManualTests;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState activationState)
	{
		// To test shell scenarios, change this to true
		bool useShell = false;

		if (!useShell)
		{
			return new Window(new NavigationPage(new HorizontalGridPage()));
		}
		else
		{
			return new Window(new AppShell());
		}
	}
}
