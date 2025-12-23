using Android.Widget;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Handlers;

internal partial class MaterialSwitchHandler : ViewHandler<ISwitch, MauiMaterialSwitch>
{
    CheckedChangeListener ChangeListener { get; } = new CheckedChangeListener();

    public static PropertyMapper<ISwitch, MaterialSwitchHandler> Mapper =
        new(ElementMapper)
        {
            [nameof(ISwitch.IsOn)] = MapIsOn,
            [nameof(ISwitch.TrackColor)] = MapTrackColor,
            [nameof(ISwitch.ThumbColor)] = MapThumbColor,
        };

    public static CommandMapper<ISwitch, MaterialSwitchHandler> CommandMapper =
        new(ViewCommandMapper);

    public MaterialSwitchHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override MauiMaterialSwitch CreatePlatformView()
    {
        return new MauiMaterialSwitch(Context);
    }

    protected override void ConnectHandler(MauiMaterialSwitch platformView)
    {
        ChangeListener.Handler = this;
        platformView.SetOnCheckedChangeListener(ChangeListener);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MauiMaterialSwitch platformView)
    {
        ChangeListener.Handler = null;
        platformView.SetOnCheckedChangeListener(null);
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        Size size = base.GetDesiredSize(widthConstraint, heightConstraint);

        if (size.Width == 0)
        {
            int width = (int)widthConstraint;

            if (widthConstraint <= 0)
                width = Context != null ? (int)Context.GetThemeAttributeDp(global::Android.Resource.Attribute.SwitchMinWidth) : 0;

            size = new Size(width, size.Height);
        }

        return size;
    }

    public static void MapIsOn(MaterialSwitchHandler handler, ISwitch view) =>
        handler.PlatformView?.UpdateIsOn(view);

    public static void MapTrackColor(MaterialSwitchHandler handler, ISwitch view) =>
        handler.PlatformView?.UpdateTrackColor(view);

    public static void MapThumbColor(MaterialSwitchHandler handler, ISwitch view) =>
        handler.PlatformView?.UpdateThumbColor(view);

    void OnCheckedChanged(bool isOn)
    {
        if (VirtualView is null || VirtualView.IsOn == isOn)
            return;

        VirtualView.IsOn = isOn;
    }

    class CheckedChangeListener : Java.Lang.Object, CompoundButton.IOnCheckedChangeListener
    {
        public MaterialSwitchHandler? Handler { get; set; }

        void CompoundButton.IOnCheckedChangeListener.OnCheckedChanged(CompoundButton? buttonView, bool isToggled)
        {
            Handler?.OnCheckedChanged(isToggled);
        }
    }
}
