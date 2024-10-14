﻿#if IOS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Bugzilla56710 : _IssuesUITest
{
	const string Success = "Success";
	const string BtnAdd = "btnAdd";

	public Bugzilla56710(TestDevice testDevice) : base(testDevice)
	{
	}

	public override string Issue => "Multi-item add in INotifyCollectionChanged causes a NSInternalInconsistencyException in bindings on iOS";

	// [Test]
	// [Category(UITestCategories.ListView)]
	// [Category(UITestCategories.Compatibility)]
	// [FailsOnIOS]
	// [FailsOnMac]
	// public void Bugzilla56771Test()
	// {
	// 	App.WaitForElement(BtnAdd);
	// 	App.Tap(BtnAdd);
	// 	App.WaitForNoElement(Success);
	// }
}
#endif