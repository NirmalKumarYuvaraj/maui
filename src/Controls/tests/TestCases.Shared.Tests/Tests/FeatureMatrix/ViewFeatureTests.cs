using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests;

[Category(UITestCategories.ViewBaseTests)]
public class ViewFeatureTests : _GalleryUITest
{
	public const string ViewFeatureMatrix = "View Feature Matrix";
	public override string GalleryPageName => ViewFeatureMatrix;

	public ViewFeatureTests(TestDevice device)
		: base(device)
	{
	}


}
