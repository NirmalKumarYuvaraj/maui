using Android.Graphics.Drawables;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Platform
{
	public static class RadioButtonExtensions
	{
		public static void UpdateBackground(this AppCompatRadioButton platformRadioButton, IRadioButton radioButton)
		{
			if (radioButton.Background.IsNullOrEmpty())
			{
				if (platformRadioButton.Background is BorderDrawable existingDrawable)
				{
					platformRadioButton.Background = null;
					existingDrawable.Dispose();
				}
				return;
			}

			platformRadioButton.UpdateBorderDrawable(radioButton);
		}

		internal static void UpdateBackground(
			this AppCompatRadioButton platformRadioButton,
			IRadioButton radioButton,
			Drawable? defaultBackground,
			ref BorderDrawable? backgroundDrawable)
		{
			bool hasCustomBorder =
				!radioButton.Background.IsNullOrEmpty() ||
				radioButton.StrokeColor is not null ||
				radioButton.StrokeThickness > 0 ||
				radioButton.CornerRadius > 0;

			if (!hasCustomBorder)
			{
				platformRadioButton.ResetBackground(defaultBackground, ref backgroundDrawable);
				return;
			}

			backgroundDrawable?.Dispose();
			backgroundDrawable = new BorderDrawable(platformRadioButton.Context);
			platformRadioButton.Background = backgroundDrawable;
			backgroundDrawable.UpdateBorderDrawable(radioButton);
		}

		internal static void ResetBackground(
			this AppCompatRadioButton platformRadioButton,
			Drawable? defaultBackground,
			ref BorderDrawable? backgroundDrawable)
		{
			if (backgroundDrawable is null)
				return;

			if (ReferenceEquals(platformRadioButton.Background, backgroundDrawable))
				platformRadioButton.Background = defaultBackground;

			backgroundDrawable.Dispose();
			backgroundDrawable = null;
		}

		public static void UpdateIsChecked(this AppCompatRadioButton platformRadioButton, IRadioButton radioButton)
		{
			platformRadioButton.Checked = radioButton.IsChecked;
		}

		public static void UpdateContent(this AppCompatRadioButton platformRadioButton, IRadioButton radioButton)
		{
			platformRadioButton.Text = $"{radioButton.Content}";
		}

		public static void UpdateStrokeColor(this AppCompatRadioButton platformRadioButton, IRadioButton radioButton)
		{
			platformRadioButton.UpdateBorderDrawable(radioButton);
		}

		public static void UpdateStrokeThickness(this AppCompatRadioButton platformRadioButton, IRadioButton radioButton)
		{
			platformRadioButton.UpdateBorderDrawable(radioButton);
		}

		public static void UpdateCornerRadius(this AppCompatRadioButton platformRadioButton, IRadioButton radioButton)
		{
			platformRadioButton.UpdateBorderDrawable(radioButton);
		}

		internal static void UpdateBorderDrawable(this AppCompatRadioButton platformView, IRadioButton radioButton)
		{
			BorderDrawable? mauiDrawable = platformView.Background as BorderDrawable;

			if (mauiDrawable == null)
			{
				mauiDrawable = new BorderDrawable(platformView.Context);

				platformView.Background = mauiDrawable;
			}

			mauiDrawable.SetBackground(radioButton.Background);

			if (radioButton.StrokeColor is null)
				mauiDrawable.SetBorderColor(null);
			else
				mauiDrawable.SetBorderBrush(new SolidPaint { Color = radioButton.StrokeColor });
			mauiDrawable.SetBorderWidth(radioButton.StrokeThickness);
			mauiDrawable.SetCornerRadius(radioButton.CornerRadius);
		}

		static void UpdateBorderDrawable(this BorderDrawable borderDrawable, IRadioButton radioButton)
		{
			borderDrawable.SetBackground(radioButton.Background);

			if (radioButton.StrokeColor is null)
				borderDrawable.SetBorderColor(null);
			else
				borderDrawable.SetBorderBrush(new SolidPaint { Color = radioButton.StrokeColor });

			borderDrawable.SetBorderWidth(radioButton.StrokeThickness);
			borderDrawable.SetCornerRadius(radioButton.CornerRadius);
		}
	}
}