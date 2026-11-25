using Android.Content;
using Google.Android.Material.CheckBox;

namespace Microsoft.Maui.Platform;

/// <summary>
/// A <see cref="MaterialCheckBox"/> whose user interaction is binary by default
/// and cycles through checked, indeterminate, and unchecked when <see cref="IsThreeState"/> is enabled.
/// </summary>
internal class MauiMaterialCheckBox : MaterialCheckBox
{
	public MauiMaterialCheckBox(Context context) : base(context)
	{
	}

	internal bool IsThreeState { get; set; }

	public override void Toggle()
	{
		CheckedState = CheckedState switch
		{
			StateUnchecked => StateChecked,
			StateChecked when IsThreeState => StateIndeterminate,
			_ => StateUnchecked
		};
	}
}
