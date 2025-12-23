using System;
using System.Collections.Specialized;
using Android.App;
using Android.Text;
using Android.Text.Style;
using Google.Android.Material.Dialog;

namespace Microsoft.Maui.Handlers;

internal partial class MaterialPickerHandler : ViewHandler<IPicker, MauiMaterialPicker>
{
    AlertDialog? _dialog;

    public static PropertyMapper<IPicker, MaterialPickerHandler> Mapper =
        new(ElementMapper)
        {
            [nameof(IPicker.Background)] = MapBackground,
            [nameof(IPicker.Items)] = MapItems,
            [nameof(IPicker.Title)] = MapTitle,
            [nameof(IPicker.TitleColor)] = MapTitleColor,
            [nameof(IPicker.SelectedIndex)] = MapSelectedIndex,
            [nameof(IPicker.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(IPicker.Font)] = MapFont,
            [nameof(IPicker.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(IPicker.TextColor)] = MapTextColor,
            [nameof(IPicker.VerticalTextAlignment)] = MapVerticalTextAlignment,
        };

    public static CommandMapper<IPicker, MaterialPickerHandler> CommandMapper =
        new(ViewCommandMapper)
        {
            [nameof(IPicker.Focus)] = MapFocus,
            [nameof(IPicker.Unfocus)] = MapUnfocus,
        };

    public MaterialPickerHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override MauiMaterialPicker CreatePlatformView() =>
        new MauiMaterialPicker(Context);

    protected override void ConnectHandler(MauiMaterialPicker platformView)
    {
        platformView.Click += OnClick;
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MauiMaterialPicker platformView)
    {
        platformView.Click -= OnClick;

        if (_dialog != null)
        {
            _dialog.ShowEvent -= OnDialogShown;
            _dialog.DismissEvent -= OnDialogDismiss;
            _dialog.Dismiss();
            _dialog = null;
        }

        base.DisconnectHandler(platformView);
    }

    public static void MapBackground(MaterialPickerHandler handler, IPicker picker) =>
        handler.PlatformView?.UpdateBackground(picker);

    internal static void MapItems(MaterialPickerHandler handler, IPicker picker) => Reload(handler);

    public static void MapTitle(MaterialPickerHandler handler, IPicker picker) =>
        handler.PlatformView?.UpdateTitle(picker);

    public static void MapTitleColor(MaterialPickerHandler handler, IPicker picker) =>
        handler.PlatformView?.UpdateTitleColor(picker);

    public static void MapSelectedIndex(MaterialPickerHandler handler, IPicker picker) =>
        handler.PlatformView?.UpdateSelectedIndex(picker);

    public static void MapCharacterSpacing(MaterialPickerHandler handler, IPicker picker) =>
        handler.PlatformView?.UpdateCharacterSpacing(picker);

    public static void MapFont(MaterialPickerHandler handler, IPicker picker)
    {
        var fontManager = handler.GetRequiredService<IFontManager>();
        handler.PlatformView?.UpdateFont(picker, fontManager);
    }

    public static void MapHorizontalTextAlignment(MaterialPickerHandler handler, IPicker picker) =>
        handler.PlatformView?.UpdateHorizontalAlignment(picker.HorizontalTextAlignment);

    public static void MapTextColor(MaterialPickerHandler handler, IPicker picker) =>
        handler.PlatformView.UpdateTextColor(picker);

    public static void MapVerticalTextAlignment(MaterialPickerHandler handler, IPicker picker) =>
        handler.PlatformView?.UpdateVerticalAlignment(picker.VerticalTextAlignment);

    internal static void MapFocus(MaterialPickerHandler handler, IPicker picker, object? args)
    {
        if (handler.IsConnected())
        {
            ViewHandler.MapFocus(handler, picker, args);
            handler.ShowDialog();
        }
    }

    internal static void MapUnfocus(MaterialPickerHandler handler, IPicker picker, object? args)
    {
        if (handler.IsConnected())
        {
            handler.DismissDialog();
            ViewHandler.MapUnfocus(handler, picker, args);
        }
    }

    void ShowDialog()
    {
        if (PlatformView.Clickable)
            PlatformView.CallOnClick();
        else
            OnClick(PlatformView, EventArgs.Empty);
    }

    void DismissDialog() => _dialog?.Dismiss();

    void OnClick(object? sender, EventArgs e)
    {
        // if (_dialog == null && VirtualView != null)
        // {
        //     using (var builder = new MaterialAlertDialogBuilder(Context))
        //     {
        //         if (VirtualView.TitleColor == null)
        //         {
        //             builder.SetTitle(VirtualView.Title ?? string.Empty);
        //         }
        //         else
        //         {
        //             var title = new SpannableString(VirtualView.Title ?? string.Empty);
        //             title.SetSpan(new ForegroundColorSpan(VirtualView.TitleColor.ToPlatform()), 0, title.Length(), SpanTypes.ExclusiveExclusive);
        //             builder.SetTitle(title);
        //         }

        //         string[] items = VirtualView.GetItemsAsArray();
        //         builder.SetItems(items, (s, e) => OnItemSelected(e.Which));

        //         _dialog = builder.Create();
        //         if (_dialog == null)
        //             return;

        //         _dialog.ShowEvent += OnDialogShown;
        //         _dialog.DismissEvent += OnDialogDismiss;
        //         _dialog.Show();
        //     }
        // }
        // VirtualView.IsFocused = true;
    }

    void OnDialogShown(object? sender, EventArgs e) =>
        VirtualView.IsOpen = true;

    void OnDialogDismiss(object? sender, EventArgs e)
    {
        VirtualView.IsOpen = false;
        VirtualView.IsFocused = false;
    }

    void OnItemSelected(int index)
    {
        if (VirtualView == null)
            return;

        VirtualView.SelectedIndex = index;
    }

    static void Reload(MaterialPickerHandler handler)
    {
        handler.PlatformView.Hint = null;
        handler.PlatformView.Hint = handler.VirtualView?.Title;

        // handler.PlatformView.UpdateSelectedIndex(handler.VirtualView);
    }
}
