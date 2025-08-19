namespace Maui.Controls.Sample;

public partial class EdgeToEdgeTestPage : ContentPage
{
    public EdgeToEdgeTestPage(string scenarioTitle)
    {
        InitializeComponent();
        ScenarioTitle.Text = scenarioTitle;
        UpdateSystemInfo();
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

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
    }

    private async void OnCloseModalClicked(object sender, EventArgs e)
    {
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
        }
    }
}
