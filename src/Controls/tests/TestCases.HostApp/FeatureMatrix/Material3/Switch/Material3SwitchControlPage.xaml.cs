namespace Maui.Controls.Sample;

// Material3 Switch uses MaterialSwitch as its native Android view (vs SwitchCompat for Material2).
// This separate HostApp page ensures Material3 Switch gets its own visual test baseline,
// since the native control renders differently from the Material2 version.
public class Material3SwitchControlPage : NavigationPage
{
    private Material3SwitchViewModel _viewModel;
    public Material3SwitchControlPage()
    {
        _viewModel = new Material3SwitchViewModel();
        PushAsync(new Material3SwitchControlMainPage(_viewModel));
    }
}

public partial class Material3SwitchControlMainPage : ContentPage
{
    private Material3SwitchViewModel _viewModel;

    public Material3SwitchControlMainPage(Material3SwitchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void NavigateToOptionsPage_Clicked(object sender, EventArgs e)
    {
        BindingContext = _viewModel = new Material3SwitchViewModel();
        ReinitializeSwitch();
        await Navigation.PushAsync(new Material3SwitchOptionsPage(_viewModel));
    }

    private void Switch_Toggled(object sender, ToggledEventArgs e)
    {
        EventLabel.Text = $"{e.Value}";
    }

    private void ReinitializeSwitch()
    {
        SwitchGrid.Children.Clear();
        var switchControl = new Switch
        {
            AutomationId = "SwitchControl",
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        switchControl.Toggled += Switch_Toggled;

        switchControl.SetBinding(Switch.FlowDirectionProperty, new Binding(nameof(Material3SwitchViewModel.FlowDirection)));
        switchControl.SetBinding(Switch.IsEnabledProperty, new Binding(nameof(Material3SwitchViewModel.IsEnabled)));
        switchControl.SetBinding(Switch.IsVisibleProperty, new Binding(nameof(Material3SwitchViewModel.IsVisible)));
        switchControl.SetBinding(Switch.IsToggledProperty, new Binding(nameof(Material3SwitchViewModel.IsToggled)));
        switchControl.SetBinding(Switch.OnColorProperty, new Binding(nameof(Material3SwitchViewModel.OnColor)));
        switchControl.SetBinding(Switch.ShadowProperty, new Binding(nameof(Material3SwitchViewModel.Shadow)));
        switchControl.SetBinding(Switch.ThumbColorProperty, new Binding(nameof(Material3SwitchViewModel.ThumbColor)));
        SwitchGrid.Children.Add(switchControl);
    }
}
