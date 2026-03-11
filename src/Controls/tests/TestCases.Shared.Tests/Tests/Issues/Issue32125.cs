using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue32125 : _IssuesUITest
{
    public Issue32125(TestDevice device) : base(device) { }

    public override string Issue => "TabBarUnselectedColor is not applied to the tab icon on iOS 26";

    [Test]
    [Category(UITestCategories.Shell)]
    public void TabBarUnselectedColorShouldApplyToIcons()
    {
        App.WaitForElement("Tab1Label");
        App.Tap("Tab 2");
        App.WaitForElement("Tab2Label");

        VerifyScreenshot();
    }
}