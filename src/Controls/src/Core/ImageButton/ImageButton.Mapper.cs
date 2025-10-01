namespace Microsoft.Maui.Controls
{
	public partial class ImageButton
	{
		internal new static void RemapForControls()
		{
#if ANDROID
			ImageButtonHandler.Mapper.ReplaceMapping<ImageButton, IImageButtonHandler>(PlatformConfiguration.AndroidSpecific.ImageButton.RippleColorProperty.PropertyName, MapRippleColor);
			ImageButtonHandler.Mapper.AppendToMapping<ImageButton, IImageButtonHandler>(nameof(Background), MapRippleColor);
#endif

			// Bridge legacy Border* bindable properties to handler stroke properties so we can
			// remove the OnPropertyChanged override in the control. This keeps dynamic runtime
			// updates working when BorderWidth/BorderColor change.
			ImageButtonHandler.Mapper.AppendToMapping<ImageButton, IImageButtonHandler>(nameof(BorderWidth), static (handler, view) =>
			{
				handler.UpdateValue(nameof(IImageButton.StrokeThickness));
			});

			ImageButtonHandler.Mapper.AppendToMapping<ImageButton, IImageButtonHandler>(nameof(BorderColor), static (handler, view) =>
			{
				handler.UpdateValue(nameof(IImageButton.StrokeColor));
			});
		}
	}
}
