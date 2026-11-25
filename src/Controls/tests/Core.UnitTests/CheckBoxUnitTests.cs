using System.Collections.Generic;
using Xunit;
using static Microsoft.Maui.Controls.Core.UnitTests.VisualStateTestHelpers;

namespace Microsoft.Maui.Controls.Core.UnitTests
{

	public class CheckBoxUnitTests : BaseTestFixture
	{
		[Fact]
		public void TestConstructor()
		{
			var checkBox = new CheckBox();

			Assert.False(checkBox.IsChecked);
		}

		[Fact]
		public void TestOnEvent()
		{
			var checkBox = new CheckBox();

			var fired = false;
			checkBox.CheckedChanged += (sender, e) => fired = true;

			checkBox.IsChecked = true;

			Assert.True(fired);
		}

		[Fact]
		public void TestOnEventNotDoubleFired()
		{
			var checkBox = new CheckBox();

			bool fired = false;
			checkBox.IsChecked = true;

			checkBox.CheckedChanged += (sender, args) => fired = true;
			checkBox.IsChecked = true;

			Assert.False(fired);
		}

		[Fact]
		public void CheckedChangedEventArgs_ShouldHaveCorrectValue()
		{
			var checkBox = new CheckBox();
			CheckedChangedEventArgs eventArgs = null;

			checkBox.CheckedChanged += (sender, e) => eventArgs = e;

			// Test changing from false to true
			checkBox.IsChecked = true;
			Assert.NotNull(eventArgs);
			Assert.True(eventArgs.Value);

			// Test changing from true to false
			checkBox.IsChecked = false;
			Assert.False(eventArgs.Value);
		}

		[Fact]
		public void CheckedChangedEvent_ShouldFireOnlyWhenValueChanges()
		{
			var checkBox = new CheckBox();
			int eventFireCount = 0;

			checkBox.CheckedChanged += (sender, e) => eventFireCount++;

			// Set to same value should not fire event
			checkBox.IsChecked = false;
			Assert.Equal(0, eventFireCount);

			// Change value should fire event
			checkBox.IsChecked = true;
			Assert.Equal(1, eventFireCount);

			// Set to same value again should not fire event
			checkBox.IsChecked = true;
			Assert.Equal(1, eventFireCount);

			// Change back should fire event
			checkBox.IsChecked = false;
			Assert.Equal(2, eventFireCount);
		}

		[Fact]
		public void CheckedVisualStates()
		{
			var vsgList = CreateTestStateGroups();
			string checkedStateName = CheckBox.IsCheckedVisualState;
			var checkedState = new VisualState() { Name = checkedStateName };
			var stateGroup = vsgList[0];
			stateGroup.States.Add(checkedState);

			var element = new CheckBox();
			VisualStateManager.SetVisualStateGroups(element, vsgList);

			element.IsChecked = true;
			Assert.Equal(checkedStateName, stateGroup.CurrentState.Name);

			element.IsChecked = false;
			Assert.NotEqual(checkedStateName, stateGroup.CurrentState.Name);
		}

		[Fact]
		public void CheckBoxClickWhenCommandCanExecuteFalse()
		{
			bool invoked = false;
			var checkBox = new CheckBox()
			{
				Command = new Command(() => invoked = true, () => false),
				IsChecked = false
			};

			checkBox.IsChecked = true;

			Assert.False(invoked);
		}

		[Fact]
		public void CheckBoxClickWhenCommandCanExecuteTrue()
		{
			bool invoked = false;
			var checkBox = new CheckBox()
			{
				Command = new Command(() => invoked = true, () => true),
				IsChecked = false
			};

			checkBox.IsChecked = true;

			Assert.True(invoked);
		}

		[Fact]
		public void Command_ShouldExecuteWithCorrectParameter()
		{
			object receivedParameter = null;
			var expectedParameter = "TestParameter";

			var checkBox = new CheckBox()
			{
				Command = new Command<object>(param => receivedParameter = param, param => true),
				CommandParameter = expectedParameter,
				IsChecked = false
			};

			checkBox.IsChecked = true;

			Assert.Equal(expectedParameter, receivedParameter);
		}

		[Fact]
		public void Command_ShouldNotExecuteWhenNull()
		{
			var checkBox = new CheckBox()
			{
				Command = null,
				IsChecked = false
			};

			// Should not throw exception when command is null
			checkBox.IsChecked = true;
			Assert.True(checkBox.IsChecked);
		}

		[Fact]
		public void CommandParameter_ShouldSupportNullValue()
		{
			object receivedParameter = "NotNull";

			var checkBox = new CheckBox()
			{
				Command = new Command<object>(param => receivedParameter = param, param => true),
				CommandParameter = null,
				IsChecked = false
			};

			checkBox.IsChecked = true;

			Assert.Null(receivedParameter);
		}

		[Fact]
		public void Command_ShouldExecuteOnlyOnCheckedStateChange()
		{
			int executeCount = 0;
			var checkBox = new CheckBox()
			{
				Command = new Command(() => executeCount++, () => true),
				IsChecked = false
			};

			// Change to true should execute command
			checkBox.IsChecked = true;
			Assert.Equal(1, executeCount);

			// Setting to same value should not execute command
			checkBox.IsChecked = true;
			Assert.Equal(1, executeCount);

			// Change to false should execute command
			checkBox.IsChecked = false;
			Assert.Equal(2, executeCount);
		}

		[Fact]
		public void Command_And_CheckedChanged_ShouldBothFire()
		{
			bool commandExecuted = false;
			bool eventFired = false;

			var checkBox = new CheckBox()
			{
				Command = new Command(() => commandExecuted = true, () => true),
				IsChecked = false
			};

			checkBox.CheckedChanged += (sender, e) => eventFired = true;

			checkBox.IsChecked = true;

			Assert.True(commandExecuted);
			Assert.True(eventFired);
		}

		[Fact]
		public void ThreeStateApiIsAdditive()
		{
			var checkBox = new CheckBox();

			Assert.Equal(typeof(bool), CheckBox.IsCheckedProperty.ReturnType);
			Assert.Equal(BindingMode.TwoWay, CheckBox.IsCheckedProperty.DefaultBindingMode);
			Assert.Equal(BindingMode.TwoWay, CheckBox.CheckStateProperty.DefaultBindingMode);
			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
			Assert.False(checkBox.IsThreeState);
		}

		[Theory]
		[InlineData(CheckBoxState.Unchecked, false)]
		[InlineData(CheckBoxState.Checked, true)]
		[InlineData(CheckBoxState.Indeterminate, false)]
		public void IsCheckedProjectsCheckState(CheckBoxState state, bool expected)
		{
			var checkBox = new CheckBox { IsThreeState = true, CheckState = state };

			Assert.Equal(state, checkBox.CheckState);
			Assert.Equal(expected, checkBox.IsChecked);
			Assert.Equal(expected, ((ICheckBox)checkBox).IsChecked);
		}

		[Theory]
		[InlineData(true, CheckBoxState.Checked)]
		[InlineData(false, CheckBoxState.Unchecked)]
		public void SettingIsCheckedUpdatesCheckState(bool isChecked, CheckBoxState expected)
		{
			var checkBox = new CheckBox { IsThreeState = true, CheckState = CheckBoxState.Indeterminate };

			checkBox.IsChecked = isChecked;

			Assert.Equal(expected, checkBox.CheckState);
		}

		[Fact]
		public void SettingIsCheckedFalseWhileIndeterminateRaisesOnlyCheckStateChanged()
		{
			var checkBox = new CheckBox { IsThreeState = true, CheckState = CheckBoxState.Indeterminate };
			var stateChanges = new List<CheckBoxState>();
			var checkedChanges = 0;
			var commands = 0;
			checkBox.CheckStateChanged += (_, e) => stateChanges.Add(e.Value);
			checkBox.CheckedChanged += (_, _) => checkedChanges++;
			checkBox.Command = new Command(() => commands++);

			checkBox.IsChecked = false;

			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
			Assert.Equal(new[] { CheckBoxState.Unchecked }, stateChanges);
			Assert.Equal(0, checkedChanges);
			Assert.Equal(1, commands);
		}

		[Fact]
		public void EventsAreRaisedInOrderWithConsistentValues()
		{
			var checkBox = new CheckBox { IsThreeState = true };
			var log = new List<string>();
			void Record(string name) => log.Add($"{name}:{checkBox.CheckState}:{checkBox.IsChecked}");

			checkBox.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName is nameof(CheckBox.IsChecked) or nameof(CheckBox.CheckState))
					Record(e.PropertyName);
			};
			checkBox.CheckStateChanged += (_, _) => Record(nameof(CheckBox.CheckStateChanged));
			checkBox.CheckedChanged += (_, _) => Record(nameof(CheckBox.CheckedChanged));
			checkBox.Command = new Command(() => Record("Command"));

			checkBox.CheckState = CheckBoxState.Checked;

			Assert.Equal(new[]
			{
				"IsChecked:Checked:True",
				"CheckState:Checked:True",
				"CheckStateChanged:Checked:True",
				"CheckedChanged:Checked:True",
				"Command:Checked:True",
			}, log);
		}

		[Fact]
		public void CycleRaisesEventsOncePerTransition()
		{
			var checkBox = new CheckBox { IsThreeState = true };
			var stateChanges = new List<CheckBoxState>();
			var checkedChanges = new List<bool>();
			var commands = 0;
			checkBox.CheckStateChanged += (_, e) => stateChanges.Add(e.Value);
			checkBox.CheckedChanged += (_, e) => checkedChanges.Add(e.Value);
			checkBox.Command = new Command(() => commands++);

			checkBox.CheckState = CheckBoxState.Checked;
			checkBox.CheckState = CheckBoxState.Indeterminate;
			checkBox.CheckState = CheckBoxState.Indeterminate;
			checkBox.CheckState = CheckBoxState.Unchecked;
			checkBox.IsChecked = false;

			Assert.Equal(new[] { CheckBoxState.Checked, CheckBoxState.Indeterminate, CheckBoxState.Unchecked }, stateChanges);
			Assert.Equal(new[] { true, false }, checkedChanges);
			Assert.Equal(3, commands);
		}

		[Fact]
		public void IndeterminateIsNormalizedWithoutThreeState()
		{
			var checkBox = new CheckBox { IsChecked = true };
			var stateChanges = new List<CheckBoxState>();
			checkBox.CheckStateChanged += (_, e) => stateChanges.Add(e.Value);

			checkBox.CheckState = CheckBoxState.Indeterminate;

			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
			Assert.False(checkBox.IsChecked);
			Assert.Equal(new[] { CheckBoxState.Unchecked }, stateChanges);
		}

		[Fact]
		public void DisablingThreeStateNormalizesIndeterminate()
		{
			var checkBox = new CheckBox { IsThreeState = true, CheckState = CheckBoxState.Indeterminate };

			checkBox.IsThreeState = false;
			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);

			checkBox.IsThreeState = true;
			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void InitializationOrderDoesNotLoseIndeterminate(bool threeStateFirst)
		{
			var checkBox = new CheckBox();
			if (threeStateFirst)
			{
				checkBox.IsThreeState = true;
				checkBox.CheckState = CheckBoxState.Indeterminate;
			}
			else
			{
				checkBox.CheckState = CheckBoxState.Indeterminate;
				checkBox.IsThreeState = true;
			}

			Assert.Equal(CheckBoxState.Indeterminate, checkBox.CheckState);
			Assert.False(checkBox.IsChecked);
		}

		[Fact]
		public void DeferredIndeterminateIsDiscardedByLaterState()
		{
			var checkBox = new CheckBox { CheckState = CheckBoxState.Indeterminate };

			checkBox.IsChecked = false;
			checkBox.IsThreeState = true;

			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void BindingInitializationOrderDoesNotLoseIndeterminate(bool threeStateFirst)
		{
			var vm = new CheckBoxViewModel { State = CheckBoxState.Indeterminate, ThreeState = true };
			var checkBox = new CheckBox();
			if (threeStateFirst)
			{
				checkBox.SetBinding(CheckBox.IsThreeStateProperty, nameof(CheckBoxViewModel.ThreeState));
				checkBox.SetBinding(CheckBox.CheckStateProperty, nameof(CheckBoxViewModel.State));
			}
			else
			{
				checkBox.SetBinding(CheckBox.CheckStateProperty, nameof(CheckBoxViewModel.State));
				checkBox.SetBinding(CheckBox.IsThreeStateProperty, nameof(CheckBoxViewModel.ThreeState));
			}

			checkBox.BindingContext = vm;

			Assert.Equal(CheckBoxState.Indeterminate, checkBox.CheckState);
			Assert.Equal(CheckBoxState.Indeterminate, vm.State);
		}

		[Fact]
		public void TwoWayBindingsStaySynchronized()
		{
			var vm = new CheckBoxViewModel { ThreeState = true };
			var checkBox = new CheckBox { BindingContext = vm };
			checkBox.SetBinding(CheckBox.IsThreeStateProperty, nameof(CheckBoxViewModel.ThreeState));
			checkBox.SetBinding(CheckBox.CheckStateProperty, nameof(CheckBoxViewModel.State));
			checkBox.SetBinding(CheckBox.IsCheckedProperty, nameof(CheckBoxViewModel.IsChecked));

			vm.State = CheckBoxState.Checked;
			Assert.True(checkBox.IsChecked);
			Assert.True(vm.IsChecked);

			vm.State = CheckBoxState.Indeterminate;
			Assert.False(checkBox.IsChecked);
			Assert.False(vm.IsChecked);

			vm.IsChecked = true;
			Assert.Equal(CheckBoxState.Checked, checkBox.CheckState);
			Assert.Equal(CheckBoxState.Checked, vm.State);

			// Simulates user interaction through the handler.
			((ICheckBox2)checkBox).CheckState = CheckBoxState.Indeterminate;
			Assert.Equal(CheckBoxState.Indeterminate, vm.State);
			Assert.False(vm.IsChecked);

			((ICheckBox)checkBox).IsChecked = false;
			Assert.Equal(CheckBoxState.Unchecked, vm.State);

			Assert.True(checkBox.IsSet(CheckBox.CheckStateProperty));
			vm.State = CheckBoxState.Checked;
			Assert.True(vm.IsChecked);
		}

		[Fact]
		public void DisablingThreeStatePushesUncheckedToBinding()
		{
			var vm = new CheckBoxViewModel { ThreeState = true, State = CheckBoxState.Indeterminate };
			var checkBox = new CheckBox { BindingContext = vm };
			checkBox.SetBinding(CheckBox.IsThreeStateProperty, nameof(CheckBoxViewModel.ThreeState));
			checkBox.SetBinding(CheckBox.CheckStateProperty, nameof(CheckBoxViewModel.State));

			vm.ThreeState = false;

			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
			Assert.Equal(CheckBoxState.Unchecked, vm.State);
		}

		[Fact]
		public void ClearingValuesKeepsPropertiesSynchronized()
		{
			var checkBox = new CheckBox { IsThreeState = true, CheckState = CheckBoxState.Checked };

			checkBox.ClearValue(CheckBox.CheckStateProperty);
			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
			Assert.False(checkBox.IsChecked);

			checkBox.IsChecked = true;
			checkBox.ClearValue(CheckBox.IsCheckedProperty);
			Assert.False(checkBox.IsChecked);
			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
		}

		[Fact]
		public void StyleSettersSynchronizeWithoutOverridingLocalValues()
		{
			var style = new Style(typeof(CheckBox));
			style.Setters.Add(new Setter { Property = CheckBox.IsThreeStateProperty, Value = true });
			style.Setters.Add(new Setter { Property = CheckBox.CheckStateProperty, Value = CheckBoxState.Indeterminate });

			var checkBox = new CheckBox { Style = style };
			Assert.Equal(CheckBoxState.Indeterminate, checkBox.CheckState);
			Assert.False(checkBox.IsChecked);

			checkBox.IsChecked = true;
			Assert.Equal(CheckBoxState.Checked, checkBox.CheckState);
		}

		[Fact]
		public void ReentrantStateChangeRaisesLatestStateLast()
		{
			var checkBox = new CheckBox { IsThreeState = true };
			var stateChanges = new List<CheckBoxState>();
			var checkedChanges = new List<bool>();
			checkBox.CheckStateChanged += (_, e) =>
			{
				stateChanges.Add(e.Value);
				if (e.Value == CheckBoxState.Checked)
					checkBox.CheckState = CheckBoxState.Indeterminate;
			};
			checkBox.CheckedChanged += (_, e) => checkedChanges.Add(e.Value);

			checkBox.IsChecked = true;

			Assert.Equal(CheckBoxState.Indeterminate, checkBox.CheckState);
			Assert.False(checkBox.IsChecked);
			Assert.Equal(new[] { CheckBoxState.Checked, CheckBoxState.Indeterminate }, stateChanges);
			Assert.Empty(checkedChanges);
		}

		[Fact]
		public void IndeterminateVisualState()
		{
			var checkBox = new CheckBox { IsThreeState = true };
			var groups = CreateTestStateGroups();
			groups[0].States.Add(new VisualState { Name = CheckBox.IsCheckedVisualState });
			groups[0].States.Add(new VisualState { Name = CheckBox.IsIndeterminateVisualState });
			VisualStateManager.SetVisualStateGroups(checkBox, groups);

			checkBox.CheckState = CheckBoxState.Indeterminate;
			Assert.Equal(CheckBox.IsIndeterminateVisualState, groups[0].CurrentState.Name);

			checkBox.IsEnabled = false;
			Assert.Equal(VisualStateManager.CommonStates.Disabled, groups[0].CurrentState.Name);

			checkBox.IsEnabled = true;
			checkBox.CheckState = CheckBoxState.Checked;
			Assert.Equal(CheckBox.IsCheckedVisualState, groups[0].CurrentState.Name);

			checkBox.CheckState = CheckBoxState.Unchecked;
			Assert.Equal(VisualStateManager.CommonStates.Normal, groups[0].CurrentState.Name);
		}

		[Fact]
		public void MissingIndeterminateVisualStateFallsBackToNormal()
		{
			var checkBox = new CheckBox { IsThreeState = true };
			var groups = CreateTestStateGroups();
			VisualStateManager.SetVisualStateGroups(checkBox, groups);

			checkBox.CheckState = CheckBoxState.Indeterminate;

			Assert.Equal(VisualStateManager.CommonStates.Normal, groups[0].CurrentState.Name);
		}

		[Theory]
		[InlineData("True", CheckBoxState.Checked)]
		[InlineData("False", CheckBoxState.Unchecked)]
		[InlineData("Indeterminate", CheckBoxState.Indeterminate)]
		public void TrySetValueParsesState(string text, CheckBoxState expected)
		{
			var checkBox = new CheckBox { IsThreeState = true };

			Assert.True(checkBox.TrySetValue(text));
			Assert.Equal(expected, checkBox.CheckState);
		}

		[Fact]
		public void TrySetValueIndeterminateNormalizesOnTwoStateCheckBox()
		{
			var checkBox = new CheckBox { IsChecked = true };

			Assert.True(checkBox.TrySetValue("Indeterminate"));
			Assert.Equal(CheckBoxState.Unchecked, checkBox.CheckState);
		}

		class CheckBoxViewModel : System.ComponentModel.INotifyPropertyChanged
		{
			CheckBoxState _state;
			bool _isChecked;
			bool _threeState;

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public CheckBoxState State
			{
				get => _state;
				set => Set(ref _state, value);
			}

			public bool IsChecked
			{
				get => _isChecked;
				set => Set(ref _isChecked, value);
			}

			public bool ThreeState
			{
				get => _threeState;
				set => Set(ref _threeState, value);
			}

			void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
			{
				if (Equals(field, value))
					return;

				field = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
			}
		}
	}
}
