namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33065, "CollectionView scrolling is jittery when ItemTemplate contains Label with TextType=Html", PlatformAffected.iOS)]
public class Issue33065 : ContentPage
{
	List<DemoListItem> ItemSource { get; set; }
	public Issue33065()
	{
		CreateItemsource();
		BackgroundColor = Colors.LightGray;

		var template = new DataTemplate(() =>
		{
			var grid = new Grid
			{
				Padding = new Thickness(16),
			};

			var border = new Border
			{
				BackgroundColor = Colors.White,
				Padding = new Thickness(16)
			};

			var stack = new VerticalStackLayout
			{
				Spacing = 16
			};

			var titleLabel = new Label { FontSize = 22 };
			titleLabel.SetBinding(Label.TextProperty, "Title");

			var descriptionLabel = new Label { FontSize = 18, TextType = TextType.Html };
			descriptionLabel.SetBinding(Label.TextProperty, "Description");

			stack.Children.Add(titleLabel);
			stack.Children.Add(descriptionLabel);

			border.Content = stack;
			grid.Children.Add(border);
			return grid;
		});

		var collectionView = new CollectionView
		{
			ItemsSource = ItemSource,
			ItemTemplate = template,
			AutomationId = "TestCollectionView"
		};

		Content = collectionView;
	}

	void CreateItemsource()
	{
		ItemSource =
		[
			new DemoListItem("1. Official Documentation",
				"""
                Read the <a href="https://learn.microsoft.com/dotnet/maui">official .NET MAUI documentation</a> to get started.
                """),


			new DemoListItem("2. Text Formatting",
				"""
                This description demonstrates <b>bold text</b>, <i>italic text</i>, and <u>underlined text</u> support.
                """),


			new DemoListItem("3. Google Search",
				"""
                Need to find something? Try <a href="https://www.google.com">Google</a> for quick answers.
                """),


			new DemoListItem("4. MVVM Pattern",
				"""
                Learn about the <strong style="color:blue;">Model-View-ViewModel</strong> pattern used in XAML frameworks.
                """),


			new DemoListItem("5. Line Breaks",
				"""
                This item has a long description.<br/>
                It is split into multiple lines<br/>
                using the HTML break tag.
                """),


			new DemoListItem("6. GitHub Repository",
				"""
                Check out the source code on <a href="https://github.com/dotnet/maui">GitHub</a> to contribute.
                """),


			new DemoListItem("7. Stack Overflow",
				"""
                Stuck on a bug? <a href="https://stackoverflow.com/questions/tagged/maui">Ask the community</a> for help.
                """),


			new DemoListItem("8. C# Features",
				"""
                This demo uses <code>Primary Constructors</code> and <code>Raw String Literals</code>.
                """),


			new DemoListItem("9. YouTube Tutorial",
				"""
                Watch this <a href="https://www.youtube.com/watch?v=DuNLR_NJv8U">beginner's guide</a> on YouTube.
                """),


			new DemoListItem("10. Markdown Style",
				"""
                <h2>Headings</h2> are sometimes supported depending on the renderer, but basic <small>small text</small> works well.
                """),


			new DemoListItem("11. Color Styling",
				"""
                You can use inline styles to make text <span style="color: red;">Red</span> or <span style="color: green;">Green</span>.
                """),


			new DemoListItem("12. XAML Overview",
				"""
                XAML allows you to define user interfaces. <a href="https://learn.microsoft.com/dotnet/desktop/wpf/xaml/">Learn more here</a>.
                """),


			new DemoListItem("13. Planet Earth",
				"""
                Wikipedia entry for <a href="https://en.wikipedia.org/wiki/Earth">Earth</a>. Click to read history.
                """),


			new DemoListItem("14. NuGet Packages",
				"""
                Manage your dependencies via <a href="https://www.nuget.org">NuGet.org</a>.
                """),


			new DemoListItem("15. Complex Links",
				"""
                Please review our <a href="https://example.com/terms">Terms of Service</a> and <a href="https://example.com/privacy">Privacy Policy</a>.
                """),


			new DemoListItem("16. Email Link",
				"""
                Contact support via email: <a href="mailto:support@example.com">support@example.com</a>.
                """),


			new DemoListItem("17. Phone Link",
				"""
                Call us directly at <a href="tel:+15550199">555-0199</a> (Works on mobile devices).
                """),


			new DemoListItem("18. List in HTML",
				"""
                Here is a mini list:
                <br/>• Item A
                <br/>• Item B
                <br/>• Item C
                """),


			new DemoListItem("19. .NET Blog",
				"""
                Stay updated with the <a href="https://devblogs.microsoft.com/dotnet/">.NET Blog</a> for the latest news.
                """),


			new DemoListItem("20. Final Item",
				"""
                Thanks for scrolling! Visit <a href="https://dotnet.microsoft.com">dot.net</a> to download the SDK.
                """)
		];
	}



	class DemoListItem(string title, string description)
	{
		public string Title { get; set; } = title;

		public string Description { get; set; } = description;
	}
}
