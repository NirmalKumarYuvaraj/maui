using System.Globalization;

namespace Maui.Controls.Sample
{
	public partial class ViewOptionsPage : ContentPage
	{
		private ViewViewModel _viewModel;

		public ViewOptionsPage(ViewViewModel viewModel)
		{
			InitializeComponent();
			_viewModel = viewModel;
			BindingContext = _viewModel;
		}

		private void ApplyButton_Clicked(object sender, EventArgs e)
		{
			Navigation.PopAsync();
		}

		private void OnHorizontalOptionChanged(object sender, CheckedChangedEventArgs e)
		{
			if (e.Value && sender is RadioButton rb)
			{
				_viewModel.SelectedHorizontalOption = rb.Content?.ToString();
			}
		}

		private void OnVerticalOptionChanged(object sender, CheckedChangedEventArgs e)
		{
			if (e.Value && sender is RadioButton rb)
			{
				_viewModel.SelectedVerticalOption = rb.Content?.ToString();
			}
		}

		private void OnBackgroundColorChanged(object sender, CheckedChangedEventArgs e)
		{
			if (e.Value && sender is RadioButton rb)
			{
				_viewModel.SelectedBackgroundColor = rb.Content?.ToString();
			}
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

		private async void NavigateToVisualOptions_Clicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new ViewVisualOptionsPage(_viewModel));
		}

		private void OnMarginEntryChanged(object sender, TextChangedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(e.NewTextValue))
			{
				return;
			}

			if (double.TryParse(e.NewTextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
			{
				_viewModel.Margin = new Thickness(value);
			}
		}
	}

	public class StringMatchConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value?.ToString() == parameter?.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((bool)value)
			{
				return parameter?.ToString();
			}

			return Binding.DoNothing;
		}
	}
}
