using NUnit.Framework;
using NUnit.Framework.Legacy;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
	public class Issue12376 : _IssuesUITest
	{
		public Issue12376(TestDevice testDevice) : base(testDevice)
		{
		}

		public override string Issue => "UriImageSource never refreshes after CacheValidity elapses";

		[Test]
		[Category(UITestCategories.Image)]
		public void UriImageSourceShouldRefreshAfterCacheValidityElapses()
		{
			VerifyInternetConnectivity();

			App.WaitForElement("CachedImage");
			App.WaitForElement("ReloadButton");

			// Give the first image time to fully download and render.
			Thread.Sleep(TimeSpan.FromSeconds(3));
			var first = App.Screenshot();

			// Wait past the 5s CacheValidity so the next request must refetch.
			Thread.Sleep(TimeSpan.FromSeconds(7));

			App.Tap("ReloadButton");

			// Give the second image time to download and render.
			Thread.Sleep(TimeSpan.FromSeconds(5));
			var second = App.Screenshot();

			// picsum.photos returns different bytes per request, so if the cache
			// honored CacheValidity and refetched, the rendered image must differ.
			ClassicAssert.That(second, Is.Not.EqualTo(first),
				"Image should have refreshed after CacheValidity elapsed (issue #12376)");
		}
	}
}
