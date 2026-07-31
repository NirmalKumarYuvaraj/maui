using Android.Content.Res;
using Android.Graphics.Drawables;
using ASwitch = AndroidX.AppCompat.Widget.SwitchCompat;
using MSwitch = Google.Android.Material.MaterialSwitch.MaterialSwitch;

namespace Microsoft.Maui.Platform
{
	public static class SwitchExtensions
	{
		public static void UpdateIsOn(this ASwitch aSwitch, ISwitch view)
		{
			aSwitch.Checked = view.IsOn;
		}

		public static void UpdateTrackColor(this ASwitch aSwitch, ISwitch view)
		{
			var trackColor = view.TrackColor;

			if (trackColor is not null)
				aSwitch.TrackDrawable?.SetColorFilter(trackColor, FilterMode.SrcAtop);
			else
				aSwitch.TrackDrawable?.ClearColorFilter();
		}

		internal static void UpdateTrackColor(this ASwitch aSwitch, ISwitch view, ColorStateList? defaultTrackTintList)
		{
			var trackColor = view.TrackColor;

			if (trackColor is not null)
			{
				aSwitch.TrackDrawable?.SetColorFilter(trackColor, FilterMode.SrcAtop);
			}
			else
			{
				aSwitch.TrackDrawable?.ClearColorFilter();
				aSwitch.TrackTintList = defaultTrackTintList;
			}
		}

		// TODO: material3 - make it public in .net 11
		internal static void UpdateTrackColor(this MSwitch materialSwitch, ISwitch view, ColorStateList? defaultTrackTintList)
		{
			var trackColor = view.TrackColor;

			if (trackColor is not null)
			{
				materialSwitch.TrackTintList = ColorStateList.ValueOf(trackColor.ToPlatform());
			}
			else
			{
				materialSwitch.TrackTintList = defaultTrackTintList;
			}
		}

		internal static void UpdateThumbColor(this MSwitch materialSwitch, ISwitch view, ColorStateList? defaultThumbTintList)
		{
			var thumbColor = view.ThumbColor;
			if (thumbColor is not null)
			{
				materialSwitch.ThumbTintList = ColorStateList.ValueOf(thumbColor.ToPlatform());
			}
			else
			{
				materialSwitch.ThumbTintList = defaultThumbTintList;
			}
		}

		public static void UpdateThumbColor(this ASwitch aSwitch, ISwitch view)
		{
			var thumbColor = view.ThumbColor;

			if (thumbColor is not null)
				aSwitch.ThumbTintList = ColorStateListExtensions.CreateDefault(thumbColor.ToPlatform());
		}

		internal static void UpdateThumbColor(this ASwitch aSwitch, ISwitch view, ColorStateList? defaultThumbTintList)
		{
			var thumbColor = view.ThumbColor;

			if (thumbColor is not null)
			{
				// Use ThumbTintList instead of SetColorFilter to preserve the thumb shadow
				// SetColorFilter flattens the drawable and removes the shadow effect
				aSwitch.ThumbTintList = ColorStateListExtensions.CreateDefault(thumbColor.ToPlatform());
			}
			else
			{
				aSwitch.ThumbTintList = defaultThumbTintList;
			}
		}

		public static Drawable? GetDefaultSwitchTrackDrawable(this ASwitch aSwitch) =>
			aSwitch.TrackDrawable;

		public static Drawable? GetDefaultSwitchThumbDrawable(this ASwitch aSwitch) =>
			aSwitch.ThumbDrawable;
	}
}