namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32125, "TabBarUnselectedColor is not applied to the tab icon on iOS 26", PlatformAffected.iOS)]
public class Issue32125 : TestShell
{
    protected override void Init()
    {
        SetValue(Shell.TabBarUnselectedColorProperty, Colors.Blue);
        SetValue(Shell.TabBarForegroundColorProperty, Colors.Green);

        var tabBar = new TabBar();

        tabBar.Items.Add(new Tab
        {
            Title = "Tab 1",
            Icon = "coffee.png",
            Items = { new ShellContent { Content = new ContentPage { Content = new Label { Text = "Tab 1 Content", AutomationId = "Tab1Label" } } } }
        });

        tabBar.Items.Add(new Tab
        {
            Title = "Tab 2",
            Icon = "calculator.png",
            Items = { new ShellContent { Content = new ContentPage { Content = new Label { Text = "Tab 2 Content", AutomationId = "Tab2Label" } } } }
        });

        tabBar.Items.Add(new Tab
        {
            Title = "Tab 3",
            Icon = "bank.png",
            Items = { new ShellContent { Content = new ContentPage { Content = new Label { Text = "Tab 3 Content", AutomationId = "Tab3Label" } } } }
        });

        Items.Add(tabBar);
    }
}