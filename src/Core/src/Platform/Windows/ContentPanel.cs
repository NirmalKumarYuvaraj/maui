using System;
using System.Numerics;
using System.Diagnostics;
using Microsoft.Graphics.Canvas;
using Microsoft.Maui.Graphics;
#if MAUI_GRAPHICS_WIN2D
using Microsoft.Maui.Graphics.Win2D;
#else
using Microsoft.Maui.Graphics.Platform;
#endif
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.Maui.Platform
{
	public class ContentPanel : MauiPanel
	{
		readonly Path? _borderPath;
		IBorderStroke? _borderStroke;
		FrameworkElement? _content;

		internal Path? BorderPath => _borderPath;
		internal IBorderStroke? BorderStroke => _borderStroke;
		internal FrameworkElement? Content
		{
			get => _content;
			set
			{
				var children = CachedChildren;

				// Remove the previous content if it exists
				if (_content is not null && children.Contains(_content) && value != _content)
				{
					System.Diagnostics.Debug.WriteLine($"[ContentPanel.Content] id={GetHashCode()} RemovingOldContent oldType={_content.GetType().Name}", "MAUI-Layout");
					children.Remove(_content);
				}

				_content = value;
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.Content] id={GetHashCode()} SettingNewContent newType={_content?.GetType().Name ?? "null"} ChildCount(beforeAdd)={children.Count}", "MAUI-Layout");

				if (_content is null)
				{
					System.Diagnostics.Debug.WriteLine($"[ContentPanel.Content] id={GetHashCode()} NewContentNull -> NoAdd", "MAUI-Layout");
					return;
				}

				if (!children.Contains(_content))
				{
					children.Add(_content);
					System.Diagnostics.Debug.WriteLine($"[ContentPanel.Content] id={GetHashCode()} AddedContent ChildCount(afterAdd)={children.Count}", "MAUI-Layout");
				}
			}
		}

		internal bool IsInnerPath { get; private set; }

		protected override global::Windows.Foundation.Size ArrangeOverride(global::Windows.Foundation.Size finalSize)
		{
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.ArrangeOverride] id={GetHashCode()} FinalSize={finalSize} Content={(Content != null ? Content.GetType().Name : "null")} BorderStrokeShape={_borderStroke?.Shape?.GetType().Name}", "MAUI-Layout");
			var actual = base.ArrangeOverride(finalSize);

			_borderPath?.Arrange(new global::Windows.Foundation.Rect(0, 0, finalSize.Width, finalSize.Height));

			var size = new global::Windows.Foundation.Size(Math.Max(0, actual.Width), Math.Max(0, actual.Height));

			// We need to update the clip since the content's position might have changed
			UpdateClip(_borderStroke?.Shape, size.Width, size.Height);
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.ArrangeOverride] id={GetHashCode()} ActualAfterBase={actual} SizeUsed={size} StrokeThickness={_borderPath?.StrokeThickness} IsInnerPath={IsInnerPath}", "MAUI-Layout");

			return size;
		}

		public ContentPanel()
		{
			_borderPath = new Path();
			EnsureBorderPath(containsCheck: false);
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.Ctor] id={GetHashCode()} Created BorderPath StrokeThickness={_borderPath.StrokeThickness}", "MAUI-Layout");

			SizeChanged += ContentPanelSizeChanged;
		}

		protected override global::Windows.Foundation.Size MeasureOverride(global::Windows.Foundation.Size availableSize)
		{
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.MeasureOverride] id={GetHashCode()} Available={availableSize} HasContent={Content != null} StrokeShape={_borderStroke?.Shape?.GetType().Name} StrokeThickness={_borderPath?.StrokeThickness}", "MAUI-Layout");
			var size = base.MeasureOverride(availableSize);
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.MeasureOverride] id={GetHashCode()} Result={size} ContentDesired=({Content?.DesiredSize.Width},{Content?.DesiredSize.Height}) ChildCount={CachedChildren.Count}", "MAUI-Layout");
			return size;
		}

		void ContentPanelSizeChanged(object sender, SizeChangedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.SizeChanged] id={GetHashCode()} Old={e.PreviousSize} New={e.NewSize}", "MAUI-Layout");
			if (_borderPath is null)
			{
				return;
			}

			var width = e.NewSize.Width;
			var height = e.NewSize.Height;

			if (width <= 0 || height <= 0)
			{
				return;
			}

			_borderPath.UpdatePath(_borderStroke?.Shape, width, height);
			UpdateClip(_borderStroke?.Shape, width, height);
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.SizeChanged] id={GetHashCode()} UpdatedPath Width={width} Height={height} StrokeShape={_borderStroke?.Shape?.GetType().Name} StrokeThickness={_borderPath?.StrokeThickness} IsInnerPath={IsInnerPath}", "MAUI-Layout");
		}

		internal void EnsureBorderPath(bool containsCheck = true)
		{
			if (containsCheck)
			{
				var children = CachedChildren;

				if (!children.Contains(_borderPath))
				{
					children.Add(_borderPath);
					System.Diagnostics.Debug.WriteLine($"[ContentPanel.EnsureBorderPath] id={GetHashCode()} AddedBorderPath containsCheck={containsCheck} ChildCount={children.Count}", "MAUI-Layout");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"[ContentPanel.EnsureBorderPath] id={GetHashCode()} BorderPathAlreadyPresent containsCheck={containsCheck} ChildCount={children.Count}", "MAUI-Layout");
				}
			}
			else
			{
				CachedChildren.Add(_borderPath);
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.EnsureBorderPath] id={GetHashCode()} AddedBorderPath(NoCheck) ChildCount={CachedChildren.Count}", "MAUI-Layout");
			}
		}

		public void UpdateBackground(Paint? background)
		{
			if (_borderPath is null)
			{
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateBackground] id={GetHashCode()} Skip(no borderPath) BackgroundType={background?.GetType().Name}", "MAUI-Layout");
				return;
			}

			_borderPath.UpdateBackground(background);
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateBackground] id={GetHashCode()} Applied BackgroundType={background?.GetType().Name}", "MAUI-Layout");
		}

		[Obsolete("Use Microsoft.Maui.Platform.UpdateBorderStroke instead")]
		public void UpdateBorderShape(IShape borderShape)
		{
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateBorderShape(Obsolete)] id={GetHashCode()} Shape={borderShape?.GetType().Name}", "MAUI-Layout");
			UpdateBorder(borderShape);
		}

		internal void UpdateBorderStroke(IBorderStroke borderStroke)
		{
			if (borderStroke is null)
			{
				return;
			}

			_borderStroke = borderStroke;
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateBorderStroke] id={GetHashCode()} StrokeShape={_borderStroke.Shape?.GetType().Name} PathSize=({ActualWidth},{ActualHeight})", "MAUI-Layout");

			if (_borderStroke is null)
			{
				return;
			}

			UpdateBorder(_borderStroke.Shape);
		}

		void UpdateBorder(IShape? strokeShape)
		{
			if (strokeShape is null || _borderPath is null)
			{
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateBorder] id={GetHashCode()} EarlyReturn strokeShapeNull={strokeShape is null} borderPathNull={_borderPath is null}", "MAUI-Layout");
				return;
			}

			_borderPath.UpdateBorderShape(strokeShape, ActualWidth, ActualHeight);
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateBorder] id={GetHashCode()} StrokeShape={strokeShape.GetType().Name} ActualSize=({ActualWidth},{ActualHeight}) StrokeThickness={_borderPath.StrokeThickness}", "MAUI-Layout");

			var width = ActualWidth;
			var height = ActualHeight;

			if (width <= 0 || height <= 0)
			{
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateBorder] id={GetHashCode()} SkipUpdateClip width={width} height={height}", "MAUI-Layout");
				return;
			}

			UpdateClip(strokeShape, width, height);
		}

		void UpdateClip(IShape? borderShape, double width, double height)
		{
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateClip] id={GetHashCode()} Enter width={width} height={height} Content={(Content != null)} BorderShapeType={borderShape?.GetType().Name} StrokeThickness={_borderPath?.StrokeThickness}", "MAUI-Layout");
			if (Content is null)
			{
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateClip] id={GetHashCode()} EarlyReturn(NoContent)", "MAUI-Layout");
				return;
			}

			if (height <= 0 && width <= 0)
			{
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateClip] id={GetHashCode()} EarlyReturn(NonPositiveSize) width={width} height={height}", "MAUI-Layout");
				return;
			}

			var clipGeometry = borderShape;

			if (clipGeometry is null)
			{
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateClip] id={GetHashCode()} EarlyReturn(NoGeometry)", "MAUI-Layout");
				return;
			}

			var visual = ElementCompositionPreview.GetElementVisual(Content);
			var compositor = visual.Compositor;

			PathF? clipPath;
			float strokeThickness = (float)(_borderPath?.StrokeThickness ?? 0);
			// The path size should consider the space taken by the border (top and bottom, left and right)
			var pathSize = new Rect(0, 0, width - strokeThickness * 2, height - strokeThickness * 2);

			if (clipGeometry is IRoundRectangle roundedRectangle)
			{
				clipPath = roundedRectangle.InnerPathForBounds(pathSize, strokeThickness / 2);
				IsInnerPath = true;
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateClip] id={GetHashCode()} UsingInnerPath bounds={pathSize} strokeThickness={strokeThickness}", "MAUI-Layout");
			}
			else
			{
				clipPath = clipGeometry.PathForBounds(pathSize);
				IsInnerPath = false;
				System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateClip] id={GetHashCode()} UsingOuterPath bounds={pathSize}", "MAUI-Layout");
			}

			var device = CanvasDevice.GetSharedDevice();
			var geometry = clipPath.AsPath(device);
			var path = new CompositionPath(geometry);
			var pathGeometry = compositor.CreatePathGeometry(path);
			var geometricClip = compositor.CreateGeometricClip(pathGeometry);

			// The clip needs to consider the content's offset in case it is in a different position because of a different alignment.
			geometricClip.Offset = new Vector2(strokeThickness - Content.ActualOffset.X, strokeThickness - Content.ActualOffset.Y);

			visual.Clip = geometricClip;
			System.Diagnostics.Debug.WriteLine($"[ContentPanel.UpdateClip] id={GetHashCode()} BorderShape={clipGeometry.GetType().Name} Width={width} Height={height} StrokeThickness={strokeThickness} InnerPath={IsInnerPath} ContentOffset={Content.ActualOffset} ClipOffset={geometricClip.Offset} PathSize={pathSize}", "MAUI-Layout");
		}
	}
}