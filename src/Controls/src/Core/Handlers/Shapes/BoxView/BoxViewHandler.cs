namespace Microsoft.Maui.Controls.Handlers
{
	public partial class BoxViewHandler : ShapeViewHandler
	{
		public static new IPropertyMapper<BoxView, IShapeViewHandler> Mapper = new PropertyMapper<BoxView, IShapeViewHandler>(ShapeViewHandler.Mapper)
		{
			[nameof(BoxView.Color)] = MapColor,
			[nameof(BoxView.CornerRadius)] = MapCornerRadius,
		};

		public BoxViewHandler() : base(Mapper)
		{
		}

		public BoxViewHandler(IPropertyMapper mapper) : base(mapper ?? Mapper)
		{
		}
	}
}