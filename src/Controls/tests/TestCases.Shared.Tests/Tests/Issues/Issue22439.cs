using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue22439 : _IssuesUITest
	{
		public Issue22439(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "Button out of sight not clickable when translated";

		[Test]
		[Category(UITestCategories.Button)]
		public void TranslatedButtonShouldBeClickable()
		{
			// Wait for the page to load
			App.WaitForElement("ResultLabel");

			// Tap the translate button to bring the translated button into view
			App.Tap("TranslateButton");

			// Give time for the translation to complete
			Task.Delay(500).Wait();

			// Tap the translated button
			App.Tap("TranslatedButton");

			// Verify the button was clicked
			var result = App.WaitForElement("ResultLabel");
			Assert.That(result.GetText(), Is.EqualTo("Button Clicked!"));
		}

		[Test]
		[Category(UITestCategories.Button)]
		public void OffscreenButtonInTranslatedLayoutShouldBeClickable()
		{
			// Wait for the page to load
			App.WaitForElement("ResultLabel");

			// Tap the slide button to reveal the offscreen button
			App.Tap("SlideButton");

			// Give time for the translation to complete
			Task.Delay(500).Wait();

			// Tap the offscreen button (now visible after sliding)
			App.Tap("OffscreenButton");

			// Verify the button was clicked
			var result = App.WaitForElement("ResultLabel");
			Assert.That(result.GetText(), Is.EqualTo("Offscreen Button Clicked!"));
		}
	}
}
