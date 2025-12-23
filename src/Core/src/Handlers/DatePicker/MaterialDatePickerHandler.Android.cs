using System;
using Android.App;
using Android.Views;
using Microsoft.Maui.Devices;

namespace Microsoft.Maui.Handlers;

internal partial class MaterialDatePickerHandler : ViewHandler<IDatePicker, MauiMaterialDatePicker>
{
    DatePickerDialog? _dialog;

    internal DatePickerDialog? DatePickerDialog => _dialog;

    public static PropertyMapper<IDatePicker, MaterialDatePickerHandler> Mapper =
        new(ElementMapper)
        {
            [nameof(IDatePicker.Background)] = MapBackground,
            [nameof(IDatePicker.Format)] = MapFormat,
            [nameof(IDatePicker.Date)] = MapDate,
            [nameof(IDatePicker.MinimumDate)] = MapMinimumDate,
            [nameof(IDatePicker.MaximumDate)] = MapMaximumDate,
            [nameof(IDatePicker.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(IDatePicker.Font)] = MapFont,
            [nameof(IDatePicker.TextColor)] = MapTextColor,
        };

    public static CommandMapper<IDatePicker, MaterialDatePickerHandler> CommandMapper =
        new(ViewCommandMapper);

    public MaterialDatePickerHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override MauiMaterialDatePicker CreatePlatformView()
    {
        var mauiDatePicker = new MauiMaterialDatePicker(Context);

        var date = VirtualView?.Date;

        if (date != null)
        {
            _dialog = CreateDatePickerDialog(date.Value.Year, date.Value.Month - 1, date.Value.Day);
        }

        return mauiDatePicker;
    }

    protected override void ConnectHandler(MauiMaterialDatePicker platformView)
    {
        base.ConnectHandler(platformView);

        platformView.ShowPicker = ShowPickerDialog;
        platformView.HidePicker = HidePickerDialog;

        platformView.ViewAttachedToWindow += OnViewAttachedToWindow;
        platformView.ViewDetachedFromWindow += OnViewDetachedFromWindow;

        if (platformView.IsAttachedToWindow)
        {
            OnViewAttachedToWindow();
        }
    }

    void OnViewDetachedFromWindow(object? sender = null, View.ViewDetachedFromWindowEventArgs? e = null)
    {
        DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;
    }

    void OnViewAttachedToWindow(object? sender = null, View.ViewAttachedToWindowEventArgs? e = null)
    {
        DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;
    }

    protected override void DisconnectHandler(MauiMaterialDatePicker platformView)
    {
        if (_dialog != null)
        {
            _dialog.DismissEvent -= OnDialogDismiss;
            _dialog.Dismiss();
            _dialog = null;
        }

        platformView.ViewAttachedToWindow -= OnViewAttachedToWindow;
        platformView.ViewDetachedFromWindow -= OnViewDetachedFromWindow;

        platformView.ShowPicker = null;
        platformView.HidePicker = null;

        OnViewDetachedFromWindow();

        base.DisconnectHandler(platformView);
    }

    protected virtual DatePickerDialog CreateDatePickerDialog(int year, int month, int day)
    {
        var dialog = new DatePickerDialog(Context!, (o, e) =>
        {
            VirtualView?.Date = e.Date;
        }, year, month, day);

        dialog.DismissEvent += OnDialogDismiss;

        return dialog;
    }

    public static void MapBackground(MaterialDatePickerHandler handler, IDatePicker datePicker) =>
        handler.PlatformView?.UpdateBackground(datePicker);

    public static void MapFormat(MaterialDatePickerHandler handler, IDatePicker datePicker) =>
        handler.PlatformView?.UpdateFormat(datePicker);

    public static void MapDate(MaterialDatePickerHandler handler, IDatePicker datePicker) =>
        handler.PlatformView?.UpdateDate(datePicker);

    public static void MapMinimumDate(MaterialDatePickerHandler handler, IDatePicker datePicker) =>
        handler.PlatformView?.UpdateMinimumDate(datePicker, handler._dialog);

    public static void MapMaximumDate(MaterialDatePickerHandler handler, IDatePicker datePicker) =>
        handler.PlatformView?.UpdateMaximumDate(datePicker, handler._dialog);

    public static void MapCharacterSpacing(MaterialDatePickerHandler handler, IDatePicker datePicker) =>
        handler.PlatformView?.UpdateCharacterSpacing(datePicker);

    public static void MapFont(MaterialDatePickerHandler handler, IDatePicker datePicker)
    {
        var fontManager = handler.GetRequiredService<IFontManager>();
        handler.PlatformView?.UpdateFont(datePicker, fontManager);
    }

    public static void MapTextColor(MaterialDatePickerHandler handler, IDatePicker datePicker) =>
        handler.PlatformView?.UpdateTextColor(datePicker);

    void ShowPickerDialog()
    {
        if (VirtualView is null)
            return;

        if (_dialog is not null && _dialog.IsShowing)
            return;

        var date = VirtualView.Date;
        ShowPickerDialog(date);
        VirtualView.IsOpen = true;
    }

    void ShowPickerDialog(DateTime? date)
    {
        var year = date?.Year ?? DateTime.Today.Year;
        var month = (date?.Month ?? DateTime.Today.Month) - 1;
        var day = date?.Day ?? DateTime.Today.Day;

        if (_dialog is null)
        {
            _dialog = CreateDatePickerDialog(year, month, day);
        }
        else
        {
            EventHandler? setDateLater = null;
            setDateLater = (sender, e) => { _dialog!.UpdateDate(year, month, day); _dialog.ShowEvent -= setDateLater; };
            _dialog.ShowEvent += setDateLater;
            _dialog.DismissEvent += OnDialogDismiss;
        }

        _dialog.Show();
    }

    void HidePickerDialog()
    {
        if (_dialog != null)
        {
            _dialog.DismissEvent -= OnDialogDismiss;
            _dialog.Hide();
        }

        VirtualView.IsOpen = false;
    }

    void OnDialogDismiss(object? sender, EventArgs e)
    {
        HidePickerDialog();
    }

    void OnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
    {
        DatePickerDialog? currentDialog = _dialog;

        if (currentDialog is not null && currentDialog.IsShowing)
        {
            currentDialog.Dismiss();
            ShowPickerDialog(currentDialog.DatePicker.DateTime);
        }
    }
}
