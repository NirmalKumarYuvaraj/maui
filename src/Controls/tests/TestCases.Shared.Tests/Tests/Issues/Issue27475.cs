using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue27475 : _IssuesUITest
	{
		public Issue27475(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "Thumb Image Disappears in Slider Control When Changing Thumb Color on iOS and Catalyst";

		[Test]
		[Category(UITestCategories.Slider)]
		public void SliderThumbShouldRetainColor()
		{
			App.WaitForElement("ChangeThumbColorButton");
			App.Tap("ChangeThumbColorButton");
			VerifyScreenshot();
		}
	}
}