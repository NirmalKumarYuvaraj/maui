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
		UpdateHostedControl();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		UpdateHostedControl();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
	}

	private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewViewModel.SelectedControlType))
		{
			UpdateHostedControl();
		}
	}

	private void UpdateHostedControl()
	{
		TransformableFrame.Content = CreateControl(_viewModel.SelectedControlType);
	}

	private static View CreateControl(string controlType)
	{
		return controlType switch
		{
			"BoxView" => new BoxView { Color = Colors.Blue, WidthRequest = 120, HeightRequest = 120 },
			"Button" => new Button { Text = "Test Button", BackgroundColor = Colors.LightBlue, WidthRequest = 120, HeightRequest = 50 },
			"ImageButton" => new ImageButton { Source = "dotnet_bot.png", WidthRequest = 120, HeightRequest = 120 },
			"Entry" => new Entry { Placeholder = "Test Entry", HeightRequest = 50 },
			"Editor" => new Editor { Placeholder = "Test Editor", HeightRequest = 120 },
			"SearchBar" => new SearchBar { Placeholder = "Test SearchBar", HeightRequest = 120 },
			"CheckBox" => new CheckBox { IsChecked = true },
			"RadioButton" => new RadioButton { Content = "Test RadioButton", IsChecked = true },
			"Switch" => new Switch { IsToggled = true },
			"Slider" => new Slider { Minimum = 0, Maximum = 100, Value = 50, WidthRequest = 120 },
			"Stepper" => new Stepper { Minimum = 0, Maximum = 100, Value = 50 },
			"Picker" => new Picker { Title = "Test Picker", ItemsSource = new[] { "Item 1", "Item 2", "Item 3" } },
			"DatePicker" => new DatePicker() { Date = new DateTime(2024, 1, 1) },
			"TimePicker" => new TimePicker() { Time = new TimeSpan(12, 0, 0) },
			"ProgressBar" => new ProgressBar { Progress = 0.5, WidthRequest = 120 },
			"ActivityIndicator" => new ActivityIndicator { IsRunning = true, HeightRequest = 120 },
			"Image" => new Image { Source = "dotnet_bot.png", WidthRequest = 120, HeightRequest = 120 },
			"Border" => new Border
			{
				Content = new Label { Text = "Border" },
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
				Padding = 20
			},
			"WebView" => new WebView { HeightRequest = 120, WidthRequest = 120 },
			"GraphicsView" => new GraphicsView { HeightRequest = 120, WidthRequest = 120, BackgroundColor = Colors.LightGray },
			_ => new Label { Text = "Transform Me!" },
		};
	}

	private async void NavigateToOptionsPage_Clicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new ViewOptionsPage(_viewModel));
	}
}
