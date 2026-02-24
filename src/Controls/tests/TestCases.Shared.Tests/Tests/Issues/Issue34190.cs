using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue34190 : _IssuesUITest
	{
		public override string Issue => "OnBackButtonPressed not firing for Shell Navigation Bar button on iOS";

		public Issue34190(TestDevice device) : base(device) { }

		[Test]
		[Category(UITestCategories.Shell)]
		public void OnBackButtonPressedShouldFireForShellNavigationBarButton()
		{
			// Verify we're on the main page
			App.WaitForElement("MainPageLabel");

			// Navigate to TestPage
			App.Tap("NavigateButton");
			App.WaitForElement("StatusLabel");

			// Verify initial state
			var statusLabel = App.WaitForElement("StatusLabel");
			Assert.That(statusLabel.GetText(), Is.EqualTo("OnBackButtonPressed not called"));

			// Tap the navigation bar back button
			// Note: On iOS/MacCatalyst, TapBackArrow needs the previous page title
#if ANDROID
			App.TapBackArrow();
#else
			App.TapBackArrow("Main Page");
#endif

			// Wait a moment for the event to fire and check if we're still on the page
			// (OnBackButtonPressed returns true, preventing navigation)
			App.WaitForElement("StatusLabel");

			// Verify OnBackButtonPressed was called
			statusLabel = App.FindElement("StatusLabel");
			Assert.That(statusLabel.GetText(), Is.EqualTo("OnBackButtonPressed was called"), 
				"OnBackButtonPressed should be called when tapping the Shell Navigation Bar back button");
		}
	}
}
