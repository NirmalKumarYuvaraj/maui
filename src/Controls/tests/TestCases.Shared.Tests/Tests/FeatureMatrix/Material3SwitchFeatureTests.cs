// Material3 Switch uses MaterialSwitch as its native Android view (vs SwitchCompat for Material2).
// This test class has its own dedicated HostApp page under FeatureMatrix/Material3/Switch/
// to produce separate screenshot baselines for the Material3 native control.
#if ANDROID
using System;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests;

public class Material3SwitchFeatureTests : _GalleryUITest
{
    public const string Material3SwitchFeatureMatrix = "Material3 Switch Feature Matrix";

    public override string GalleryPageName => Material3SwitchFeatureMatrix;

    public Material3SwitchFeatureTests(TestDevice device)
        : base(device)
    {
    }

    [Test, Order(1)]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_InitialState_VerifyVisualState()
    {
        App.WaitForElement("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }

    [Test, Order(2)]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_Click_VerifyVisualState()
    {
        App.WaitForElement("SwitchControl");
        App.Tap("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetFlowDirectionAndToggled_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("FlowDirectionRightToLeftCheckBox");
        App.Tap("FlowDirectionRightToLeftCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElement("SwitchControl");
        App.Tap("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetEnabled_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("IsEnabledFalseCheckBox");
        App.Tap("IsEnabledFalseCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElement("SwitchControl");
        App.Tap("SwitchControl");
        App.WaitForElement("ToggledEventLabel");
        Assert.That(App.FindElement("ToggledEventLabel").GetText(), Is.EqualTo("False"));
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetVisibleAndToggled_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("IsVisibleFalseCheckBox");
        App.Tap("IsVisibleFalseCheckBox");
        App.WaitForElement("IsToggledTrueCheckBox");
        App.Tap("IsToggledTrueCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForNoElement("SwitchControl");
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetToggledAndOnColor_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("IsToggledTrueCheckBox");
        App.Tap("IsToggledTrueCheckBox");
        App.WaitForElement("OnColorRedCheckBox");
        App.Tap("OnColorRedCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetOnColorAndThumbColor_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("OnColorRedCheckBox");
        App.Tap("OnColorRedCheckBox");
        App.WaitForElement("ThumbColorGreenCheckBox");
        App.Tap("ThumbColorGreenCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElement("SwitchControl");
        App.Tap("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetShadowOpacityAndToggled_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("ShadowTrueCheckBox");
        App.Tap("ShadowTrueCheckBox");
        App.WaitForElement("IsToggledTrueCheckBox");
        App.Tap("IsToggledTrueCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetShadowAndOnColor_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("ShadowTrueCheckBox");
        App.Tap("ShadowTrueCheckBox");
        App.WaitForElement("OnColorRedCheckBox");
        App.Tap("OnColorRedCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElement("SwitchControl");
        App.Tap("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetShadowAndThumbColor_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("ShadowTrueCheckBox");
        App.Tap("ShadowTrueCheckBox");
        App.WaitForElement("ThumbColorGreenCheckBox");
        App.Tap("ThumbColorGreenCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElement("SwitchControl");
        App.Tap("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(UITestCategories.Material3)]
    public void Material3Switch_SetThumbColorAndOnColor_VerifyVisualState()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("ThumbColorRedCheckBox");
        App.Tap("ThumbColorRedCheckBox");
        App.WaitForElement("OnColorGreenCheckBox");
        App.Tap("OnColorGreenCheckBox");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElement("SwitchControl");
        App.Tap("SwitchControl");
        VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
    }
}
#endif
