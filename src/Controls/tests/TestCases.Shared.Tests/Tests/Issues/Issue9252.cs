using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue9252 : _IssuesUITest
{
	public override string Issue => "Custom fonts registered via AddFont cannot be used in GraphicsView DrawString";

	public Issue9252(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.GraphicsView)]
	public void GraphicsViewDrawStringDisplaysText()
	{
		// Wait for the GraphicsView to be ready
		App.WaitForElement("DescriptionLabel");

		// Take a screenshot to verify the text is displayed
		// The GraphicsView should render "Hello MAUI!" text
		VerifyScreenshot();
	}
}
