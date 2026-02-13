using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue13487(TestDevice device) : _IssuesUITest(device)
{
	public override string Issue => "Shadow with a gradient brush does not work";

	[Test]
	[Category(UITestCategories.Border)]
	public void GradientShadowsShouldRender()
	{
		// Wait for all shadows to render
		App.WaitForElement("LinearGradientShadowBorder");
		App.WaitForElement("RadialGradientShadowBorder");
		App.WaitForElement("SolidShadowBorder");

		// Use retryTimeout to allow shadow rendering to complete
		// On iOS/Android, gradient shadows should be visible
		// On Windows, a blended color approximation is shown
		VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
	}
}
