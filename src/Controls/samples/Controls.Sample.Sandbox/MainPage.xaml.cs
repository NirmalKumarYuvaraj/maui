using System.ComponentModel;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	readonly GraphicsView graphicsView;
	readonly CustomBlendDrawable customBlendDrawable;
	readonly BlendModeViewModel blendModeViewModel;
	Label? _label;

	public MainPage()
	{
		InitializeComponent();

		graphicsView = new GraphicsView();

		blendModeViewModel = new BlendModeViewModel();
		BindingContext = blendModeViewModel;

		customBlendDrawable = new CustomBlendDrawable();
		graphicsView.Drawable = customBlendDrawable;

		blendModeViewModel.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(BlendModeViewModel.CustomBlendMode))
			{
				customBlendDrawable.SetBlendMode(
					blendModeViewModel.CustomBlendMode);

				_label?.Text = $"Blend Mode : {blendModeViewModel.CustomBlendMode}";

				MainThread.BeginInvokeOnMainThread(() =>
				{
					graphicsView.Invalidate();
				});
			}
		};

		BuildUI();
	}

	void BuildUI()
	{
		Grid rootLayout = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star },
				new RowDefinition { Height = GridLength.Auto }
			}
		};

		rootLayout.Add(graphicsView, 0, 1);

		Button button = new Button
		{
			Text = "Options"
		};

		button.Clicked += async (s, e) =>
		{
			await Navigation.PushAsync(
				new BlendModeOptionPage(blendModeViewModel));
		};

		_label = new Label
		{
			Text = $"Blend Mode : {blendModeViewModel.CustomBlendMode}",
			FontSize = 16,
			FontAttributes = FontAttributes.Bold
		};

		rootLayout.Add(_label, 0, 0);

		rootLayout.Add(button, 0, 2);

		Content = rootLayout;
	}
}

public class BlendModeViewModel : INotifyPropertyChanged
{
	BlendMode _blendMode = BlendMode.Normal;

	public BlendMode CustomBlendMode
	{
		get => _blendMode;
		set
		{
			if (_blendMode != value)
			{
				_blendMode = value;
				OnPropertyChanged(nameof(CustomBlendMode));
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(
			this,
			new PropertyChangedEventArgs(propertyName));
	}
}

public class BlendModeOptionPage : ContentPage
{
	readonly BlendModeViewModel _blendModeViewModel;

	readonly List<string> _separable =
	[
		"Normal",
		"Multiply",
		"Screen",
		"Overlay",
		"Darken",
		"Lighten",
		"ColorDodge",
		"ColorBurn",
		"SoftLight",
		"HardLight",
		"Difference",
		"Exclusion"
	];

	readonly List<string> _nonSeparable =
	[
		"Hue",
		"Saturation",
		"Color",
		"Luminosity"
	];

	readonly List<string> _porterDuff =
	[
		"Clear",
		"Copy",
		"SourceIn",
		"SourceOut",
		"SourceAtop",
		"DestinationOver",
		"DestinationIn",
		"DestinationOut",
		"DestinationAtop",
		"Xor",
		"PlusDarker",
		"PlusLighter"
	];

	public BlendModeOptionPage(BlendModeViewModel blendModeViewModel)
	{
		_blendModeViewModel = blendModeViewModel;

		Title = "Blend Mode Options";

		var grid = new Grid
		{
			Padding = 15,
			ColumnDefinitions =
		{
			new ColumnDefinition(GridLength.Star),
			new ColumnDefinition(GridLength.Star),
			new ColumnDefinition(GridLength.Star)
		},
			RowDefinitions =
		{
			new RowDefinition(GridLength.Auto),
			new RowDefinition(GridLength.Star)
		},
			ColumnSpacing = 15
		};

		var title = new Label
		{
			Text = "Select a blend mode option.",
			FontSize = 18,
			FontAttributes = FontAttributes.Bold
		};

		grid.Add(title);
		Grid.SetColumnSpan(title, 3);

		AddBlendModeSection(grid, "Separable", _separable, 0);
		AddBlendModeSection(grid, "Non-Separable", _nonSeparable, 1);
		AddBlendModeSection(grid, "Porter-Duff", _porterDuff, 2);

		Content = grid;
	}

	void AddBlendModeSection(
		Grid grid,
		string heading,
		List<string> blendModes,
		int column)
	{
		var section = new VerticalStackLayout
		{
			Spacing = 8,
			Padding = new Thickness(5, 15)
		};

		section.Children.Add(new Label
		{
			Text = heading,
			FontSize = 18,
			FontAttributes = FontAttributes.Bold
		});

		foreach (string mode in blendModes)
		{
			var radioButton = new RadioButton
			{
				Content = mode,
				Value = mode,
				GroupName = "BlendModes",
				IsChecked = mode == _blendModeViewModel
					.CustomBlendMode.ToString(),
				FontSize = 14
			};

			radioButton.CheckedChanged += (s, e) =>
			{
				if (e.Value &&
					Enum.TryParse<BlendMode>(mode, out var selectedMode))
				{
					_blendModeViewModel.CustomBlendMode = selectedMode;
				}
			};

			section.Children.Add(radioButton);
		}

		grid.Add(section, column, 1);
	}
}

public class CustomBlendDrawable : IDrawable
{
	BlendMode _blendMode = BlendMode.Normal;

	public void SetBlendMode(BlendMode blendMode)
	{
		_blendMode = blendMode;
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		PointF center = new(
			dirtyRect.Center.X,
			dirtyRect.Center.Y);

		float radius = Math.Min(
			dirtyRect.Width,
			dirtyRect.Height) / 4;

		float distance = 0.8f * radius;

		PointF center1 = new(
			distance * (float)Math.Cos(9 * Math.PI / 6) + center.X,
			distance * (float)Math.Sin(9 * Math.PI / 6) + center.Y);

		PointF center2 = new(
			distance * (float)Math.Cos(1 * Math.PI / 6) + center.X,
			distance * (float)Math.Sin(1 * Math.PI / 6) + center.Y);

		PointF center3 = new(
			distance * (float)Math.Cos(5 * Math.PI / 6) + center.X,
			distance * (float)Math.Sin(5 * Math.PI / 6) + center.Y);

		canvas.BlendMode = _blendMode;

		canvas.FillColor = Colors.Cyan;
		canvas.FillCircle(center1, radius);

		canvas.FillColor = Colors.Magenta;
		canvas.FillCircle(center2, radius);

		canvas.FillColor = Colors.Yellow;
		canvas.FillCircle(center3, radius);
	}
}