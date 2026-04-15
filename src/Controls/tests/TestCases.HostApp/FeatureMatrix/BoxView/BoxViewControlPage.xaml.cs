using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample;

public class BoxViewControlPage : NavigationPage
{
	private BoxViewViewModel _viewModel;

	public BoxViewControlPage()
	{
		_viewModel = new BoxViewViewModel();
		PushAsync(new BoxViewControlMainPage(_viewModel));
	}
}

public partial class BoxViewControlMainPage : ContentPage
{
	private BoxViewViewModel _viewModel;

	public BoxViewControlMainPage(BoxViewViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	private async void NavigateToOptionsPage_Clicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new BoxViewOptionsPage(_viewModel));
	}
}
