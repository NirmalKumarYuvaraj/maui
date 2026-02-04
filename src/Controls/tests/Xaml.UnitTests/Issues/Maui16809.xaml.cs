using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Xaml.UnitTests
{
	/// <summary>
	/// Tests for GitHub issue #16809: Label().LoadFromXaml does not work when using Label.FormattedText
	/// The fix enables implicit xmlns for runtime XAML loading, allowing XAML strings to work without
	/// explicit xmlns declarations.
	/// </summary>
	public class Maui16809
	{
		/// <summary>
		/// Tests that LoadFromXaml works with FormattedText without requiring xmlns declaration.
		/// This is the core scenario from issue #16809.
		/// </summary>
		[Fact]
		public void LoadFromXamlWithFormattedTextWithoutXmlns()
		{
			// XAML without xmlns declaration - this is the key test case
			var xaml = """
				<Label LineBreakMode="WordWrap">
				  <Label.FormattedText>
				    <FormattedString>
				      <Span Text="Red Bold, " TextColor="Red" FontAttributes="Bold" />
				      <Span Text="default, " FontSize="14"/>
				      <Span Text="italic small." FontAttributes="Italic" FontSize="12" />
				    </FormattedString>
				  </Label.FormattedText>
				</Label>
				""";

			var label = new Label().LoadFromXaml(xaml);

			Assert.NotNull(label);
			Assert.NotNull(label.FormattedText);
			Assert.Equal(3, label.FormattedText.Spans.Count);
			Assert.Equal("Red Bold, ", label.FormattedText.Spans[0].Text);
			Assert.Equal(Colors.Red, label.FormattedText.Spans[0].TextColor);
			Assert.Equal(FontAttributes.Bold, label.FormattedText.Spans[0].FontAttributes);
			Assert.Equal("default, ", label.FormattedText.Spans[1].Text);
			Assert.Equal("italic small.", label.FormattedText.Spans[2].Text);
			Assert.Equal(FontAttributes.Italic, label.FormattedText.Spans[2].FontAttributes);
		}

		/// <summary>
		/// Tests that simple Label attributes work without xmlns (this already worked before the fix)
		/// </summary>
		[Fact]
		public void LoadFromXamlSimpleLabelWithoutXmlns()
		{
			var xaml = """
				<Label Text="Hello World" TextColor="Red" FontAttributes="Bold" />
				""";

			var label = new Label().LoadFromXaml(xaml);

			Assert.NotNull(label);
			Assert.Equal("Hello World", label.Text);
			Assert.Equal(Colors.Red, label.TextColor);
			Assert.Equal(FontAttributes.Bold, label.FontAttributes);
		}

		/// <summary>
		/// Tests that LoadFromXaml still works with explicit xmlns (backwards compatibility)
		/// </summary>
		[Fact]
		public void LoadFromXamlWithFormattedTextWithXmlns()
		{
			var xaml = """
				<Label xmlns="http://schemas.microsoft.com/dotnet/2021/maui" LineBreakMode="WordWrap">
				  <Label.FormattedText>
				    <FormattedString>
				      <Span Text="With xmlns" TextColor="Blue" />
				    </FormattedString>
				  </Label.FormattedText>
				</Label>
				""";

			var label = new Label().LoadFromXaml(xaml);

			Assert.NotNull(label);
			Assert.NotNull(label.FormattedText);
			Assert.Single(label.FormattedText.Spans);
			Assert.Equal("With xmlns", label.FormattedText.Spans[0].Text);
		}
	}
}
