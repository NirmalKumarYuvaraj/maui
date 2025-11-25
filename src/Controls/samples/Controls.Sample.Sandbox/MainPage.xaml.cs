#nullable disable
namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	Border _border;

	double _scale = 1;
	double _rotation = 0;
	double _rotationX = 0;
	double _rotationY = 0;
	double _anchorX = 0.5;
	double _anchorY = 0.5;

	bool _shadowApplied = false;

	public MainPage()
	{
		InitializeComponent();
		_border = new Border
		{
			BackgroundColor = Colors.Red,
			HeightRequest = 120,
			WidthRequest = 120,
			HorizontalOptions = LayoutOptions.Center
		};

		var btnScale = CreateButton("Scale +", OnScaleClicked, "ScaleButton");
		var btnRot = CreateButton("Rotation +", OnRotationClicked, "RotationButton");
		var btnRotX = CreateButton("RotationX +", OnRotationXClicked, "RotationXButton");
		var btnRotY = CreateButton("RotationY +", OnRotationYClicked, "RotationYButton");
		var btnAnchorX = CreateButton("AnchorX +", OnAnchorXClicked, "AnchorXButton");
		var btnAnchorY = CreateButton("AnchorY +", OnAnchorYClicked, "AnchorYButton");
		var btnShadow = CreateButton("Toggle Shadow", OnToggleShadowClicked, "ToggleShadowButton");
		var btnReset = CreateButton("Reset", OnResetClicked, "ResetButton");

		Content = new ScrollView
		{
			VerticalOptions = LayoutOptions.Center,
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 20,
				Children =
					{
						_border,

						new HorizontalStackLayout
						{
							Spacing = 10,
							HorizontalOptions = LayoutOptions.Center,
							Children = { btnScale, btnRot, btnRotX, btnRotY }
						},

						new HorizontalStackLayout
						{
							Spacing = 10,
							HorizontalOptions = LayoutOptions.Center,
							Children = { btnAnchorX, btnAnchorY }
						},

						new HorizontalStackLayout
						{
							Spacing = 10,
							HorizontalOptions = LayoutOptions.Center,
							Children = { btnShadow, btnReset }
						}
					}
			}
		};
	}

	Button CreateButton(string text, EventHandler clicked, string automationId)
	{
		var button = new Button { Text = text, Padding = 10, AutomationId = automationId };
		button.Clicked += clicked;
		return button;
	}

	void OnScaleClicked(object sender, EventArgs e)
	{
		_scale += 0.5;
		_border.Scale = _scale;
		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnScaleClicked: Scale={_scale}");
	}

	void OnRotationClicked(object sender, EventArgs e)
	{
		_rotation += 45;
		if (_rotation >= 360)
			_rotation = 0;
		_border.Rotation = _rotation;
		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnRotationClicked: Rotation={_rotation}");
	}

	void OnRotationXClicked(object sender, EventArgs e)
	{
		_rotationX += 45;
		if (_rotationX >= 360)
			_rotationX = 0;
		_border.RotationX = _rotationX;
		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnRotationXClicked: RotationX={_rotationX}");
	}

	void OnRotationYClicked(object sender, EventArgs e)
	{
		_rotationY += 45;
		if (_rotationY >= 360)
			_rotationY = 0;
		_border.RotationY = _rotationY;
		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnRotationYClicked: RotationY={_rotationY}");
	}

	void OnAnchorXClicked(object sender, EventArgs e)
	{
		_anchorX += 0.25;
		if (_anchorX > 1)
			_anchorX = 0;
		_border.AnchorX = _anchorX;
		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnAnchorXClicked: AnchorX={_anchorX}");
	}

	void OnAnchorYClicked(object sender, EventArgs e)
	{
		_anchorY += 0.25;
		if (_anchorY > 1)
			_anchorY = 0;
		_border.AnchorY = _anchorY;
		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnAnchorYClicked: AnchorY={_anchorY}");
	}

	void OnToggleShadowClicked(object sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnToggleShadowClicked: _shadowApplied={_shadowApplied} (before toggle)");

		if (_shadowApplied)
		{
			// Remove shadow completely
			_border.Shadow = null;
			_shadowApplied = false;
			System.Diagnostics.Debug.WriteLine("[ShadowIssue] Shadow removed (set to null)");
		}
		else
		{
			_border.Shadow = new Shadow
			{
				Brush = Brush.Black,
				Opacity = 0.8f,
				Offset = new Point(10, 10)
			};
			_shadowApplied = true;
			System.Diagnostics.Debug.WriteLine("[ShadowIssue] Shadow applied: Brush=Black, Opacity=0.8, Offset=(10,10)");
		}

		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnToggleShadowClicked: _shadowApplied={_shadowApplied} (after toggle)");
	}

	void OnResetClicked(object sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[ShadowIssue] OnResetClicked: Resetting all values");

		_scale = 1;
		_rotation = 0;
		_rotationX = 0;
		_rotationY = 0;
		_anchorX = 0.5;
		_anchorY = 0.5;
		_shadowApplied = false;

		_border.Scale = _scale;
		_border.Rotation = _rotation;
		_border.RotationX = _rotationX;
		_border.RotationY = _rotationY;
		_border.AnchorX = _anchorX;
		_border.AnchorY = _anchorY;
		_border.Shadow = new Shadow
		{
			Brush = Brush.Transparent,
			Opacity = 0f,
			Offset = new Point(0, 0)
		};

		System.Diagnostics.Debug.WriteLine($"[ShadowIssue] OnResetClicked: Reset complete - Scale={_scale}, Rotation={_rotation}, RotationX={_rotationX}, RotationY={_rotationY}, AnchorX={_anchorX}, AnchorY={_anchorY}, Shadow=Transparent");
	}
}