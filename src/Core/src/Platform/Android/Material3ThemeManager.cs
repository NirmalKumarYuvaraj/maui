using System;
using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Google.Android.Material.CheckBox;
using Google.Android.Material.MaterialSwitch;
using Google.Android.Material.RadioButton;
using Google.Android.Material.Slider;
using Google.Android.Material.TextField;

namespace Microsoft.Maui.Platform;

static class Material3ThemeManager
{
	public static event EventHandler? ThemeChanged;

	public static void NotifyThemeChanged()
	{
		if (Material3Configuration.Enabled)
			ThemeChanged?.Invoke(null, EventArgs.Empty);
	}
}

static class Material3ThemeDefaults
{
	public static ColorStateList? GetPrimaryTextColors(Context? context)
	{
		if (context?.Theme is not Resources.Theme theme)
			return null;

		using var attributes = theme.ObtainStyledAttributes([global::Android.Resource.Attribute.TextColorPrimary]);
		return attributes.GetColorStateList(0);
	}

	public static ColorStateList? GetHintTextColors(Context? context)
	{
		if (context?.Theme is not Resources.Theme theme)
			return null;

		using var attributes = theme.ObtainStyledAttributes([global::Android.Resource.Attribute.TextColorHint]);
		return attributes.GetColorStateList(0);
	}

	public static ColorStateList? GetButtonTextColors(Context? context)
	{
		if (context is null)
			return null;

		using var button = new MauiMaterialButton(context);
		return button.TextColors;
	}

	public static (Drawable? Background, ColorStateList? BackgroundTint) GetButtonBackground(Context? context)
	{
		if (context is null)
			return (null, null);

		using var button = new MauiMaterialButton(context);
		return (CloneDrawable(button.Background), button.BackgroundTintList);
	}

	public static ColorStateList? GetCheckBoxTint(Context? context)
	{
		if (context is null)
			return null;

		using var checkBox = new MaterialCheckBox(MauiMaterialContextThemeWrapper.Create(context));
		return checkBox.ButtonTintList;
	}

	public static Drawable? GetRadioButtonBackground(Context? context)
	{
		if (context is null)
			return null;

		using var radioButton = new MaterialRadioButton(MauiMaterialContextThemeWrapper.Create(context));
		return CloneDrawable(radioButton.Background);
	}

	public static ColorStateList? GetSwitchTrackTint(Context? context)
	{
		if (context is null)
			return null;

		using var materialSwitch = new MaterialSwitch(MauiMaterialContextThemeWrapper.Create(context));
		return materialSwitch.TrackTintList;
	}

	public static ColorStateList? GetSwitchThumbTint(Context? context)
	{
		if (context is null)
			return null;

		using var materialSwitch = new MaterialSwitch(MauiMaterialContextThemeWrapper.Create(context));
		return materialSwitch.ThumbTintList;
	}

	public static ColorStateList? GetSliderActiveTrackTint(Context? context)
	{
		if (context is null)
			return null;

		using var slider = new Slider(MauiMaterialContextThemeWrapper.Create(context));
		return slider.TrackActiveTintList;
	}

	public static ColorStateList? GetSliderInactiveTrackTint(Context? context)
	{
		if (context is null)
			return null;

		using var slider = new Slider(MauiMaterialContextThemeWrapper.Create(context));
		return slider.TrackInactiveTintList;
	}

	public static ColorStateList? GetSliderThumbTint(Context? context)
	{
		if (context is null)
			return null;

		using var slider = new Slider(MauiMaterialContextThemeWrapper.Create(context));
		return slider.ThumbTintList;
	}

	public static ColorStateList? GetEntryHintTextColors(Context? context)
	{
		if (context is null)
			return null;

		using var layout = new MauiMaterialTextInputLayout(context)
		{
			BoxBackgroundMode = TextInputLayout.BoxBackgroundOutline
		};

		return layout.DefaultHintTextColor;
	}

	public static ColorStateList? GetSearchBarHintTextColors(Context? context)
	{
		if (context is null)
			return null;

		using var layout = new MauiMaterialSearchBarTextInputLayout(context);
		using var editText = new MauiMaterialSearchBarTextInputEditText(layout.Context!);
		layout.AddView(editText);
		var hintTextColors = editText.HintTextColors;
		layout.RemoveView(editText);
		return hintTextColors;
	}

	static Drawable? CloneDrawable(Drawable? drawable)
	{
		using var constantState = drawable?.GetConstantState();
		return constantState?.NewDrawable()?.Mutate();
	}
}
