using Microsoft.Maui.Controls.Shapes;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 13487, "Shadow with a gradient brush does not work", PlatformAffected.All)]
public class Issue13487 : ContentPage
{
	public Issue13487()
	{
		var linearGradientShadow = new Shadow
		{
			Brush = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1),
				GradientStops =
				[
					new GradientStop(Colors.Red, 0.0f),
					new GradientStop(Colors.Blue, 1.0f)
				]
			},
			Offset = new Point(10, 10),
			Radius = 15,
			Opacity = 0.8f
		};

		var radialGradientShadow = new Shadow
		{
			Brush = new RadialGradientBrush
			{
				Center = new Point(0.5, 0.5),
				Radius = 0.5,
				GradientStops =
				[
					new GradientStop(Colors.Yellow, 0.0f),
					new GradientStop(Colors.Green, 1.0f)
				]
			},
			Offset = new Point(10, 10),
			Radius = 15,
			Opacity = 0.8f
		};

		var solidShadow = new Shadow
		{
			Brush = Colors.Purple,
			Offset = new Point(10, 10),
			Radius = 15,
			Opacity = 0.8f
		};

		var linearBorder = new Border
		{
			AutomationId = "LinearGradientShadowBorder",
			HeightRequest = 100,
			WidthRequest = 150,
			BackgroundColor = Colors.White,
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			Shadow = linearGradientShadow,
			Content = new Label
			{
				Text = "Linear Gradient",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			}
		};

		var radialBorder = new Border
		{
			AutomationId = "RadialGradientShadowBorder",
			HeightRequest = 100,
			WidthRequest = 150,
			BackgroundColor = Colors.White,
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			Shadow = radialGradientShadow,
			Content = new Label
			{
				Text = "Radial Gradient",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			}
		};

		var solidBorder = new Border
		{
			AutomationId = "SolidShadowBorder",
			HeightRequest = 100,
			WidthRequest = 150,
			BackgroundColor = Colors.White,
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			Shadow = solidShadow,
			Content = new Label
			{
				Text = "Solid (Reference)",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			}
		};

		var statusLabel = new Label
		{
			AutomationId = "StatusLabel",
			Text = "All shadows should be visible. Linear shows red-to-blue gradient, Radial shows yellow-to-green gradient.",
			HorizontalOptions = LayoutOptions.Center,
			Margin = new Thickness(20)
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 30,
				Spacing = 40,
				Children =
				{
					statusLabel,
					linearBorder,
					radialBorder,
					solidBorder
				}
			}
		};
	}
}
