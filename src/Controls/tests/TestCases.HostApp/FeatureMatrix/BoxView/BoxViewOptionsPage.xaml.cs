using System.Globalization;

namespace Maui.Controls.Sample;

public partial class BoxViewOptionsPage : ContentPage
{
	private BoxViewViewModel _viewModel;

	public BoxViewOptionsPage(BoxViewViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	private void ApplyButton_Clicked(object sender, EventArgs e)
	{
		Navigation.PopAsync();
	}

	private void OnCornerRadiusEntryChanged(object sender, TextChangedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(e.NewTextValue))
			return;

		var parts = e.NewTextValue.Split(',');

		if (parts.Length == 1)
		{
			if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double uniform))
				_viewModel.CornerRadius = new CornerRadius(uniform);
		}
		else if (parts.Length == 4)
		{
			if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double topLeft) &&
				double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double topRight) &&
				double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double bottomLeft) &&
				double.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double bottomRight))
			{
				_viewModel.CornerRadius = new CornerRadius(topLeft, topRight, bottomLeft, bottomRight);
			}
		}
	}

	private void OnResetChangesClicked(object sender, EventArgs e)
	{
		_viewModel.Reset();
	}

	private async void NavigateToOptionsPage_Clicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new BoxViewOptionsPage(_viewModel));
	}
}