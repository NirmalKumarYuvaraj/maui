using System;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.CheckBox;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	public partial class CheckBoxHandlerTests
	{
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task NullStateHonorsIsThreeStateWithoutChangingTapBehavior(bool isThreeState)
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var checkBox = new CheckBoxStub { IsThreeState = isThreeState, IsChecked = null };
				var handler = CreateHandler(checkBox);
				var native = Assert.IsAssignableFrom<MaterialCheckBox>(handler.PlatformView);

				Assert.Equal(isThreeState ? MaterialCheckBox.StateIndeterminate : MaterialCheckBox.StateUnchecked, native.CheckedState);

				native.PerformClick();
				Assert.True(checkBox.IsChecked);

				native.PerformClick();
				Assert.False(checkBox.IsChecked);
			});
		}

		[Fact]
		public async Task DisablingThreeStateClearsNativeIndeterminateState()
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var checkBox = new CheckBoxStub { IsThreeState = true, IsChecked = null };
				var handler = CreateHandler(checkBox);
				var native = Assert.IsAssignableFrom<MaterialCheckBox>(handler.PlatformView);
				Assert.Equal(MaterialCheckBox.StateIndeterminate, native.CheckedState);

				checkBox.IsThreeState = false;
				handler.UpdateValue(nameof(ICheckBox.IsThreeState));

				Assert.Equal(MaterialCheckBox.StateUnchecked, native.CheckedState);
			});
		}

		[Theory(DisplayName = "Color (Foreground) Updates Correctly")]
		[InlineData(0xFFFF0000)]
		[InlineData(0xFF0000FF)]
		public async Task ColorUpdatesCorrectly(uint color)
		{
			var layout = new LayoutStub();

			var checkBox = new CheckBoxStub
			{
				Foreground = new SolidPaint(Colors.Black),
				IsChecked = true
			};

			var button = new ButtonStub
			{
				Text = "Change Foreground"
			};

			layout.Add(checkBox);
			layout.Add(button);

			var clicked = false;

			var expected = Color.FromUint(color);

			button.Clicked += delegate
			{
				checkBox.Foreground = new SolidPaint(expected);
				clicked = true;
			};

			await PerformClick(button);

			Assert.True(clicked);

			await ValidateHasColor(checkBox, expected);
		}

		AppCompatCheckBox GetNativeCheckBox(CheckBoxHandler checkBoxHandler) =>
			(AppCompatCheckBox)checkBoxHandler.PlatformView;

		bool GetNativeIsChecked(CheckBoxHandler checkBoxHandler) =>
			GetNativeCheckBox(checkBoxHandler).Checked;

		Task ValidateColor(ICheckBox checkBoxStub, Color color, Action action = null) =>
			ValidateHasColor(checkBoxStub, color, action);

		AppCompatButton GetNativeButton(ButtonHandler buttonHandler) =>
			buttonHandler.PlatformView;

		Task PerformClick(IButton button)
		{
			return InvokeOnMainThreadAsync(() =>
			{
				GetNativeButton(CreateHandler<ButtonHandler>(button)).PerformClick();
			});
		}
	}
}