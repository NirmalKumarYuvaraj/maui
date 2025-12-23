using System;
using Android.App;
using Android.Text.Format;
using DateFormat = Android.Text.Format.DateFormat;

namespace Microsoft.Maui.Handlers;

internal partial class MaterialTimePickerHandler : ViewHandler<ITimePicker, MauiMaterialTimePicker>
{
    MauiMaterialTimePicker? _timePicker;
    TimePickerDialog? _dialog;

    public static PropertyMapper<ITimePicker, MaterialTimePickerHandler> Mapper =
        new(ElementMapper)
        {
            [nameof(ITimePicker.Background)] = MapBackground,
            [nameof(ITimePicker.Format)] = MapFormat,
            [nameof(ITimePicker.Time)] = MapTime,
            [nameof(ITimePicker.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(ITimePicker.Font)] = MapFont,
            [nameof(ITimePicker.TextColor)] = MapTextColor,
        };

    public static CommandMapper<ITimePicker, MaterialTimePickerHandler> CommandMapper =
        new(ViewCommandMapper);

    public MaterialTimePickerHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override MauiMaterialTimePicker CreatePlatformView()
    {
        _timePicker = new MauiMaterialTimePicker(Context);
        return _timePicker;
    }

    protected override void ConnectHandler(MauiMaterialTimePicker platformView)
    {
        base.ConnectHandler(platformView);
        platformView.ShowPicker = ShowPickerDialog;
        platformView.HidePicker = HidePickerDialog;
    }

    protected override void DisconnectHandler(MauiMaterialTimePicker platformView)
    {
        if (_dialog != null)
        {
            _dialog.DismissEvent -= OnDialogDismiss;
            _dialog.Dismiss();
            _dialog = null;
        }

        platformView.ShowPicker = null;
        platformView.HidePicker = null;
    }

    protected virtual TimePickerDialog CreateTimePickerDialog(int hour, int minute)
    {
        void onTimeSetCallback(object? obj, TimePickerDialog.TimeSetEventArgs args)
        {
            if (VirtualView == null || PlatformView == null)
                return;

            VirtualView.Time = new TimeSpan(args.HourOfDay, args.Minute, 0);
            VirtualView.IsFocused = false;

            if (_dialog != null)
            {
                _dialog = null;
            }
        }

        var dialog = new TimePickerDialog(Context!, onTimeSetCallback, hour, minute, Use24HourView);
        dialog.DismissEvent += OnDialogDismiss;

        return dialog;
    }

    public static void MapBackground(MaterialTimePickerHandler handler, ITimePicker timePicker) =>
        handler.PlatformView?.UpdateBackground(timePicker);

    public static void MapFormat(MaterialTimePickerHandler handler, ITimePicker timePicker) =>
        handler.PlatformView?.UpdateFormat(timePicker);

    public static void MapTime(MaterialTimePickerHandler handler, ITimePicker timePicker) =>
        handler.PlatformView?.UpdateTime(timePicker);

    public static void MapCharacterSpacing(MaterialTimePickerHandler handler, ITimePicker timePicker) =>
        handler.PlatformView?.UpdateCharacterSpacing(timePicker);

    public static void MapFont(MaterialTimePickerHandler handler, ITimePicker timePicker)
    {
        var fontManager = handler.GetRequiredService<IFontManager>();
        handler.PlatformView?.UpdateFont(timePicker, fontManager);
    }

    public static void MapTextColor(MaterialTimePickerHandler handler, ITimePicker timePicker) =>
        handler.PlatformView?.UpdateTextColor(timePicker);

    void ShowPickerDialog()
    {
        if (VirtualView is null)
            return;

        ShowPickerDialog(VirtualView.Time);
    }

    void ShowPickerDialog(TimeSpan? time)
    {
        if (_dialog is not null && _dialog.IsShowing)
            return;

        var hour = time?.Hours ?? 0;
        var minute = time?.Minutes ?? 0;

        _dialog = CreateTimePickerDialog(hour, minute);
        _dialog.Show();

        VirtualView?.IsOpen = true;
    }

    void HidePickerDialog()
    {
        if (_dialog is not null)
        {
            _dialog.DismissEvent -= OnDialogDismiss;
            _dialog.Hide();
        }

        _dialog = null;
        VirtualView.IsOpen = false;
    }

    void OnDialogDismiss(object? sender, EventArgs e)
    {
        HidePickerDialog();
    }

    bool Use24HourView => VirtualView != null && (DateFormat.Is24HourFormat(PlatformView?.Context)
        && VirtualView.Format == "t" || VirtualView.Format == "HH:mm");
}
