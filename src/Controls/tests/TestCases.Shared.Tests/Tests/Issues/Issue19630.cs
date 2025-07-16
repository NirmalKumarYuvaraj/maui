using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue19630 : _IssuesUITest
{
	public Issue19630(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "TapGestureRecognizer in SwipeItemView not working";

	[Test]
	[Category(UITestCategories.SwipeView)]
	[Category(UITestCategories.Gesture)]
	public void TapGestureRecognizerInSwipeItemViewShouldWork()
	{
		// Wait for the page to load
		App.WaitForElement("StatusLabel");
		
		// Initial status should show waiting
		var initialStatus = App.FindElement("StatusLabel").GetText();
		Assert.That(initialStatus, Does.Contain("Waiting"));

		// Find the first item and swipe left to reveal actions
		App.WaitForElement("ItemLabel");
		App.SwipeRightToLeft("ItemLabel");

		// Wait for swipe animation to complete
		System.Threading.Thread.Sleep(1000);

		// Try to tap the Edit label with TapGestureRecognizer
		App.WaitForElement("EditLabel");
		App.Tap("EditLabel");

		// Wait for the command to be processed
		System.Threading.Thread.Sleep(500);

		// Check if the status was updated (this should work but might fail due to the bug)
		var statusAfterEdit = App.FindElement("StatusLabel").GetText();
		Assert.That(statusAfterEdit, Does.Contain("Edit tapped"), 
			"TapGestureRecognizer on Edit label in SwipeItemView should work");

		// Reset by swiping back
		App.SwipeLeftToRight("ItemLabel");
		System.Threading.Thread.Sleep(500);

		// Test Delete label as well
		App.SwipeRightToLeft("ItemLabel");
		System.Threading.Thread.Sleep(1000);

		App.WaitForElement("DeleteLabel");
		App.Tap("DeleteLabel");
		System.Threading.Thread.Sleep(500);

		var statusAfterDelete = App.FindElement("StatusLabel").GetText();
		Assert.That(statusAfterDelete, Does.Contain("Delete tapped"), 
			"TapGestureRecognizer on Delete label in SwipeItemView should work");
	}

	[Test]
	[Category(UITestCategories.SwipeView)]
	public void RegularSwipeItemShouldStillWork()
	{
		// Wait for the page to load
		App.WaitForElement("StatusLabel");

		// Find the first item and swipe left to reveal actions
		App.WaitForElement("ItemLabel");
		App.SwipeRightToLeft("ItemLabel");

		// Wait for swipe animation to complete
		System.Threading.Thread.Sleep(1000);

		// Tap the regular SwipeItem (this should always work)
		App.Tap("Working");
		System.Threading.Thread.Sleep(500);

		var status = App.FindElement("StatusLabel").GetText();
		Assert.That(status, Does.Contain("Working button tapped"), 
			"Regular SwipeItem should continue to work");
	}
}