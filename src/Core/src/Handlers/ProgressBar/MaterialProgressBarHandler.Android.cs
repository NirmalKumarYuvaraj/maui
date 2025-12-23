namespace Microsoft.Maui.Handlers;

internal partial class MaterialProgressBarHandler : ViewHandler<IProgress, MauiMaterialProgressBar>
{
    public static PropertyMapper<IProgress, MaterialProgressBarHandler> Mapper =
        new(ElementMapper)
        {
            [nameof(IProgress.Progress)] = MapProgress,
            [nameof(IProgress.ProgressColor)] = MapProgressColor,
        };

    public static CommandMapper<IProgress, MaterialProgressBarHandler> CommandMapper =
        new(ViewCommandMapper);

    public MaterialProgressBarHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override MauiMaterialProgressBar CreatePlatformView()
    {
        var progressBar = new MauiMaterialProgressBar(Context);
        // Material LinearProgressIndicator uses 0-100 scale by default
        progressBar.Max = 100;
        progressBar.SetProgressCompat(0, false);
        return progressBar;
    }

    public static void MapProgress(MaterialProgressBarHandler handler, IProgress progress)
    {
        // Convert MAUI's 0-1 range to Material's 0-100 range
        var materialProgress = (int)(progress.Progress * 100);
        handler.PlatformView?.SetProgressCompat(materialProgress, true);
    }

    public static void MapProgressColor(MaterialProgressBarHandler handler, IProgress progress)
    {
        handler.PlatformView?.UpdateProgressColor(progress);
    }
}
