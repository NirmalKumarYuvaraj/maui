﻿#if !MACCATALYST
using Microsoft.Maui.Platform;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue25473 : _IssuesUITest
	{
		public Issue25473(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "MAUI Entry in Windows always shows ClearButton despite ClearButtonVisibility set to 'Never'";

		[Test]
		[Category(UITestCategories.Entry)]
		public void VerifyEntryClearButtonVisibilitySetToWhileEditing()
		{
			App.WaitForElement("ToggleClearButtonVisibilityButton");
			App.Tap("MainEntryField");
			App.EnterText("MainEntryField", "ClearButton is set to WhileEditing");
#if IOS
			var app = App as AppiumApp;

			KeyboardScrolling.HideKeyboard(app!, app!.Driver, true);
#endif
			VerifyScreenshot();
		}

		[Test]
		[Category(UITestCategories.Entry)]
		public void VerifyEntryClearButtonVisibilitySetToNever()
		{
			App.Click("ToggleClearButtonVisibilityButton");
			App.ClearText("MainEntryField");
			App.EnterText("MainEntryField", "ClearButton is set to Never");
#if IOS
			var app = App as AppiumApp;

			KeyboardScrolling.HideKeyboard(app!, app!.Driver, true);
#endif
			VerifyScreenshot();
		}
	}
}
#endif