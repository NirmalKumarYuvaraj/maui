using System.Windows.Input;

namespace Maui.Controls.Sample;

public partial class EdgeToEdgeShellBottomTabs : Shell
{
    public ICommand ResetCommand { get; }

    public EdgeToEdgeShellBottomTabs()
    {
        InitializeComponent();
        ResetCommand = new Command(OnResetToMain);
        BindingContext = this;
    }
    private void OnResetToMain()
    {
        var window = Application.Current?.Windows?.FirstOrDefault();
        if (window != null)
        {
            window.Page = new NavigationPage(new MainPage());
        }
    }
}
