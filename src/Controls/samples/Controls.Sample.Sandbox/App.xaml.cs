namespace Maui.Controls.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// To test shell scenarios, change this to true
		bool useShell = false;

		if (!useShell)
		{
			var navPage = new NavigationPage(new MainPage())
			{
				BarBackgroundColor = Colors.Transparent // Test edge-to-edge with transparent toolbar
			};
			return new Window(navPage);
		}
		else
		{
			return new Window(new SandboxShell());
		}
	}
}
