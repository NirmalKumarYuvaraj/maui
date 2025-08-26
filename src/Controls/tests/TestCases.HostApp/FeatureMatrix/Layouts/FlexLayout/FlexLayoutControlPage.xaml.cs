namespace Maui.Controls.Sample;

public class FlexLayoutControlPage : NavigationPage
{
    public FlexLayoutControlPage()
    {
        PushAsync(new FlexLayoutControlMainPage());
    }
}

public partial class FlexLayoutControlMainPage : ContentPage
{
    public FlexLayoutViewModel _viewModel;

    public FlexLayoutControlMainPage()
    {
        InitializeComponent();
        BindingContext = _viewModel = new FlexLayoutViewModel();
        UpdateChildFlexProperties();
    }

    public async void NavigateToOptionsPage_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FlexLayoutOptionsPage(_viewModel));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateChildFlexProperties();
    }

    void UpdateChildFlexProperties()
    {
        // Update child properties based on test scenarios
        var children = TestFlexLayout.Children;
        if (children.Count >= 5)
        {
            // Set different Grow values for testing
            Microsoft.Maui.Controls.FlexLayout.SetGrow((BindableObject)children[0], _viewModel.Child1Grow);
            Microsoft.Maui.Controls.FlexLayout.SetGrow((BindableObject)children[1], _viewModel.Child2Grow);
            Microsoft.Maui.Controls.FlexLayout.SetGrow((BindableObject)children[2], _viewModel.Child3Grow);

            // Set different Shrink values for testing
            Microsoft.Maui.Controls.FlexLayout.SetShrink((BindableObject)children[0], _viewModel.Child1Shrink);
            Microsoft.Maui.Controls.FlexLayout.SetShrink((BindableObject)children[1], _viewModel.Child2Shrink);
            Microsoft.Maui.Controls.FlexLayout.SetShrink((BindableObject)children[2], _viewModel.Child3Shrink);

            // Set different Order values for testing
            Microsoft.Maui.Controls.FlexLayout.SetOrder((BindableObject)children[0], _viewModel.Child1Order);
            Microsoft.Maui.Controls.FlexLayout.SetOrder((BindableObject)children[1], _viewModel.Child2Order);
            Microsoft.Maui.Controls.FlexLayout.SetOrder((BindableObject)children[2], _viewModel.Child3Order);

            // Set different AlignSelf values for testing
            Microsoft.Maui.Controls.FlexLayout.SetAlignSelf((BindableObject)children[0], _viewModel.Child1AlignSelf);
            Microsoft.Maui.Controls.FlexLayout.SetAlignSelf((BindableObject)children[1], _viewModel.Child2AlignSelf);
            Microsoft.Maui.Controls.FlexLayout.SetAlignSelf((BindableObject)children[2], _viewModel.Child3AlignSelf);

            // Set different Basis values for testing
            Microsoft.Maui.Controls.FlexLayout.SetBasis((BindableObject)children[0], _viewModel.Child1Basis);
            Microsoft.Maui.Controls.FlexLayout.SetBasis((BindableObject)children[1], _viewModel.Child2Basis);
            Microsoft.Maui.Controls.FlexLayout.SetBasis((BindableObject)children[2], _viewModel.Child3Basis);
        }
    }
}
