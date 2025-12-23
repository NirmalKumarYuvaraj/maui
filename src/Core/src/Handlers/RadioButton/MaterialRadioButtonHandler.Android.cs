using Android.Views;
using Android.Widget;

namespace Microsoft.Maui.Handlers;

internal partial class MaterialRadioButtonHandler : ViewHandler<IRadioButton, View>
{
    static MauiMaterialRadioButton? GetPlatformRadioButton(MaterialRadioButtonHandler handler) => handler.PlatformView as MauiMaterialRadioButton;

    public static PropertyMapper<IRadioButton, MaterialRadioButtonHandler> Mapper =
        new(ElementMapper)
        {
            [nameof(IRadioButton.Background)] = MapBackground,
            [nameof(IRadioButton.IsChecked)] = MapIsChecked,
            [nameof(IRadioButton.Content)] = MapContent,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(ITextStyle.Font)] = MapFont,
            [nameof(IRadioButton.StrokeColor)] = MapStrokeColor,
            [nameof(IRadioButton.StrokeThickness)] = MapStrokeThickness,
            [nameof(IRadioButton.CornerRadius)] = MapCornerRadius,
        };

    public static CommandMapper<IRadioButton, MaterialRadioButtonHandler> CommandMapper =
        new(ViewCommandMapper);

    public MaterialRadioButtonHandler() : base(Mapper, CommandMapper)
    {
    }

    public override void PlatformArrange(Graphics.Rect frame)
    {
        this.PrepareForTextViewArrange(frame);
        base.PlatformArrange(frame);
    }

    protected override MauiMaterialRadioButton CreatePlatformView()
    {
        return new MauiMaterialRadioButton(Context);
    }

    protected override void ConnectHandler(View platformView)
    {
        MauiMaterialRadioButton? platformRadioButton = GetPlatformRadioButton(this);
        platformRadioButton?.CheckedChange += OnCheckChanged;
    }

    protected override void DisconnectHandler(View platformView)
    {
        if (platformView is MauiMaterialRadioButton platformRadioButton)
        {
            platformRadioButton.CheckedChange -= OnCheckChanged;
        }
    }

    public static void MapBackground(MaterialRadioButtonHandler handler, IRadioButton radioButton) =>
        GetPlatformRadioButton(handler)?.UpdateBackground(radioButton);

    public static void MapIsChecked(MaterialRadioButtonHandler handler, IRadioButton radioButton) =>
        GetPlatformRadioButton(handler)?.UpdateIsChecked(radioButton);

    public static void MapContent(MaterialRadioButtonHandler handler, IRadioButton radioButton) =>
        GetPlatformRadioButton(handler)?.UpdateContent(radioButton);

    public static void MapTextColor(MaterialRadioButtonHandler handler, ITextStyle textStyle) =>
        GetPlatformRadioButton(handler)?.UpdateTextColor(textStyle);

    public static void MapCharacterSpacing(MaterialRadioButtonHandler handler, ITextStyle textStyle) =>
        GetPlatformRadioButton(handler)?.UpdateCharacterSpacing(textStyle);

    public static void MapFont(MaterialRadioButtonHandler handler, ITextStyle textStyle)
    {
        var fontManager = handler.GetRequiredService<IFontManager>();
        GetPlatformRadioButton(handler)?.UpdateFont(textStyle, fontManager);
    }

    public static void MapStrokeColor(MaterialRadioButtonHandler handler, IRadioButton radioButton) =>
        GetPlatformRadioButton(handler)?.UpdateStrokeColor(radioButton);

    public static void MapStrokeThickness(MaterialRadioButtonHandler handler, IRadioButton radioButton) =>
        GetPlatformRadioButton(handler)?.UpdateStrokeThickness(radioButton);

    public static void MapCornerRadius(MaterialRadioButtonHandler handler, IRadioButton radioButton) =>
        GetPlatformRadioButton(handler)?.UpdateCornerRadius(radioButton);

    void OnCheckChanged(object? sender, CompoundButton.CheckedChangeEventArgs e)
    {
        if (VirtualView == null)
        {
            return;
        }

        VirtualView.IsChecked = e.IsChecked;
    }
}
