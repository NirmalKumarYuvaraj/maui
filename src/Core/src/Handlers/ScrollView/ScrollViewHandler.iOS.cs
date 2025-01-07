using System;
using CoreGraphics;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Primitives;
using UIKit;

namespace Microsoft.Maui.Handlers
{
	public partial class ScrollViewHandler : ViewHandler<IScrollView, UIScrollView>, ICrossPlatformLayout
	{
		const nint ContentTag = 0x845fed;

		readonly ScrollEventProxy _eventProxy = new();

		public override bool NeedsContainer
		{
			get
			{
				//if we are being wrapped by a BorderView we need a container
				//so we can handle masks and clip shapes
				if (VirtualView?.Parent is IBorderView)
				{
					return true;
				}
				return base.NeedsContainer;
			}
		}

		internal ScrollToRequest? PendingScrollToRequest { get; private set; }

		protected override UIScrollView CreatePlatformView()
		{
			return new MauiScrollView();
		}

		protected override void ConnectHandler(UIScrollView platformView)
		{
			base.ConnectHandler(platformView);

			if (platformView is MauiScrollView platformScrollView)
			{
				platformScrollView.CrossPlatformLayout = this;
			}

			_eventProxy.Connect(VirtualView, platformView);
		}

		protected override void DisconnectHandler(UIScrollView platformView)
		{
			if (platformView is MauiScrollView platformScrollView)
			{
				platformScrollView.CrossPlatformLayout = null;
			}

			base.DisconnectHandler(platformView);

			PendingScrollToRequest = null;
			_eventProxy.Disconnect(platformView);
		}

		public static void MapContent(IScrollViewHandler handler, IScrollView scrollView)
		{
			if (handler.PlatformView == null || handler.MauiContext == null)
				return;
			if (handler is not ICrossPlatformLayout crossPlatformLayout)
			{
				return;
			}
			// We'll use the local cross-platform layout methods defined in our handler (which wrap the ScrollView's default methods)
			// so we can normalize the behavior of the scrollview to match the other platforms
			UpdateContentView(scrollView, handler, crossPlatformLayout);
		}

		// We don't actually have this mapped because we don't need it, but we can't remove it because it's public
		public static void MapContentSize(IScrollViewHandler handler, IScrollView scrollView)
		{
			handler.PlatformView?.UpdateContentSize(scrollView.ContentSize);
		}

		public static void MapIsEnabled(IScrollViewHandler handler, IScrollView scrollView)
		{
			handler.PlatformView?.UpdateIsEnabled(scrollView);
		}

		public static void MapHorizontalScrollBarVisibility(IScrollViewHandler handler, IScrollView scrollView)
		{
			handler.PlatformView?.UpdateHorizontalScrollBarVisibility(scrollView.HorizontalScrollBarVisibility);
		}

		public static void MapVerticalScrollBarVisibility(IScrollViewHandler handler, IScrollView scrollView)
		{
			handler.PlatformView?.UpdateVerticalScrollBarVisibility(scrollView.VerticalScrollBarVisibility);
		}

		public static void MapOrientation(IScrollViewHandler handler, IScrollView scrollView)
		{
			if (handler?.PlatformView is not { } platformView)
			{
				return;
			}

			platformView.InvalidateMeasure(scrollView);
		}

		public static void MapRequestScrollTo(IScrollViewHandler handler, IScrollView scrollView, object? args)
		{
			if (args is ScrollToRequest request)
			{
				var uiScrollView = handler.PlatformView;

				if (uiScrollView.ContentSize == CGSize.Empty && handler is ScrollViewHandler scrollViewHandler)
				{
					// If the ContentSize of the UIScrollView has not yet been defined,
					// we create a pending scroll request that we will launch after performing the Layout and sizing process.
					scrollViewHandler.PendingScrollToRequest = request;
					return;
				}

				var availableScrollHeight = uiScrollView.ContentSize.Height - uiScrollView.Frame.Height;
				var availableScrollWidth = uiScrollView.ContentSize.Width - uiScrollView.Frame.Width;
				var minScrollHorizontal = Math.Min(request.HorizontalOffset, availableScrollWidth);
				var minScrollVertical = Math.Min(request.VerticalOffset, availableScrollHeight);
				uiScrollView.SetContentOffset(new CGPoint(minScrollHorizontal, minScrollVertical), !request.Instant);

				if (request.Instant)
				{
					scrollView.ScrollFinished();
				}
			}
		}

		static UIView? GetContentView(UIScrollView scrollView)
		{
			for (int i = 0; i < scrollView.Subviews.Length; i++)
			{
				if (scrollView.Subviews[i] is { Tag: ContentTag } contentView)
				{
					return contentView;
				}
			}

			return null;
		}

		static void UpdateContentView(IScrollView scrollView, IScrollViewHandler handler, ICrossPlatformLayout crossPlatformLayout)
		{
			if (scrollView.PresentedContent == null || handler.MauiContext == null)
			{
				return;
			}

			var platformScrollView = handler.PlatformView;
			var nativeContent = scrollView.PresentedContent.ToPlatform(handler.MauiContext);

			if (GetContentView(platformScrollView) is ContentView currentContentContainer)
			{
				if (currentContentContainer.Subviews.Length == 0 || currentContentContainer.Subviews[0] != nativeContent)
				{
					currentContentContainer.ClearSubviews();
					currentContentContainer.AddSubview(nativeContent);
					currentContentContainer.View = scrollView.PresentedContent;
				}
			}
			else
			{
				InsertContentView(platformScrollView, scrollView, nativeContent, crossPlatformLayout);
			}
		}

		static void InsertContentView(UIScrollView platformScrollView, IScrollView scrollView, UIView platformContent, ICrossPlatformLayout crossPlatformLayout)
		{
			if (scrollView.PresentedContent == null)
			{
				return;
			}

			var contentContainer = new ContentView()
			{
				View = scrollView.PresentedContent,
				Tag = ContentTag
			};

			// This is where we normally would inject the cross-platform ScrollView's layout logic; instead, we're injecting the
			// methods from this handler so it can make some adjustments for things like Padding before the default logic is invoked
			contentContainer.CrossPlatformLayout = crossPlatformLayout;

			platformScrollView.ClearSubviews();
			contentContainer.AddSubview(platformContent);
			platformScrollView.AddSubview(contentContainer);
		}

		public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			var virtualView = VirtualView;
			var crossPlatformLayout = virtualView as ICrossPlatformLayout;
			var platformView = PlatformView;

			if (platformView == null || virtualView == null)
			{
				return new Size(widthConstraint, heightConstraint);
			}

			var padding = virtualView.Padding;

			// Account for the ScrollView Padding before measuring the content
			widthConstraint = AccountForPadding(widthConstraint, padding.HorizontalThickness);
			heightConstraint = AccountForPadding(heightConstraint, padding.VerticalThickness);

			var crossPlatformContentSize = crossPlatformLayout.CrossPlatformMeasure(widthConstraint, heightConstraint);

			// Add the padding back in for the final size
			crossPlatformContentSize.Width += padding.HorizontalThickness;
			crossPlatformContentSize.Height += padding.VerticalThickness;

			var viewportWidth = Math.Min(crossPlatformContentSize.Width, widthConstraint);
			var viewportHeight = Math.Min(crossPlatformContentSize.Height, heightConstraint);

			// Since the UIScrollView might not be arranged yet, we can't rely on its Bounds for the viewport height/width
			// So we'll use the constraints instead.
			SetContentSizeForOrientation(platformView, widthConstraint, heightConstraint, virtualView.Orientation, crossPlatformContentSize);

			var finalWidth = ViewHandlerExtensions.ResolveConstraints(viewportWidth, virtualView.Width, virtualView.MinimumWidth, virtualView.MaximumWidth);
			var finalHeight = ViewHandlerExtensions.ResolveConstraints(viewportHeight, virtualView.Height, virtualView.MinimumHeight, virtualView.MaximumHeight);

			return new Size(finalWidth, finalHeight);
		}
		static void SetContentSizeForOrientation(UIScrollView uiScrollView, double viewportWidth, double viewportHeight, ScrollOrientation orientation, Size contentSize)
		{
			if (orientation is ScrollOrientation.Vertical or ScrollOrientation.Neither)
			{
				contentSize.Width = Math.Min(contentSize.Width, viewportWidth);
			}

			if (orientation is ScrollOrientation.Horizontal or ScrollOrientation.Neither)
			{
				contentSize.Height = Math.Min(contentSize.Height, viewportHeight);
			}

			uiScrollView.ContentSize = contentSize;
		}
		static double AccountForPadding(double constraint, double padding)
		{
			// Remove the padding from the constraint, but don't allow it to go negative
			return Math.Max(0, constraint - padding);
		}

		Size ICrossPlatformLayout.CrossPlatformMeasure(double widthConstraint, double heightConstraint)
		{
			// if (VirtualView is not { } scrollView)
			// {
			// 	return Size.Zero;
			// }

			// var scrollOrientation = scrollView.Orientation;
			// var contentWidthConstraint = scrollOrientation is ScrollOrientation.Horizontal or ScrollOrientation.Both ? double.PositiveInfinity : widthConstraint;
			// var contentHeightConstraint = scrollOrientation is ScrollOrientation.Vertical or ScrollOrientation.Both ? double.PositiveInfinity : heightConstraint;
			// var contentSize = MeasureContent(scrollView, scrollView.Padding, contentWidthConstraint, contentHeightConstraint);

			// // Our target size is the smaller of it and the constraints
			// var width = contentSize.Width <= widthConstraint ? contentSize.Width : widthConstraint;
			// var height = contentSize.Height <= heightConstraint ? contentSize.Height : heightConstraint;

			// width = ViewHandlerExtensions.ResolveConstraints(width, scrollView.Width, scrollView.MinimumWidth, scrollView.MaximumWidth);
			// height = ViewHandlerExtensions.ResolveConstraints(height, scrollView.Height, scrollView.MinimumHeight, scrollView.MaximumHeight);

			//return new Size(width, height);

			var scrollView = VirtualView;
			var crossPlatformLayout = scrollView as ICrossPlatformLayout;
			var platformScrollView = PlatformView;

			var presentedContent = scrollView.PresentedContent;
			if (presentedContent == null)
			{
				return Size.Zero;
			}

			var viewportSize = GetViewportSize(platformScrollView);

			var padding = scrollView.Padding;

			if (widthConstraint == 0)
			{
				widthConstraint = viewportSize.Width;
			}

			if (heightConstraint == 0)
			{
				heightConstraint = viewportSize.Height;
			}

			// Account for the ScrollView Padding before measuring the content
			widthConstraint = AccountForPadding(widthConstraint, padding.HorizontalThickness);
			heightConstraint = AccountForPadding(heightConstraint, padding.VerticalThickness);

			// Now handle the actual cross-platform measurement of the ScrollView's content
			var result = crossPlatformLayout.CrossPlatformMeasure(widthConstraint, heightConstraint);

			return result.AdjustForFill(new Rect(0, 0, widthConstraint, heightConstraint), presentedContent);
		}
		static CGSize GetViewportSize(UIScrollView platformScrollView)
		{
			return platformScrollView.AdjustedContentInset.InsetRect(platformScrollView.Bounds).Size;
		}
		static Size MeasureContent(IContentView contentView, Thickness inset, double widthConstraint, double heightConstraint)
		{
			var content = contentView.PresentedContent;

			var contentSize = Size.Zero;

			if (!double.IsInfinity(widthConstraint) && Dimension.IsExplicitSet(contentView.Width))
			{
				widthConstraint = contentView.Width;
			}

			if (!double.IsInfinity(heightConstraint) && Dimension.IsExplicitSet(contentView.Height))
			{
				heightConstraint = contentView.Height;
			}

			if (content is not null)
			{
				contentSize = content.Measure(widthConstraint - inset.HorizontalThickness,
					heightConstraint - inset.VerticalThickness);
			}

			return new Size(contentSize.Width + inset.HorizontalThickness, contentSize.Height + inset.VerticalThickness);
		}

		Size ICrossPlatformLayout.CrossPlatformArrange(Rect bounds)
		{
			return (VirtualView as ICrossPlatformLayout)?.CrossPlatformArrange(bounds) ?? Size.Zero;
		}

		class ScrollEventProxy
		{
			WeakReference<IScrollView>? _virtualView;

			IScrollView? VirtualView => _virtualView is not null && _virtualView.TryGetTarget(out var v) ? v : null;

			public void Connect(IScrollView virtualView, UIScrollView platformView)
			{
				_virtualView = new(virtualView);

				platformView.Scrolled += Scrolled;
				platformView.ScrollAnimationEnded += ScrollAnimationEnded;
			}

			public void Disconnect(UIScrollView platformView)
			{
				_virtualView = null;

				platformView.Scrolled -= Scrolled;
				platformView.ScrollAnimationEnded -= ScrollAnimationEnded;
			}

			void ScrollAnimationEnded(object? sender, EventArgs e)
			{
				VirtualView?.ScrollFinished();
			}

			void Scrolled(object? sender, EventArgs e)
			{
				if (VirtualView == null)
				{
					return;
				}

				if (sender is not UIScrollView platformView)
				{
					return;
				}

				VirtualView.HorizontalOffset = platformView.ContentOffset.X;
				VirtualView.VerticalOffset = platformView.ContentOffset.Y;
			}
		}
	}
}
