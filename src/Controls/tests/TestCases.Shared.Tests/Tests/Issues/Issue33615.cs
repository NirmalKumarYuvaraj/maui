using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue33615 : _IssuesUITest
	{
		public override string Issue => "[Android] Title of FlyOutPage is not updating anymore after showing a NonFlyOutPage";

		public Issue33615(TestDevice device) : base(device) { }

		[Test]
		[Category(UITestCategories.FlyoutPage)]
		public void FlyoutPageTitleShouldUpdateAfterWindowPageSwap()
		{
			// Initial state - should be on DetailPage1
			App.WaitForElement("DetailPage1");

			// Verify we're starting on DetailPage1
			var detailPage1Label = App.FindElement("DetailPage1Label");
			Assert.That(detailPage1Label, Is.Not.Null, "Should start on DetailPage1");

			// Navigate to DetailPage2 - title should update normally
			App.Tap("GoToDetailPage2Button");
			App.WaitForElement("DetailPage2");

			var detailPage2Label = App.FindElement("DetailPage2Label");
			Assert.That(detailPage2Label, Is.Not.Null, "Should have navigated to DetailPage2");

			// Navigate back to DetailPage1 - title should update normally
			App.Tap("GoToDetailPage1Button");
			App.WaitForElement("DetailPage1");

			detailPage1Label = App.FindElement("DetailPage1Label");
			Assert.That(detailPage1Label, Is.Not.Null, "Should have navigated back to DetailPage1");

			// Now trigger the bug: Swap Window.Page temporarily
			Console.WriteLine("SANDBOX: Triggering bug - swapping Window.Page");
			App.Tap("TriggerBugButton");

			// Wait for temporary page to appear
			App.WaitForElement("TemporaryPage", timeout: TimeSpan.FromSeconds(5));
			var tempPageLabel = App.FindElement("TemporaryPageLabel");
			Assert.That(tempPageLabel, Is.Not.Null, "Temporary page should be shown");

			// Wait for FlyoutPage to be restored (after 2 second delay in code)
			System.Threading.Thread.Sleep(3000); // Wait for restore + a bit extra

			App.WaitForElement("DetailPage1");

			// Now navigate to DetailPage2 again - this is where the bug manifests
			// The title should update, but it won't due to the bug
			App.Tap("GoToDetailPage2Button");
			VerifyScreenshot();
		}
	}
}
