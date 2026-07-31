using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using Google.Android.Material.Slider;

namespace Microsoft.Maui.Platform
{
	public static class SliderExtensions
	{
		public const double PlatformMaxValue = int.MaxValue;

		//Material 2 design spec - https://m2.material.io/components/sliders/android#discrete-slider
		//Additional info - https://github.com/material-components/material-components-android/blob/60b0325b39741784fca4d7aba079b65453bc7c66/lib/java/com/google/android/material/slider/res/values/dimens.xml#L27
		// Thumb diameter per Material Design spec: https://m2.material.io/components/sliders
		const int ThumbDiameterDp = 20;

		public static void UpdateMinimum(this SeekBar seekBar, ISlider slider) => UpdateValue(seekBar, slider);

		// TODO: Make this public in NET 11.
		internal static void UpdateMinimum(this Slider mSlider, ISlider slider)
		{
			mSlider.UpdateRange(slider);
		}

		public static void UpdateMaximum(this SeekBar seekBar, ISlider slider) => UpdateValue(seekBar, slider);

		// TODO: Make this public in NET 11.
		internal static void UpdateMaximum(this Slider mSlider, ISlider slider)
		{
			mSlider.UpdateRange(slider);
		}

		public static void UpdateValue(this SeekBar seekBar, ISlider slider)
		{
			var min = slider.Minimum;
			var max = slider.Maximum;
			var value = slider.Value;

			seekBar.Progress = (int)((value - min) / (max - min) * PlatformMaxValue);
		}

		// TODO: Make this public in NET 11.
		internal static void UpdateValue(this Slider mSlider, ISlider slider)
		{
			float value = Math.Clamp((float)slider.Value, mSlider.ValueFrom, mSlider.ValueTo);

			if (value != mSlider.Value)
			{
				mSlider.Value = value;
			}
		}

		static void UpdateRange(this Slider mSlider, ISlider slider)
		{
			float minimum = (float)slider.Minimum;
			float maximum = (float)slider.Maximum;

			if (minimum >= mSlider.ValueTo)
			{
				mSlider.ValueTo = maximum;
				mSlider.ValueFrom = minimum;
			}
			else
			{
				mSlider.ValueFrom = minimum;
				mSlider.ValueTo = maximum;
			}

			mSlider.UpdateValue(slider);
		}

		public static void UpdateMinimumTrackColor(this SeekBar seekBar, ISlider slider)
		{
			if (slider.MinimumTrackColor is not null)
			{
				seekBar.ProgressTintList = ColorStateList.ValueOf(slider.MinimumTrackColor.ToPlatform());
				seekBar.ProgressTintMode = PorterDuff.Mode.SrcIn;
			}
		}

		// TODO: Make this public in NET 11.
		internal static void UpdateMinimumTrackColor(this Slider mSlider, ISlider slider, ColorStateList? defaultTrackTintList)
		{
			if (slider.MinimumTrackColor is not null)
			{
				mSlider.TrackActiveTintList = ColorStateList.ValueOf(slider.MinimumTrackColor.ToPlatform());
			}
			else
			{
				mSlider.TrackActiveTintList = defaultTrackTintList!;
			}
		}

		public static void UpdateMaximumTrackColor(this SeekBar seekBar, ISlider slider)
		{
			if (slider.MaximumTrackColor is not null)
			{
				seekBar.ProgressBackgroundTintList = ColorStateList.ValueOf(slider.MaximumTrackColor.ToPlatform());
				seekBar.ProgressBackgroundTintMode = PorterDuff.Mode.SrcIn;
			}
		}

		// TODO: Make this public in NET 11.
		internal static void UpdateMaximumTrackColor(this Slider mSlider, ISlider slider, ColorStateList? defaultTrackTintList)
		{
			if (slider.MaximumTrackColor is not null)
			{
				mSlider.TrackInactiveTintList = ColorStateList.ValueOf(slider.MaximumTrackColor.ToPlatform());
			}
			else
			{
				mSlider.TrackInactiveTintList = defaultTrackTintList!;
			}
		}

		public static void UpdateThumbColor(this SeekBar seekBar, ISlider slider) =>
			seekBar.Thumb?.SetColorFilter(slider.ThumbColor, FilterMode.SrcIn);

		// TODO: Make this public in NET 11.
		internal static void UpdateThumbColor(this Slider mSlider, ISlider slider, ColorStateList? defaultThumbTintList)
		{
			if (slider.ThumbColor is not null)
			{
				mSlider.ThumbTintList = ColorStateList.ValueOf(slider.ThumbColor.ToPlatform());
			}
			else
			{
				mSlider.ThumbTintList = defaultThumbTintList!;
			}
		}

		public static async Task UpdateThumbImageSourceAsync(this SeekBar seekBar, ISlider slider, IImageSourceServiceProvider provider)
		{
			var context = seekBar.Context;
			if (context is null)
			{
				return;
			}

			var thumbImageSource = slider.ThumbImageSource;
			if (thumbImageSource is not null)
			{
				var service = provider.GetRequiredImageSourceService(thumbImageSource);
				var result = await service.GetDrawableAsync(thumbImageSource, context);
				var thumbDrawable = result?.Value;

				if (seekBar.IsAlive())
				{
					if (thumbDrawable is not null)
					{
						SetThumbDrawable(seekBar, context, thumbDrawable);
					}
					else
					{
						SetDefaultThumb(seekBar, slider, context);
					}
				}
			}
			else
			{
				SetDefaultThumb(seekBar, slider, context);
			}
		}

		static void SetThumbDrawable(SeekBar seekBar, Context context, Drawable thumbDrawable)
		{
			int thumbSize = (int)context.ToPixels(ThumbDiameterDp);

			if (thumbSize <= 0)
			{
				return;
			}

			using (Bitmap bitmap = Bitmap.CreateBitmap(thumbSize, thumbSize, Bitmap.Config.Argb8888!))
			using (Canvas canvas = new Canvas(bitmap))
			{
				thumbDrawable.SetBounds(0, 0, thumbSize, thumbSize);
				thumbDrawable.Draw(canvas);

				using (BitmapDrawable finalDrawable = new BitmapDrawable(context.Resources, bitmap))
				{
					seekBar.SetThumb(finalDrawable);
				}
			}
		}

		static void SetDefaultThumb(SeekBar seekBar, ISlider slider, Context context)
		{
			seekBar.SetThumb(context.GetDrawable(Resource.Drawable.abc_seekbar_thumb_material));

			if (slider.ThumbColor is null && context.Theme is not null)
			{
				using var value = new TypedValue();
				if (context.Theme.ResolveAttribute(global::Android.Resource.Attribute.ColorAccent, value, true))
				{
					seekBar.Thumb?.SetColorFilter(new Color(value.Data), FilterMode.SrcIn);
				}
			}
			else
			{
				seekBar.UpdateThumbColor(slider);
			}
		}

		// TODO: Make this public in NET 11.
		internal static async Task UpdateThumbImageSourceAsync(this Slider mSlider, ISlider slider, IImageSourceServiceProvider provider)
		{
			var context = mSlider.Context;

			if (context is null)
			{
				return;
			}

			var thumbImageSource = slider.ThumbImageSource;

			if (thumbImageSource is not null)
			{
				var service = provider.GetRequiredImageSourceService(thumbImageSource);
				var result = await service.GetDrawableAsync(thumbImageSource, context);

				var thumbDrawable = result?.Value;

				if (mSlider.IsAlive() && thumbDrawable is not null)
				{
					if (slider.ThumbColor is not null)
					{
						// Mutate the drawable to avoid affecting other instances
						thumbDrawable = thumbDrawable.Mutate();
						thumbDrawable.SetColorFilter(slider.ThumbColor.ToPlatform(), FilterMode.SrcIn);
					}
					mSlider.SetCustomThumbDrawable(thumbDrawable);
				}
			}
		}
	}
}
