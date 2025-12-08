#nullable disable
using System;
using System.ComponentModel;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Layouts;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;

namespace Microsoft.Maui.Controls.Platform.Compatibility
{
	public class ShellFlyoutTemplatedContentRenderer : Java.Lang.Object, IShellFlyoutContentRenderer
	{
		#region IShellFlyoutContentView

		public AView AndroidView => _rootView;

		#endregion IShellFlyoutContentView

		IShellContext _shellContext;
		bool _disposed;
		HeaderContainer _headerView;
		ViewGroup _rootView;
		Drawable _defaultBackgroundColor;
		ImageView _bgImage;
		AView _flyoutContentView;
		ShellViewRenderer _contentView;
		View _flyoutHeader;
		ContainerView _footerView;
		int _flyoutHeight;
		int _flyoutWidth;
		protected IMauiContext MauiContext => _shellContext.Shell.Handler.MauiContext;
		bool _initialLayoutChangeFired;
		IFlyoutView FlyoutView => _shellContext?.Shell;
		protected IShellContext ShellContext => _shellContext;
		protected AView FooterView => _footerView?.PlatformView;
		protected AView View => _rootView;
		WindowsListener _windowsListener;


		public ShellFlyoutTemplatedContentRenderer(IShellContext shellContext)
		{
			_shellContext = shellContext;
			_shellContext.CurrentDrawerLayout.DrawerStateChanged += OnFlyoutStateChanging;
			LoadView(shellContext);
		}

		void OnFlyoutStateChanging(object sender, AndroidX.DrawerLayout.Widget.DrawerLayout.DrawerStateChangedEventArgs e)
		{
			if (e.NewState != DrawerLayout.StateIdle)
			{
				if (_flyoutContentView == null)
				{
					UpdateFlyoutContent();
				}

				_shellContext.CurrentDrawerLayout.DrawerStateChanged -= OnFlyoutStateChanging;
			}
		}

		// Temporary workaround:
		// Android 15 / API 36 removed the prior opt‑out path for edge‑to‑edge
		// (legacy "edge to edge ignore" + decor fitting). This placeholder exists
		// so we can keep apps from regressing (content accidentally covered by
		// system bars) until a proper, unified edge‑to‑edge + system bar inset
		// configuration API is implemented in MAUI.
		//
		// NOTE:
		// - Keep this minimal.
		// - Will be replaced by the planned comprehensive window insets solution.
		// - Do not extend; add new logic to the forthcoming implementation instead.
		internal class WindowsListener : MauiWindowInsetListener, IOnApplyWindowInsetsListener
		{
			private WeakReference<ImageView> _bgImageRef;
			private WeakReference<AView> _flyoutViewRef;
			private WeakReference<AView> _footerViewRef;

			public AView FlyoutView
			{
				get
				{
					if (_flyoutViewRef != null && _flyoutViewRef.TryGetTarget(out var flyoutView))
						return flyoutView;

					return null;
				}
				set
				{
					_flyoutViewRef = new WeakReference<AView>(value);
				}
			}
			public AView FooterView
			{
				get
				{
					if (_footerViewRef != null && _footerViewRef.TryGetTarget(out var footerView))
						return footerView;

					return null;
				}
				set
				{
					_footerViewRef = new WeakReference<AView>(value);
				}
			}

			public WindowsListener(ImageView bgImage)
			{
				_bgImageRef = new WeakReference<ImageView>(bgImage);
			}

			public override WindowInsetsCompat OnApplyWindowInsets(AView v, WindowInsetsCompat insets)
			{
				if (insets == null || v == null)
					return insets;

				// The flyout overlaps the status bar so we apply insets directly
				var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
				var displayCutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
				var topInset = Math.Max(systemBars?.Top ?? 0, displayCutout?.Top ?? 0);
				var bottomInset = Math.Max(systemBars?.Bottom ?? 0, displayCutout?.Bottom ?? 0);

				int flyoutViewBottomInset = 0;

				if (FooterView is not null)
				{
					v.SetPadding(0, 0, 0, bottomInset);
					flyoutViewBottomInset = 0;
				}
				else
				{
					flyoutViewBottomInset = bottomInset;
					v.SetPadding(0, 0, 0, 0);
				}

				FlyoutView?.SetPadding(0, topInset, 0, flyoutViewBottomInset);

				if (_bgImageRef != null && _bgImageRef.TryGetTarget(out var bgImage) && bgImage != null)
				{
					bgImage.SetPadding(0, topInset, 0, bottomInset);
				}

				return WindowInsetsCompat.Consumed;
			}
		}

		protected virtual void LoadView(IShellContext shellContext)
		{

			var context = shellContext.AndroidContext;

			// Create simple FrameLayout root
			_rootView = new FrameLayout(context)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent)
			};

			var metrics = context.Resources.DisplayMetrics;

			var width = Math.Min(metrics.WidthPixels, metrics.HeightPixels);

			var actionBarHeight = context.GetActionBarHeight();

			width -= actionBarHeight;

			_rootView.LayoutParameters = new LP(width, LP.MatchParent);

			_bgImage = new ImageView(context)
			{
				LayoutParameters = new FrameLayout.LayoutParams(LP.MatchParent, LP.MatchParent)
			};

			_windowsListener = new WindowsListener(_bgImage);

			MauiWindowInsetListener.SetupViewWithLocalListener(_rootView, _windowsListener);

			UpdateFlyoutHeader();

			UpdateFlyoutHeaderBehavior();

			_shellContext.Shell.PropertyChanged += OnShellPropertyChanged;

			UpdateFlyoutBackground();

			UpdateVerticalScrollMode();

			UpdateFlyoutFooter();

			if (FlyoutView.FlyoutBehavior == FlyoutBehavior.Locked)
			{
				OnFlyoutViewLayoutChanging();
			}

			// Trigger initial layout
			OnFlyoutViewLayoutChanging();
		}

		void OnFlyoutHeaderMeasureInvalidated(object sender, EventArgs e)
		{
			if (_headerView != null)
			{
				UpdateFlyoutHeaderBehavior();
			}
		}

		protected void OnElementSelected(Element element)
		{
			((IShellController)_shellContext.Shell).OnFlyoutItemSelected(element);
		}

		protected virtual void OnShellPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Shell.FlyoutHeaderBehaviorProperty.PropertyName)
			{
				UpdateFlyoutHeaderBehavior();
			}
			else if (e.IsOneOf(
				Shell.FlyoutBackgroundColorProperty,
				Shell.FlyoutBackgroundProperty,
				Shell.FlyoutBackgroundImageProperty,
				Shell.FlyoutBackgroundImageAspectProperty))
			{
				UpdateFlyoutBackground();
			}
			else if (e.Is(Shell.FlyoutVerticalScrollModeProperty))
			{
				UpdateVerticalScrollMode();
			}
			else if (e.IsOneOf(
				Shell.FlyoutHeaderProperty,
				Shell.FlyoutHeaderTemplateProperty))
			{
				UpdateFlyoutHeader();
			}
			else if (e.IsOneOf(
				Shell.FlyoutFooterProperty,
				Shell.FlyoutFooterTemplateProperty))
			{
				UpdateFlyoutFooter();
			}
			else if (e.IsOneOf(
				Shell.FlyoutContentProperty,
				Shell.FlyoutContentTemplateProperty))
			{
				UpdateFlyoutContent();
			}
		}

		protected virtual void UpdateFlyoutContent()
		{
			if (!_rootView.IsAlive())
			{
				return;
			}

			var index = 0;

			if (_flyoutContentView != null)
			{
				index = _rootView.IndexOfChild(_flyoutContentView);
				_rootView.RemoveView(_flyoutContentView);
			}

			_flyoutContentView = CreateFlyoutContent(_rootView);
			_windowsListener.FlyoutView = _flyoutContentView;
			if (_flyoutContentView == null)
			{
				return;
			}

			_rootView.AddView(_flyoutContentView, index);
			UpdateContentPadding();
		}


		void DisconnectRecyclerView()
		{
			if (_flyoutContentView.IsAlive() &&
				_flyoutContentView is RecyclerViewContainer rvc &&
				rvc.GetAdapter() is ShellFlyoutRecyclerAdapter sfra)
			{
				sfra.Disconnect();
			}
		}

		AView CreateFlyoutContent(ViewGroup rootView)
		{
			_rootView = rootView;

			if (_contentView != null)
			{
				var oldContentView = _contentView;
				_contentView = null;
				oldContentView.View = null;
			}

			DisconnectRecyclerView();

			var content = ((IShellController)ShellContext.Shell).FlyoutContent;

			if (content == null)
			{
				var context = ShellContext.AndroidContext;

				var recyclerView = new RecyclerViewContainer(context)
				{
					LayoutParameters = new FrameLayout.LayoutParams(LP.MatchParent, LP.MatchParent)
				};

				var adapter = new ShellFlyoutRecyclerAdapter(ShellContext, OnElementSelected);

				recyclerView.SetAdapter(adapter);

				return recyclerView;
			}

			_contentView = new ShellViewRenderer(ShellContext.AndroidContext, content, MauiContext);

			_contentView.PlatformView.LayoutParameters = new FrameLayout.LayoutParams(LP.MatchParent, LP.MatchParent);

			return _contentView.PlatformView;
		}

		protected virtual void UpdateFlyoutHeader()
		{
			if (_headerView != null)
			{
				_headerView.LayoutChange -= OnHeaderViewLayoutChange;

				var oldHeaderView = _headerView;
				_headerView = null;

				_rootView.RemoveView(oldHeaderView);

				oldHeaderView.Dispose();
			}

			if (_flyoutHeader != null)
			{
				_flyoutHeader.MeasureInvalidated -= OnFlyoutHeaderMeasureInvalidated;
			}

			_flyoutHeader = ((IShellController)_shellContext.Shell).FlyoutHeader;

			if (_flyoutHeader != null)
			{
				_flyoutHeader.MeasureInvalidated += OnFlyoutHeaderMeasureInvalidated;

				_headerView = new HeaderContainer(_rootView.Context, _flyoutHeader, MauiContext)
				{
					MatchWidth = true,
					LayoutParameters = new FrameLayout.LayoutParams(LP.MatchParent, LP.WrapContent)
					{
						Gravity = GravityFlags.Top
					}
				};

				_headerView.LayoutChange += OnHeaderViewLayoutChange;

				_rootView.AddView(_headerView, 0);

				UpdateFlyoutHeaderBehavior();
			}

			UpdateContentPadding();
		}

		void OnHeaderViewLayoutChange(object sender, AView.LayoutChangeEventArgs e)
		{
			// If the flyoutheader/footer have changed size then we need to 
			// remeasure the flyout content
			UpdateContentPadding();
		}

		protected virtual void UpdateFlyoutFooter()
		{
			if (_footerView != null)
			{
				var oldFooterView = _footerView;
				_rootView.RemoveView(_footerView);

				_footerView = null;

				_windowsListener.FooterView = null;

				oldFooterView.View = null;
			}

			var footer = ((IShellController)_shellContext.Shell).FlyoutFooter;

			if (footer == null)
			{
<<<<<<< Updated upstream
				UpdateContentPadding();
=======
>>>>>>> Stashed changes
				return;
			}

			if (_flyoutWidth == 0)
			{
				return;
			}

			_footerView = new ContainerView(_shellContext.AndroidContext, footer, MauiContext)
			{
				MatchWidth = true
			};

			_windowsListener.FooterView = _footerView;

			var footerViewLP = new FrameLayout.LayoutParams(LP.MatchParent, LP.WrapContent)
			{
				Gravity = GravityFlags.Bottom
			};

			_rootView.AddView(_footerView, footerViewLP);

			UpdateContentPadding();
		}

		int GetFooterViewTotalHeight()
		{
			var margin = Thickness.Zero;

			var measuredHeight = (FooterView?.MeasuredHeight ?? 0);

			if (_footerView?.View != null)
			{
				margin = _footerView.View.Margin;
			}

			var verticalThickness = (int)(_rootView.Context.ToPixels(margin.VerticalThickness));

			var totalHeight = measuredHeight + verticalThickness;

			return totalHeight;
		}

		bool UpdateContentPadding()
		{
			bool returnValue = false;

			var flyoutView = _flyoutContentView ?? _contentView?.PlatformView;

			if (flyoutView == null)
			{
				return false;
			}

			var viewMargin = _contentView?.View?.Margin ?? Thickness.Zero;

			var headerViewHeight = _headerView?.MeasuredHeight ?? 0;

			var footerViewHeight = GetFooterViewTotalHeight();

			// Calculate offsets for header and footer
			var topOffset = headerViewHeight + (int)_rootView.Context.ToPixels(viewMargin.Top);

			var bottomOffset = footerViewHeight + (int)_rootView.Context.ToPixels(viewMargin.Bottom);

			// For scrollable content (RecyclerView), use padding instead of margins
			// This allows proper scrolling with SetClipToPadding(false)
			if (flyoutView is AndroidX.Core.View.IScrollingView &&
				flyoutView is ViewGroup vg)
			{
				if (vg.PaddingTop != topOffset || vg.PaddingBottom != bottomOffset)
				{
					vg.SetPadding(0, topOffset, 0, bottomOffset);
					returnValue = true;
				}

				vg.SetClipToPadding(false);
			}
			else
			{
				// For non-scrollable content, use margins
				var flp = flyoutView.LayoutParameters as FrameLayout.LayoutParams;

				if (flp == null)
				{
					flp = new FrameLayout.LayoutParams(LP.MatchParent, LP.MatchParent);
					flyoutView.LayoutParameters = flp;
				}

				if (flp.TopMargin != topOffset)
				{
					flp.TopMargin = topOffset;
					returnValue = true;
				}

				if (flp.BottomMargin != bottomOffset)
				{
					flp.BottomMargin = bottomOffset;
					returnValue = true;
				}

				// Set left/right margins for XPLAT content
				if (_contentView?.PlatformView != null)
				{
					SetContentViewFrame();

					var leftMargin = (int)_rootView.Context.ToPixels(viewMargin.Left);

					var rightMargin = (int)_rootView.Context.ToPixels(viewMargin.Right);

					if (flp.LeftMargin != leftMargin)
					{
						flp.LeftMargin = leftMargin;
						returnValue = true;
					}

					if (flp.RightMargin != rightMargin)
					{
						flp.RightMargin = rightMargin;
						returnValue = true;
					}
				}
			}

			if (returnValue)
			{
				flyoutView.RequestLayout();
			}

			return returnValue;
		}

		void SetContentViewFrame()
		{
			var measuredWidth = _contentView.PlatformView.MeasuredWidth;
			var dpWidth = _rootView.Context.FromPixels(measuredWidth);
			var measuredHeight = _contentView.PlatformView.MeasuredHeight;
			var dpHeight = _rootView.Context.FromPixels(measuredHeight);
			var newFrame = _contentView.View.ComputeFrame(new Graphics.Rect(0, 0, dpWidth, dpHeight));
			_contentView.View.Frame = newFrame;
		}

		void OnFlyoutViewLayoutChanging()
		{
			// The second time this fires the non flyout part of the view
			// is visible to the user. I haven't found a better
			// mechanism to wire into in order to detect this
			if ((_initialLayoutChangeFired || FlyoutView.FlyoutBehavior == FlyoutBehavior.Locked) &&
				_flyoutContentView == null)
			{
				UpdateFlyoutContent();
			}

			_initialLayoutChangeFired = true;

			if (View?.MeasuredHeight > 0 &&
				View?.MeasuredWidth > 0 &&
				(_flyoutHeight != View.MeasuredHeight ||
				_flyoutWidth != View.MeasuredWidth)
			)
			{
				_flyoutHeight = View.MeasuredHeight;
				_flyoutWidth = View.MeasuredWidth;

				// We wait to instantiate the flyout footer until we know the WxH of the flyout container
				if (_footerView == null)
				{
					UpdateFlyoutFooter();
				}

				UpdateContentPadding();
			}
			else if (_contentView?.PlatformView is not null &&
				((_contentView.PlatformView.MeasuredHeight > 0 && _contentView.View.Frame.Width == 0) ||
				 (_contentView.PlatformView.MeasuredWidth > 0 && _contentView.View.Frame.Height == 0)))
			{
				SetContentViewFrame();
			}
		}

		void UpdateVerticalScrollMode()
		{
			if (_flyoutContentView is RecyclerView rv && rv.GetLayoutManager() is ScrollLayoutManager lm)
			{
				var scrollMode = _shellContext.Shell.FlyoutVerticalScrollMode;
				lm.ScrollVertically = scrollMode;
			}
		}

		protected virtual void UpdateFlyoutBackground()
		{
			var brush = _shellContext.Shell.FlyoutBackground;

			if (Brush.IsNullOrEmpty(brush))
			{
				var color = _shellContext.Shell.FlyoutBackgroundColor;

				if (_defaultBackgroundColor == null)
				{
					_defaultBackgroundColor = _rootView.Background;
				}

				if (color == null)
				{
					_rootView.Background = _defaultBackgroundColor;
				}
				else
				{
					_rootView.Background = new ColorDrawable(color.ToPlatform());
				}
			}
			else
			{
				_rootView.UpdateBackground(brush);
			}

			UpdateFlyoutBgImageAsync();
		}

		void UpdateFlyoutBgImageAsync()
		{
			var imageSource = _shellContext.Shell.FlyoutBackgroundImage;

			if (imageSource == null || !_shellContext.Shell.IsSet(Shell.FlyoutBackgroundImageProperty))
			{
				if (_rootView.IndexOfChild(_bgImage) != -1)
				{
					_rootView.RemoveView(_bgImage);
				}
				return;
			}

			var services = MauiContext.Services;
			var provider = services.GetRequiredService<IImageSourceServiceProvider>();

			_bgImage.Clear();

			imageSource.LoadImage(MauiContext, result =>
			{
				_bgImage.SetImageDrawable(result?.Value);

				if (!_rootView.IsAlive())
				{
					return;
				}

				if (result?.Value == null)
				{
					if (_rootView.IndexOfChild(_bgImage) != -1)
					{
						_rootView.RemoveView(_bgImage);
					}

					return;
				}

				var aspect = _shellContext.Shell.FlyoutBackgroundImageAspect;

				switch (aspect)
				{
					default:
					case Aspect.AspectFit:
						_bgImage.SetScaleType(ImageView.ScaleType.FitCenter);
						break;
					case Aspect.AspectFill:
						_bgImage.SetScaleType(ImageView.ScaleType.CenterCrop);
						break;
					case Aspect.Fill:
						_bgImage.SetScaleType(ImageView.ScaleType.FitXy);
						break;
				}

				if (_rootView.IndexOfChild(_bgImage) == -1)
				{
					if (_bgImage.SetElevation(float.MinValue))
					{
						_rootView.AddView(_bgImage);
					}
					else
					{
						_rootView.AddView(_bgImage, 0);
					}
				}
			});
		}

		protected virtual void UpdateFlyoutHeaderBehavior()
		{
			if (_headerView == null)
			{
				return;
			}

			var behavior = _shellContext.Shell.FlyoutHeaderBehavior;
			_headerView.SetFlyoutHeaderBehavior(behavior);
		}

		internal void Disconnect()
		{
			if (_rootView != null)
			{
				MauiWindowInsetListener.RemoveViewWithLocalListener(_rootView);
			}

			if (_shellContext?.Shell != null)
			{
				_shellContext.Shell.PropertyChanged -= OnShellPropertyChanged;
			}

			if (_flyoutHeader != null)
			{
				_flyoutHeader.MeasureInvalidated -= OnFlyoutHeaderMeasureInvalidated;
			}

			_flyoutHeader = null;
			_footerView?.View = null;
			_headerView?.Disconnect();
			DisconnectRecyclerView();
			_contentView?.View = null;
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (disposing)
			{
				Disconnect();

				if (_rootView != null && _footerView != null)
				{
					_rootView.RemoveView(_footerView);
				}

				if (_headerView != null)
				{
					_headerView.LayoutChange -= OnHeaderViewLayoutChange;
				}

				_contentView?.View = null;
				_flyoutContentView?.Dispose();
				_headerView?.Dispose();
				_rootView?.Dispose();
				_defaultBackgroundColor?.Dispose();
				_bgImage?.Dispose();

				_contentView = null;
				_rootView = null;
				_headerView = null;
				_shellContext = null;
				_flyoutContentView = null;
				_defaultBackgroundColor = null;
				_bgImage = null;
				_footerView = null;
			}

			base.Dispose(disposing);
		}

		// Simplified header container without AppBarLayout-specific logic
		public class HeaderContainer : ContainerView
		{
			bool _isdisposed = false;
			private FlyoutHeaderBehavior _flyoutHeaderBehavior;

			public HeaderContainer(Context context, View view, IMauiContext mauiContext) : base(context, view, mauiContext)
			{
				Initialize(view);
			}

			void Initialize(View view)
			{
				if (view != null)
				{
					view.PropertyChanged += OnViewPropertyChanged;
				}
			}

			void OnViewPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == PlatformConfiguration.AndroidSpecific.VisualElement.ElevationProperty.PropertyName)
				{
					UpdateElevation();
				}
			}

			void UpdateElevation()
			{
				if (Parent is AView view)
				{
					ElevationHelper.SetElevation(view, View);
				}
			}

			protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
			{
				UpdateMinimumHeight();
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			}

			protected override void OnLayout(bool changed, int l, int t, int r, int b)
			{
				UpdateElevation();
				base.OnLayout(changed, l, t, r, b);
			}

			protected override void Dispose(bool disposing)
			{
				if (_isdisposed)
					return;

				_isdisposed = true;

				if (disposing)
				{
					Disconnect();
				}

				base.Dispose(disposing);
			}

			internal void Disconnect()
			{
				if (View != null)
				{
					View.PropertyChanged -= OnViewPropertyChanged;
					View = null;
				}
			}

			internal void SetFlyoutHeaderBehavior(FlyoutHeaderBehavior flyoutHeaderBehavior)
			{
				if (_flyoutHeaderBehavior == flyoutHeaderBehavior)
					return;

				_flyoutHeaderBehavior = flyoutHeaderBehavior;
				UpdateMinimumHeight();
			}

			void UpdateMinimumHeight()
			{
				var minHeight = 0;

				if (View?.MinimumHeightRequest > 0)
				{
					minHeight = (int)Context.ToPixels(View.MinimumHeightRequest);
				}

				if (MinimumHeight != minHeight)
				{
					this.SetMinimumHeight(minHeight);
				}

				if (PlatformView is not null)
				{
					if (PlatformView.MinimumHeight != minHeight)
					{
						PlatformView.SetMinimumHeight(minHeight);
					}
				}
			}
		}
	}

	class RecyclerViewContainer : RecyclerView
	{
		bool _disposed;
		ScrollLayoutManager _layoutManager;

		public RecyclerViewContainer(Context context) : base(context)
		{
			SetClipToPadding(false);
			SetLayoutManager(_layoutManager = new ScrollLayoutManager(context, (int)Orientation.Vertical, false));
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;
			if (disposing)
			{
				SetLayoutManager(null);
				var adapter = this.GetAdapter();
				SetAdapter(null);
				adapter?.Dispose();
				_layoutManager?.Dispose();
				_layoutManager = null;
			}

			base.Dispose(disposing);
		}
	}

	internal sealed class ScrollLayoutManager : LinearLayoutManager
	{
		public ScrollMode ScrollVertically { get; set; } = ScrollMode.Auto;

		public ScrollLayoutManager(Context context, int orientation, bool reverseLayout) : base(context, orientation, reverseLayout)
		{
		}

		int GetVisibleChildCount()
		{
			var firstVisibleIndex = FindFirstCompletelyVisibleItemPosition();

			// If no items are completely visible, return 0
			if (firstVisibleIndex == -1)
			{
				return 0;
			}

			var lastVisibleIndex = FindLastCompletelyVisibleItemPosition();
			return lastVisibleIndex - firstVisibleIndex + 1;
		}

		public override bool CanScrollVertically()
		{
			// Use ItemCount (total items in adapter) instead of ChildCount (currently attached views)
			return ScrollVertically switch
			{
				ScrollMode.Disabled => false,
				ScrollMode.Enabled => true,
				_ => ItemCount > GetVisibleChildCount() && ItemCount > 0
			};
		}
	}
}
