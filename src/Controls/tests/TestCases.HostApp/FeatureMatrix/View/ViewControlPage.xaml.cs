namespace Maui.Controls.Sample;

public partial class ViewControlPage : NavigationPage
{
	private readonly ViewViewModel _viewModel;

	public ViewControlPage()
	{
		_viewModel = new ViewViewModel();
		PushAsync(new ViewControlMainPage(_viewModel));
	}
}

public partial class ViewControlMainPage : ContentPage
{
	private readonly ViewViewModel _viewModel;

	public ViewControlMainPage(ViewViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	private async void NavigateToOptionsPage_Clicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new ViewOptionsPage(_viewModel));
	}
}
