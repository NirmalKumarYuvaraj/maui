using Android.Content;
using Android.Views;
using Google.Android.Material.CheckBox;

namespace Microsoft.Maui.Platform;

public class MauiMaterialCheckBox : MaterialCheckBox
{
	public MauiMaterialCheckBox(Context context) : base(context)
	{

	}

	internal bool IsThreeState { get; set; }

	public override bool PerformClick()
	{
		if (Enabled == false)
		{
			return false;
		}

		var nextState = CheckedState switch
		{
			MaterialCheckBox.StateUnchecked => MaterialCheckBox.StateChecked,
			MaterialCheckBox.StateChecked when IsThreeState => MaterialCheckBox.StateIndeterminate,
			MaterialCheckBox.StateChecked => MaterialCheckBox.StateUnchecked,
			MaterialCheckBox.StateIndeterminate => MaterialCheckBox.StateUnchecked,
			_ => MaterialCheckBox.StateUnchecked
		};
		CheckedState = nextState;

		PlaySoundEffect(SoundEffects.Click);
 
		SendAccessibilityEvent(global::Android.Views.Accessibility.EventTypes.ViewClicked);
		return true;
	}
}