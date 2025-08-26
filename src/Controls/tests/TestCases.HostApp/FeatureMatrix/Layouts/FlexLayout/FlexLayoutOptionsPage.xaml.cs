using Microsoft.Maui.Layouts;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample;

public partial class FlexLayoutOptionsPage : ContentPage
{
    private FlexLayoutViewModel _viewModel;

    public FlexLayoutOptionsPage(FlexLayoutViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public async void ApplyButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // Direction handlers
    public void OnDirectionRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            _viewModel.Direction = radioButton.Content.ToString() switch
            {
                "Row" => FlexDirection.Row,
                "Column" => FlexDirection.Column,
                "RowReverse" => FlexDirection.RowReverse,
                "ColumnReverse" => FlexDirection.ColumnReverse,
                _ => FlexDirection.Row
            };
        }
    }

    // JustifyContent handlers
    public void OnJustifyContentRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            _viewModel.JustifyContent = radioButton.Content.ToString() switch
            {
                "Start" => FlexJustify.Start,
                "Center" => FlexJustify.Center,
                "End" => FlexJustify.End,
                "SpaceBetween" => FlexJustify.SpaceBetween,
                "SpaceAround" => FlexJustify.SpaceAround,
                "SpaceEvenly" => FlexJustify.SpaceEvenly,
                _ => FlexJustify.Start
            };
        }
    }

    // AlignContent handlers
    public void OnAlignContentRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            _viewModel.AlignContent = radioButton.Content.ToString() switch
            {
                "Stretch" => FlexAlignContent.Stretch,
                "Center" => FlexAlignContent.Center,
                "Start" => FlexAlignContent.Start,
                "End" => FlexAlignContent.End,
                "SpaceBetween" => FlexAlignContent.SpaceBetween,
                "SpaceAround" => FlexAlignContent.SpaceAround,
                "SpaceEvenly" => FlexAlignContent.SpaceEvenly,
                _ => FlexAlignContent.Stretch
            };
        }
    }

    // AlignItems handlers
    public void OnAlignItemsRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            _viewModel.AlignItems = radioButton.Content.ToString() switch
            {
                "Stretch" => FlexAlignItems.Stretch,
                "Center" => FlexAlignItems.Center,
                "Start" => FlexAlignItems.Start,
                "End" => FlexAlignItems.End,
                _ => FlexAlignItems.Stretch
            };
        }
    }

    // Position handlers
    public void OnPositionRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            _viewModel.Position = radioButton.Content.ToString() switch
            {
                "Relative" => FlexPosition.Relative,
                "Absolute" => FlexPosition.Absolute,
                _ => FlexPosition.Relative
            };
        }
    }

    // Wrap handlers
    public void OnWrapRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            _viewModel.Wrap = radioButton.Content.ToString() switch
            {
                "NoWrap" => FlexWrap.NoWrap,
                "Wrap" => FlexWrap.Wrap,
                "Reverse" => FlexWrap.Reverse,
                _ => FlexWrap.NoWrap
            };
        }
    }

    // Child 1 AlignSelf handlers
    public void OnChild1AlignSelfRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            _viewModel.Child1AlignSelf = radioButton.Content.ToString() switch
            {
                "Auto" => FlexAlignSelf.Auto,
                "Stretch" => FlexAlignSelf.Stretch,
                "Center" => FlexAlignSelf.Center,
                "Start" => FlexAlignSelf.Start,
                "End" => FlexAlignSelf.End,
                _ => FlexAlignSelf.Auto
            };
        }
    }

    // Child 2 AlignSelf handlers
    public void OnChild2AlignSelfRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            _viewModel.Child2AlignSelf = radioButton.Content.ToString() switch
            {
                "Auto" => FlexAlignSelf.Auto,
                "Stretch" => FlexAlignSelf.Stretch,
                "Center" => FlexAlignSelf.Center,
                "Start" => FlexAlignSelf.Start,
                "End" => FlexAlignSelf.End,
                _ => FlexAlignSelf.Auto
            };
        }
    }

    // Basis test handlers
    public void OnBasisTestRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            switch (radioButton.Content.ToString())
            {
                case "Auto":
                    _viewModel.Child1Basis = FlexBasis.Auto;
                    _viewModel.Child2Basis = FlexBasis.Auto;
                    _viewModel.Child3Basis = FlexBasis.Auto;
                    break;
                case "Fixed":
                    _viewModel.Child1Basis = new FlexBasis(100);
                    _viewModel.Child2Basis = new FlexBasis(150);
                    _viewModel.Child3Basis = new FlexBasis(80);
                    break;
                case "Relative":
                    _viewModel.Child1Basis = new FlexBasis(0.3f, true);
                    _viewModel.Child2Basis = new FlexBasis(0.4f, true);
                    _viewModel.Child3Basis = new FlexBasis(0.3f, true);
                    break;
            }
        }
    }

    // Layout test handlers for complex scenarios
    public void OnLayoutTestRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            switch (radioButton.Content.ToString())
            {
                case "Complex1":
                    // Test scenario: Column direction with wrap, mixed grow/shrink values
                    _viewModel.Direction = FlexDirection.Column;
                    _viewModel.Wrap = FlexWrap.Wrap;
                    _viewModel.JustifyContent = FlexJustify.SpaceBetween;
                    _viewModel.AlignItems = FlexAlignItems.Center;
                    _viewModel.Child1Grow = 1f;
                    _viewModel.Child2Grow = 2f;
                    _viewModel.Child3Grow = 1f;
                    _viewModel.Child1Order = 3;
                    _viewModel.Child2Order = 1;
                    _viewModel.Child3Order = 2;
                    break;
                case "Complex2":
                    // Test scenario: Row direction with different align self values
                    _viewModel.Direction = FlexDirection.Row;
                    _viewModel.Wrap = FlexWrap.NoWrap;
                    _viewModel.JustifyContent = FlexJustify.Center;
                    _viewModel.AlignItems = FlexAlignItems.Stretch;
                    _viewModel.Child1AlignSelf = FlexAlignSelf.Start;
                    _viewModel.Child2AlignSelf = FlexAlignSelf.Center;
                    _viewModel.Child3AlignSelf = FlexAlignSelf.End;
                    _viewModel.Child1Shrink = 0f;
                    _viewModel.Child2Shrink = 1f;
                    _viewModel.Child3Shrink = 2f;
                    break;
                case "Reset":
                    // Reset to defaults
                    _viewModel.Direction = FlexDirection.Row;
                    _viewModel.JustifyContent = FlexJustify.Start;
                    _viewModel.AlignContent = FlexAlignContent.Stretch;
                    _viewModel.AlignItems = FlexAlignItems.Stretch;
                    _viewModel.Position = FlexPosition.Relative;
                    _viewModel.Wrap = FlexWrap.NoWrap;
                    _viewModel.Child1Grow = 0f;
                    _viewModel.Child2Grow = 0f;
                    _viewModel.Child3Grow = 0f;
                    _viewModel.Child1Shrink = 1f;
                    _viewModel.Child2Shrink = 1f;
                    _viewModel.Child3Shrink = 1f;
                    _viewModel.Child1Order = 0;
                    _viewModel.Child2Order = 0;
                    _viewModel.Child3Order = 0;
                    _viewModel.Child1AlignSelf = FlexAlignSelf.Auto;
                    _viewModel.Child2AlignSelf = FlexAlignSelf.Auto;
                    _viewModel.Child3AlignSelf = FlexAlignSelf.Auto;
                    _viewModel.Child1Basis = FlexBasis.Auto;
                    _viewModel.Child2Basis = FlexBasis.Auto;
                    _viewModel.Child3Basis = FlexBasis.Auto;
                    break;
            }
        }
    }
}
