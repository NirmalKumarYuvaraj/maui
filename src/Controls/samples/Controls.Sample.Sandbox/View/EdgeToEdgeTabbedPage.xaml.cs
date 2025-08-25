namespace Maui.Controls.Sample;

public partial class EdgeToEdgeTabbedPage : TabbedPage
{
    public EdgeToEdgeTabbedPage()
    {
        InitializeComponent();

        // Add test pages to tabs
        Children.Clear();
        Children.Add(new EdgeToEdgeTestPage("Tabbed Page - Tab 1") { Title = "Tab 1", IconImageSource = "tab1.png" });
        Children.Add(new EdgeToEdgeTestPage("Tabbed Page - Tab 2") { Title = "Tab 2", IconImageSource = "tab2.png" });
        Children.Add(new EdgeToEdgeTestPage("Tabbed Page - Tab 3") { Title = "Tab 3", IconImageSource = "tab3.png" });
    }
}
