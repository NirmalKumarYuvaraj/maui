using System;

namespace Microsoft.Maui.Handlers
{
	public partial class ContentViewHandler : ViewHandler<IContentView, ContentPanel>
	{
		IPlatformViewHandler? _contentHandler;

		public override void SetVirtualView(IView view)
		{
			base.SetVirtualView(view);

			_ = PlatformView ?? throw new InvalidOperationException($"{nameof(PlatformView)} should have been set by base class.");
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} should have been set by base class.");

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
				contentViewHandler._contentHandler = null;
			}

			handler.PlatformView.CachedChildren.Clear();

			if (handler.VirtualView.PresentedContent is IView view)
			{
				handler.PlatformView.CachedChildren.Add(view.ToPlatform(handler.MauiContext));

				// Store the new content handler so we can disconnect it later
				if (handler is ContentViewHandler contentHandler && view.Handler is IPlatformViewHandler viewHandler)
				{
					contentHandler._contentHandler = viewHandler;
				}
			}
		}

		protected override ContentPanel CreatePlatformView()
		{
			if (VirtualView == null)
			{
				throw new InvalidOperationException($"{nameof(VirtualView)} must be set to create a LayoutView");
			}

			var view = new ContentPanel
			{
				CrossPlatformLayout = VirtualView
			};

			return view;
		}

		public static partial void MapContent(IContentViewHandler handler, IContentView page)
		{
			UpdateContent(handler);
		}

		protected override void DisconnectHandler(ContentPanel platformView)
		{
			// Disconnect the entire content handler tree (including all children) when the ContentView itself is being disconnected
			if (_contentHandler?.VirtualView is IView view)
			{
				view.DisconnectHandlers();
			}
			_contentHandler = null;

			platformView.CrossPlatformLayout = null;
			platformView.CachedChildren?.Clear();

			base.DisconnectHandler(platformView);
		}
	}
}