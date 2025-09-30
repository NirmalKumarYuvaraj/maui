namespace Microsoft.Maui.Controls.Handlers
{
	public partial class BoxViewHandler : ShapeViewHandler
	{
		public override bool NeedsContainer =>
			VirtualView?.Clip is not null ||
			VirtualView?.Shadow is not null;

		public static void MapColor(IShapeViewHandler handler, BoxView boxView) { }

		public static void MapCornerRadius(IShapeViewHandler handler, BoxView boxView) { }
	}
}