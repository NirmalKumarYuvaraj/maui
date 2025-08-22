using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Primitives;
using static Microsoft.Maui.Primitives.Dimension;

namespace Microsoft.Maui.Layouts
{
	public static class LayoutExtensions
	{
		public static Size ComputeDesiredSize(this IView view, double widthConstraint, double heightConstraint)
		{
			_ = view ?? throw new ArgumentNullException(nameof(view));
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.ComputeDesiredSize] view={view.GetType().Name}#{view.GetHashCode()} Constraints=({widthConstraint},{heightConstraint}) Margin={view.Margin}", "MAUI-Layout");

			if (view.Handler == null)
			{
				return Size.Zero;
			}

			var margin = view.Margin;

			// Adjust the constraints to account for the margins
			widthConstraint -= margin.HorizontalThickness;
			heightConstraint -= margin.VerticalThickness;

			// Ask the handler to do the actual measuring
			var measureWithoutMargins = view.Handler.GetDesiredSize(widthConstraint, heightConstraint);

			// Account for the margins when reporting the desired size value
			var result = new Size(measureWithoutMargins.Width + margin.HorizontalThickness,
				measureWithoutMargins.Height + margin.VerticalThickness);
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.ComputeDesiredSize] view={view.GetType().Name}#{view.GetHashCode()} Result={result} MeasureCore={measureWithoutMargins}", "MAUI-Layout");
			return result;
		}

		public static Rect ComputeFrame(this IView view, Rect bounds)
		{
			Thickness margin = view.Margin;
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.ComputeFrame] view={view.GetType().Name}#{view.GetHashCode()} Bounds={bounds} Desired={view.DesiredSize} Margin={margin} HAlign={view.HorizontalLayoutAlignment} VAlign={view.VerticalLayoutAlignment}", "MAUI-Layout");

			// We need to determine the width the element wants to consume; normally that's the element's DesiredSize.Width
			var consumedWidth = view.DesiredSize.Width;

			// But if the element is set to fill horizontally and it doesn't have an explicitly set width,
			// then we want the minimum between its MaximumWidth and the bounds' width
			// MaximumWidth is always positive infinity if not defined by the user
			if (view.HorizontalLayoutAlignment == LayoutAlignment.Fill && !IsExplicitSet(view.Width))
			{
				consumedWidth = Math.Min(bounds.Width, view.MaximumWidth);
			}

			// And the actual frame width needs to subtract the margins
			var frameWidth = Math.Max(0, consumedWidth - margin.HorizontalThickness);

			// We need to determine the height the element wants to consume; normally that's the element's DesiredSize.Height
			var consumedHeight = view.DesiredSize.Height;

			// But, if the element is set to fill vertically and it doesn't have an explicitly set height,
			// then we want the minimum between its MaximumHeight  and the bounds' height
			// MaximumHeight is always positive infinity if not defined by the user
			if (view.VerticalLayoutAlignment == LayoutAlignment.Fill && !IsExplicitSet(view.Height))
			{
				consumedHeight = Math.Min(bounds.Height, view.MaximumHeight);
			}

			// And the actual frame height needs to subtract the margins
			var frameHeight = Math.Max(0, consumedHeight - margin.VerticalThickness);

			var frameX = AlignHorizontal(view, bounds, margin);
			var frameY = AlignVertical(view, bounds, margin);

			var frame = new Rect(frameX, frameY, frameWidth, frameHeight);
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.ComputeFrame] view={view.GetType().Name}#{view.GetHashCode()} Frame={frame}", "MAUI-Layout");
			return frame;
		}

		static double AlignHorizontal(IView view, Rect bounds, Thickness margin)
		{
			var alignment = view.HorizontalLayoutAlignment;
			var desiredWidth = view.DesiredSize.Width;

			if (alignment == LayoutAlignment.Fill && (IsExplicitSet(view.Width) || !double.IsInfinity(view.MaximumWidth)))
			{
				// If the view has an explicit width (or non-infinite MaxWidth) set and the layout alignment is Fill,
				// we just treat the view as centered within the space it "fills"
				alignment = LayoutAlignment.Center;

				// If the width is not set, we use the minimum between the MaxWidth or the bound's width
				desiredWidth = IsExplicitSet(view.Width) ? desiredWidth : Math.Min(bounds.Width, view.MaximumWidth);
			}

			var x = AlignHorizontal(bounds.X, margin.Left, margin.Right, bounds.Width, desiredWidth, alignment);
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.AlignHorizontal] view={view.GetType().Name}#{view.GetHashCode()} Alignment={alignment} DesiredWidth={desiredWidth} ResultX={x}", "MAUI-Layout");
			return x;
		}

		static double AlignHorizontal(double startX, double startMargin, double endMargin, double boundsWidth,
			double desiredWidth, LayoutAlignment horizontalLayoutAlignment)
		{
			double frameX = startX + startMargin;

			switch (horizontalLayoutAlignment)
			{
				case LayoutAlignment.Center:
					frameX += (boundsWidth - desiredWidth) / 2;
					break;

				case LayoutAlignment.End:
					frameX += boundsWidth - desiredWidth;
					break;
			}

			return frameX;
		}

		static double AlignVertical(IView view, Rect bounds, Thickness margin)
		{
			var alignment = view.VerticalLayoutAlignment;
			var desiredHeight = view.DesiredSize.Height;

			if (alignment == LayoutAlignment.Fill && (IsExplicitSet(view.Height) || !double.IsInfinity(view.MaximumHeight)))
			{
				// If the view has an explicit height (or non-infinite MaxHeight) set and the layout alignment is Fill,
				// we just treat the view as centered within the space it "fills"
				alignment = LayoutAlignment.Center;

				// If the height is not set, we use the minimum between the MaxHeight or the bound's height
				desiredHeight = IsExplicitSet(view.Height) ? desiredHeight : Math.Min(bounds.Height, view.MaximumHeight);
			}

			double frameY = bounds.Y + margin.Top;

			switch (alignment)
			{
				case LayoutAlignment.Center:
					frameY += (bounds.Height - desiredHeight) / 2;
					break;

				case LayoutAlignment.End:
					frameY += bounds.Height - desiredHeight;
					break;
			}

			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.AlignVertical] view={view.GetType().Name}#{view.GetHashCode()} Alignment={alignment} DesiredHeight={desiredHeight} ResultY={frameY}", "MAUI-Layout");
			return frameY;
		}

		public static Size MeasureContent(this IContentView contentView, double widthConstraint, double heightConstraint)
		{
			return contentView.MeasureContent(contentView.Padding, widthConstraint, heightConstraint);
		}

		public static Size MeasureContent(this IContentView contentView, Thickness inset, double widthConstraint, double heightConstraint)
		{
			var content = contentView.PresentedContent;
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.MeasureContent] view={contentView.GetType().Name}#{contentView.GetHashCode()} Content={(content as IView)?.GetType().Name} Inset={inset} Constraints=({widthConstraint},{heightConstraint})", "MAUI-Layout");

			if (Dimension.IsExplicitSet(contentView.Width))
			{
				widthConstraint = contentView.Width;
			}

			if (Dimension.IsExplicitSet(contentView.Height))
			{
				heightConstraint = contentView.Height;
			}

			var contentSize = Size.Zero;

			if (content != null)
			{
				contentSize = content.Measure(widthConstraint - inset.HorizontalThickness,
					heightConstraint - inset.VerticalThickness);
			}

			var total = new Size(contentSize.Width + inset.HorizontalThickness, contentSize.Height + inset.VerticalThickness);
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.MeasureContent] view={contentView.GetType().Name}#{contentView.GetHashCode()} Result={total} ContentCore={contentSize}", "MAUI-Layout");
			return total;
		}

		public static void ArrangeContent(this IContentView contentView, Rect bounds)
		{
			if (contentView.PresentedContent == null)
			{
				return;
			}
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.ArrangeContent] view={contentView.GetType().Name}#{contentView.GetHashCode()} Bounds={bounds} Padding={contentView.Padding}", "MAUI-Layout");

			var padding = contentView.Padding;

			var targetBounds = new Rect(bounds.Left + padding.Left, bounds.Top + padding.Top,
				bounds.Width - padding.HorizontalThickness, bounds.Height - padding.VerticalThickness);

			_ = contentView.PresentedContent.Arrange(targetBounds);
			if (contentView.PresentedContent is IView cv)
				System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.ArrangeContent] ContentFrame={cv.Frame} TargetBounds={targetBounds}", "MAUI-Layout");
		}

		public static Size AdjustForFill(this Size size, Rect bounds, IView view)
		{
			var original = size;
			if (view.HorizontalLayoutAlignment == LayoutAlignment.Fill)
			{
				size.Width = Math.Max(bounds.Width, size.Width);
			}

			if (view.VerticalLayoutAlignment == LayoutAlignment.Fill)
			{
				size.Height = Math.Max(bounds.Height, size.Height);
			}

			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.AdjustForFill] view={view.GetType().Name}#{view.GetHashCode()} Original={original} Adjusted={size} Bounds={bounds}", "MAUI-Layout");
			return size;
		}

		/// <summary>
		/// Arranges content which can exceed the bounds of the IContentView.
		/// </summary>
		/// <remarks>
		/// Useful for arranging content where the IContentView provides a viewport to a portion of the content (e.g, 
		/// the content of an IScrollView).
		/// </remarks>
		/// <param name="contentView"></param>
		/// <param name="bounds"></param>
		/// <returns>The Size of the arranged content</returns>
		public static Size ArrangeContentUnbounded(this IContentView contentView, Rect bounds)
		{
			var presentedContent = contentView.PresentedContent;
			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.ArrangeContentUnbounded] view={contentView.GetType().Name}#{contentView.GetHashCode()} InitialBounds={bounds} Padding={contentView.Padding} Presented={(presentedContent as IView)?.GetType().Name}", "MAUI-Layout");

			if (presentedContent == null)
			{
				return bounds.Size;
			}

			var padding = contentView.Padding;

			// Normally we'd just want the content to be arranged within the ContentView's Frame,
			// but in this case the content may exceed the size of the Frame.
			// So in each dimension, we assume the larger of the two values.

			bounds.Width = Math.Max(bounds.Width, presentedContent.DesiredSize.Width + padding.HorizontalThickness);
			bounds.Height = Math.Max(bounds.Height, presentedContent.DesiredSize.Height + padding.VerticalThickness);

			contentView.ArrangeContent(bounds);

			System.Diagnostics.Debug.WriteLine($"[LayoutExtensions.ArrangeContentUnbounded] view={contentView.GetType().Name}#{contentView.GetHashCode()} FinalBounds={bounds} ContentDesired={presentedContent.DesiredSize}", "MAUI-Layout");
			return bounds.Size;
		}


	}
}
