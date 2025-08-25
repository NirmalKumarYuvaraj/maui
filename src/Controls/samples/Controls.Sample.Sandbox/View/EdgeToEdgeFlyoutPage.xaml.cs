namespace Maui.Controls.Sample;

public partial class EdgeToEdgeFlyoutPage : FlyoutPage
{
    public EdgeToEdgeFlyoutPage()
    {
        InitializeComponent();
        Detail = new NavigationPage(new EdgeToEdgeTestPage("Flyout Page - Detail"));
    }

    private void OnPage1Clicked(object sender, EventArgs e)
    {
        Detail = new NavigationPage(new EdgeToEdgeTestPage("Flyout Page - Page 1"));
        IsPresented = false;
    }

    private void OnPage2Clicked(object sender, EventArgs e)
    {
        Detail = new NavigationPage(new EdgeToEdgeTestPage("Flyout Page - Page 2"));
        IsPresented = false;
    }

    private void OnPage3Clicked(object sender, EventArgs e)
    {
        Detail = new NavigationPage(new EdgeToEdgeTestPage("Flyout Page - Page 3"));
        IsPresented = false;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
