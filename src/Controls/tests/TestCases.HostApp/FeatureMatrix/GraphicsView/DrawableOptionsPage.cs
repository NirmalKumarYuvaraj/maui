using System;

namespace Maui.Controls.Sample;

public class DrawableOptionsPage : ContentPage
{
    private GraphicsViewViewModel _viewModel;

    public DrawableOptionsPage(GraphicsViewViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    override protected void OnAppearing()
    {
        base.OnAppearing();
        // Update the UI based on the current drawable type
        UpdateDrawableOptionsUI();
    }

    private void UpdateDrawableOptionsUI()
    {
        var button = new Button
        {
            Text = "Set Stroke Color to Red",
            Command = new Command(() => (_viewModel.Drawable as FeatureMartixDrawable)?.SetStrokeColor(Colors.Red))
        };
        Content = new StackLayout
        {
            Children =
            {
                button,
            }
        };
    }
}
