using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Controls.Sample;

public partial class EdgeToEdgeTestPageDynamicChanges : ContentPage
{
    public EdgeToEdgeTestPageDynamicChanges(string scenarioTitle)
    {
        InitializeComponent();
    }

    private NavigationPage? GetParentNavigationPage()
    {
        Element parent = Parent;
        while (parent != null)
        {
            if (parent is NavigationPage navPage)
            {
                return navPage;
            }
            parent = parent.Parent;
        }
        return null;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
    }
    private async void OnCloseModalClicked(object sender, EventArgs e)
    {
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
        }
    }


    #region Navigation Feature Button Handlers

    // Row 1
    private async void OnPushAsyncClicked(object sender, EventArgs e)
    {
        var newPage = new ContentPage
        {
            Title = "Pushed Page",
            BackgroundColor = Colors.AliceBlue,
            Content = new StackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = "This page was pushed asynchronously",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        FontSize = 18,
                        Margin = new Thickness(20)
                    },
                    new Button
                    {
                        Text = "Go Back",
                        HorizontalOptions = LayoutOptions.Center,
                        Command = new Command(async () => await Navigation.PopAsync())
                    }
                },
                VerticalOptions = LayoutOptions.Center
            }
        };

        await Navigation.PushAsync(newPage);
        UpdateConfigStatus("Pushed a new page to the stack");
    }

    private async void OnPopAsyncClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
            UpdateConfigStatus("Popped a page from the stack");
        }
        else
        {
            await DisplayAlertAsync("Navigation", "No pages to pop from the stack", "OK");
        }
    }

    // Row 2
    private async void OnPushModalClicked(object sender, EventArgs e)
    {
        var modalPage = new ContentPage
        {
            Title = "Modal Page",
            BackgroundColor = Colors.Lavender,
            Content = new StackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = "This is a modal page",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        FontSize = 18,
                        Margin = new Thickness(20)
                    },
                    new Button
                    {
                        Text = "Close Modal",
                        HorizontalOptions = LayoutOptions.Center,
                        Command = new Command(async () => await Navigation.PopModalAsync())
                    }
                },
                VerticalOptions = LayoutOptions.Center
            }
        };

        await Navigation.PushModalAsync(modalPage);
        UpdateConfigStatus("Pushed a modal page");
    }

    private async void OnPopModalClicked(object sender, EventArgs e)
    {
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
            UpdateConfigStatus("Popped a modal page");
        }
        else
        {
            await DisplayAlertAsync("Navigation", "No modal pages to pop", "OK");
        }
    }

    // Row 3
    private bool _isNavBarVisible = true;
    private void OnToggleNavBarClicked(object sender, EventArgs e)
    {
        _isNavBarVisible = !_isNavBarVisible;
        NavigationPage.SetHasNavigationBar(this, _isNavBarVisible);
        UpdateConfigStatus($"Navigation bar visibility: {(_isNavBarVisible ? "Visible" : "Hidden")}");
    }

    private bool _hasBackButton = true;
    private void OnToggleBackButtonClicked(object sender, EventArgs e)
    {
        _hasBackButton = !_hasBackButton;
        NavigationPage.SetHasBackButton(this, _hasBackButton);
        UpdateConfigStatus($"Back button visibility: {(_hasBackButton ? "Visible" : "Hidden")}");
    }

    // Row 4
    private int _barColorIndex = 0;
    private void OnChangeBarBackgroundColorClicked(object sender, EventArgs e)
    {
        var colors = new[]
        {
            Colors.Blue,
            Colors.Red,
            Colors.Green,
            Colors.Purple,
            Colors.Orange,
            Colors.Teal
        };

        _barColorIndex = (_barColorIndex + 1) % colors.Length;
        var navPage = GetParentNavigationPage();
        if (navPage != null)
        {
            navPage.BarBackgroundColor = colors[_barColorIndex];
            UpdateConfigStatus($"Changed bar background to {colors[_barColorIndex]}");
        }
    }

    private int _gradientIndex = 0;
    private void OnChangeBarBackgroundClicked(object sender, EventArgs e)
    {
        // Define simpler solid colors as fallback
        var colors = new[]
        {
            Colors.DarkBlue,
            Colors.Purple,
            Colors.DarkRed,
            Colors.DarkGreen
        };

        _gradientIndex = (_gradientIndex + 1) % colors.Length;
        var navPage = GetParentNavigationPage();
        if (navPage != null)
        {
            try
            {
                // First try with LinearGradientBrush
                var brush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Colors.White, Offset = 0.0f },
                        new GradientStop { Color = colors[_gradientIndex], Offset = 1.0f }
                    }
                };
                navPage.BarBackground = brush;
                UpdateConfigStatus($"Changed bar background to gradient");
            }
            catch (Exception ex)
            {
                // Fallback to solid color if gradient doesn't work
                navPage.BarBackgroundColor = colors[_gradientIndex];
                UpdateConfigStatus($"Changed bar to solid color (gradient failed: {ex.Message})");
            }
        }
    }

    // Row 5
    private bool _toolbarItemsAdded = false;
    private void OnAddToolbarItemsClicked(object sender, EventArgs e)
    {
        if (!_toolbarItemsAdded)
        {
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Save",
                IconImageSource = "save.png",
                Order = ToolbarItemOrder.Primary,
                Command = new Command(async () =>
                {
                    await DisplayAlertAsync("Toolbar Action", "Save clicked", "OK");
                })
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Edit",
                IconImageSource = "edit.png",
                Order = ToolbarItemOrder.Primary,
                Command = new Command(async () =>
                {
                    await DisplayAlertAsync("Toolbar Action", "Edit clicked", "OK");
                })
            });

            _toolbarItemsAdded = true;
            UpdateConfigStatus("Added primary toolbar items");
        }
        else
        {
            // Remove toolbar items
            while (ToolbarItems.Count > 0)
            {
                ToolbarItems.RemoveAt(0);
            }
            _toolbarItemsAdded = false;
            _secondaryItemsAdded = false;
            UpdateConfigStatus("Removed all toolbar items");
        }
    }

    private bool _secondaryItemsAdded = false;
    private void OnAddSecondaryItemsClicked(object sender, EventArgs e)
    {
        if (!_secondaryItemsAdded)
        {
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Settings",
                IconImageSource = "settings.png",
                Order = ToolbarItemOrder.Secondary,
                Command = new Command(async () =>
                {
                    await DisplayAlertAsync("Toolbar Action", "Settings clicked", "OK");
                })
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Help",
                IconImageSource = "help.png",
                Order = ToolbarItemOrder.Secondary,
                Command = new Command(async () =>
                {
                    await DisplayAlertAsync("Toolbar Action", "Help clicked", "OK");
                })
            });

            _secondaryItemsAdded = true;
            UpdateConfigStatus("Added secondary toolbar items");
        }
        else
        {
            // Remove only secondary items
            for (int i = ToolbarItems.Count - 1; i >= 0; i--)
            {
                if (ToolbarItems[i].Order == ToolbarItemOrder.Secondary)
                {
                    ToolbarItems.RemoveAt(i);
                }
            }
            _secondaryItemsAdded = false;
            UpdateConfigStatus("Removed secondary toolbar items");
        }
    }

    // Row 6
    private bool _titleViewSet = false;
    private void OnSetTitleViewClicked(object sender, EventArgs e)
    {
        var navPage = GetParentNavigationPage();
        if (navPage != null)
        {
            if (!_titleViewSet)
            {
                // Create custom title view
                var titleView = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Image
                        {
                            Source = "xamarin_128x128.png",
                            HeightRequest = 30
                        },
                        new Label
                        {
                            Text = "Custom Title",
                            TextColor = Colors.White,
                            FontAttributes = FontAttributes.Bold,
                            VerticalTextAlignment = TextAlignment.Center,
                            Margin = new Thickness(5, 0, 0, 0)
                        }
                    }
                };

                NavigationPage.SetTitleView(this, titleView);
                _titleViewSet = true;
                UpdateConfigStatus("Set custom title view");
            }
            else
            {
                NavigationPage.SetTitleView(this, null);
                _titleViewSet = false;
                UpdateConfigStatus("Removed custom title view");
            }
        }
    }

    private bool _titleIconSet = false;
    private void OnSetTitleIconClicked(object sender, EventArgs e)
    {
        if (!_titleIconSet)
        {
            // Use an embedded resource image that definitely exists in MAUI
            NavigationPage.SetTitleIconImageSource(this, new FileImageSource { File = "dotnet_bot.png" });
            _titleIconSet = true;
            UpdateConfigStatus("Set title icon to dotnet_bot.png");
        }
        else
        {
            NavigationPage.SetTitleIconImageSource(this, null);
            _titleIconSet = false;
            UpdateConfigStatus("Removed title icon");
        }
    }

    // Row 7
    private bool _barSeparatorHidden = false;
    private void OnToggleBarSeparatorClicked(object sender, EventArgs e)
    {
        _barSeparatorHidden = !_barSeparatorHidden;

        try
        {
            var navPage = GetParentNavigationPage();
            if (navPage != null)
            {
                // Direct implementation of HideNavigationBarSeparator
                if (_barSeparatorHidden)
                {
                    // To hide separator, we'll use a transparent background color
                    // and set the bar background to a solid color with no border
                    navPage.BackgroundColor = Colors.Transparent;

                    // Create a solid color brush with no bottom border
                    var solidColorBrush = new SolidColorBrush(Colors.DarkBlue);
                    navPage.BarBackground = solidColorBrush;
                }
                else
                {
                    // To show separator, use default system values
                    navPage.BackgroundColor = null;
                    navPage.BarBackground = null;
                    navPage.BarBackgroundColor = Colors.LightSlateGray;
                }

                UpdateConfigStatus($"Navigation bar separator: {(_barSeparatorHidden ? "Hidden" : "Visible")}");
            }
        }
        catch (Exception ex)
        {
            UpdateConfigStatus($"Error toggling bar separator: {ex.Message}");
        }
    }
    private int _barHeightState = 0;
    private void OnChangeBarHeightClicked(object sender, EventArgs e)
    {
        var navPage = GetParentNavigationPage();
        if (navPage != null)
        {
            _barHeightState = (_barHeightState + 1) % 3;

            // Define height values for different states (in device-independent pixels)
            int[] heights = { 56, 80, 40 }; // Normal, Tall, Short
            string[] labels = { "Normal", "Tall", "Short" };

#if ANDROID
            // There is no SetBarHeight API for NavigationPage in .NET MAUI.
            // You may need to use a custom renderer or platform-specific code for advanced bar height changes.
            // For now, skip this block and use the scaling approach below.
#endif

            // Alternative approach for all platforms
            try
            {
                switch (_barHeightState)
                {
                    case 0: // Normal size
                        // Try to set the height using reflection for Android
                        var androidNavPage = navPage.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>();
                        var setBarHeightMethod = androidNavPage.GetType().GetMethod("SetBarHeight");
                        if (setBarHeightMethod != null)
                        {
                            setBarHeightMethod.Invoke(androidNavPage, new object[] { heights[_barHeightState] });
                            UpdateConfigStatus($"Set bar height to {heights[_barHeightState]}dp (Normal)");
                        }
                        else
                        {
                            navPage.Scale = 1.0;
                            UpdateConfigStatus("Set bar to normal height (via scaling)");
                        }
                        break;

                    case 1: // Taller
                        navPage.Scale = 1.05;
                        UpdateConfigStatus("Set bar to taller height (via scaling)");
                        break;

                    case 2: // Shorter
                        navPage.Scale = 0.95;
                        UpdateConfigStatus("Set bar to shorter height (via scaling)");
                        break;
                }
            }
            catch (Exception ex)
            {
                UpdateConfigStatus($"Error changing bar height: {ex.Message}");
            }
        }
        else
        {
            UpdateConfigStatus("NavigationPage not found for bar height change");
        }
    }

    // Row 8
    private void OnSetBackButtonTitleClicked(object sender, EventArgs e)
    {
        var title = DateTime.Now.ToString("HH:mm:ss");

        try
        {
            var navPage = GetParentNavigationPage();
            if (navPage != null)
            {
                // For iOS, try the standard API
                // This only works when this page is the target of navigation (not the current page)
                NavigationPage.SetBackButtonTitle(this, title);

                // Most important approach: Set title of previous page
                var navStack = Navigation.NavigationStack;
                if (navStack.Count > 1)
                {
                    // Get the page before this one
                    int previousIndex = navStack.Count - 2;
                    if (previousIndex >= 0 && previousIndex < navStack.Count)
                    {
                        var previousPage = navStack[previousIndex];
                        if (previousPage != null)
                        {
                            // Setting the title of the previous page affects the back button on this page
                            previousPage.Title = title;

                            // For Android, we need to push a new page to see the effect
                            var demoPage = new ContentPage
                            {
                                Title = "Back Button Demo",
                                Content = new VerticalStackLayout
                                {
                                    Children = {
                                        new Label {
                                            Text = $"The back button should say '{title}'",
                                            HorizontalOptions = LayoutOptions.Center,
                                            VerticalOptions = LayoutOptions.Center
                                        },
                                        new Button {
                                            Text = "Go Back",
                                            Command = new Command(async () => await Navigation.PopAsync()),
                                            HorizontalOptions = LayoutOptions.Center,
                                            Margin = new Thickness(0, 20, 0, 0)
                                        }
                                    },
                                    VerticalOptions = LayoutOptions.Center,
                                    HorizontalOptions = LayoutOptions.Center,
                                    Spacing = 20
                                }
                            };

                            // Push the demo page to show the back button
                            Navigation.PushAsync(demoPage);
                        }
                    }
                }
                else
                {
                    // Create a temporary page to show the effect
                    var tempPage = new ContentPage
                    {
                        Title = "Temporary Page"
                    };

                    // Push the temporary page
                    Navigation.PushAsync(tempPage).ContinueWith(async (t) =>
                    {
                        // This page is now in the stack
                        this.Title = title;

                        // Create the demo page
                        var demoPage = new ContentPage
                        {
                            Title = "Back Button Demo",
                            Content = new VerticalStackLayout
                            {
                                Children = {
                                    new Label {
                                        Text = $"The back button should say '{title}'",
                                        HorizontalOptions = LayoutOptions.Center,
                                        VerticalOptions = LayoutOptions.Center
                                    },
                                    new Button {
                                        Text = "Go Back Twice",
                                        Command = new Command(async () => {
                                            await Navigation.PopAsync();
                                            await Navigation.PopAsync();
                                        }),
                                        HorizontalOptions = LayoutOptions.Center,
                                        Margin = new Thickness(0, 20, 0, 0)
                                    }
                                },
                                VerticalOptions = LayoutOptions.Center,
                                HorizontalOptions = LayoutOptions.Center,
                                Spacing = 20
                            }
                        };

                        // Push the demo page
                        await Navigation.PushAsync(demoPage);
                    });
                }

                UpdateConfigStatus($"Set back button title to: {title}");
            }
            else
            {
                UpdateConfigStatus("NavigationPage not found for back button title");
            }
        }
        catch (Exception ex)
        {
            UpdateConfigStatus($"Error setting back button title: {ex.Message}");
        }
    }

    private int _barTextColorIndex = 0;
    private void OnChangeBarTextColorClicked(object sender, EventArgs e)
    {
        var colors = new[]
        {
            Colors.White,
            Colors.Black,
            Colors.Yellow,
            Colors.Lime,
            Colors.Cyan
        };

        _barTextColorIndex = (_barTextColorIndex + 1) % colors.Length;
        var navPage = GetParentNavigationPage();
        if (navPage != null)
        {
            navPage.BarTextColor = colors[_barTextColorIndex];
            UpdateConfigStatus($"Changed bar text color");
        }
    }

    // Row 9
    private bool _tooltipsAdded = false;
    private void OnAddToolbarTooltipsClicked(object sender, EventArgs e)
    {
        if (!_tooltipsAdded)
        {
            var saveItem = new ToolbarItem
            {
                Text = "Save",
                IconImageSource = "save.png",
                Command = new Command(async () => await DisplayAlertAsync("Tooltip Item", "Save clicked", "OK"))
            };

            var editItem = new ToolbarItem
            {
                Text = "Edit",
                IconImageSource = "edit.png",
                Command = new Command(async () => await DisplayAlertAsync("Tooltip Item", "Edit clicked", "OK"))
            };

            // Set tooltip text using automation properties
            AutomationProperties.SetHelpText(saveItem, "Save your changes");
            AutomationProperties.SetHelpText(editItem, "Edit this document");

            ToolbarItems.Add(saveItem);
            ToolbarItems.Add(editItem);

            _tooltipsAdded = true;
            UpdateConfigStatus("Added toolbar items with tooltips");
        }
        else
        {
            // Remove toolbar items
            while (ToolbarItems.Count > 0)
            {
                ToolbarItems.RemoveAt(0);
            }
            _tooltipsAdded = false;
            _toolbarItemsAdded = false;
            _secondaryItemsAdded = false;
            UpdateConfigStatus("Removed all toolbar items");
        }
    }

    private void OnFullComboTestClicked(object sender, EventArgs e)
    {
        var navPage = GetParentNavigationPage();
        if (navPage != null)
        {
            // Set a combination of navigation page features
            navPage.BarTextColor = Colors.White;
            navPage.BarBackgroundColor = Colors.DarkBlue;

            NavigationPage.SetTitleIconImageSource(this, "xamarin_128x128.png");

            // Clear and add new toolbar items
            while (ToolbarItems.Count > 0)
            {
                ToolbarItems.RemoveAt(0);
            }

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Save",
                IconImageSource = "save.png",
                Order = ToolbarItemOrder.Primary,
                Command = new Command(async () => await DisplayAlertAsync("Action", "Save clicked", "OK"))
            });

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Settings",
                IconImageSource = "settings.png",
                Order = ToolbarItemOrder.Secondary,
                Command = new Command(async () => await DisplayAlertAsync("Action", "Settings clicked", "OK"))
            });

            UpdateConfigStatus("Applied full combo test with multiple features");
        }
    }

    #endregion

    // Helper methods
    private void UpdateConfigStatus(string status)
    {
        if (ConfigStatusLabel != null)
        {
            ConfigStatusLabel.Text = $"{status} ({DateTime.Now.ToString("HH:mm:ss")})";
        }

        if (SystemInfoLabel != null)
        {
            var deviceInfo = DeviceInfo.Current;
            SystemInfoLabel.Text = $"Platform: {deviceInfo.Platform}\n" +
                                  $"OS Version: {deviceInfo.VersionString}\n" +
                                  $"Device Type: {deviceInfo.Idiom}\n" +
                                  $"Manufacturer: {deviceInfo.Manufacturer}";
        }
    }
}
