using System;
using Google.Android.Material.Slider;

namespace Microsoft.Maui.Handlers;

internal partial class MaterialSliderHandler : ViewHandler<ISlider, MauiMaterialSlider>
{
	public static PropertyMapper<ISlider, MaterialSliderHandler> Mapper =
			new(ElementMapper)
			{
				[nameof(ISlider.Value)] = MapValue,
				[nameof(ISlider.Minimum)] = MapMinimum,
				[nameof(ISlider.Maximum)] = MapMaximum,
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
	MaterialSliderChangeListener ChangeListener { get; } = new MaterialSliderChangeListener();
	internal static void MapValue(MaterialSliderHandler handler, ISlider slider)
	{
		handler.PlatformView.Value = (float)slider.Value;
	}

	protected override MauiMaterialSlider CreatePlatformView()
	{
		return new MauiMaterialSlider(Context)
		{
			DuplicateParentStateEnabled = false,
			ValueTo = (int)SliderExtensions.PlatformMaxValue
		};
	}

	protected override void ConnectHandler(MauiMaterialSlider platformView)
	{
		ChangeListener.Handler = this;
		//https://github.com/dotnet/android-libraries/issues/230
	}

	protected override void DisconnectHandler(MauiMaterialSlider platformView)
	{
		ChangeListener.Handler = null;
		platformView.ClearOnChangeListeners();
		platformView.ClearOnSliderTouchListeners();
	}

	public static void MapMinimum(MaterialSliderHandler handler, ISlider slider)
	{
		handler.PlatformView?.UpdateMinimum(slider);
	}

	public static void MapMaximum(MaterialSliderHandler handler, ISlider slider)
	{
		handler.PlatformView?.UpdateMaximum(slider);
	}

	public static void MapMinimumTrackColor(MaterialSliderHandler handler, ISlider slider)
	{
		handler.PlatformView?.UpdateMinimumTrackColor(slider);
	}

	public static void MapMaximumTrackColor(MaterialSliderHandler handler, ISlider slider)
	{
		handler.PlatformView?.UpdateMaximumTrackColor(slider);
	}

	public static void MapThumbColor(MaterialSliderHandler handler, ISlider slider)
	{
		handler.PlatformView?.UpdateThumbColor(slider);
	}

	public static void MapThumbImageSource(MaterialSliderHandler handler, ISlider slider)
	{
		var provider = handler.GetRequiredService<IImageSourceServiceProvider>();

		handler.PlatformView?.UpdateThumbImageSourceAsync(slider, provider)
			.FireAndForget(handler);
	}

	internal void OnValueChanged(MauiMaterialSlider slider, float value, bool fromUser)
	{
		if (VirtualView == null || !fromUser)
			return;

		var min = VirtualView.Minimum;
		var max = VirtualView.Maximum;
		var sliderValue = min + (max - min) * (value / slider.ValueTo);

		VirtualView.Value = sliderValue;
	}

	internal void OnStartTrackingTouch(MauiMaterialSlider slider) =>
		VirtualView?.DragStarted();

	internal void OnStopTrackingTouch(MauiMaterialSlider slider) =>
		VirtualView?.DragCompleted();
}

internal class MaterialSliderChangeListener : Java.Lang.Object, Slider.IOnChangeListener, Slider.IOnSliderTouchListener
{
	private WeakReference<MaterialSliderHandler>? _handler;

	public MaterialSliderHandler? Handler
	{
		get => _handler != null && _handler.TryGetTarget(out var handler) ? handler : null;
		set => _handler = value == null ? null : new WeakReference<MaterialSliderHandler>(value);
	}

	public MaterialSliderChangeListener()
	{
	}

	public void OnValueChange(Slider slider, float value, bool fromUser)
	{
		if (Handler == null || slider == null)
			return;

		if (slider is MauiMaterialSlider materialSlider)
			Handler.OnValueChanged(materialSlider, value, fromUser);
	}

	public void OnValueChange(Java.Lang.Object slider, float value, bool fromUser)
	{
		if (slider is Slider s)
			OnValueChange(s, value, fromUser);
	}

	public void OnStartTrackingTouch(Slider slider)
	{
		if (Handler == null || slider == null)
			return;

		if (slider is MauiMaterialSlider materialSlider)
			Handler.OnStartTrackingTouch(materialSlider);
	}

	public void OnStopTrackingTouch(Slider slider)
	{
		if (Handler == null || slider == null)
			return;

		if (slider is MauiMaterialSlider materialSlider)
			Handler.OnStopTrackingTouch(materialSlider);
	}

	public void OnStartTrackingTouch(Java.Lang.Object slider)
	{
		if (slider is Slider s)
			OnStartTrackingTouch(s);
	}

	public void OnStopTrackingTouch(Java.Lang.Object slider)
	{
		if (slider is Slider s)
			OnStopTrackingTouch(s);
	}
}