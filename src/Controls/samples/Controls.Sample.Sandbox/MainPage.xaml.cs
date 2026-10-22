namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		Entry entry = new Entry() { Placeholder = "Enter text" };
		Button button = new Button() { Text = "Change Theme" };
		button.Clicked += (s, e) =>
		{
			Application.Current!.UserAppTheme = Application.Current.RequestedTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
		};

		Content = new StackLayout
		{
			Padding = new Thickness(20),
			Spacing = 10,
			Children =
			{
				entry,
				button

			}
		};
	}
}