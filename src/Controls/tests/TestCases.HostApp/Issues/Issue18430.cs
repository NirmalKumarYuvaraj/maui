#nullable disable
using System.Reflection;
using IImage = Microsoft.Maui.Graphics.IImage;
using Microsoft.Maui.Graphics.Platform;
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 18430, "ScrollView content can become stuck on orientation change (iOS)", PlatformAffected.iOS)]
public class Issue18430 : TestContentPage
{
	ClippingDrawable clippingDrawable;
	SubtractClippingDrawable subtractClippingDrawable;

	protected override void Init()
	{
		clippingDrawable = new ClippingDrawable();
		subtractClippingDrawable = new SubtractClippingDrawable();

		ScrollView scrollView = new ScrollView();

		VerticalStackLayout rootLayout = new VerticalStackLayout() { Margin = 15 };

		Label label1 = new Label() { Text = "Clipping" };

		GraphicsView graphicsView1 = new GraphicsView();
		graphicsView1.Drawable = clippingDrawable;
		graphicsView1.HeightRequest = 300;
		graphicsView1.WidthRequest = 400;

		Label label2 = new Label() { Text = "Subtract clipping" };

		GraphicsView graphicsView2 = new GraphicsView();
		graphicsView2.Drawable = subtractClippingDrawable;
		graphicsView2.HeightRequest = 300;
		graphicsView2.WidthRequest = 400;

		rootLayout.Children.Add(label1);
		rootLayout.Children.Add(graphicsView1);
		rootLayout.Children.Add(label2);
		rootLayout.Children.Add(graphicsView2);
		scrollView.Content = rootLayout;

		Content = scrollView;


	}
}


internal class ClippingDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		IImage image;
		var assembly = GetType().GetTypeInfo().Assembly;
		using (var stream = assembly.GetManifestResourceStream("Controls.TestCases.HostApp.Resources.Images.royals.png"))
		{
			image = PlatformImage.FromStream(stream);
		}

		if (image != null)
		{
			PathF path = new PathF();
			path.AppendCircle(100, 90, 80);
			canvas.ClipPath(path);  // Must be called before DrawImage
			canvas.DrawImage(image, 10, 10, image.Width, image.Height);
		}
	}
}

internal class SubtractClippingDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		IImage image;
		var assembly = GetType().GetTypeInfo().Assembly;
		using (var stream = assembly.GetManifestResourceStream("Controls.TestCases.HostApp.Resources.Images.royals.png"))
		{
			image = PlatformImage.FromStream(stream);
		}

		if (image != null)
		{
			canvas.SubtractFromClip(60, 60, 90, 90);
			canvas.DrawImage(image, 10, 10, image.Width, image.Height);
		}
	}
}