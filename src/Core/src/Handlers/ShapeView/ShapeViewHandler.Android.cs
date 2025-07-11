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

			// Handle partial dimension specifications for shapes
			// When Width or Height is not explicitly set, we should use the constraint
			// rather than defaulting to 0, which would make shapes invisible in layout scenarios

			if (double.IsNaN(VirtualView.Width) && !double.IsInfinity(widthConstraint) && widthConstraint > 0)
			{
				// If no explicit width but we have a valid width constraint, use it
				result.Width = widthConstraint;
			}

			if (double.IsNaN(VirtualView.Height) && !double.IsInfinity(heightConstraint) && heightConstraint > 0)
			{
				// If no explicit height but we have a valid height constraint, use it
				result.Height = heightConstraint;
			}

			return result;
		}

	}
}