using System;
using Microsoft.Maui.Controls.Shapes;

namespace Maui.Controls.Sample;

public class UITestHostControl : ContentView
{
    public static readonly BindableProperty InnerViewProperty =
        BindableProperty.Create(
            nameof(InnerView),
            typeof(View),
            typeof(UITestHostControl),
            propertyChanged: OnInnerViewChanged);

    public View InnerView
    {
        get => (View)GetValue(InnerViewProperty);
        set => SetValue(InnerViewProperty, value);
    }

    static void OnInnerViewChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (UITestHostControl)bindable;
        control.Content = (View)newValue;
        control.ApplyAllForwardedProperties();
    }

    // Forwarding properties — these apply values to Content instead of the host
    public static readonly BindableProperty TargetRotationProperty =
        BindableProperty.Create(nameof(TargetRotation), typeof(double), typeof(UITestHostControl), 0.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.RotationProperty, n));
    public double TargetRotation { get => (double)GetValue(TargetRotationProperty); set => SetValue(TargetRotationProperty, value); }

    public static readonly BindableProperty TargetRotationXProperty =
        BindableProperty.Create(nameof(TargetRotationX), typeof(double), typeof(UITestHostControl), 0.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.RotationXProperty, n));
    public double TargetRotationX { get => (double)GetValue(TargetRotationXProperty); set => SetValue(TargetRotationXProperty, value); }

    public static readonly BindableProperty TargetRotationYProperty =
        BindableProperty.Create(nameof(TargetRotationY), typeof(double), typeof(UITestHostControl), 0.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.RotationYProperty, n));
    public double TargetRotationY { get => (double)GetValue(TargetRotationYProperty); set => SetValue(TargetRotationYProperty, value); }

    public static readonly BindableProperty TargetScaleProperty =
        BindableProperty.Create(nameof(TargetScale), typeof(double), typeof(UITestHostControl), 1.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.ScaleProperty, n));
    public double TargetScale { get => (double)GetValue(TargetScaleProperty); set => SetValue(TargetScaleProperty, value); }

    public static readonly BindableProperty TargetScaleXProperty =
        BindableProperty.Create(nameof(TargetScaleX), typeof(double), typeof(UITestHostControl), 1.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.ScaleXProperty, n));
    public double TargetScaleX { get => (double)GetValue(TargetScaleXProperty); set => SetValue(TargetScaleXProperty, value); }

    public static readonly BindableProperty TargetScaleYProperty =
        BindableProperty.Create(nameof(TargetScaleY), typeof(double), typeof(UITestHostControl), 1.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.ScaleYProperty, n));
    public double TargetScaleY { get => (double)GetValue(TargetScaleYProperty); set => SetValue(TargetScaleYProperty, value); }

    public static readonly BindableProperty TargetTranslationXProperty =
        BindableProperty.Create(nameof(TargetTranslationX), typeof(double), typeof(UITestHostControl), 0.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.TranslationXProperty, n));
    public double TargetTranslationX { get => (double)GetValue(TargetTranslationXProperty); set => SetValue(TargetTranslationXProperty, value); }

    public static readonly BindableProperty TargetTranslationYProperty =
        BindableProperty.Create(nameof(TargetTranslationY), typeof(double), typeof(UITestHostControl), 0.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.TranslationYProperty, n));
    public double TargetTranslationY { get => (double)GetValue(TargetTranslationYProperty); set => SetValue(TargetTranslationYProperty, value); }

    public static readonly BindableProperty TargetAnchorXProperty =
        BindableProperty.Create(nameof(TargetAnchorX), typeof(double), typeof(UITestHostControl), 0.5,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.AnchorXProperty, n));
    public double TargetAnchorX { get => (double)GetValue(TargetAnchorXProperty); set => SetValue(TargetAnchorXProperty, value); }

    public static readonly BindableProperty TargetAnchorYProperty =
        BindableProperty.Create(nameof(TargetAnchorY), typeof(double), typeof(UITestHostControl), 0.5,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.AnchorYProperty, n));
    public double TargetAnchorY { get => (double)GetValue(TargetAnchorYProperty); set => SetValue(TargetAnchorYProperty, value); }

    public static readonly BindableProperty TargetIsVisibleProperty =
        BindableProperty.Create(nameof(TargetIsVisible), typeof(bool), typeof(UITestHostControl), true,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.IsVisibleProperty, n));
    public bool TargetIsVisible { get => (bool)GetValue(TargetIsVisibleProperty); set => SetValue(TargetIsVisibleProperty, value); }

    public static readonly BindableProperty TargetShadowProperty =
        BindableProperty.Create(nameof(TargetShadow), typeof(Shadow), typeof(UITestHostControl), null,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.ShadowProperty, n));
    public Shadow TargetShadow { get => (Shadow)GetValue(TargetShadowProperty); set => SetValue(TargetShadowProperty, value); }

    public static readonly BindableProperty TargetOpacityProperty =
        BindableProperty.Create(nameof(TargetOpacity), typeof(double), typeof(UITestHostControl), 1.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.OpacityProperty, n));
    public double TargetOpacity { get => (double)GetValue(TargetOpacityProperty); set => SetValue(TargetOpacityProperty, value); }

    public static readonly BindableProperty TargetBackgroundColorProperty =
        BindableProperty.Create(nameof(TargetBackgroundColor), typeof(Color), typeof(UITestHostControl), null,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.BackgroundColorProperty, n));
    public Color TargetBackgroundColor { get => (Color)GetValue(TargetBackgroundColorProperty); set => SetValue(TargetBackgroundColorProperty, value); }

    public static readonly BindableProperty TargetBackgroundProperty =
        BindableProperty.Create(nameof(TargetBackground), typeof(Brush), typeof(UITestHostControl), Brush.Default,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.BackgroundProperty, n));
    public Brush TargetBackground { get => (Brush)GetValue(TargetBackgroundProperty); set => SetValue(TargetBackgroundProperty, value); }

    public static readonly BindableProperty TargetWidthRequestProperty =
        BindableProperty.Create(nameof(TargetWidthRequest), typeof(double), typeof(UITestHostControl), -1.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.WidthRequestProperty, n));
    public double TargetWidthRequest { get => (double)GetValue(TargetWidthRequestProperty); set => SetValue(TargetWidthRequestProperty, value); }

    public static readonly BindableProperty TargetHeightRequestProperty =
        BindableProperty.Create(nameof(TargetHeightRequest), typeof(double), typeof(UITestHostControl), -1.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.HeightRequestProperty, n));
    public double TargetHeightRequest { get => (double)GetValue(TargetHeightRequestProperty); set => SetValue(TargetHeightRequestProperty, value); }

    public static readonly BindableProperty TargetMinimumWidthRequestProperty =
        BindableProperty.Create(nameof(TargetMinimumWidthRequest), typeof(double), typeof(UITestHostControl), -1.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.MinimumWidthRequestProperty, n));
    public double TargetMinimumWidthRequest { get => (double)GetValue(TargetMinimumWidthRequestProperty); set => SetValue(TargetMinimumWidthRequestProperty, value); }

    public static readonly BindableProperty TargetMinimumHeightRequestProperty =
        BindableProperty.Create(nameof(TargetMinimumHeightRequest), typeof(double), typeof(UITestHostControl), -1.0,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.MinimumHeightRequestProperty, n));
    public double TargetMinimumHeightRequest { get => (double)GetValue(TargetMinimumHeightRequestProperty); set => SetValue(TargetMinimumHeightRequestProperty, value); }

    public static readonly BindableProperty TargetMaximumWidthRequestProperty =
        BindableProperty.Create(nameof(TargetMaximumWidthRequest), typeof(double), typeof(UITestHostControl), double.PositiveInfinity,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.MaximumWidthRequestProperty, n));
    public double TargetMaximumWidthRequest { get => (double)GetValue(TargetMaximumWidthRequestProperty); set => SetValue(TargetMaximumWidthRequestProperty, value); }

    public static readonly BindableProperty TargetMaximumHeightRequestProperty =
        BindableProperty.Create(nameof(TargetMaximumHeightRequest), typeof(double), typeof(UITestHostControl), double.PositiveInfinity,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.MaximumHeightRequestProperty, n));
    public double TargetMaximumHeightRequest { get => (double)GetValue(TargetMaximumHeightRequestProperty); set => SetValue(TargetMaximumHeightRequestProperty, value); }

    public static readonly BindableProperty TargetMarginProperty =
        BindableProperty.Create(nameof(TargetMargin), typeof(Thickness), typeof(UITestHostControl), default(Thickness),
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(View.MarginProperty, n));
    public Thickness TargetMargin { get => (Thickness)GetValue(TargetMarginProperty); set => SetValue(TargetMarginProperty, value); }

    public static readonly BindableProperty TargetHorizontalOptionsProperty =
        BindableProperty.Create(nameof(TargetHorizontalOptions), typeof(LayoutOptions), typeof(UITestHostControl), LayoutOptions.Fill,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(View.HorizontalOptionsProperty, n));
    public LayoutOptions TargetHorizontalOptions { get => (LayoutOptions)GetValue(TargetHorizontalOptionsProperty); set => SetValue(TargetHorizontalOptionsProperty, value); }

    public static readonly BindableProperty TargetVerticalOptionsProperty =
        BindableProperty.Create(nameof(TargetVerticalOptions), typeof(LayoutOptions), typeof(UITestHostControl), LayoutOptions.Fill,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(View.VerticalOptionsProperty, n));
    public LayoutOptions TargetVerticalOptions { get => (LayoutOptions)GetValue(TargetVerticalOptionsProperty); set => SetValue(TargetVerticalOptionsProperty, value); }

    public static readonly BindableProperty TargetIsEnabledProperty =
        BindableProperty.Create(nameof(TargetIsEnabled), typeof(bool), typeof(UITestHostControl), true,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.IsEnabledProperty, n));
    public bool TargetIsEnabled { get => (bool)GetValue(TargetIsEnabledProperty); set => SetValue(TargetIsEnabledProperty, value); }

    public static readonly BindableProperty TargetInputTransparentProperty =
        BindableProperty.Create(nameof(TargetInputTransparent), typeof(bool), typeof(UITestHostControl), false,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.InputTransparentProperty, n));
    public bool TargetInputTransparent { get => (bool)GetValue(TargetInputTransparentProperty); set => SetValue(TargetInputTransparentProperty, value); }

    public static readonly BindableProperty TargetFlowDirectionProperty =
        BindableProperty.Create(nameof(TargetFlowDirection), typeof(FlowDirection), typeof(UITestHostControl), FlowDirection.MatchParent,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.FlowDirectionProperty, n));
    public FlowDirection TargetFlowDirection { get => (FlowDirection)GetValue(TargetFlowDirectionProperty); set => SetValue(TargetFlowDirectionProperty, value); }

    public static readonly BindableProperty TargetClipProperty =
        BindableProperty.Create(nameof(TargetClip), typeof(Geometry), typeof(UITestHostControl), null,
            propertyChanged: (b, _, n) => ((UITestHostControl)b).ForwardToContent(VisualElement.ClipProperty, n));
    public Geometry TargetClip { get => (Geometry)GetValue(TargetClipProperty); set => SetValue(TargetClipProperty, value); }

    void ForwardToContent(BindableProperty property, object value)
    {
        if (Content is View view)
        {
            view.SetValue(property, value);
        }
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == ContentProperty.PropertyName)
        {
            ApplyAllForwardedProperties();
        }
    }

    void ApplyAllForwardedProperties()
    {
        if (Content is not View view)
        {
            return;
        }

        // Transform
        view.Rotation = TargetRotation;
        view.RotationX = TargetRotationX;
        view.RotationY = TargetRotationY;
        view.Scale = TargetScale;
        view.ScaleX = TargetScaleX;
        view.ScaleY = TargetScaleY;
        view.TranslationX = TargetTranslationX;
        view.TranslationY = TargetTranslationY;
        view.AnchorX = TargetAnchorX;
        view.AnchorY = TargetAnchorY;

        // Appearance
        view.IsVisible = TargetIsVisible;
        view.Shadow = TargetShadow;
        view.Opacity = TargetOpacity;
        view.BackgroundColor = TargetBackgroundColor;
        view.Background = TargetBackground;
        view.Clip = TargetClip;

        // Layout
        view.WidthRequest = TargetWidthRequest;
        view.HeightRequest = TargetHeightRequest;
        view.MinimumWidthRequest = TargetMinimumWidthRequest;
        view.MinimumHeightRequest = TargetMinimumHeightRequest;
        view.MaximumWidthRequest = TargetMaximumWidthRequest;
        view.MaximumHeightRequest = TargetMaximumHeightRequest;
        view.Margin = TargetMargin;
        view.HorizontalOptions = TargetHorizontalOptions;
        view.VerticalOptions = TargetVerticalOptions;

        // Input
        view.IsEnabled = TargetIsEnabled;
        view.InputTransparent = TargetInputTransparent;

        // Visual
        view.FlowDirection = TargetFlowDirection;
    }
}
