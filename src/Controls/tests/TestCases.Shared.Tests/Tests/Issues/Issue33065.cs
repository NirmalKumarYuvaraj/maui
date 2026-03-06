using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33065 : _IssuesUITest
{
	public override string Issue => "CollectionView scrolling is jittery when ItemTemplate contains Label with TextType=Html";

	public Issue33065(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void HtmlLabelCellHeightShouldBeStableAfterScrolling()
	{
		// Wait for CollectionView and first HTML label to be rendered
		App.WaitForElement("TestCollectionView");
		App.WaitForElement("Item_0");

		// Allow HTML label to complete its initial render
		Thread.Sleep(1000);

		// Capture the height of the HTML label in the first cell
		// (this is the element that causes jitter when it resizes)
		var htmlLabelHeightBeforeScroll = App.FindElement("Item_0").GetRect().Height;

		Assert.That(htmlLabelHeightBeforeScroll, Is.GreaterThan(0),
			"HTML label (Item_0) should have a non-zero height after initial render.");

		// Scroll down enough to recycle Item_0 off-screen and load new cells
		App.ScrollDown("TestCollectionView", ScrollStrategy.Gesture, swipePercentage: 0.5);
		App.ScrollDown("TestCollectionView", ScrollStrategy.Gesture, swipePercentage: 0.5);

		// Wait for newly visible cells' HTML labels to render
		Thread.Sleep(1000);

		// Scroll back up — Item_0 is re-created from a recycled cell
		App.ScrollUp("TestCollectionView", ScrollStrategy.Gesture, swipePercentage: 0.5);
		App.ScrollUp("TestCollectionView", ScrollStrategy.Gesture, swipePercentage: 0.5);

		// Wait for Item_0 to re-appear
		App.WaitForElement("Item_0");
		Thread.Sleep(500);

		// Capture the HTML label height after the scroll cycle
		var htmlLabelHeightAfterScroll = App.FindElement("Item_0").GetRect().Height;

		// The HTML label height must be stable after recycling.
		// When the bug is present, the label initially renders at an incorrect height
		// (because the HTML hasn't been measured yet), then snaps to the correct height —
		// causing the visible jitter. The measured heights will differ.
		Assert.That(htmlLabelHeightAfterScroll, Is.EqualTo(htmlLabelHeightBeforeScroll).Within(2),
			$"HTML label height changed after scroll cycle. " +
			$"Before scroll: {htmlLabelHeightBeforeScroll}px, After scroll: {htmlLabelHeightAfterScroll}px. " +
			$"The HTML label is causing cell resize (jitter) when items are recycled during scrolling.");
	}
}
