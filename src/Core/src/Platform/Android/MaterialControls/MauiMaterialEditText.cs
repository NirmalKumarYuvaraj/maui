using System;
using Android.Content;
using Android.Views;
using Android.Widget;
using Google.Android.Material.TextField;
namespace Microsoft.Maui.Platform;

internal class MauiMaterialEditText : TextInputEditText
{
	public event EventHandler? SelectionChanged;

	public MauiMaterialEditText(Context context) : base(MauiMaterialContextThemeWrapper.Create(context))
	{
	}

	protected override void OnSelectionChanged(int selStart, int selEnd)
	{
		base.OnSelectionChanged(selStart, selEnd);

		SelectionChanged?.Invoke(this, EventArgs.Empty);
	}
}

internal class MauiMaterialTextInputLayout : TextInputLayout
{
	public MauiMaterialTextInputLayout(Context context)
	 : base(MauiMaterialContextThemeWrapper.Create(context))
	{

	}



	// protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
	// {
	// 	// Get the measure spec mode and size
	// 	var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
	// 	var widthSize = MeasureSpec.GetSize(widthMeasureSpec);

	// 	// If we have an AtMost constraint with a specific size, treat it as Exactly
	// 	// to force the TextInputLayout to expand to the available width
	// 	// This ensures the Material TextInputLayout fills the available space
	// 	// instead of wrapping its content
	// 	if (widthMode == MeasureSpecMode.AtMost && widthSize > 0)
	// 	{
	// 		widthMeasureSpec = MeasureSpec.MakeMeasureSpec(widthSize, MeasureSpecMode.Exactly);
	// 	}

	// 	base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
	// }

	// public override EditText? EditText => base.EditText as MauiMaterialEditText;
}

