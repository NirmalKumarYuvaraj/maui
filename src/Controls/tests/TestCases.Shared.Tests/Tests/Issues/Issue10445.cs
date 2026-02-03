using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue10445 : _IssuesUITest
{
	public override string Issue => "Shell.Background does not support gradient brushes";

	public Issue10445(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.Shell)]
	public void ShellBackgroundSupportsLinearGradientBrush()
	{
		// Wait for the shell page to load
		App.WaitForElement("TitleLabel");

		// Verify the content loaded successfully
		var titleText = App.FindElement("TitleLabel").GetText();
		Assert.That(titleText, Is.EqualTo("Shell Background Gradient Test"));

		// Verify the reference gradient is visible (proves gradient rendering works)
		App.WaitForElement("ReferenceGradient");

		// Visual verification: The Shell navigation bar at the top should show
		// a horizontal gradient from red (#FF6B6B) on the left to cyan (#4ECDC4) on the right.
		// The reference BoxView in the content area shows what the nav bar gradient should look like.
		VerifyScreenshot();
	}
}
