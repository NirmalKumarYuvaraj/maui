#nullable enable
using System.Linq;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class SharedTransitionResolverTests : BaseTestFixture
	{
		static (ContentPage Page, Image Image) CreatePage(string? name, string? group = null)
		{
			var image = new Image();
			SharedTransition.SetName(image, name);
			SharedTransition.SetGroup(image, group);
			var page = new ContentPage { Content = new VerticalStackLayout { new Label(), image } };
			return (page, image);
		}

		[Fact]
		public void MatchesElementsByName()
		{
			var (list, listImage) = CreatePage("hero");
			var (detail, detailImage) = CreatePage("hero");

			var pair = Assert.Single(SharedTransitionResolver.Resolve(list, detail, isPopping: false));

			Assert.Same(listImage, pair.Source);
			Assert.Same(detailImage, pair.Destination);
			Assert.Equal("hero", pair.Key);
		}

		[Fact]
		public void PopSwapsSourceAndDestination()
		{
			var (list, listImage) = CreatePage("hero");
			var (detail, detailImage) = CreatePage("hero");

			var pair = Assert.Single(SharedTransitionResolver.Resolve(detail, list, isPopping: true));

			Assert.Same(detailImage, pair.Source);
			Assert.Same(listImage, pair.Destination);
		}

		[Theory]
		[InlineData("hero", "1", "hero", "1", true)]
		[InlineData("hero", "1", "hero", "2", false)]
		[InlineData("hero", null!, "hero", "1", false)]
		[InlineData("hero", "", "hero", null!, true)]
		[InlineData("hero", null!, "title", null!, false)]
		public void MatchRequiresSameNameAndGroup(string sourceName, string? sourceGroup, string destinationName, string? destinationGroup, bool matches)
		{
			var (list, _) = CreatePage(sourceName, sourceGroup);
			var (detail, _) = CreatePage(destinationName, destinationGroup);

			Assert.Equal(matches, SharedTransitionResolver.Resolve(list, detail, isPopping: false).Count == 1);
		}

		[Fact]
		public void GroupDistinguishesItemsOnSamePage()
		{
			var first = new Image();
			var second = new Image();
			SharedTransition.SetName(first, "photo");
			SharedTransition.SetGroup(first, "1");
			SharedTransition.SetName(second, "photo");
			SharedTransition.SetGroup(second, "2");
			var list = new ContentPage { Content = new VerticalStackLayout { first, second } };
			var (detail, _) = CreatePage("photo", "2");

			var pair = Assert.Single(SharedTransitionResolver.Resolve(list, detail, isPopping: false));

			Assert.Same(second, pair.Source);
			Assert.Equal("photo|2", pair.Key);
		}

		[Fact]
		public void DuplicateKeyUsesFirstVisibleElement()
		{
			var hidden = new Image { IsVisible = false };
			var first = new Image();
			var second = new Image();
			foreach (var image in new[] { hidden, first, second })
				SharedTransition.SetName(image, "hero");
			var list = new ContentPage { Content = new VerticalStackLayout { hidden, first, second } };
			var (detail, _) = CreatePage("hero");

			var pair = Assert.Single(SharedTransitionResolver.Resolve(list, detail, isPopping: false));

			Assert.Same(first, pair.Source);
		}

		[Fact]
		public void OptOutAtAppPageOrElementLevelDisablesTransition()
		{
			var (list, listImage) = CreatePage("hero");
			var (detail, _) = CreatePage("hero");

			Assert.Empty(SharedTransitionResolver.Resolve(list, detail, false, new SharedTransitionOptions { IsEnabled = false }));

			SharedTransition.SetIsEnabled(detail, false);
			Assert.Empty(SharedTransitionResolver.Resolve(list, detail, false));
			detail.ClearValue(SharedTransition.IsEnabledProperty);

			SharedTransition.SetIsEnabled(listImage, false);
			Assert.Empty(SharedTransitionResolver.Resolve(list, detail, false));
		}

		[Fact]
		public void SettingsPrecedenceIsElementThenPageThenApp()
		{
			var options = new SharedTransitionOptions { DefaultDuration = 100, DefaultEasing = Easing.Linear };
			var (list, listImage) = CreatePage("hero");
			var (detail, detailImage) = CreatePage("hero");

			var pair = SharedTransitionResolver.Resolve(list, detail, false, options).Single();
			Assert.Equal(100u, pair.Duration);
			Assert.Same(Easing.Linear, pair.Easing);

			SharedTransition.SetDuration(detail, 200);
			SharedTransition.SetEasing(detail, Easing.SinIn);
			pair = SharedTransitionResolver.Resolve(list, detail, false, options).Single();
			Assert.Equal(200u, pair.Duration);
			Assert.Same(Easing.SinIn, pair.Easing);

			SharedTransition.SetDuration(listImage, 300);
			pair = SharedTransitionResolver.Resolve(list, detail, false, options).Single();
			Assert.Equal(300u, pair.Duration);

			SharedTransition.SetDuration(detailImage, 400);
			SharedTransition.SetEasing(detailImage, Easing.BounceOut);
			pair = SharedTransitionResolver.Resolve(list, detail, false, options).Single();
			Assert.Equal(400u, pair.Duration);
			Assert.Same(Easing.BounceOut, pair.Easing);
		}

		[Fact]
		public void PopUsesSettingsFromPoppedPage()
		{
			var options = new SharedTransitionOptions { DefaultDuration = 100 };
			var (list, _) = CreatePage("hero");
			var (detail, _) = CreatePage("hero");
			SharedTransition.SetDuration(list, 999);
			SharedTransition.SetDuration(detail, 250);

			Assert.Equal(250u, SharedTransitionResolver.Resolve(detail, list, isPopping: true, options).Single().Duration);
		}
	}
}
