using NUnit.Framework;
using UITest.Appium;
using UITest.Core;


namespace Microsoft.Maui.TestCases.Tests
{
	[Category(UITestCategories.BoxView)]
	public class BoxViewFeatureTests : _GalleryUITest
	{
		public const string BoxViewFeatureMatrix = "BoxView Feature Matrix";

		public override string GalleryPageName => BoxViewFeatureMatrix;

		public BoxViewFeatureTests(TestDevice device)
			: base(device)
		{
		}

		void ResetBoxView()
		{
			App.WaitForElement("ResetButton");
			App.Tap("ResetButton");
		}

		// ── Color tests (Order 1–3) ──

		[Test, Order(1)]
		public void BoxView_Color()
		{
			ResetBoxView();
			App.WaitForElement("RedRadioButton");
			App.Tap("RedRadioButton");
			VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
		}

		[Test, Order(2)]
		public void BoxView_GreenColor()
		{
			ResetBoxView();
			App.WaitForElement("GreenRadioButton");
			App.Tap("GreenRadioButton");
			VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
		}

		[Test, Order(3)]
		public void BoxView_BlueColor()
		{
			ResetBoxView();
			// Switch away from the default Blue then back to explicitly verify the Blue radio button
			App.WaitForElement("RedRadioButton");
			App.Tap("RedRadioButton");
			App.WaitForElement("BlueRadioButton");
			App.Tap("BlueRadioButton");
			VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
		}

		// ── CornerRadius tests (Order 4–5) ──

		[Test, Order(4)]
		public void BoxView_UniformCornerRadius()
		{
			ResetBoxView();
			App.WaitForElement("CornerRadiusEntry");
			App.ClearText("CornerRadiusEntry");
			App.EnterText("CornerRadiusEntry", "30");
			VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
		}

		[Test, Order(5)]
		public void BoxView_CornerRadiusWithColor()
		{
			ResetBoxView();
			App.WaitForElement("CornerRadiusEntry");
			App.ClearText("CornerRadiusEntry");
			App.EnterText("CornerRadiusEntry", "60,10,20,40");
			App.WaitForElement("RedRadioButton");
			App.Tap("RedRadioButton");
			VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
		}

		// ── Reset test (Order 6) ──

		[Test, Order(6)]
		public void BoxView_Reset()
		{
			ResetBoxView();
			App.WaitForElement("RedRadioButton");
			App.Tap("RedRadioButton");
			App.WaitForElement("CornerRadiusEntry");
			App.ClearText("CornerRadiusEntry");
			App.EnterText("CornerRadiusEntry", "30,30,30,30");

			// Reset back to default state and verify
			App.WaitForElement("ResetButton");
			App.Tap("ResetButton");
			VerifyScreenshot(tolerance: 0.5, retryTimeout: TimeSpan.FromSeconds(2));
		}
	}
}
