using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue31044 : _IssuesUITest
{


	public Issue31044(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Handlers are not disconnected when a ContentView is unloaded via ControlTemplate change";

	[Test]
	[Category(UITestCategories.ViewBaseTests)]
	public void ContentView_DisconnectsHandlers_WhenUnloaded()
	{
		App.WaitForElement("ToggleTemplateButton");
		App.Tap("ToggleTemplateButton");
		VerifyScreenshot();
	}
}