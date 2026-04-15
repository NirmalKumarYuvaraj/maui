using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui.Controls.Sample;

public class ViewViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	// Core Transformation Properties
	double _rotation = 0.0;
	double _rotationX = 0.0;
	double _rotationY = 0.0;
	double _scale = 1.0;
	double _scaleX = 1.0;
	double _scaleY = 1.0;
	bool _isVisible = true;

	// Additional Related Properties
	double _translationX = 0.0;
	double _translationY = 0.0;
	double _anchorX = 0.5;
	double _anchorY = 0.5;

	// View-level properties
	double _opacity = 1.0;
	double _widthRequest = 120;
	double _heightRequest = 120;
	double _minimumWidthRequest = -1;
	double _minimumHeightRequest = -1;
	double _maximumWidthRequest = double.PositiveInfinity;
	double _maximumHeightRequest = double.PositiveInfinity;
	Color _backgroundColor = null;
	bool _isEnabled = true;
	bool _inputTransparent = false;
	FlowDirection _flowDirection = FlowDirection.MatchParent;
	int _zIndex = 0;
	Thickness _margin = new Thickness(0);
	LayoutOptions _horizontalOptions = LayoutOptions.Center;
	LayoutOptions _verticalOptions = LayoutOptions.Center;

	// Control type selection
	string _selectedControlType = "Label";

	public static readonly string[] AvailableControlTypes =
	[
		"Label",
		"BoxView",
		"Button",
		"ImageButton",
		"Entry",
		"Editor",
		"SearchBar",
		"CheckBox",
		"RadioButton",
		"Switch",
		"Slider",
		"Stepper",
		"Picker",
		"DatePicker",
		"TimePicker",
		"ProgressBar",
		"ActivityIndicator",
		"Image",
		"Border",
		"Frame",
		"WebView",
		"GraphicsView",
		"CollectionView",
		"ListView",
		"CarouselView",
		"ScrollView",
		"Grid",
		"StackLayout",
		"HorizontalStackLayout",
		"VerticalStackLayout",
		"FlexLayout",
		"AbsoluteLayout",
	];

	public string SelectedControlType
	{
		get => _selectedControlType;
		set
		{
			if (_selectedControlType != value)
			{
				_selectedControlType = value;
				OnPropertyChanged();
			}
		}
	}

	// Core Transformation Properties
	public bool IsVisible
	{
		get => _isVisible;
		set
		{
			if (_isVisible != value)
			{
				_isVisible = value;
				OnPropertyChanged();
			}
		}
	}

	bool _hasShadow = false;
	Shadow _boxShadow = null;

	public bool HasShadow
	{
		get => _hasShadow;
		set
		{
			if (_hasShadow != value)
			{
				_hasShadow = value;
				BoxShadow = value
					? new Shadow
					{
						Radius = 2,
						Opacity = 0.9f,
						Brush = new SolidColorBrush(Colors.Gray),
						Offset = new Point(10, 10)
					}
					: null;
				OnPropertyChanged(nameof(HasShadow));
			}
		}
	}

	public Shadow BoxShadow
	{
		get => _boxShadow;
		set
		{
			if (_boxShadow != value)
			{
				_boxShadow = value;
				OnPropertyChanged(nameof(BoxShadow));
			}
		}
	}

	public double Rotation
	{
		get => _rotation;
		set
		{
			if (_rotation != value)
			{
				_rotation = value;
				OnPropertyChanged();
			}
		}
	}

	public double RotationX
	{
		get => _rotationX;
		set
		{
			if (_rotationX != value)
			{
				_rotationX = value;
				OnPropertyChanged();
			}
		}
	}

	public double RotationY
	{
		get => _rotationY;
		set
		{
			if (_rotationY != value)
			{
				_rotationY = value;
				OnPropertyChanged();
			}
		}
	}

	public double Scale
	{
		get => _scale;
		set
		{
			if (_scale != value)
			{
				_scale = value;
				OnPropertyChanged();
			}
		}
	}

	public double ScaleX
	{
		get => _scaleX;
		set
		{
			if (_scaleX != value)
			{
				_scaleX = value;
				OnPropertyChanged();
			}
		}
	}

	public double ScaleY
	{
		get => _scaleY;
		set
		{
			if (_scaleY != value)
			{
				_scaleY = value;
				OnPropertyChanged();
			}
		}
	}

	public double TranslationX
	{
		get => _translationX;
		set
		{
			if (_translationX != value)
			{
				_translationX = value;
				OnPropertyChanged();
			}
		}
	}

	public double TranslationY
	{
		get => _translationY;
		set
		{
			if (_translationY != value)
			{
				_translationY = value;
				OnPropertyChanged();
			}
		}
	}

	public double AnchorX
	{
		get => _anchorX;
		set
		{
			if (_anchorX != value)
			{
				_anchorX = value;
				OnPropertyChanged();
			}
		}
	}

	public double AnchorY
	{
		get => _anchorY;
		set
		{
			if (_anchorY != value)
			{
				_anchorY = value;
				OnPropertyChanged();
			}
		}
	}

	// View-level Properties
	public double Opacity
	{
		get => _opacity;
		set
		{
			if (_opacity != value)
			{
				_opacity = value;
				OnPropertyChanged();
			}
		}
	}

	public double WidthRequest
	{
		get => _widthRequest;
		set
		{
			if (_widthRequest != value)
			{
				_widthRequest = value;
				OnPropertyChanged();
			}
		}
	}

	public double HeightRequest
	{
		get => _heightRequest;
		set
		{
			if (_heightRequest != value)
			{
				_heightRequest = value;
				OnPropertyChanged();
			}
		}
	}

	public double MinimumWidthRequest
	{
		get => _minimumWidthRequest;
		set
		{
			if (_minimumWidthRequest != value)
			{
				_minimumWidthRequest = value;
				OnPropertyChanged();
			}
		}
	}

	public double MinimumHeightRequest
	{
		get => _minimumHeightRequest;
		set
		{
			if (_minimumHeightRequest != value)
			{
				_minimumHeightRequest = value;
				OnPropertyChanged();
			}
		}
	}

	public double MaximumWidthRequest
	{
		get => _maximumWidthRequest;
		set
		{
			if (_maximumWidthRequest != value)
			{
				_maximumWidthRequest = value;
				OnPropertyChanged();
			}
		}
	}

	public double MaximumHeightRequest
	{
		get => _maximumHeightRequest;
		set
		{
			if (_maximumHeightRequest != value)
			{
				_maximumHeightRequest = value;
				OnPropertyChanged();
			}
		}
	}

	public Color BackgroundColor
	{
		get => _backgroundColor;
		set
		{
			if (_backgroundColor != value)
			{
				_backgroundColor = value;
				OnPropertyChanged();
			}
		}
	}

	public bool IsEnabled
	{
		get => _isEnabled;
		set
		{
			if (_isEnabled != value)
			{
				_isEnabled = value;
				OnPropertyChanged();
			}
		}
	}

	public bool InputTransparent
	{
		get => _inputTransparent;
		set
		{
			if (_inputTransparent != value)
			{
				_inputTransparent = value;
				OnPropertyChanged();
			}
		}
	}

	public FlowDirection FlowDirection
	{
		get => _flowDirection;
		set
		{
			if (_flowDirection != value)
			{
				_flowDirection = value;
				OnPropertyChanged();
			}
		}
	}

	public int ZIndex
	{
		get => _zIndex;
		set
		{
			if (_zIndex != value)
			{
				_zIndex = value;
				OnPropertyChanged();
			}
		}
	}

	public Thickness Margin
	{
		get => _margin;
		set
		{
			if (_margin != value)
			{
				_margin = value;
				OnPropertyChanged();
			}
		}
	}

	public LayoutOptions HorizontalOptions
	{
		get => _horizontalOptions;
		set
		{
			if (_horizontalOptions.Alignment != value.Alignment)
			{
				_horizontalOptions = value;
				OnPropertyChanged();
			}
		}
	}

	public LayoutOptions VerticalOptions
	{
		get => _verticalOptions;
		set
		{
			if (_verticalOptions.Alignment != value.Alignment)
			{
				_verticalOptions = value;
				OnPropertyChanged();
			}
		}
	}

	// Selected option strings for pickers
	string _selectedHorizontalOption = "Center";
	public string SelectedHorizontalOption
	{
		get => _selectedHorizontalOption;
		set
		{
			if (_selectedHorizontalOption != value)
			{
				_selectedHorizontalOption = value;
				HorizontalOptions = value switch
				{
					"Start" => LayoutOptions.Start,
					"Center" => LayoutOptions.Center,
					"End" => LayoutOptions.End,
					"Fill" => LayoutOptions.Fill,
					_ => LayoutOptions.Center,
				};
				OnPropertyChanged();
			}
		}
	}

	string _selectedVerticalOption = "Center";
	public string SelectedVerticalOption
	{
		get => _selectedVerticalOption;
		set
		{
			if (_selectedVerticalOption != value)
			{
				_selectedVerticalOption = value;
				VerticalOptions = value switch
				{
					"Start" => LayoutOptions.Start,
					"Center" => LayoutOptions.Center,
					"End" => LayoutOptions.End,
					"Fill" => LayoutOptions.Fill,
					_ => LayoutOptions.Center,
				};
				OnPropertyChanged();
			}
		}
	}

	string _selectedBackgroundColor = "None";
	public string SelectedBackgroundColor
	{
		get => _selectedBackgroundColor;
		set
		{
			if (_selectedBackgroundColor != value)
			{
				_selectedBackgroundColor = value;
				BackgroundColor = value switch
				{
					"Red" => Colors.Red,
					"Green" => Colors.Green,
					"Blue" => Colors.Blue,
					"Yellow" => Colors.Yellow,
					_ => null,
				};
				OnPropertyChanged();
			}
		}
	}

	// Reset Command
	public ICommand ResetCommand => new Command(Reset);

	void Reset()
	{
		Rotation = 0.0;
		RotationX = 0.0;
		RotationY = 0.0;
		Scale = 1.0;
		ScaleX = 1.0;
		ScaleY = 1.0;
		TranslationX = 0.0;
		TranslationY = 0.0;
		AnchorX = 0.5;
		AnchorY = 0.5;
		IsVisible = true;
		HasShadow = false;
		Opacity = 1.0;
		WidthRequest = 120;
		HeightRequest = 120;
		MinimumWidthRequest = -1;
		MinimumHeightRequest = -1;
		MaximumWidthRequest = double.PositiveInfinity;
		MaximumHeightRequest = double.PositiveInfinity;
		BackgroundColor = null;
		IsEnabled = true;
		InputTransparent = false;
		FlowDirection = FlowDirection.MatchParent;
		ZIndex = 0;
		Margin = new Thickness(0);
		HorizontalOptions = LayoutOptions.Center;
		VerticalOptions = LayoutOptions.Center;
		SelectedHorizontalOption = "Center";
		SelectedVerticalOption = "Center";
		SelectedBackgroundColor = "None";
		SelectedControlType = string.Empty;
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
