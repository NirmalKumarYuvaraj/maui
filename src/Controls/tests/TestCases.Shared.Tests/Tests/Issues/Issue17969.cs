using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue17969 : _IssuesUITest
	{
		public Issue17969(TestDevice device) : base(device)
		{
		}

		public override string Issue => "CollectionView with grouping show duplicates groups or crash when adding/removing data";
		
		[Test]
		[Category(UITestCategories.CollectionView)]
		[FailsOnMac]
		public void CollectionViewShouldAddItemsWithFooterWhenGrouped()
		{
			App.WaitForElement("CollectionView");
			App.Tap("add");
			VerifyScreenshot();

		}

		[Test]
		[Category(UITestCategories.CollectionView)]
		[FailsOnMac]
		public void CollectionViewShouldClearAndAddItemsOnGrouping()
		{
			App.WaitForElement("CollectionView");
			App.Tap("add");
			App.Tap("clear");
			VerifyScreenshot();
		}
	}
}
