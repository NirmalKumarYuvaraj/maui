namespace Maui.Controls.Sample;

public partial class EdgeToEdgeTestPageForShell : ContentPage
{
    public EdgeToEdgeTestPageForShell()
    {
        InitializeComponent();

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

    private void OnResetClicked(object sender, EventArgs e)
    {
        var window = Application.Current?.Windows?.FirstOrDefault();
        if (window != null)
        {
            window.Page = new NavigationPage(new MainPage());
        }
    }
}
