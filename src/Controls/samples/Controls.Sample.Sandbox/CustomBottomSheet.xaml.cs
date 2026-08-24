namespace Maui.Controls.Sample;

public partial class CustomBottomSheet : ContentView
{
    public CustomBottomSheet()
    {
        InitializeComponent();
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        if (GetTemplateChild("Border") is View border)
        {
            await border.TranslateToAsync(border.X, border.Height, 300, Easing.CubicIn);
        }
        await Shell.Current.GoToAsync("..");
    }

    protected override async void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (GetTemplateChild("Border") is not View border)
            return;
        border.TranslationY = border.Measure(double.PositiveInfinity, double.PositiveInfinity).Height;
        border.IsVisible = true;
        await border.TranslateToAsync(border.X, 0, 300, Easing.CubicIn);
    }
}