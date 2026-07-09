namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	private static readonly List<ControlGroup> Controls =
	[
		new("Controls",
		[
			"Button",
			"CheckBox",
			"DatePicker",
			"Editor",
			"Entry",
			"Image",
			"Label",
			"Picker",
			"ProgressBar",
			"RadioButton",
			"SearchBar",
			"Slider",
			"Stepper",
			"Switch",
			"TimePicker",
			"WebView"
		]),

		new("Layouts",
		[
			"AbsoluteLayout",
			"Grid",
			"HorizontalStackLayout",
			"VerticalStackLayout",
			"FlexLayout",
			"ContentView",
			"ScrollView"
		])
	];

	public MainPage()
	{
		InitializeComponent();

		var collectionView = new CollectionView
		{
			ItemsSource = Controls,
			IsGrouped = true,
			SelectionMode = SelectionMode.Single,

			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");

				return new StackLayout
				{
					Padding = 10,
					Children = { label }
				};
			}),

			GroupHeaderTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					FontAttributes = FontAttributes.Bold,
					Margin = new Thickness(10, 0)
				};

				label.SetBinding(Label.TextProperty, nameof(ControlGroup.Name));

				return new StackLayout
				{
					BackgroundColor = Colors.LightGray,
					Children = { label }
				};
			})
		};

		collectionView.SelectionChanged += async (_, e) =>
		{
			if (e.CurrentSelection.FirstOrDefault() is not string control)
				return;

			collectionView.SelectedItem = null;

			await Navigation.PushAsync(new TestPage(control));
		};

		Content = collectionView;
	}
}

public class ControlGroup(string name, IEnumerable<string> controls)
	: List<string>(controls)
{
	public string Name { get; } = name;
}

public class TestPage : ContentPage
{
	private static readonly Dictionary<string, Func<View>> ViewFactory = new()
	{
		["Button"] = () => new Button { Text = "Click Me", HeightRequest = 50, WidthRequest = 150 },

		["CheckBox"] = () => new CheckBox
		{
			IsChecked = true,
			HeightRequest = 50,
			WidthRequest = 150
		},

		["DatePicker"] = () => new DatePicker()
		{
			HeightRequest = 50,
			WidthRequest = 150
		},

		["Editor"] = () => new Editor
		{
			Text = "Enter text here...",
			HeightRequest = 100,
			WidthRequest = 150
		},

		["Entry"] = () => new Entry
		{
			Placeholder = "Enter text",
			HeightRequest = 100,
			WidthRequest = 150
		},

		["Image"] = () => new Image
		{
			Source = "dotnet_bot.png",
			HeightRequest = 100,
			WidthRequest = 150
		},

		["Label"] = () => new Label
		{
			Text = "This is a label",
			HeightRequest = 100,
			WidthRequest = 150
		},

		["Picker"] = () => new Picker
		{
			ItemsSource = new List<string>
			{
				"Option 1",
				"Option 2",
				"Option 3"
			},
			HeightRequest = 100,
			WidthRequest = 150
		},

		["ProgressBar"] = () => new ProgressBar
		{
			Progress = 0.5,
			HeightRequest = 100,
			WidthRequest = 150
		},

		["RadioButton"] = () => new RadioButton
		{
			Content = "Option A",
			HeightRequest = 100,
			WidthRequest = 150
		},

		["SearchBar"] = () => new SearchBar
		{
			Placeholder = "Search...",
			HeightRequest = 100,
			WidthRequest = 150
		},

		["Slider"] = () => new Slider
		{
			Minimum = 0,
			Maximum = 100,
			Value = 50,
			HeightRequest = 100,
			WidthRequest = 150
		},

		["Stepper"] = () => new Stepper
		{
			Minimum = 0,
			Maximum = 10,
			Value = 5,
			HeightRequest = 100,
			WidthRequest = 150
		},

		["Switch"] = () => new Switch
		{
			IsToggled = true,
			HeightRequest = 100,
			WidthRequest = 150
		},

		["TimePicker"] = () => new TimePicker()
		{
			HeightRequest = 100,
			WidthRequest = 150
		},

		["WebView"] = () => new WebView
		{
			Source = "https://dotnet.microsoft.com/",
			HeightRequest = 100,
			WidthRequest = 150
		},

		["AbsoluteLayout"] = () => new AbsoluteLayout(),

		["Grid"] = () => new Grid(),

		["HorizontalStackLayout"] = () => new HorizontalStackLayout(),

		["VerticalStackLayout"] = () => new VerticalStackLayout(),

		["FlexLayout"] = () => new FlexLayout(),

		["ContentView"] = () => new ContentView(),

		["ScrollView"] = () => new ScrollView()
	};

	public TestPage(string controlName)
	{
		var view = ViewFactory.TryGetValue(controlName, out var factory)
			? factory()
			: new Label
			{
				Text = $"No control found for '{controlName}'"
			};

		view.Background = new ImageBrush
		{
			ImageSource = "dotnet_bot.png"
		};

		var view2 = ViewFactory.TryGetValue(controlName, out var factory2)
			? factory2()
			: new Label
			{
				Text = $"No control found for '{controlName}'"
			};

		view2.Background = Colors.Red;

		var stackLayout = new StackLayout
		{
			Spacing = 10,
			Children =
				{
					new Label
					{
						Text = $"Normal Color {controlName}",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold,
						Margin = new Thickness(0, 20, 0, 10),
						HorizontalOptions = LayoutOptions.Center
					},
					view2,
					new Label
					{
						Text = $"ImageBrush {controlName}",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold,
						Margin = new Thickness(0, 20, 0, 10),
						HorizontalOptions = LayoutOptions.Center
					},
					view
				}
		};
		Content = stackLayout;
	}
}