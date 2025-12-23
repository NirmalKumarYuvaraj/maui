using Android.Content.Res;
using Google.Android.Material.Slider;
using Java.Lang;

namespace Microsoft.Maui.Handlers;

internal partial class MaterialSliderHandler : ViewHandler<ISlider, MauiMaterialSlider>
{

    public static PropertyMapper<ISlider, MaterialSliderHandler> Mapper =
        new(ElementMapper)
        {
            [nameof(ISlider.Minimum)] = MapMinimum,
            [nameof(ISlider.Maximum)] = MapMaximum,
            [nameof(ISlider.Value)] = MapValue,
            [nameof(ISlider.MinimumTrackColor)] = MapMinimumTrackColor,
            [nameof(ISlider.MaximumTrackColor)] = MapMaximumTrackColor,
            [nameof(ISlider.ThumbColor)] = MapThumbColor,
            [nameof(ISlider.ThumbImageSource)] = MapThumbImageSource,
        };

    public static CommandMapper<ISlider, MaterialSliderHandler> CommandMapper =
        new(ViewCommandMapper);

    public MaterialSliderHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override MauiMaterialSlider CreatePlatformView()
    {
        return new MauiMaterialSlider(Context);
    }

    protected override void ConnectHandler(MauiMaterialSlider platformView)
    {


    }

    protected override void DisconnectHandler(MauiMaterialSlider platformView)
    {

    }

    public static void MapMinimum(MaterialSliderHandler handler, ISlider slider)
    {
        // Material Slider uses ValueFrom instead of minimum
        handler.PlatformView.ValueFrom = (float)slider.Minimum;
        // Re-apply value when minimum changes
        MapValue(handler, slider);
    }

    public static void MapMaximum(MaterialSliderHandler handler, ISlider slider)
    {
        // Material Slider uses ValueTo instead of maximum
        handler.PlatformView.ValueTo = (float)slider.Maximum;
        // Re-apply value when maximum changes
        MapValue(handler, slider);
    }

    public static void MapValue(MaterialSliderHandler handler, ISlider slider)
    {
        // Material Slider uses Value property directly
        handler.PlatformView.Value = (float)slider.Value;
    }

    public static void MapMinimumTrackColor(MaterialSliderHandler handler, ISlider slider)
    {
        if (slider.MinimumTrackColor != null)
            handler.PlatformView.TrackInactiveTintList = ColorStateList.ValueOf(slider.MinimumTrackColor.ToPlatform());
    }

    public static void MapMaximumTrackColor(MaterialSliderHandler handler, ISlider slider)
    {
        if (slider.MaximumTrackColor != null)
            handler.PlatformView.TrackActiveTintList = ColorStateList.ValueOf(slider.MaximumTrackColor.ToPlatform());
    }

    public static void MapThumbColor(MaterialSliderHandler handler, ISlider slider)
    {
    }

    public static void MapThumbImageSource(MaterialSliderHandler handler, ISlider slider)
    {

    }

    void OnValueChanged(MauiMaterialSlider slider, float value, bool fromUser)
    {
        if (VirtualView == null || !fromUser)
            return;

        VirtualView.Value = value;
    }

    void OnStartTrackingTouch(MauiMaterialSlider slider) =>
        VirtualView?.DragStarted();

    void OnStopTrackingTouch(MauiMaterialSlider slider) =>
        VirtualView?.DragCompleted();

}
