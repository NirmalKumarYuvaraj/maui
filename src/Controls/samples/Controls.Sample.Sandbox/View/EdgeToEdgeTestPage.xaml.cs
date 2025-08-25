namespace Maui.Controls.Sample;

public partial class EdgeToEdgeTestPage : ContentPage
{
    public EdgeToEdgeTestPage(string scenarioTitle)
    {
        InitializeComponent();
        ScenarioTitle.Text = scenarioTitle;
    }



    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnCloseModalClicked(object sender, EventArgs e)
    {
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
        }
    }
}
