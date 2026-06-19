using System;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls
{
	public partial class SwipeView
	{
		internal static new void RemapForControls()
		{
			// Adjusted the mapping to preserve SwipeView.Entry legacy behavior
			SwipeViewHandler.Mapper.AppendToMapping<SwipeView, ISwipeViewHandler>(nameof(Background), MapBackground);
		}

		static void MapBackground(ISwipeViewHandler handler, SwipeView swipeView)
		{
			if (swipeView.Content is not null)
			{
				// Use the IView.Background implementation which also accounts for the (now-deprecated) BackgroundColor
				var contentBackground = ((IView)swipeView.Content).Background;

				if (contentBackground is null)
				{
					// Get effective background from swipe view (covers both Background brush and deprecated BackgroundColor)
					Brush swipeBackground = swipeView.Background;
					if (Brush.IsNullOrEmpty(swipeBackground))
					{
						var bgColor = (Color)swipeView.GetValue(BackgroundColorProperty);
						if (bgColor is not null)
							swipeBackground = new SolidColorBrush(bgColor);
					}

					if (!Brush.IsNullOrEmpty(swipeBackground))
					{
						swipeView.Content.Background = swipeBackground;
					}
				}
			}
		}
	}
}
