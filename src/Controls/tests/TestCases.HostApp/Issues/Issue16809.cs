namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 16809, "Label().LoadFromXaml does not work when using Label.FormattedText", PlatformAffected.All)]
public class Issue16809 : TestContentPage
{
	/// <summary>
	/// Taken from https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/label#use-formatted-text with the guesture recognizer removed.
	/// </summary>
	static string xamlwithFormattedString = """
		  <Label  LineBreakMode="WordWrap">
		    <Label.FormattedText>
		        <FormattedString>
		            <Span Text="Red Bold, " TextColor="Red" FontAttributes="Bold" />
		            <Span Text="default, " FontSize="14"/>		          
		            <Span Text="italic small." FontAttributes="Italic" FontSize="12" />
		        </FormattedString>
		    </Label.FormattedText>
		</Label>
		""";

	static string xamlwithoutFormattedString = """
		  <Label Text="Red Bold, " TextColor="Red" FontAttributes="Bold" LineBreakMode="WordWrap">
		</Label>
		""";

	protected override void Init()
	{
		var verticalStackLayout = new VerticalStackLayout();
		var labelWithFormattedString = new Label().LoadFromXaml(xamlwithFormattedString);
		if (labelWithFormattedString.FormattedText is null)
		{
			throw new Exception("This does not work");
		}
		verticalStackLayout.Add(labelWithFormattedString);
		var labelWithoutFormattedString = new Label().LoadFromXaml(xamlwithoutFormattedString);
		verticalStackLayout.Add(labelWithoutFormattedString);
		Content = verticalStackLayout;
	}
}
