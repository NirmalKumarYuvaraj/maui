namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void Button_OnClicked(object? sender, EventArgs e)
	{
		try
		{
			Shell.Current.GoToAsync(nameof(ModalBottomSheetPage));
		}
		catch (Exception exception)
		{
			Console.WriteLine(exception);
			throw;
		}
	}
}