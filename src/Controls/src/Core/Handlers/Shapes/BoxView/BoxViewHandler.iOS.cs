namespace Microsoft.Maui.Controls.Handlers
{
	public partial class BoxViewHandler : ShapeViewHandler
	{
		public static void MapColor(IShapeViewHandler handler, BoxView boxView)
		{
			handler.PlatformView?.InvalidateShape(boxView);
		}

		public static void MapCornerRadius(IShapeViewHandler handler, BoxView boxView)
		{
			handler.PlatformView?.InvalidateShape(boxView);
		}
	}
}