using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample;

public enum DrawableType
{
	Line,
	Ellipse,
	Rectangle,
	RoundedRectangle,
	Arc,
	Path,
	Image,
	String,
	AttributedString,
	Shadow,
	Clip
}

public class GraphicsViewViewModel : INotifyPropertyChanged
{
	public Action RequestInvalidate;

	public event PropertyChangedEventHandler PropertyChanged;

	public IDrawable Drawable { get; set; }

	public GraphicsViewViewModel()
	{
		Drawable = new FeatureMartixDrawable(this);
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}


public class FeatureMartixDrawable : IDrawable
{
	Color _fillColor = Colors.Orange;
	Color _strokeColor = Colors.Blue;
	float _strokeDashOffset;
	float[] _strokeDashPattern;
	LineCap _strokeLineCap;
	LineJoin _strokeLineJoin;
	float _strokeSize = 5;
	DrawableType _drawableType = DrawableType.Line;
	Microsoft.Maui.Graphics.IImage _image;

	readonly GraphicsViewViewModel _viewModel;

	public FeatureMartixDrawable(GraphicsViewViewModel viewModel)
	{
		_viewModel = viewModel;
		LoadImage();
	}

	public void SetFillColor(Color color)
	{
		_fillColor = color;
		_viewModel.RequestInvalidate?.Invoke();
	}

	public void SetStrokeColor(Color color)
	{
		_strokeColor = color;
		_viewModel.RequestInvalidate?.Invoke();
	}

	public void SetStrokeDashOffset(float dashOffset)
	{
		_strokeDashOffset = dashOffset;
		_viewModel.RequestInvalidate?.Invoke();
	}

	public void SetStrokeDashPattern(float[] dashPattern)
	{
		_strokeDashPattern = dashPattern;
		_viewModel.RequestInvalidate?.Invoke();
	}

	public void SetStrokeLineCap(LineCap lineCap)
	{
		_strokeLineCap = lineCap;
		_viewModel.RequestInvalidate?.Invoke();
	}

	public void SetStrokeLineJoin(LineJoin lineJoin)
	{
		_strokeLineJoin = lineJoin;
		_viewModel.RequestInvalidate?.Invoke();
	}

	public void SetStrokeSize(float strokeSize)
	{
		_strokeSize = strokeSize;
		_viewModel.RequestInvalidate?.Invoke();
	}

	public void SetDrawableType(DrawableType drawableType)
	{
		_drawableType = drawableType;
		_viewModel.RequestInvalidate?.Invoke();
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.SaveState();
		canvas.StrokeColor = _strokeColor;
		canvas.StrokeSize = _strokeSize;
		canvas.StrokeLineCap = _strokeLineCap;
		canvas.StrokeLineJoin = _strokeLineJoin;
		canvas.StrokeDashOffset = _strokeDashOffset;
		canvas.StrokeDashPattern = _strokeDashPattern;
		canvas.FillColor = _fillColor;

		switch (_drawableType)
		{
			case DrawableType.Line:
				DrawLine(canvas, dirtyRect);
				break;
			case DrawableType.Ellipse:
				DrawEllipse(canvas, dirtyRect);
				break;
			case DrawableType.Rectangle:
				DrawRectangle(canvas, dirtyRect);
				break;
			case DrawableType.RoundedRectangle:
				DrawRoundedRectangle(canvas, dirtyRect);
				break;
			case DrawableType.Arc:
				DrawArc(canvas, dirtyRect);
				break;
			case DrawableType.Path:
				DrawPath(canvas, dirtyRect);
				break;
			case DrawableType.Image:
				DrawImage(canvas, dirtyRect);
				break;
			case DrawableType.String:
				DrawString(canvas, dirtyRect);
				break;
			case DrawableType.AttributedString:
				DrawAttributedText(canvas, dirtyRect);
				break;
			case DrawableType.Shadow:
				DrawShadow(canvas, dirtyRect);
				break;
			case DrawableType.Clip:
				DrawClip(canvas, dirtyRect);
				break;

		}

		canvas.RestoreState();
	}

	public void DrawLine(ICanvas canvas, RectF dirtyRect)
	{
		canvas.DrawLine(10, 10, 90, 100);
	}

	public void DrawEllipse(ICanvas canvas, RectF dirtyRect)
	{
		canvas.DrawEllipse(10, 10, 100, 50);
	}

	public void DrawRectangle(ICanvas canvas, RectF dirtyRect)
	{
		canvas.DrawRectangle(10, 10, 100, 50);
	}

	public void DrawRoundedRectangle(ICanvas canvas, RectF dirtyRect)
	{
		canvas.DrawRoundedRectangle(10, 10, 100, 50, 12);
	}

	public void DrawArc(ICanvas canvas, RectF dirtyRect)
	{
		canvas.DrawArc(10, 10, 100, 100, 0, 180, true, false);
	}

	public void DrawPath(ICanvas canvas, RectF dirtyRect)
	{
		PathF path = new PathF();
		path.MoveTo(40, 10);
		path.LineTo(70, 80);
		path.LineTo(10, 50);
		path.Close();
		canvas.DrawPath(path);
	}

	public void DrawImage(ICanvas canvas, RectF dirtyRect)
	{
		if (_image != null)
		{
			canvas.DrawImage(_image, 10, 10, _image.Width, _image.Height);
		}
	}

	public void DrawString(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FontColor = Colors.Blue;
		canvas.FontSize = 18;

		canvas.DrawString("Text is left aligned.", 20, 20, 380, 100, HorizontalAlignment.Left, VerticalAlignment.Top);
		canvas.DrawString("Text is centered.", 20, 60, 380, 100, HorizontalAlignment.Center, VerticalAlignment.Top);
		canvas.DrawString("Text is right aligned.", 20, 100, 380, 100, HorizontalAlignment.Right, VerticalAlignment.Top);

		canvas.DrawString("This text is displayed using the bold system font.", 20, 140, 350, 100, HorizontalAlignment.Left, VerticalAlignment.Top);

		canvas.FontColor = Colors.Black;
		canvas.SetShadow(new SizeF(6, 6), 4, Colors.Gray);
		canvas.DrawString("This text has a shadow.", 20, 200, 300, 100, HorizontalAlignment.Left, VerticalAlignment.Top);
	}

	public void DrawAttributedText(ICanvas canvas, RectF dirtyRect)
	{

	}

	public void DrawShadow(ICanvas canvas, RectF dirtyRect)
	{
		canvas.SetShadow(new SizeF(10, 10), 4, Colors.Grey);
		canvas.FillRectangle(10, 10, 90, 100);
	}

	public void DrawClip(ICanvas canvas, RectF dirtyRect)
	{
		if (_image != null)
		{
			PathF path = new PathF();
			path.AppendCircle(100, 90, 80);
			canvas.ClipPath(path);
			canvas.DrawImage(_image, 10, 10, _image.Width, _image.Height);
		}
	}

	void LoadImage()
	{
		try
		{
			var assembly = GetType().GetTypeInfo().Assembly;

			// Try different possible resource names
			string[] possibleNames = {
					"Maui.Controls.Sample.Resources.Images.royals.png",
					"Controls.TestCases.HostApp.Resources.Images.royals.png",
					"royals.png",
					"Resources.Images.royals.png"
				};

			foreach (var resourceName in possibleNames)
			{
				using (var stream = assembly.GetManifestResourceStream(resourceName))
				{
					if (stream != null)
					{
						_image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
						Debug.WriteLine($"Successfully loaded image with resource name: {resourceName}");
						return;
					}
				}
			}

			// If we get here, none of the resource names worked
			Debug.WriteLine("Could not find embedded image resource. Available resources:");
			foreach (var name in assembly.GetManifestResourceNames())
			{
				Debug.WriteLine($" - {name}");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error loading image: {ex.Message}");
			_image = null;
		}
	}

}

