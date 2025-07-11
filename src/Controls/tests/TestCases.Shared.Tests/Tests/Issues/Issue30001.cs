using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue30001 : _IssuesUITest
	{
		public Issue30001(TestDevice device) : base(device) { }

		public override string Issue => "Rectangle inside grid is not properly aligned and does not appear on Android";

		[Test]
		[Category(UITestCategories.Layout)]
		public void RectangleShouldBeVisibleInGrid()
		{
			_ = App.WaitForElement("TestLabel");
			_ = App.WaitForElement("TestRectangle");
			
			// Verify that the rectangle has non-zero dimensions
			var rectangle = App.FindElement("TestRectangle");
			var rectBounds = rectangle.GetRect();
			
			Assert.That(rectBounds.Width, Is.GreaterThan(0), "Rectangle should have width greater than 0");
			Assert.That(rectBounds.Height, Is.GreaterThan(0), "Rectangle should have height greater than 0");
		}
	}
}