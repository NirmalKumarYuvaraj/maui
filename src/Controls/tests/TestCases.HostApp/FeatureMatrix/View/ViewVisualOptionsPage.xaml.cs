namespace Maui.Controls.Sample;

public partial class ViewVisualOptionsPage : ContentPage
{
    private ViewViewModel _viewModel;

    public ViewVisualOptionsPage(ViewViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void ApplyButton_Clicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private void OnFlowDirectionChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton rb)
        {
            _viewModel.FlowDirection = rb.Content?.ToString() switch
            {
                "LTR" => FlowDirection.LeftToRight,
                "RTL" => FlowDirection.RightToLeft,
                _ => FlowDirection.MatchParent,
            };
        }
    }
}
