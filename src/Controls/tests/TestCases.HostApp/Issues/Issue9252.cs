using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 9252, "Can't consume a custom font in GraphicsView", PlatformAffected.All)]
public class Issue9252 : ContentPage
{
	const string OpenSansFontFamily = "OpenSansRegular";
	const string DokdoFontFamily = "Dokdo";
	const string MontserratFontFamily = "MontserratBold";
	public Issue9252()
	{
		// Description label explaining the test
		var descriptionLabel = new Label
		{
			AutomationId = "DescriptionLabel",
			Text = "This test verifies that custom fonts registered via ConfigureFonts can be used in both Label and GraphicsView.DrawString. " +
				   "Each row shows a Label and a GraphicsView using the same font family. " +
				   "The text should appear identical in both controls for each font.",
			HorizontalOptions = LayoutOptions.Fill,
			HorizontalTextAlignment = TextAlignment.Center,
			Margin = new Thickness(10)
		};

		var dokdoLabel = Issue9252.CreateLabel(DokdoFontFamily);
		var dokdoGraphicsView = CreateGraphicsView(DokdoFontFamily);

		var openSansLabel = Issue9252.CreateLabel(OpenSansFontFamily);
		var openSansGraphicsView = CreateGraphicsView(OpenSansFontFamily);

		var montserratLabel = Issue9252.CreateLabel(MontserratFontFamily);
		var montserratGraphicsView = CreateGraphicsView(MontserratFontFamily);

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Spacing = 10,
				Padding = 20,
				Children =
				{
					descriptionLabel,
					
					// Dokdo section
					dokdoLabel,
					dokdoGraphicsView,
					
					// OpenSansRegular section
					openSansLabel,
					openSansGraphicsView,
					
					// MontserratBold section
					montserratLabel,
					montserratGraphicsView
				}
			}
		};
	}

	static Label CreateLabel(string fontFamily)
	{
		return new Label
		{
			Text = "Hello MAUI",
			FontFamily = fontFamily,
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center
		};
	}

	static GraphicsView CreateGraphicsView(string fontFamily)
	{
		return new GraphicsView
		{
			HeightRequest = 60,
			WidthRequest = 300,
			BackgroundColor = Colors.LightGray,
			Drawable = new Issue9252Drawable(fontFamily, "Hello MAUI")
		};
	}
}

public class Issue9252Drawable : IDrawable
{
	private readonly string _fontFamily;
	private readonly string _text;

	public Issue9252Drawable(string fontFamily, string text)
	{
		_fontFamily = fontFamily;
		_text = text;
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		// Draw a background rectangle
		canvas.FillColor = Colors.White;
		canvas.FillRectangle(dirtyRect);

		// Set the custom font
		canvas.Font = new Microsoft.Maui.Graphics.Font(_fontFamily);
		canvas.FontColor = Colors.Black;
		canvas.FontSize = 24;

		// Draw the string in the center of the canvas
		canvas.DrawString(
			_text,
			dirtyRect.X,
			dirtyRect.Y,
			dirtyRect.Width,
			dirtyRect.Height,
			HorizontalAlignment.Center,
			VerticalAlignment.Center);
	}
}
