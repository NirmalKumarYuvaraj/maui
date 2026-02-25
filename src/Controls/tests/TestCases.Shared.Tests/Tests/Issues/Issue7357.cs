using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue7357 : _IssuesUITest
	{
		public Issue7357(TestDevice device) : base(device) { }

		public override string Issue => "[Android] FontImageSource.Size property not working in ToolbarItem";

		[Test]
		[Category(UITestCategories.ToolbarItem)]
		public void FontImageSourceSizeRespectedInToolbarItem()
		{
			// Wait for the page to load
			App.WaitForElement("InstructionLabel");

			// Get the expected size image rect (40x40 in logical pixels)
			var expectedSizeRect = App.WaitForElement("ExpectedSizeImage").GetRect();

			// Get the default size image rect (24x24 in logical pixels)
			var defaultSizeRect = App.WaitForElement("DefaultSizeImage").GetRect();

			// The expected image should be larger than the default image
			// This validates our reference images are correctly set up
			Assert.That(expectedSizeRect.Width, Is.GreaterThan(defaultSizeRect.Width),
				"Expected size image should be larger than default size image");

			// Use screenshot verification to verify the toolbar icon size
			// On Android with the bug, the toolbar icon will be 24x24 instead of 40x40
			// The screenshot captures the full screen including the toolbar
			VerifyScreenshot();
		}
	}
}
