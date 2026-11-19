namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		VerticalStackLayout verticalStackLayout = new VerticalStackLayout();
		verticalStackLayout.Spacing = 10;
		verticalStackLayout.Padding = new Thickness(10);

		CheckBox checkBox = new CheckBox();
		checkBox.IsThreeState = true;
		verticalStackLayout.Children.Add(checkBox);

		Button button = new Button();
		button.Text = "Set IsChecked True";
		verticalStackLayout.Children.Add(button);
		button.Clicked += (s, e) =>
		{
			checkBox.IsChecked = true;
		};

		Button button2 = new Button();
		button2.Text = "Set IsChecked False";
		verticalStackLayout.Children.Add(button2);
		button2.Clicked += (s, e) =>
		{
			checkBox.IsChecked = false;
		};

		Button button3 = new Button();
		button3.Text = "set indeterminate";
		verticalStackLayout.Children.Add(button3);
		button3.Clicked += (s, e) =>
		{
			checkBox.IsChecked = null;
		};

		Button button4 = new Button();
		button4.Text = "Toggle IsChecked";
		verticalStackLayout.Children.Add(button4);
		button4.Clicked += (s, e) =>
		{
			checkBox.IsChecked = !checkBox.IsChecked;
		};

		Button button5 = new Button();
		button5.Text = "Toggle IsThreeState";
		verticalStackLayout.Children.Add(button5);

		Label label = new Label();
		label.Text = "IsThreeState: " + checkBox.IsThreeState;
		verticalStackLayout.Children.Add(label);
		button5.Clicked += (s, e) =>
		{
			checkBox.IsThreeState = !checkBox.IsThreeState;
			label.Text = "IsThreeState: " + checkBox.IsThreeState;
		};

		Content = verticalStackLayout;
	}
}