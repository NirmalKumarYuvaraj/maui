using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;

namespace Microsoft.Maui.Maps.Handlers
{
	public partial class MapHandler : ViewHandler<IMap, MapControl>
	{
		List<MapIcon>? _mapIcons;
		List<MapElement>? _mapElements;

		protected override MapControl CreatePlatformView() => new MapControl();

		protected override void ConnectHandler(MapControl platformView)
		{
			base.ConnectHandler(platformView);

			if (platformView != null)
			{
				platformView.MapTapped += OnMapTapped;
				platformView.CenterChanged += OnCenterChanged;
				platformView.ZoomLevelChanged += OnZoomLevelChanged;
			}
		}

		protected override void DisconnectHandler(MapControl platformView)
		{
			base.DisconnectHandler(platformView);

			if (platformView != null)
			{
				platformView.MapTapped -= OnMapTapped;
				platformView.CenterChanged -= OnCenterChanged;
				platformView.ZoomLevelChanged -= OnZoomLevelChanged;
			}
		}

		public static void MapMapType(IMapHandler handler, IMap map)
		{
			var mapControl = handler.PlatformView;
			if (mapControl == null) return;

			mapControl.Style = map.MapType switch
			{
				MapType.Street => MapStyle.Road,
				MapType.Satellite => MapStyle.Aerial,
				MapType.Hybrid => MapStyle.AerialWithRoads,
				_ => MapStyle.Road
			};
		}

		public static void MapIsZoomEnabled(IMapHandler handler, IMap map)
		{
			var mapControl = handler.PlatformView;
			if (mapControl == null) return;

			mapControl.ZoomInteractionMode = map.IsZoomEnabled ? MapInteractionMode.Auto : MapInteractionMode.Disabled;
		}

		public static void MapIsScrollEnabled(IMapHandler handler, IMap map)
		{
			var mapControl = handler.PlatformView;
			if (mapControl == null) return;

			mapControl.PanInteractionMode = map.IsScrollEnabled ? MapPanInteractionMode.Auto : MapPanInteractionMode.Disabled;
		}

		public static void MapIsTrafficEnabled(IMapHandler handler, IMap map)
		{
			var mapControl = handler.PlatformView;
			if (mapControl == null) return;

			mapControl.TrafficFlowVisible = map.IsTrafficEnabled;
		}

		public static void MapIsShowingUser(IMapHandler handler, IMap map)
		{
			// Note: Windows MapControl doesn't have a direct equivalent to iOS/Android ShowsUserLocation
			// This would typically require implementing location services and adding a custom user location indicator
			// For now, we'll leave this empty as it requires more complex implementation
		}

		public static void MapMoveToRegion(IMapHandler handler, IMap map, object? arg)
		{
			if (arg is MapSpan newRegion && handler is MapHandler mapHandler)
			{
				mapHandler.MoveToRegion(newRegion, true);
			}
		}

		public static void MapPins(IMapHandler handler, IMap map)
		{
			if (handler is MapHandler mapHandler)
			{
				mapHandler.UpdatePins((IList)map.Pins);
			}
		}

		public static void MapElements(IMapHandler handler, IMap map)
		{
			if (handler is MapHandler mapHandler)
			{
				mapHandler.UpdateMapElements((IList)map.Elements);
			}
		}

		public void UpdateMapElement(IMapElement element)
		{
			// Remove and re-add the element to update it
			RemoveMapElement(element);
			AddMapElement(element);
		}

		void MoveToRegion(MapSpan mapSpan, bool animated = true)
		{
			if (PlatformView == null) return;

			var center = new BasicGeoposition
			{
				Latitude = mapSpan.Center.Latitude,
				Longitude = mapSpan.Center.Longitude
			};

			var northEast = new BasicGeoposition
			{
				Latitude = mapSpan.Center.Latitude + mapSpan.LatitudeDegrees / 2,
				Longitude = mapSpan.Center.Longitude + mapSpan.LongitudeDegrees / 2
			};

			var southWest = new BasicGeoposition
			{
				Latitude = mapSpan.Center.Latitude - mapSpan.LatitudeDegrees / 2,
				Longitude = mapSpan.Center.Longitude - mapSpan.LongitudeDegrees / 2
			};

			var boundingBox = GeoboundingBox.TryCreate(northEast, southWest);
			if (boundingBox != null)
			{
				_ = PlatformView.TrySetViewBoundsAsync(boundingBox, null, animated ? MapAnimationKind.Default : MapAnimationKind.None);
			}
		}

		void UpdatePins(IList pins)
		{
			if (PlatformView == null) return;

			// Clear existing pins
			if (_mapIcons != null)
			{
				foreach (var icon in _mapIcons)
				{
					PlatformView.MapElements.Remove(icon);
				}
				_mapIcons.Clear();
			}
			else
			{
				_mapIcons = new List<MapIcon>();
			}

			// Add new pins
			foreach (IMapPin pin in pins)
			{
				var mapIcon = new MapIcon
				{
					Location = new Geopoint(new BasicGeoposition
					{
						Latitude = pin.Location.Latitude,
						Longitude = pin.Location.Longitude
					}),
					Title = pin.Label,
					ZIndex = 0
				};

				_mapIcons.Add(mapIcon);
				PlatformView.MapElements.Add(mapIcon);
			}
		}

		void UpdateMapElements(IList elements)
		{
			if (PlatformView == null) return;

			// Clear existing map elements (excluding pins)
			if (_mapElements != null)
			{
				foreach (var element in _mapElements)
				{
					PlatformView.MapElements.Remove(element);
				}
				_mapElements.Clear();
			}
			else
			{
				_mapElements = new List<MapElement>();
			}

			// Add new elements
			foreach (IMapElement element in elements)
			{
				AddMapElement(element);
			}
		}

		void AddMapElement(IMapElement element)
		{
			if (PlatformView == null) return;

			MapElement? nativeElement = null;

			switch (element)
			{
				case ICircleMapElement circle:
					// Note: Windows MapControl doesn't have a direct circle element
					// This would need to be implemented as a custom polygon approximating a circle
					break;

				case IGeoPathMapElement geoPath when element is IFilledMapElement:
					// Polygon
					var polygon = new MapPolygon();
					polygon.Path = new Geopath(geoPath.Select(pos => new BasicGeoposition
					{
						Latitude = pos.Latitude,
						Longitude = pos.Longitude
					}));

					// Set styling if available
					if (geoPath.Stroke is SolidPaint strokePaint)
					{
						polygon.StrokeColor = strokePaint.Color.ToWindowsColor();
					}

					if ((element as IFilledMapElement)?.Fill is SolidPaint fillPaint)
					{
						polygon.FillColor = fillPaint.Color.ToWindowsColor();
					}

					polygon.StrokeThickness = geoPath.StrokeThickness;
					nativeElement = polygon;
					break;

				case IGeoPathMapElement geoPath:
					// Polyline
					var polyline = new MapPolyline();
					polyline.Path = new Geopath(geoPath.Select(pos => new BasicGeoposition
					{
						Latitude = pos.Latitude,
						Longitude = pos.Longitude
					}));

					// Set styling if available
					if (geoPath.Stroke is SolidPaint strokePaint)
					{
						polyline.StrokeColor = strokePaint.Color.ToWindowsColor();
					}

					polyline.StrokeThickness = geoPath.StrokeThickness;
					nativeElement = polyline;
					break;
			}

			if (nativeElement != null)
			{
				_mapElements ??= new List<MapElement>();
				_mapElements.Add(nativeElement);
				PlatformView.MapElements.Add(nativeElement);

				// Store the reference for later updates
				element.MapElementId = nativeElement.GetHashCode().ToString();
			}
		}

		void RemoveMapElement(IMapElement element)
		{
			if (PlatformView == null || _mapElements == null || element.MapElementId == null) return;

			var elementToRemove = _mapElements.FirstOrDefault(e => e.GetHashCode().ToString() == element.MapElementId.ToString());
			if (elementToRemove != null)
			{
				PlatformView.MapElements.Remove(elementToRemove);
				_mapElements.Remove(elementToRemove);
			}
		}

		void OnMapTapped(MapControl sender, MapInputEventArgs args)
		{
			if (args.Location?.Position != null)
			{
				var position = args.Location.Position;
				var location = new Location(position.Latitude, position.Longitude);
				VirtualView.Clicked(location);
			}
		}

		void OnCenterChanged(MapControl sender, object args)
		{
			UpdateVisibleRegion();
		}

		void OnZoomLevelChanged(MapControl sender, object args)
		{
			UpdateVisibleRegion();
		}

		void UpdateVisibleRegion()
		{
			if (PlatformView?.Center?.Position == null) return;

			try
			{
				// Calculate the visible bounds based on the current map view
				var center = PlatformView.Center.Position;
				var zoomLevel = PlatformView.ZoomLevel;

				// Approximate calculation for visible region based on zoom level
				// At zoom level 1, the entire world is visible (360 degrees longitude, 180 degrees latitude)
				var latitudeDegrees = 180.0 / Math.Pow(2, zoomLevel - 1);
				var longitudeDegrees = 360.0 / Math.Pow(2, zoomLevel - 1);

				// Adjust for aspect ratio if available
				if (PlatformView.ActualHeight > 0 && PlatformView.ActualWidth > 0)
				{
					var aspectRatio = PlatformView.ActualWidth / PlatformView.ActualHeight;
					if (aspectRatio > 1)
					{
						longitudeDegrees *= aspectRatio;
					}
					else
					{
						latitudeDegrees /= aspectRatio;
					}
				}

				var visibleRegion = new MapSpan(
					new Location(center.Latitude, center.Longitude),
					latitudeDegrees,
					longitudeDegrees);

				VirtualView.VisibleRegion = visibleRegion;
			}
			catch
			{
				// Ignore errors during visible region calculation
			}
		}
	}
}
