namespace Maui.Controls.Sample;

public partial class EdgeToEdgeTestPageForShell : ContentPage
{
    public EdgeToEdgeTestPageForShell()
    {
        InitializeComponent();
        UpdateSystemInfo();

        // Set title based on shell context
        if (Shell.Current != null)
        {
            var currentShell = Shell.Current.GetType().Name;
            var currentItem = Shell.Current.CurrentItem?.Title ?? "Unknown";
            ScenarioTitle.Text = $"Shell Test: {currentShell} - {currentItem}";
        }
        else
        {
            ScenarioTitle.Text = "Shell Test Page";
        }
    }
    private void UpdateSystemInfo()
    {
        var deviceInfo = DeviceInfo.Current;
        var displayInfo = DeviceDisplay.Current;

        SystemInfoLabel.Text = $"Platform: {deviceInfo.Platform}\n" +
                              $"Version: {deviceInfo.VersionString}\n" +
                              $"Screen: {displayInfo.MainDisplayInfo.Width}x{displayInfo.MainDisplayInfo.Height}\n" +
                              $"Density: {displayInfo.MainDisplayInfo.Density}\n" +
                              $"Orientation: {displayInfo.MainDisplayInfo.Orientation}";
    }

    private async void OnTopButtonClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Button Test", "Top button was clicked successfully!", "OK");
    }

    private async void OnBottomButtonClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Button Test", "Bottom button was clicked successfully!", "OK");
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        var window = Application.Current?.Windows?.FirstOrDefault();
        if (window != null)
        {
            window.Page = new NavigationPage(new MainPage());
        }
    }
}
