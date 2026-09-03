namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	async void OnLaunchReproClicked(object? sender, EventArgs e)
	{
		var repro = new Issue37892View();
		var reproPage = new ContentPage
		{
			Content = repro
		};

		Console.WriteLine("SANDBOX: ISSUE37892 launching nested NavigationPage repro");
		await Navigation.PushAsync(new NavigationPage(reproPage));
	}
}