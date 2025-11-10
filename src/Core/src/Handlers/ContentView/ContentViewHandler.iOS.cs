using System;
using PlatformView = UIKit.UIView;

namespace Microsoft.Maui.Handlers
{
	public partial class ContentViewHandler : ViewHandler<IContentView, ContentView>
	{
		IPlatformViewHandler? _contentHandler;

		protected override ContentView CreatePlatformView()
		{
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} must be set to create a {nameof(ContentView)}");
			_ = MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null");

			return new ContentView
			{
				CrossPlatformLayout = VirtualView
			};
		}

		public override void SetVirtualView(IView view)
		{
			base.SetVirtualView(view);
			_ = PlatformView ?? throw new InvalidOperationException($"{nameof(PlatformView)} should have been set by base class.");
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} should have been set by base class.");

			PlatformView.View = view;
			PlatformView.CrossPlatformLayout = VirtualView;
		}

		static void UpdateContent(IContentViewHandler handler)
		{
			_ = handler.PlatformView ?? throw new InvalidOperationException($"{nameof(PlatformView)} should have been set by base class.");
			_ = handler.VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} should have been set by base class.");
			_ = handler.MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} should have been set by base class.");

			// Disconnect the old content's handler tree (including all children) before clearing
			if (handler is ContentViewHandler contentViewHandler && contentViewHandler._contentHandler?.VirtualView is IView oldView)
			{
				oldView.DisconnectHandlers();
				System.Diagnostics.Debug.WriteLine("Disconnecting old content view handlers");
				contentViewHandler._contentHandler = null;
			}

			// Cleanup the old view when reused
			handler.PlatformView.ClearSubviews();

			if (handler.VirtualView.PresentedContent is IView view)
			{
				var platformView = view.ToPlatform(handler.MauiContext);
				handler.PlatformView.AddSubview(platformView);

				if (view.FlowDirection == FlowDirection.MatchParent)
				{
					platformView.UpdateFlowDirection(view);
				}

				// Store the new content handler so we can disconnect it later
				if (handler is ContentViewHandler contentHandler && view.Handler is IPlatformViewHandler viewHandler)
				{
					contentHandler._contentHandler = viewHandler;
				}

				// we need to trigger an invalidation of ancestor measures after the view has been added
				// so that it can walk up the hierarchy
				platformView.InvalidateAncestorsMeasures();
			}
		}

		public static partial void MapContent(IContentViewHandler handler, IContentView page)
		{
			UpdateContent(handler);
		}

		protected override void DisconnectHandler(ContentView platformView)
		{
			// Disconnect the entire content handler tree (including all children) when the ContentView itself is being disconnected
			if (_contentHandler?.VirtualView is IView view)
			{
				view.DisconnectHandlers();
			}
			_contentHandler = null;

			platformView.CrossPlatformLayout = null;
			platformView.RemoveFromSuperview();
			base.DisconnectHandler(platformView);
		}
	}
}
