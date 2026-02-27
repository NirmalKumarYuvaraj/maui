#if ANDROID
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34256 : _IssuesUITest
{
	public override string Issue => "Grid with SafeAreaEdges=Container has incorrect size when tab bar appears";

	public Issue34256(TestDevice device) : base(device) { }

	[Test]
	[Category(UITestCategories.SafeAreaEdges)]
	public void GridShouldNotHaveExtraSpaceAfterNavigatingBackFromHiddenTabBarPage()
	{
		// Record baseline position of the bottom marker before any navigation.
		// The gap between InnerGrid's bottom and BottomMarker's bottom should be
		// small when no extra padding is applied (inner grid fills correctly).
		App.WaitForElement("BottomMarker");
		var initialMarkerRect = App.WaitForElement("BottomMarker").GetRect();
		var initialGridRect = App.WaitForElement("InnerGrid").GetRect();
		var initialGap = initialGridRect.Bottom - initialMarkerRect.Bottom;

		// Navigate to HiddenTabBarPage (tab bar hidden) then navigate back
		App.Tap("NavigateButton");
		App.WaitForElement("GoBackButton");
		App.Tap("GoBackButton");

		// Re-measure after navigation. If the bug is present, InnerGrid gets extra
		// bottom padding which pushes BottomMarker up, increasing the gap.
		App.WaitForElement("BottomMarker");
		var afterMarkerRect = App.WaitForElement("BottomMarker").GetRect();
		var afterGridRect = App.WaitForElement("InnerGrid").GetRect();
		var afterGap = afterGridRect.Bottom - afterMarkerRect.Bottom;

		Assert.That(afterGap, Is.EqualTo(initialGap).Within(5),
			$"BottomMarker shifted up after navigation back. Before: gap={initialGap}px, After: gap={afterGap}px. " +
			"InnerGrid has stale bottom padding from when the tab bar was hidden.");
	}
}
#endif
