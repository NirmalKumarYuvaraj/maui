namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33065, "CollectionView scrolling is jittery when ItemTemplate contains Label with TextType=Html", PlatformAffected.iOS)]
public class Issue33065 : ContentPage
{
	public Issue33065()
	{
		var items = Enumerable.Range(0, 20).Select(i => new HtmlCollectionItem(i)).ToList();

		var template = new DataTemplate(() =>
		{
			var stack = new VerticalStackLayout
			{
				Padding = new Thickness(12),
				Spacing = 6,
				BackgroundColor = Colors.White
			};

			var titleLabel = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
			titleLabel.SetBinding(Label.TextProperty, "Title");

			// The HTML label is the element that causes jitter — bind its AutomationId
			// so we can measure its height before and after scrolling.
			var htmlLabel = new Label { FontSize = 14, TextType = TextType.Html };
			htmlLabel.SetBinding(Label.TextProperty, "HtmlContent");
			htmlLabel.SetBinding(Label.AutomationIdProperty, "AutomationId");

			stack.Children.Add(titleLabel);
			stack.Children.Add(htmlLabel);

			return stack;
		});

		var collectionView = new CollectionView
		{
			ItemsSource = items,
			ItemTemplate = template,
			AutomationId = "TestCollectionView"
		};

		Content = collectionView;
	}

	class HtmlCollectionItem
	{
		public string AutomationId { get; }
		public string Title { get; }
		public string HtmlContent { get; }

		public HtmlCollectionItem(int index)
		{
			AutomationId = $"Item_{index}";
			Title = $"{index + 1}. Sample Item";
			HtmlContent = $"This item has <b>bold</b> and <i>italic</i> content. " +
				$"Visit <a href='https://learn.microsoft.com'>Microsoft Docs</a> for more info about item {index + 1}.";
		}
	}
}
