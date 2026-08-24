namespace Maui.Controls.Sample;

public partial class SandboxShell : Shell
{
	public SandboxShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(ModalBottomSheetPage), typeof(ModalBottomSheetPage));
	}
}
