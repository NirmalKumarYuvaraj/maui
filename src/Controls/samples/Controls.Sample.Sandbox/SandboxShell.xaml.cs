namespace Maui.Controls.Sample;

public partial class SandboxShell : Shell
{
	public SandboxShell()
	{
		Navigating += (_, args) => FlyoutPerf.BeginNavigation(args);
		Navigated += (_, args) => FlyoutPerf.Write("Navigated", CurrentPage?.Title ?? "Main",
			detail: $"location={args.Current.Location} source={args.Source} milestone=managed-not-rendered");
		PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(FlyoutIsPresented))
			{
				FlyoutPerf.Write(FlyoutIsPresented ? "FlyoutOpenRequestedOrSynced" : "FlyoutCloseRequestedOrSynced",
					CurrentPage?.Title ?? "Main");
			}
		};
		HandlerChanging += (_, _) => DisconnectDrawerTiming();
		HandlerChanged += (_, _) => ConnectDrawerTiming();
		InitializeComponent();
	}

	partial void ConnectDrawerTiming();
	partial void DisconnectDrawerTiming();
}