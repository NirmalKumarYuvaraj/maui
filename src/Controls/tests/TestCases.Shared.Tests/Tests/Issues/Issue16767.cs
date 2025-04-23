using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;
public class Issue16767 : _IssuesUITest
{
	public Issue16767(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Resize and Downsize function in W2DImage class";

	[Test]
	[Category(UITestCategories.GraphicsView)]
	public void ImagePaintWithResizeModeFit()
	{
		App.WaitForElement("ResizeModeFit");
		App.Tap("ResizeModeFit");
		VerifyScreenshot();
	}

	[Test]
	[Category(UITestCategories.GraphicsView)]
	public void ImagePaintWithResizeModeBleed()
	{
		App.WaitForElement("ResizeModeBleed");
		App.Tap("ResizeModeBleed");
		VerifyScreenshot();
	}

	[Test]
	[Category(UITestCategories.GraphicsView)]
	public void ImagePaintWithResizeModeStretch()
	{
		App.WaitForElement("ResizeModeStretch");
		App.Tap("ResizeModeStretch");
		VerifyScreenshot();
	}
}
