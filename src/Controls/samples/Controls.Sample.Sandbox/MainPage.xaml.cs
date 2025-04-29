namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public static LinearGradientPaint gradientPaint = new LinearGradientPaint
	{
		StartColor = Colors.Yellow,
		EndColor = Colors.Red,
		StartPoint = new Point(0, 0),
		EndPoint = new Point(1, 0) // Diagonal gradient
	};
	public MainPage()
	{
		InitializeComponent();
		graphicsView.Drawable = new LineDrawable();
		border.Stroke = gradientPaint;
	}
}

public class LineDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{

		
		canvas.Stroke = MainPage.gradientPaint;
		canvas.StrokeSize = 6;
		canvas.DrawLine(10, 10, 90, 100);
	}

	//public void Draw(ICanvas canvas, RectF dirtyRect)
	//{

	//	var gradientPaint = new LinearGradientPaint
	//	{
	//		StartColor = Colors.Yellow,
	//		EndColor = Colors.Red,
	//		StartPoint = new Point(0, 0),
	//		EndPoint = new Point(1, 0) // Diagonal gradient
	//	};
	//	RectF linearRectangle = new RectF(10, 10, 200, 100);
	//	canvas.SetFillPaint(gradientPaint, linearRectangle);
	//	canvas.FillRoundedRectangle(linearRectangle, 12);
	//}
}
