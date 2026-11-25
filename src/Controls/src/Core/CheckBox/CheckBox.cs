#nullable disable
using System;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// Represents a control that a user can select or clear.
	/// </summary>
	/// <remarks>
	/// A <see cref="CheckBox"/> is a type of button that can be checked, unchecked, or, when
	/// <see cref="IsThreeState"/> is <see langword="true"/>, indeterminate.
	/// Use <see cref="IsChecked"/> for two-state scenarios and <see cref="CheckState"/> for the complete state.
	/// <see cref="IsChecked"/> is <see langword="true"/> only when <see cref="CheckState"/> is <see cref="CheckBoxState.Checked"/>.
	/// </remarks>
	[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
	public partial class CheckBox : View, IElementConfiguration<CheckBox>, IBorderElement, IColorElement, ICheckBox2, ICommandElement
	{
		readonly Lazy<PlatformConfigurationRegistry<CheckBox>> _platformConfigurationRegistry;

		// The last state for which events were raised.
		CheckBoxState _committedState;
		bool _reportedIsChecked;
		int _commitVersion;
		bool _synchronizing;

		// Synchronizing writes that BindableObject queued because the target property was already being set.
		int _queuedIsCheckedSyncs;
		int _queuedCheckStateSyncs;

		// An indeterminate request that was normalized because IsThreeState was false at the time.
		// Restored if IsThreeState becomes true before another state is requested, so declaration order does not matter.
		bool _deferredIndeterminate;

		/// <summary>
		/// The visual state name for the checked state of the <see cref="CheckBox"/>.
		/// </summary>
		/// <value>The string "IsChecked".</value>
		public const string IsCheckedVisualState = "IsChecked";

		/// <summary>
		/// The visual state name for the indeterminate state of the <see cref="CheckBox"/>.
		/// </summary>
		/// <value>The string "IsIndeterminate".</value>
		public const string IsIndeterminateVisualState = "IsIndeterminate";

		/// <summary>Bindable property for <see cref="IsChecked"/>. This is a bindable property.</summary>
		public static readonly BindableProperty IsCheckedProperty =
			BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(CheckBox), BooleanBoxes.FalseBox, defaultBindingMode: BindingMode.TwoWay);

		/// <summary>Bindable property for <see cref="CheckState"/>. This is a bindable property.</summary>
		public static readonly BindableProperty CheckStateProperty =
			BindableProperty.Create(nameof(CheckState), typeof(CheckBoxState), typeof(CheckBox), CheckBoxState.Unchecked,
				defaultBindingMode: BindingMode.TwoWay, coerceValue: CoerceCheckState);

		/// <summary>Bindable property for <see cref="IsThreeState"/>. This is a bindable property.</summary>
		public static readonly BindableProperty IsThreeStateProperty =
			BindableProperty.Create(nameof(IsThreeState), typeof(bool), typeof(CheckBox), BooleanBoxes.FalseBox);

		/// <summary>Bindable property for the <see cref="Command"/> property.</summary>
		public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CheckBox), null, propertyChanging: CommandElement.OnCommandChanging, propertyChanged: CommandElement.OnCommandChanged);

		/// <summary>Bindable property for the <see cref="CommandParameter"/> property.</summary>
		public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(CheckBox), null, propertyChanged: CommandElement.OnCommandParameterChanged);

		/// <summary>
		/// Gets or sets the command that is executed when the CheckBox is checked or unchecked. This is a bindable property.
		/// </summary>
		public ICommand Command
		{
			get => (ICommand)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		/// <summary>
		/// Gets or sets the parameter to pass to the <see cref="Command"/> when it is executed. This is a bindable property.
		/// </summary>
		public object CommandParameter
		{
			get => GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}

		/// <summary>Bindable property for <see cref="Color"/>. This is a bindable property.</summary>
		public static readonly BindableProperty ColorProperty = ColorElement.ColorProperty;

		/// <summary>
		/// Gets or sets the color of the checkbox.
		/// This is a bindable property.
		/// </summary>
		/// <value>The <see cref="Graphics.Color"/> of the checkbox. The default is platform-specific.</value>
		public Color Color
		{
			get => (Color)GetValue(ColorProperty);
			set => SetValue(ColorProperty, value);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CheckBox"/> class.
		/// </summary>
		public CheckBox() => _platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<CheckBox>>(() => new PlatformConfigurationRegistry<CheckBox>(this));

		/// <summary>
		/// Gets or sets a value indicating whether the <see cref="CheckBox"/> is checked.
		/// This is a bindable property.
		/// </summary>
		/// <value><see langword="true"/> if the checkbox is checked; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
		/// <remarks>
		/// This is a Boolean projection of <see cref="CheckState"/>: it returns <see langword="false"/> when the checkbox is indeterminate.
		/// Setting it to <see langword="false"/> clears the indeterminate state, even if the value was already <see langword="false"/>.
		/// </remarks>
		public bool IsChecked
		{
			get => (bool)GetValue(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, BooleanBoxes.Box(value));
		}

		/// <summary>
		/// Gets or sets the complete state of the <see cref="CheckBox"/>. This is a bindable property.
		/// </summary>
		/// <value>The state of the checkbox. The default is <see cref="CheckBoxState.Unchecked"/>.</value>
		/// <remarks>
		/// <see cref="CheckBoxState.Indeterminate"/> is only retained when <see cref="IsThreeState"/> is <see langword="true"/>;
		/// otherwise it is normalized to <see cref="CheckBoxState.Unchecked"/>. If <see cref="IsThreeState"/> becomes
		/// <see langword="true"/> before any other state is requested, the indeterminate request is restored, so the
		/// two properties can be declared or bound in either order.
		/// </remarks>
		public CheckBoxState CheckState
		{
			get => (CheckBoxState)GetValue(CheckStateProperty);
			set => SetValue(CheckStateProperty, value);
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the <see cref="CheckBox"/> supports the indeterminate state. This is a bindable property.
		/// </summary>
		/// <value>
		/// <see langword="true"/> to allow <see cref="CheckBoxState.Indeterminate"/> and have user interaction cycle
		/// unchecked, checked, then indeterminate; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
		/// </value>
		/// <remarks>Setting this to <see langword="false"/> while the checkbox is indeterminate sets <see cref="CheckState"/> to <see cref="CheckBoxState.Unchecked"/>.</remarks>
		public bool IsThreeState
		{
			get => (bool)GetValue(IsThreeStateProperty);
			set => SetValue(IsThreeStateProperty, BooleanBoxes.Box(value));
		}

		static object CoerceCheckState(BindableObject bindable, object value)
		{
			var checkBox = (CheckBox)bindable;
			var state = (CheckBoxState)value;

			checkBox._deferredIndeterminate = state == CheckBoxState.Indeterminate && !checkBox.IsThreeState;

			return checkBox._deferredIndeterminate ? CheckBoxState.Unchecked : value;
		}

		private protected override void OnBindablePropertySet(BindableProperty property, object original, object value, bool changed, bool willFirePropertyChanged)
		{
			if (_synchronizing || TryConsumeQueuedSync(property))
			{
				base.OnBindablePropertySet(property, original, value, changed, willFirePropertyChanged);
				return;
			}

			if (property == IsCheckedProperty)
			{
				// Runs even when the value is unchanged so that IsChecked = false clears an indeterminate state.
				var isChecked = (bool)value;
				if (isChecked)
				{
					SynchronizeValue(CheckStateProperty, CheckBoxState.Checked, property);
				}
				else
				{
					_deferredIndeterminate = false;
					if (CheckState != CheckBoxState.Unchecked)
						SynchronizeValue(CheckStateProperty, CheckBoxState.Unchecked, property);
				}
			}
			else if (property == CheckStateProperty && changed)
			{
				var state = (CheckBoxState)value;
				if (state == CheckBoxState.Indeterminate && !IsThreeState)
				{
					// A lower-specificity indeterminate value was revealed (e.g. by ClearValue) without being coerced.
					SynchronizeValue(CheckStateProperty, CheckBoxState.Unchecked, property);
					state = CheckBoxState.Unchecked;
				}

				SynchronizeValue(IsCheckedProperty, BooleanBoxes.Box(state == CheckBoxState.Checked), property);
			}
			else if (property == IsThreeStateProperty && changed)
			{
				if (!IsThreeState && CheckState == CheckBoxState.Indeterminate)
				{
					// Normal (non-synchronizing) write so IsChecked and events follow.
					SetValue(CheckStateProperty, CheckBoxState.Unchecked, GetEffectiveSpecificity(CheckStateProperty));
				}
				else if (IsThreeState && _deferredIndeterminate && CheckState == CheckBoxState.Unchecked)
				{
					SetValue(CheckStateProperty, CheckBoxState.Indeterminate, GetEffectiveSpecificity(CheckStateProperty));
				}
			}

			base.OnBindablePropertySet(property, original, value, changed, willFirePropertyChanged);

			if (property == IsCheckedProperty || property == CheckStateProperty)
				CommitState();
		}

		// Writes the counterpart property without replacing bindings or lowering the effective specificity.
		void SynchronizeValue(BindableProperty target, object value, BindableProperty source)
		{
			if (Equals(GetValue(target), value))
				return;

			var specificity = GetSynchronizationSpecificity(target, source);
			if (IsBeingSet(target))
			{
				// BindableObject replays this write after the current one completes; mark it so it is not treated as a user write.
				if (target == IsCheckedProperty)
					_queuedIsCheckedSyncs++;
				else
					_queuedCheckStateSyncs++;
			}

			_synchronizing = true;
			try
			{
				SetValue(target, value, specificity);
			}
			finally
			{
				_synchronizing = false;
			}
		}

		bool IsBeingSet(BindableProperty property) =>
			(GetContext(property)?.Attributes & BindableContextAttributes.IsBeingSet) == BindableContextAttributes.IsBeingSet;

		bool TryConsumeQueuedSync(BindableProperty property)
		{
			if (property == IsCheckedProperty && _queuedIsCheckedSyncs > 0)
			{
				_queuedIsCheckedSyncs--;
				return true;
			}

			if (property == CheckStateProperty && _queuedCheckStateSyncs > 0)
			{
				_queuedCheckStateSyncs--;
				return true;
			}

			return false;
		}

		SetterSpecificity GetSynchronizationSpecificity(BindableProperty target, BindableProperty source)
		{
			var targetSpecificity = GetSpecificity(target);
			var sourceSpecificity = GetSpecificity(source);
			var specificity = targetSpecificity < sourceSpecificity ? sourceSpecificity : targetSpecificity;

			return specificity == SetterSpecificity.DefaultValue ? SetterSpecificity.ManualValueSetter : specificity;
		}

		SetterSpecificity GetEffectiveSpecificity(BindableProperty property)
		{
			var specificity = GetSpecificity(property);
			return specificity == SetterSpecificity.DefaultValue ? SetterSpecificity.ManualValueSetter : specificity;
		}

		SetterSpecificity GetSpecificity(BindableProperty property) =>
			GetContext(property)?.Values.GetSpecificity() ?? SetterSpecificity.DefaultValue;

		void CommitState()
		{
			var state = CheckState;
			var previous = _committedState;
			if (state == previous)
				return;

			_committedState = state;
			var version = ++_commitVersion;
			var isChecked = state == CheckBoxState.Checked;

			Handler?.UpdateValue(nameof(ICheckBox.Foreground));

			CheckStateChanged?.Invoke(this, new CheckStateChangedEventArgs(state));
			if (version != _commitVersion)
				return; // A handler changed the state again; the nested commit raised the remaining notifications.

			if (isChecked != _reportedIsChecked)
			{
				_reportedIsChecked = isChecked;
				CheckedChanged?.Invoke(this, new CheckedChangedEventArgs(isChecked));
				if (version != _commitVersion)
					return;
			}

			if (Command?.CanExecute(CommandParameter) == true)
			{
				Command.Execute(CommandParameter);
				if (version != _commitVersion)
					return;
			}

			ChangeVisualState();
		}

		protected internal override void ChangeVisualState()
		{
			var state = CheckState;
			if (IsEnabled && state != CheckBoxState.Unchecked)
			{
				var stateName = state == CheckBoxState.Checked ? IsCheckedVisualState : IsIndeterminateVisualState;
				bool isCheckedStateAvailable = false;
				var visualStates = VisualStateManager.GetVisualStateGroups(this);
				foreach (var group in visualStates)
				{
					if (group.Name is not "CommonStates")
					{
						continue;
					}

					foreach (var visualState in group.States)
					{
						if (visualState.Name == stateName)
						{
							isCheckedStateAvailable = true;
							break;
						}
					}

					break;
				}

				if (isCheckedStateAvailable)
				{
					VisualStateManager.GoToState(this, stateName);
				}
				else
				{
					VisualStateManager.GoToState(this, VisualStateManager.CommonStates.Normal);
				}
			}
			else
				base.ChangeVisualState();
		}

		/// <summary>
		/// Occurs when the <see cref="IsChecked"/> property changes.
		/// </summary>
		/// <remarks>Not raised for transitions that keep <see cref="IsChecked"/> unchanged, such as unchecked to indeterminate.</remarks>
		public event EventHandler<CheckedChangedEventArgs> CheckedChanged;

		/// <summary>
		/// Occurs when the <see cref="CheckState"/> property changes.
		/// </summary>
		/// <remarks>Raised before <see cref="CheckedChanged"/>, after both <see cref="CheckState"/> and <see cref="IsChecked"/> are updated.</remarks>
		public event EventHandler<CheckStateChangedEventArgs> CheckStateChanged;

		/// <inheritdoc/>
		public IPlatformElementConfiguration<T, CheckBox> On<T>() where T : IConfigPlatform
		{
			return _platformConfigurationRegistry.Value.On<T>();
		}

		void IBorderElement.OnBorderColorPropertyChanged(Color oldValue, Color newValue)
		{
		}

		Color IBorderElement.BorderColor => Colors.Transparent;
		int IBorderElement.CornerRadius => 0;
		double IBorderElement.BorderWidth => 0;
		int IBorderElement.CornerRadiusDefaultValue => 0;
		Color IBorderElement.BorderColorDefaultValue => Colors.Transparent;
		double IBorderElement.BorderWidthDefaultValue => 0;
		bool IBorderElement.IsCornerRadiusSet() => false;
		bool IBorderElement.IsBackgroundColorSet() => IsSet(BackgroundColorProperty);
		bool IBorderElement.IsBackgroundSet() => IsSet(BackgroundProperty);
		bool IBorderElement.IsBorderColorSet() => false;
		bool IBorderElement.IsBorderWidthSet() => false;
		void ICommandElement.CanExecuteChanged(object sender, EventArgs e) =>
			RefreshIsEnabledProperty();

		protected override bool IsEnabledCore =>
			base.IsEnabledCore && CommandElement.GetCanExecute(this, CommandProperty);
		public Paint Foreground => Color?.AsPaint();

		bool ICheckBox.IsChecked
		{
			get => IsChecked;
			set => SetValue(IsCheckedProperty, BooleanBoxes.Box(value), SetterSpecificity.FromHandler);
		}

		CheckBoxState ICheckBox2.CheckState
		{
			get => CheckState;
			set => SetValue(CheckStateProperty, value, SetterSpecificity.FromHandler);
		}

		bool ICheckBox2.IsThreeState => IsThreeState;

		ICommand ICommandElement.Command => Command;

		object ICommandElement.CommandParameter => CommandParameter;

		WeakCommandSubscription ICommandElement.CleanupTracker { get; set; }

		private protected override string GetDebuggerDisplay()
		{
			return $"{base.GetDebuggerDisplay()}, CheckState = {CheckState}";
		}

		internal override bool TrySetValue(string text)
		{
			if (string.Equals(text, nameof(CheckBoxState.Indeterminate), StringComparison.OrdinalIgnoreCase))
			{
				CheckState = CheckBoxState.Indeterminate;
				return true;
			}

			if (bool.TryParse(text, out bool result))
			{
				IsChecked = result;
				return true;
			}

			return false;
		}
	}
}