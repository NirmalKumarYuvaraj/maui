namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 12376, "UriImageSource never refreshes after CacheValidity elapses", PlatformAffected.Android | PlatformAffected.iOS, isInternetRequired: true)]
	public class Issue12376 : ContentPage
	{
		readonly Image _image;

		public Issue12376()
		{
			_image = new Image
			{
				AutomationId = "CachedImage",
				HeightRequest = 200,
				WidthRequest = 200,
				Source = CreateSource(),
			};

			var reload = new Button
			{
				AutomationId = "ReloadButton",
				Text = "Reload",
			};
			reload.Clicked += (_, __) => _image.Source = CreateSource();

			Content = new VerticalStackLayout { Children = { _image, reload } };
		}

		static UriImageSource CreateSource() => new UriImageSource
		{
			Uri = new Uri("https://picsum.photos/200"),
			CachingEnabled = true,
			CacheValidity = TimeSpan.FromSeconds(5),
		};
	}
}
