#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33162 : _IssuesUITest
{
	public Issue33162(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "MediaPicker.CapturePhotoAsync calls OnAppearing after capturing image in iOS";

	[Test]
	[Category(UITestCategories.LifeCycle)]
	public void OnAppearingShouldNotFireWhenNativeModalDismissed()
	{
		// Wait for page to load
		App.WaitForElement("ResultLabel");

		// Verify OnAppearing was called once on initial page load
		var initialCount = App.FindElement("ResultLabel").GetText();
		Assert.That(initialCount, Is.EqualTo("OnAppearing count: 1"), 
			"OnAppearing should be called once when page first appears");

		// Present a native iOS modal (simulating MediaPicker.CapturePhotoAsync which presents UIImagePickerController)
		App.Tap("TriggerButton");

		// Wait for native modal to appear
		App.WaitForElement("NativeModalDismissButton");

		// Dismiss the native modal
		App.Tap("NativeModalDismissButton");

		// Wait a moment for the modal to dismiss and any lifecycle events to fire
		Task.Delay(1000).Wait();

		// Wait for the main page to be visible again
		App.WaitForElement("ResultLabel");

		// Verify OnAppearing count remains 1 (should NOT increment when native modal is dismissed)
		var finalCount = App.FindElement("ResultLabel").GetText();
		Assert.That(finalCount, Is.EqualTo("OnAppearing count: 1"),
			"OnAppearing should NOT be called when native iOS modal is dismissed");
	}
}
#endif
