using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Handlers
{
	public partial class ShapeViewHandler : ViewHandler<IShapeView, MauiShapeView>
	{
		protected override MauiShapeView CreatePlatformView()
			=> new MauiShapeView(Context);

		public override bool NeedsContainer =>
			VirtualView?.Background != null ||
			base.NeedsContainer;

		public static void MapBackground(IShapeViewHandler handler, IShapeView shapeView)
		{
			// If Fill and Background are not null, will use Fill for the Shape background
			// and Background for the ShapeView background.
			if (shapeView.Background is not null && shapeView.Fill is not null)
			{
				handler.UpdateValue(nameof(IViewHandler.ContainerView));
				handler.ToPlatform().UpdateBackground(shapeView);

				handler.PlatformView?.InvalidateShape(shapeView);
			}
		}

		public static void MapShape(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.UpdateShape(shapeView);
		}

		public static void MapAspect(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public static void MapFill(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public static void MapStroke(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public static void MapStrokeThickness(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public static void MapStrokeDashPattern(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public static void MapStrokeDashOffset(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public static void MapStrokeLineCap(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public static void MapStrokeLineJoin(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public static void MapStrokeMiterLimit(IShapeViewHandler handler, IShapeView shapeView)
		{
			handler.PlatformView?.InvalidateShape(shapeView);
		}

		public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			var result = base.GetDesiredSize(widthConstraint, heightConstraint);

			// Ensure Rectangle shapes have reasonable minimum dimensions when not explicitly set
			// This prevents shapes from being invisible while maintaining proper layout behavior

			if (double.IsNaN(VirtualView.Width) && result.Width <= 0)
			{
				// If no explicit width and base measurement returned 0 or negative, use a small default
				result.Width = 1;
			}

			if (double.IsNaN(VirtualView.Height) && result.Height <= 0)
			{
				// If no explicit height and base measurement returned 0 or negative, use a small default
				result.Height = 1;
			}

			return result;
		}



	}
}