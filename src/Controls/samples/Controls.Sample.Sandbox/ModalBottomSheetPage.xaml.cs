namespace Maui.Controls.Sample;

public partial class ModalBottomSheetPage : ContentPage
{
    public ModalBottomSheetPage()
    {
        InitializeComponent();
    }

    async void OnDismissRequested(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}