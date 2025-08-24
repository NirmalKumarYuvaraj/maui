using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Graphics.Platform;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public class MauiShapeView : PlatformGraphicsView, IUIViewLifeCycleEvents
	{
		public MauiShapeView()
		{
			BackgroundColor = UIColor.Clear;
		}

		[UnconditionalSuppressMessage("Memory", "MEM0002", Justification = IUIViewLifeCycleEvents.UnconditionalSuppressMessage)]
		EventHandler? _movedToWindow;
		event EventHandler IUIViewLifeCycleEvents.MovedToWindow
		{
			add => _movedToWindow += value;
			remove => _movedToWindow -= value;
		}

		public override void MovedToWindow()
		{
			base.MovedToWindow();
			_movedToWindow?.Invoke(this, EventArgs.Empty);
		}

		public override bool Hidden
		{
			get => base.Hidden;
			set
			{
				bool wasHidden = base.Hidden;
				base.Hidden = value;

				// If the view was hidden and is now becoming visible, ensure it gets redrawn
				// This fixes the issue where BoxViews in initially invisible parents don't render
				// We need to invalidate the drawable to trigger the draw method
				if (wasHidden && !value && Window != null)
				{
					InvalidateDrawable();
				}
			}
		}
	}
}