using System;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample;

public partial class GraphicsViewOptionsPage : ContentPage
{
	private GraphicsViewViewModel _viewModel;

	public GraphicsViewOptionsPage(GraphicsViewViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	private void ApplyButton_Clicked(object sender, EventArgs e)
	{
		Navigation.PopAsync();
	}

	private async void NavigateToDrawableOptionsButton_Clicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new DrawableOptionsPage(_viewModel));
	}

	private void OnDrawableChanged(object sender, CheckedChangedEventArgs e)
	{
		if (sender is RadioButton radioButton && Enum.TryParse(radioButton.Content.ToString(), out DrawableType selectedDrawable))
		{
			(_viewModel.Drawable as FeatureMartixDrawable)?.SetDrawableType(selectedDrawable);
		}
	}

	private void OnIsEnabledCheckedChanged(object sender, CheckedChangedEventArgs e)
	{
		//_viewModel.IsEnabled = IsEnabledTrueRadio.IsChecked;
	}

	private void OnIsVisibleCheckedChanged(object sender, CheckedChangedEventArgs e)
	{
		// if (IsVisibleTrueRadio.IsChecked)
		// 	_viewModel.IsVisible = true;
		// else
		// 	_viewModel.IsVisible = false;
	}

	private void OnShadowInputChanged(object sender, TextChangedEventArgs e)
	{
		// var input = ShadowInputEntry.Text;
		// var parts = input.Split(',');

		// // Ensure Shadow is initialized
		// if (_viewModel.Shadow == null)
		// {
		// 	_viewModel.Shadow = new Shadow();
		// }

		// if (parts.Length == 4 &&
		// 	double.TryParse(parts[0], out double offsetX) &&
		// 	double.TryParse(parts[1], out double offsetY) &&
		// 	double.TryParse(parts[2], out double radius) &&
		// 	double.TryParse(parts[3], out double opacity))
		// {
		// 	_viewModel.Shadow.Offset = new Point(offsetX, offsetY);
		// 	_viewModel.Shadow.Radius = (float)radius;
		// 	_viewModel.Shadow.Opacity = (float)opacity;
		// }
		// else
		// {
		// 	// Handle invalid input gracefully
		// 	_viewModel.Shadow.Offset = new Point(0, 0);
		// 	_viewModel.Shadow.Radius = 0;
		// 	_viewModel.Shadow.Opacity = 0;
		// }
	}
}
