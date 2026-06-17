using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.CheckBox;

namespace Microsoft.Maui.Handlers
{
	public partial class CheckBoxHandler : ViewHandler<ICheckBox, MaterialCheckBox>
	{
		// Prevents re-entrancy when we programmatically update the platform state.
		bool _isUpdatingState;

		protected override MaterialCheckBox CreatePlatformView()
		{
			var platformCheckBox = new MaterialCheckBox(MauiMaterialContextThemeWrapper.Create(Context))
			{
				SoundEffectsEnabled = false
			};

			platformCheckBox.SetClipToOutline(true);
			return platformCheckBox;
		}

		protected override void ConnectHandler(MaterialCheckBox platformView)
		{
			platformView.CheckedChange += OnCheckedChange;
		}

		protected override void DisconnectHandler(MaterialCheckBox platformView)
		{
			platformView.CheckedChange -= OnCheckedChange;
		}

		// This is an Android-specific mapping
		public static partial void MapBackground(ICheckBoxHandler handler, ICheckBox check)
		{
			handler.PlatformView?.UpdateBackground(check);
		}

		public static partial void MapIsChecked(ICheckBoxHandler handler, ICheckBox check)
		{
			handler.PlatformView?.UpdateCheckState(check);
		}

		public static partial void MapCheckState(ICheckBoxHandler handler, ICheckBox check)
		{
			if (handler is CheckBoxHandler checkBoxHandler)
				checkBoxHandler.UpdateCheckStatePlatform(check);
		}

		public static partial void MapIsThreeState(ICheckBoxHandler handler, ICheckBox check)
		{
			// Android MaterialCheckBox cycles are handled via our OnCheckedChange override;
			// no native property to set for three-state mode.
		}

		public static partial void MapForeground(ICheckBoxHandler handler, ICheckBox check)
		{
			handler.PlatformView?.UpdateForeground(check);
		}

		void UpdateCheckStatePlatform(ICheckBox check)
		{
			if (PlatformView == null)
				return;

			_isUpdatingState = true;
			try
			{
				PlatformView.UpdateCheckState(check);
			}
			finally
			{
				_isUpdatingState = false;
			}
		}

		void OnCheckedChange(object? sender, CompoundButton.CheckedChangeEventArgs e)
		{
			if (_isUpdatingState || VirtualView == null)
				return;

			if (VirtualView.IsThreeState)
			{
				// Cycle: Unchecked → Checked → Indeterminate → Unchecked
				var nextState = VirtualView.CheckState switch
				{
					CheckState.Unchecked => CheckState.Checked,
					CheckState.Checked => CheckState.Indeterminate,
					_ => CheckState.Unchecked,
				};

				_isUpdatingState = true;
				try
				{
					VirtualView.CheckState = nextState;
				}
				finally
				{
					_isUpdatingState = false;
				}
			}
			else
			{
				VirtualView.IsChecked = e.IsChecked;
			}
		}
	}
}