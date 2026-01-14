namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 9794, "[iOS] Tabbar Disappears with linker",
		PlatformAffected.iOS)]
	public class Issue9794 : TestShell
	{
		protected override void Init()
		{
			var page1 = AddBottomTab("tab1");
			AddBottomTab("tab2");
			var scrollView = new ScrollView();
			var stackLayout = new StackLayout()
			{
				Children =
				{
					new Label()
					{
						Text = "Push a page, click back button, and then click between the tabs. If the tab bar disappears the test has failed.",
					},
					new Button()
					{
						Text = "Push Page",
						AutomationId = "GoForward",
						Command = new Command(async () =>
						{
							await Navigation.PushAsync(new Issue9794Modal());
						})
					},
					new Label()
					{
						Text = "PlaceHolder to increase Page",
						HeightRequest = 600
					},
					new Entry()
				}
			};

			scrollView.Content = stackLayout;
			page1.Content = scrollView;
		}

		public class Issue9794Modal : ContentPage
		{
			public Issue9794Modal()
			{
				Shell.SetTabBarIsVisible(this, false);
				Content = new StackLayout()
				{
					Children =
					{
						new Label()
						{
							Text = "Click Back Button"
						}
					}
				};
			}
		}
	}
}
