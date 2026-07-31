#nullable disable
using Android.Content;
using Android.Views;
using AndroidX.Core.Content;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Graphics;
using AColor = Android.Graphics.Color;
using AColorRes = Android.Resource.Color;
using AView = Android.Views.View;

namespace Microsoft.Maui.Controls.Platform.Compatibility
{
	internal class ShellPageContainer : ViewGroup
	{
		static int? DarkBackground;
		static int? LightBackground;
		public IViewHandler Child { get; set; }

		public bool IsInFragment { get; set; }

		public ShellPageContainer(Context context, IPlatformViewHandler child, bool inFragment = false) : base(context)
		{
			Child = child;
			IsInFragment = inFragment;
			if (child.VirtualView.Background is null)
			{
				bool isDark = ShellRenderer.IsDarkTheme;

				int color = Material3Configuration.Enabled
				 ? Material3ThemeResolver.ResolveColor(context, Material3ColorRole.Surface)
				 : GetResourceBackground(context, isDark);

				child.PlatformView.SetBackgroundColor(new AColor(color));
			}
			child.PlatformView.RemoveFromParent();
			AddView(child.PlatformView);
		}

		int GetResourceBackground(Context context, bool isDark)
		{
			int color;
			if (isDark)
			{
				color = DarkBackground ??= ContextCompat.GetColor(context, AColorRes.BackgroundDark);
			}
			else
			{
				color = LightBackground ??= ContextCompat.GetColor(context, AColorRes.BackgroundLight);
			}
			return color;
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			var width = r - l;
			var height = b - t;

			if (Child.PlatformView is AView aView)
				aView.Layout(0, 0, width, height);
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (Child.PlatformView is AView aView)
			{
				aView.Measure(widthMeasureSpec, heightMeasureSpec);
				SetMeasuredDimension(aView.MeasuredWidth, aView.MeasuredHeight);
			}
			else
				SetMeasuredDimension(0, 0);
		}
	}
}
