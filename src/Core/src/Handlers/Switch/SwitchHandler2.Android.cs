using Google.Android.Material.MaterialSwitch;
using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.Handlers;

public class SwitchHandler2 : SwitchHandler
{
	public static new PropertyMapper<ISwitch, SwitchHandler2> Mapper =
  	new(SwitchHandler.Mapper)
  	{
		  [nameof(ISwitch.TrackColor)] = MapTrackColor,
	  	[nameof(ISwitch.ThumbColor)] = MapThumbColor,
	  };

	public static new CommandMapper<ISwitch, SwitchHandler2> CommandMapper =
	new(SwitchHandler.CommandMapper)
	{
		[nameof(MapThemeChanged)] = MapThemeChanged,
	};

	private static void MapThemeChanged(SwitchHandler2 handler, ISwitch view, object? arg3)
	{
		// update the theme for the entry
		var platformView = handler.PlatformView;
		//handler.PlatformView?.BoxStrokeColor = MaterialColorTokens.GetThemeAttrColor(platformView, Resource.Attribute.colorPrimary, arg3 as AppTheme?);
	}

	public SwitchHandler2() : base(Mapper, CommandMapper)
	{
	}

	protected override MaterialSwitch CreatePlatformView()
	{
		return new MaterialSwitch(MauiMaterialContextThemeWrapper.Create(Context));
	}

	public static new void MapTrackColor(ISwitchHandler handler, ISwitch view)
	{
		(handler.PlatformView as MaterialSwitch)?.UpdateTrackColor(view);
	}

	public static new void MapThumbColor(ISwitchHandler handler, ISwitch view)
	{
		(handler.PlatformView as MaterialSwitch)?.UpdateThumbColor(view);
	}
}
