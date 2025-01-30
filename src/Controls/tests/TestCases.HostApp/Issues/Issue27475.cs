namespace Maui.Controls.Sample.Issues
{

	[Issue(IssueTracker.Github, 27475, "Thumb Image Disappears in Slider Control When Changing Thumb Color on iOS and Catalyst",
		PlatformAffected.iOS | PlatformAffected.macOS)]
	public class Issue27475 : TestContentPage
	{

		protected override void Init()
		{
			VerticalStackLayout rootLayout = new VerticalStackLayout();

			Slider slider = new Slider() { AutomationId = "slider", ThumbImageSource = "coffee.png" };
			Button button = new Button() { Text = "Change Thumb Color", AutomationId = "ChangeThumbColorButton" };
			button.Clicked += (s, e) => slider.ThumbColor = Colors.Red;
			rootLayout.Spacing = 15;
			rootLayout.Children.Add(slider);
			rootLayout.Children.Add(button);
		}
	}
}