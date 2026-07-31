using System;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Widget;
using Microsoft.Maui.Graphics;
using ASwitch = AndroidX.AppCompat.Widget.SwitchCompat;

namespace Microsoft.Maui.Handlers
{
	public partial class SwitchHandler : ViewHandler<ISwitch, ASwitch>
	{
		CheckedChangeListener? _changeListener;
		internal ColorStateList? DefaultTrackTintList { get; private set; }
		internal ColorStateList? DefaultThumbTintList { get; private set; }

		protected override ASwitch CreatePlatformView()
		{
			return new ASwitch(Context);
		}

		protected override void ConnectHandler(ASwitch platformView)
		{
			DefaultTrackTintList = platformView.TrackTintList;
			DefaultThumbTintList = platformView.ThumbTintList;

			_changeListener = new CheckedChangeListener(this);
			platformView.SetOnCheckedChangeListener(_changeListener);

			base.ConnectHandler(platformView);
			Material3ThemeManager.ThemeChanged += OnMaterial3ThemeChanged;
		}

		protected override void DisconnectHandler(ASwitch platformView)
		{
			platformView.SetOnCheckedChangeListener(null);
			_changeListener = null;
			Material3ThemeManager.ThemeChanged -= OnMaterial3ThemeChanged;
			DefaultTrackTintList = null;
			DefaultThumbTintList = null;

			base.DisconnectHandler(platformView);
		}

		public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			Size size = base.GetDesiredSize(widthConstraint, heightConstraint);

			if (size.Width == 0)
			{
				int width = (int)widthConstraint;

				if (widthConstraint <= 0)
					width = Context != null ? (int)Context.GetThemeAttributeDp(global::Android.Resource.Attribute.SwitchMinWidth) : 0;

				size = new Size(width, size.Height);
			}

			return size;
		}

		public static void MapIsOn(ISwitchHandler handler, ISwitch view)
		{
			handler.PlatformView?.UpdateIsOn(view);
		}

		public static void MapTrackColor(ISwitchHandler handler, ISwitch view)
		{
			if (handler is SwitchHandler platformHandler)
			{
				handler.PlatformView?.UpdateTrackColor(view, platformHandler.ResolveDefaultTrackTint(view));
			}
			else
				handler.PlatformView?.UpdateTrackColor(view);
		}

		public static void MapThumbColor(ISwitchHandler handler, ISwitch view)
		{
			if (handler is SwitchHandler platformHandler)
			{
				handler.PlatformView?.UpdateThumbColor(view, platformHandler.ResolveDefaultThumbTint(view));
			}
			else
				handler.PlatformView?.UpdateThumbColor(view);
		}

		internal ColorStateList? ResolveDefaultTrackTint(ISwitch view)
		{
			if (Material3Configuration.Enabled && view.TrackColor is null)
				DefaultTrackTintList = Material3ThemeDefaults.GetSwitchTrackTint(PlatformView.Context);

			return DefaultTrackTintList;
		}

		internal ColorStateList? ResolveDefaultThumbTint(ISwitch view)
		{
			if (Material3Configuration.Enabled && view.ThumbColor is null)
				DefaultThumbTintList = Material3ThemeDefaults.GetSwitchThumbTint(PlatformView.Context);

			return DefaultThumbTintList;
		}

		void OnCheckedChanged(bool isOn)
		{
			if (VirtualView is null || VirtualView.IsOn == isOn)
				return;

			VirtualView.IsOn = isOn;
		}

		void OnMaterial3ThemeChanged(object? sender, EventArgs e)
		{
			if (VirtualView is null)
				return;

			if (VirtualView.TrackColor is null)
				UpdateValue(nameof(ISwitch.TrackColor));

			if (VirtualView.ThumbColor is null)
				UpdateValue(nameof(ISwitch.ThumbColor));
		}

		sealed class CheckedChangeListener : Java.Lang.Object, CompoundButton.IOnCheckedChangeListener
		{
			readonly WeakReference<SwitchHandler> _handler;

			public CheckedChangeListener(SwitchHandler handler)
			{
				_handler = new WeakReference<SwitchHandler>(handler);
			}

			void CompoundButton.IOnCheckedChangeListener.OnCheckedChanged(CompoundButton? buttonView, bool isToggled)
			{
				if (_handler.TryGetTarget(out var handler))
				{
					handler.OnCheckedChanged(isToggled);
				}
			}
		}
	}
}