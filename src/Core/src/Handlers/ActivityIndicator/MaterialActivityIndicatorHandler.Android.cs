using System;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Handlers;

internal partial class MaterialActivityIndicatorHandler : ViewHandler<IActivityIndicator, MauiMaterialActivityIndicator>
{
    public static PropertyMapper<IActivityIndicator, MaterialActivityIndicatorHandler> Mapper =
        new(ElementMapper)
        {
            [nameof(IActivityIndicator.IsRunning)] = MapIsRunning,
            [nameof(IActivityIndicator.Color)] = MapColor,
        };

    public static CommandMapper<IActivityIndicator, MaterialActivityIndicatorHandler> CommandMapper =
        new(ViewCommandMapper);

    public MaterialActivityIndicatorHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override MauiMaterialActivityIndicator CreatePlatformView() =>
        new MauiMaterialActivityIndicator(Context);

    public static void MapIsRunning(MaterialActivityIndicatorHandler handler, IActivityIndicator activityIndicator) =>
        handler.PlatformView?.UpdateIsRunning(activityIndicator);

    public static void MapColor(MaterialActivityIndicatorHandler handler, IActivityIndicator activityIndicator) =>
        handler.PlatformView?.UpdateColor(activityIndicator);

    public override void PlatformArrange(Rect frame)
    {
        if (Context == null || PlatformView == null || VirtualView == null)
        {
            return;
        }

        // Get the child's desired size (what it measured at)
        var desiredWidth = VirtualView.DesiredSize.Width;
        var desiredHeight = VirtualView.DesiredSize.Height;

        // Constrain to desired size (don't let parent stretch us)
        var constrainedWidth = Math.Min(frame.Width, desiredWidth);
        var constrainedHeight = Math.Min(frame.Height, desiredHeight);

        // Create new frame with constrained size, centered if necessary
        var arrangeFrame = new Rect(
            frame.X + (frame.Width - constrainedWidth) / 2,
            frame.Y + (frame.Height - constrainedHeight) / 2,
            constrainedWidth,
            constrainedHeight);

        // Call base with constrained frame
        base.PlatformArrange(arrangeFrame);
    }
}
