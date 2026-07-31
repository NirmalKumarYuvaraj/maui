#if ANDROID
using System;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests;

public class Material3ActivityIndicatorFeatureTests : _GalleryUITest
{
	public override string GalleryPageName => "ActivityIndicator Gallery";

	public Material3ActivityIndicatorFeatureTests(TestDevice device)
		: base(device)
	{
	}

	[Test, Order(1)]
	[Category(UITestCategories.Material3)]
	public void Material3ActivityIndicator_DefaultRunningState_VerifyVisualState()
	{
		NavigateToEnabledState();
		App.WaitForElement("IsEnabledVisualElement");
		VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
	}

	[Test, Order(2)]
	[Category(UITestCategories.Material3)]
	public void Material3ActivityIndicator_DisabledState_VerifyVisualState()
	{
		NavigateToEnabledState();
		App.WaitForElement("IsEnabledStateButton");
		App.Tap("IsEnabledStateButton");
		App.WaitForElement("IsEnabledVisualElement");
		Assert.That(App.FindElement("IsEnabledStateLabel").GetText(), Is.EqualTo("False"));
		VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
	}

	void NavigateToEnabledState()
	{
		App.WaitForElement("TargetViewContainer");
		App.Tap("TargetViewContainer");
		App.EnterText("TargetViewContainer", "IsEnabledVisualElement");
		App.WaitForElement("GoButton");
		App.Tap("GoButton");
	}
}
#endif
