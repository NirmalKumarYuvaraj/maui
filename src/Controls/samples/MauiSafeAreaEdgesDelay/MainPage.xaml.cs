namespace MauiSafeAreaEdgesDelay;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	private void OnLoaded(object? sender, EventArgs e) => OnLoaded_Platform();
}
