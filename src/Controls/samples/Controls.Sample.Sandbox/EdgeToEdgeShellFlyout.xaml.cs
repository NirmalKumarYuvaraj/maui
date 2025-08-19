namespace Maui.Controls.Sample;

public partial class EdgeToEdgeShellFlyout : Shell
{
    public EdgeToEdgeShellFlyout()
    {
        InitializeComponent();
    }

    private void OnResetToMainClicked(object sender, EventArgs e)
    {
        OnResetToMain();
    }

    private void OnResetToMain()
    {
        var window = Application.Current?.Windows?.FirstOrDefault();
        if (window != null)
        {
            window.Page = new NavigationPage(new MainPage());
        }
    }
}
