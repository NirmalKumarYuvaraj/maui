﻿using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue27730 : _IssuesUITest
	{
		public Issue27730(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "Shadow not updated when Clipping a View with a shadow";

		[Test]
		[Category(UITestCategories.Border)]
		public void ShadowShouldUpdateWhenClipping()
		{
			App.WaitForElement("ApplyClipBtn");
			App.Tap("ApplyClipBtn");
			VerifyScreenshot();
		}
	}
}