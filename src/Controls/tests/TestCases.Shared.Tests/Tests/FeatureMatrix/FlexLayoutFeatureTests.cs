using System;
using Microsoft.Maui.Layouts;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests;

public class FlexLayoutFeatureTests : UITest
{
    public const string FlexLayoutFeatureMatrix = "FlexLayout Feature Matrix";

    public FlexLayoutFeatureTests(TestDevice testDevice) : base(testDevice)
    {
    }

    protected override void FixtureSetup()
    {
        base.FixtureSetup();
        App.NavigateToGallery(FlexLayoutFeatureMatrix);
    }


    [SetUp]
    public override void TestSetup()
    {
        base.TestSetup();
        ResetLayoutToDefault();
    }

    void ResetLayoutToDefault()
    {
        try
        {
            App.WaitForElement("Options");
            App.Tap("Options");
            App.WaitForElement("LayoutTestResetButton");
            App.Tap("LayoutTestResetButton");
            App.WaitForElement("Apply");
            App.Tap("Apply");
            App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");
        }
        catch
        {
            // If reset fails, continue with tests
        }
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetDirectionRow_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("DirectionRowButton");
        App.Tap("DirectionRowButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        // Verify direction is displayed correctly
        Assert.That(App.FindElement("DirectionLabel").GetText(), Is.EqualTo("Row"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetDirectionColumn_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("DirectionColumnButton");
        App.Tap("DirectionColumnButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("DirectionLabel").GetText(), Is.EqualTo("Column"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetDirectionRowReverse_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("DirectionRowReverseButton");
        App.Tap("DirectionRowReverseButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("DirectionLabel").GetText(), Is.EqualTo("RowReverse"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetDirectionColumnReverse_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("DirectionColumnReverseButton");
        App.Tap("DirectionColumnReverseButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("DirectionLabel").GetText(), Is.EqualTo("ColumnReverse"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetJustifyContentCenter_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("JustifyContentCenterButton");
        App.Tap("JustifyContentCenterButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("JustifyContentLabel").GetText(), Is.EqualTo("Center"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetJustifyContentEnd_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("JustifyContentEndButton");
        App.Tap("JustifyContentEndButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("JustifyContentLabel").GetText(), Is.EqualTo("End"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetJustifyContentSpaceBetween_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("JustifyContentSpaceBetweenButton");
        App.Tap("JustifyContentSpaceBetweenButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("JustifyContentLabel").GetText(), Is.EqualTo("SpaceBetween"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetJustifyContentSpaceAround_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("JustifyContentSpaceAroundButton");
        App.Tap("JustifyContentSpaceAroundButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("JustifyContentLabel").GetText(), Is.EqualTo("SpaceAround"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetJustifyContentSpaceEvenly_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("JustifyContentSpaceEvenlyButton");
        App.Tap("JustifyContentSpaceEvenlyButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("JustifyContentLabel").GetText(), Is.EqualTo("SpaceEvenly"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetAlignItemsCenter_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("AlignItemsCenterButton");
        App.Tap("AlignItemsCenterButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("AlignItemsLabel").GetText(), Is.EqualTo("Center"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetAlignItemsStart_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("AlignItemsStartButton");
        App.Tap("AlignItemsStartButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("AlignItemsLabel").GetText(), Is.EqualTo("Start"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetAlignItemsEnd_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("AlignItemsEndButton");
        App.Tap("AlignItemsEndButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("AlignItemsLabel").GetText(), Is.EqualTo("End"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetWrapWrap_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("WrapWrapButton");
        App.Tap("WrapWrapButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("WrapLabel").GetText(), Is.EqualTo("Wrap"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetWrapReverse_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("WrapReverseButton");
        App.Tap("WrapReverseButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("WrapLabel").GetText(), Is.EqualTo("Reverse"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetAlignContentCenter_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        // First enable wrap to make AlignContent effective
        App.WaitForElement("WrapWrapButton");
        App.Tap("WrapWrapButton");
        App.WaitForElement("AlignContentCenterButton");
        App.Tap("AlignContentCenterButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("AlignContentLabel").GetText(), Is.EqualTo("Center"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetAlignContentSpaceBetween_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        // First enable wrap to make AlignContent effective
        App.WaitForElement("WrapWrapButton");
        App.Tap("WrapWrapButton");
        App.WaitForElement("AlignContentSpaceBetweenButton");
        App.Tap("AlignContentSpaceBetweenButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("AlignContentLabel").GetText(), Is.EqualTo("SpaceBetween"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetChildGrowValues_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("Child1GrowEntry");
        App.ClearText("Child1GrowEntry");
        App.EnterText("Child1GrowEntry", "1");
        App.WaitForElement("Child2GrowEntry");
        App.ClearText("Child2GrowEntry");
        App.EnterText("Child2GrowEntry", "2");
        App.WaitForElement("Child3GrowEntry");
        App.ClearText("Child3GrowEntry");
        App.EnterText("Child3GrowEntry", "1");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetChildOrderValues_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("Child1OrderEntry");
        App.ClearText("Child1OrderEntry");
        App.EnterText("Child1OrderEntry", "3");
        App.WaitForElement("Child2OrderEntry");
        App.ClearText("Child2OrderEntry");
        App.EnterText("Child2OrderEntry", "1");
        App.WaitForElement("Child3OrderEntry");
        App.ClearText("Child3OrderEntry");
        App.EnterText("Child3OrderEntry", "2");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetChildAlignSelfValues_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("Child1AlignSelfStartButton");
        App.Tap("Child1AlignSelfStartButton");
        App.WaitForElement("Child2AlignSelfCenterButton");
        App.Tap("Child2AlignSelfCenterButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetBasisAutoValues_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("BasisAutoButton");
        App.Tap("BasisAutoButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetBasisFixedValues_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("BasisFixedButton");
        App.Tap("BasisFixedButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetBasisRelativeValues_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("BasisRelativeButton");
        App.Tap("BasisRelativeButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetPositionAbsolute_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("PositionAbsoluteButton");
        App.Tap("PositionAbsoluteButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("PositionLabel").GetText(), Is.EqualTo("Absolute"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_ComplexLayoutTest1_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("LayoutTestComplex1Button");
        App.Tap("LayoutTestComplex1Button");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        // Verify the complex layout properties are applied
        Assert.That(App.FindElement("DirectionLabel").GetText(), Is.EqualTo("Column"));
        Assert.That(App.FindElement("WrapLabel").GetText(), Is.EqualTo("Wrap"));
        Assert.That(App.FindElement("JustifyContentLabel").GetText(), Is.EqualTo("SpaceBetween"));
        Assert.That(App.FindElement("AlignItemsLabel").GetText(), Is.EqualTo("Center"));

        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_ComplexLayoutTest2_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("LayoutTestComplex2Button");
        App.Tap("LayoutTestComplex2Button");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        // Verify the complex layout properties are applied
        Assert.That(App.FindElement("DirectionLabel").GetText(), Is.EqualTo("Row"));
        Assert.That(App.FindElement("WrapLabel").GetText(), Is.EqualTo("NoWrap"));
        Assert.That(App.FindElement("JustifyContentLabel").GetText(), Is.EqualTo("Center"));
        Assert.That(App.FindElement("AlignItemsLabel").GetText(), Is.EqualTo("Stretch"));

        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_CombinedDirectionAndJustifyContent_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("DirectionColumnButton");
        App.Tap("DirectionColumnButton");
        App.WaitForElement("JustifyContentSpaceEvenlyButton");
        App.Tap("JustifyContentSpaceEvenlyButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("DirectionLabel").GetText(), Is.EqualTo("Column"));
        Assert.That(App.FindElement("JustifyContentLabel").GetText(), Is.EqualTo("SpaceEvenly"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_CombinedWrapAndAlignContent_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("WrapWrapButton");
        App.Tap("WrapWrapButton");
        App.WaitForElement("AlignContentSpaceAroundButton");
        App.Tap("AlignContentSpaceAroundButton");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        Assert.That(App.FindElement("WrapLabel").GetText(), Is.EqualTo("Wrap"));
        Assert.That(App.FindElement("AlignContentLabel").GetText(), Is.EqualTo("SpaceAround"));
        VerifyScreenshot();
    }

    [Test]
    [Category(UITestCategories.Layout)]
    public void FlexLayout_SetChildShrinkValues_VerifyLayoutArrangement()
    {
        App.WaitForElement("Options");
        App.Tap("Options");
        App.WaitForElement("Child1ShrinkEntry");
        App.ClearText("Child1ShrinkEntry");
        App.EnterText("Child1ShrinkEntry", "0");
        App.WaitForElement("Apply");
        App.Tap("Apply");
        App.WaitForElementTillPageNavigationSettled("FlexLayoutControl");

        VerifyScreenshot();
    }
}
