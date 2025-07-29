using System;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.Maui.Maps.Handlers
{
	public partial class MapHandler : ViewHandler<IMap, MapControl>
	{

		protected override MapControl CreatePlatformView() => new MapControl();

		public static void MapMapType(IMapHandler handler, IMap map)
		{

		}

		public static void MapIsZoomEnabled(IMapHandler handler, IMap map)
		{

		}

		public static void MapIsScrollEnabled(IMapHandler handler, IMap map)
		{

		}

		public static void MapIsTrafficEnabled(IMapHandler handler, IMap map)
		{

		}

		public static void MapIsShowingUser(IMapHandler handler, IMap map)
		{

		}

		public static void MapMoveToRegion(IMapHandler handler, IMap map, object? arg) { }

		public static void MapPins(IMapHandler handler, IMap map) {}

		public static void MapElements(IMapHandler handler, IMap map) { }

		public void UpdateMapElement(IMapElement element) { }
	}
}
