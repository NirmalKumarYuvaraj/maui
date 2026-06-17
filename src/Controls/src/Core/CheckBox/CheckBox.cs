#nullable disable
using System;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls
{
	/// <summary>
	/// Represents a control that a user can select, clear, or set to an indeterminate state.
	/// </summary>
	/// <remarks>
	/// A <see cref="CheckBox"/> is a type of button that can be checked, unchecked, or — when
	/// <see cref="IsThreeState"/> is <see langword="true"/> — indeterminate.
	/// Use the <see cref="IsChecked"/> property for binary use-cases or
	/// <see cref="CheckState"/> for the full three-state value.
	/// </remarks>
	[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
	public partial class CheckBox : View, IElementConfiguration<CheckBox>, IBorderElement, IColorElement, ICheckBox, ICommandElement
	{
		readonly Lazy<PlatformConfigurationRegistry<CheckBox>> _platformConfigurationRegistry;

		// Guards against re-entrancy when CheckState syncs IsChecked and vice-versa.
		bool _syncingFromCheckState;
		
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
			BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(CheckBox), false,
				propertyChanged: (bindable, oldValue, newValue) =>
				{
					if (bindable is not CheckBox checkBox)
						return;

					// Keep CheckState in sync, but only when not already being set by CheckState itself.
					if (!checkBox._syncingFromCheckState)
					{
						var boolValue = (bool)newValue;
						var targetState = boolValue ? CheckState.Checked : CheckState.Unchecked;
						if (checkBox.CheckState != targetState)
							checkBox.CheckState = targetState;
					}

					checkBox.Handler?.UpdateValue(nameof(ICheckBox.Foreground));
					checkBox.CheckedChanged?.Invoke(bindable, new CheckedChangedEventArgs((bool)newValue));
					if (checkBox.Command?.CanExecute(checkBox.CommandParameter) == true)
						checkBox.Command.Execute(checkBox.CommandParameter);

					checkBox.ChangeVisualState();
				}, defaultBindingMode: BindingMode.TwoWay);

		/// <summary>Bindable property for <see cref="CheckState"/>. This is a bindable property.</summary>
		public static readonly BindableProperty CheckStateProperty =
			BindableProperty.Create(nameof(CheckState), typeof(CheckState), typeof(CheckBox), CheckState.Unchecked,
				propertyChanged: (bindable, oldValue, newValue) =>
				{
					if (bindable is not CheckBox checkBox)
						return;

					var state = (CheckState)newValue;
					var isChecked = state == CheckState.Checked;

					// Sync IsChecked, guarding so IsCheckedProperty.propertyChanged won't
					// flip CheckState back to Unchecked when we set it to false for Indeterminate.
					if (checkBox.IsChecked != isChecked)
					{
						checkBox._syncingFromCheckState = true;
						try
						{
							checkBox.SetValue(IsCheckedProperty, isChecked);
						}
						finally
						{
							checkBox._syncingFromCheckState = false;
						}
					}

					checkBox.Handler?.UpdateValue(nameof(ICheckBox.CheckState));
					checkBox.CheckStateChanged?.Invoke(checkBox, new CheckStateChangedEventArgs(state));
					checkBox.ChangeVisualState();
				}, defaultBindingMode: BindingMode.TwoWay);

		/// <summary>Bindable property for <see cref="IsThreeState"/>. This is a bindable property.</summary>
		public static readonly BindableProperty IsThreeStateProperty =
			BindableProperty.Create(nameof(IsThreeState), typeof(bool), typeof(CheckBox), false,
				propertyChanged: (bindable, oldValue, newValue) =>
				{
					if (bindable is CheckBox checkBox)
						checkBox.Handler?.UpdateValue(nameof(ICheckBox.IsThreeState));
				});

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
		/// This is a bindable property. Setting this to <see langword="true"/> puts the checkbox
		/// in the <see cref="CheckState.Checked"/> state; setting it to <see langword="false"/> puts it
		/// in the <see cref="CheckState.Unchecked"/> state (never indeterminate).
		/// </summary>
		/// <value><see langword="true"/> if the checkbox is checked; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
		public bool IsChecked
		{
			get => (bool)GetValue(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, value);
		}

		/// <summary>
		/// Gets or sets the three-state check state of this <see cref="CheckBox"/>.
		/// This is a bindable property.
		/// </summary>
		/// <value>
		/// <see cref="CheckState.Unchecked"/>, <see cref="CheckState.Indeterminate"/>,
		/// or <see cref="CheckState.Checked"/>. The default is <see cref="CheckState.Unchecked"/>.
		/// </value>
		public CheckState CheckState
		{
			get => (CheckState)GetValue(CheckStateProperty);
			set => SetValue(CheckStateProperty, value);
		}

		/// <summary>
		/// Gets or sets whether the <see cref="CheckBox"/> supports three states
		/// (unchecked, indeterminate, checked). When <see langword="false"/> (the default),
		/// only checked and unchecked are available.
		/// This is a bindable property.
		/// </summary>
		public bool IsThreeState
		{
			get => (bool)GetValue(IsThreeStateProperty);
			set => SetValue(IsThreeStateProperty, value);
		}

		protected internal override void ChangeVisualState()
		{
			if (IsEnabled)
			{
				if (CheckState == CheckState.Indeterminate)
				{
					VisualStateManager.GoToState(this, IsIndeterminateVisualState);
					return;
				}

				if (IsChecked)
				{
					bool isCheckedStateAvailable = false;
					var visualStates = VisualStateManager.GetVisualStateGroups(this);
					foreach (var group in visualStates)
					{
						if (group.Name is not "CommonStates")
							continue;

						foreach (var state in group.States)
						{
							if (state.Name is IsCheckedVisualState)
							{
								isCheckedStateAvailable = true;
								break;
							}
						}

						break;
					}

					if (isCheckedStateAvailable)
					{
						VisualStateManager.GoToState(this, IsCheckedVisualState);
						return;
					}
				}
			}

			base.ChangeVisualState();
		}

		/// <summary>
		/// Occurs when the <see cref="IsChecked"/> property changes.
		/// </summary>
		public event EventHandler<CheckedChangedEventArgs> CheckedChanged;

		/// <summary>
		/// Occurs when the <see cref="CheckState"/> property changes.
		/// </summary>
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
			set => SetValue(IsCheckedProperty, value, SetterSpecificity.FromHandler);
		}

		CheckState ICheckBox.CheckState
		{
			get => CheckState;
			set => SetValue(CheckStateProperty, value, SetterSpecificity.FromHandler);
		}

		bool ICheckBox.IsThreeState => IsThreeState;

		ICommand ICommandElement.Command => Command;

		object ICommandElement.CommandParameter => CommandParameter;

		WeakCommandSubscription ICommandElement.CleanupTracker { get; set; }

		private protected override string GetDebuggerDisplay()
		{
			return $"{base.GetDebuggerDisplay()}, CheckState = {CheckState}";
		}

		internal override bool TrySetValue(string text)
		{
			if (bool.TryParse(text, out bool result))
			{
				IsChecked = result;
				return true;
			}

			return false;
		}
	}
}