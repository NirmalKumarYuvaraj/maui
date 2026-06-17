using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.DeviceTests.Stubs
{
	public partial class CheckBoxStub : StubBase, ICheckBox
	{
		CheckState _checkState;

		public bool IsChecked
		{
			get => _checkState == CheckState.Checked;
			set
			{
				_checkState = value ? CheckState.Checked : CheckState.Unchecked;
			}
		}

		public CheckState CheckState
		{
			get => _checkState;
			set => _checkState = value;
		}

		public bool IsThreeState { get; set; }

		public Paint Foreground { get; set; }
	}
}