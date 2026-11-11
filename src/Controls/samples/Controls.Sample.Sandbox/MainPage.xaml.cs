using System.ComponentModel;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	bool _isSyncingToppings;

	public MainPage()
	{
		InitializeComponent();
		UpdateSelectAllStateLabel();
		UpdateThreeStateLabel();
		Console.WriteLine("SANDBOX: Three-state checkbox sample initialized");
	}

	IEnumerable<CheckBox> Toppings =>
		ToppingsLayout.Children.OfType<HorizontalStackLayout>().SelectMany(row => row.Children.OfType<CheckBox>());

	void OnSelectAllCheckedChanged(object? sender, CheckBoxCheckedChangedEventArgs e)
	{
		if (_isSyncingToppings || e.Value is null)
			return;

		_isSyncingToppings = true;
		try
		{
			foreach (var topping in Toppings)
				topping.IsChecked = e.Value;
		}
		finally
		{
			_isSyncingToppings = false;
		}
		UpdateSelectAllStateLabel();
	}

	void OnToppingCheckedChanged(object? sender, CheckBoxCheckedChangedEventArgs e)
	{
		if (_isSyncingToppings)
			return;

		var checkedCount = Toppings.Count(t => t.IsChecked == true);
		var total = Toppings.Count();

		_isSyncingToppings = true;
		try
		{
			SelectAllCheckBox.IsChecked = checkedCount == 0 ? false : checkedCount == total ? true : null;
		}
		finally
		{
			_isSyncingToppings = false;
		}
		UpdateSelectAllStateLabel();
	}

	void UpdateSelectAllStateLabel()
	{
		if (SelectAllStateLabel is null)
			return;

		SelectAllStateLabel.Text = $"Select all: {SelectAllCheckBox.IsChecked?.ToString() ?? "null"}";
		Console.WriteLine($"SANDBOX: {SelectAllStateLabel.Text}");
	}

	void OnThreeStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(CheckBox.IsChecked) or nameof(CheckBox.IsEnabled) or nameof(CheckBox.IsThreeState))
		{
			UpdateThreeStateLabel();
		}
	}

	void UpdateThreeStateLabel()
	{
		if (ThreeStateCheckBox is null || ThreeStateLabel is null)
			return;

		var cb = ThreeStateCheckBox;
		var state = cb.IsChecked switch { true => "Checked", false => "Unchecked", null => "Indeterminate" };
		ThreeStateLabel.Text =
			$"{state}  (IsChecked={cb.IsChecked?.ToString() ?? "null"}, IsThreeState={cb.IsThreeState}, IsEnabled={cb.IsEnabled})";
		Console.WriteLine($"SANDBOX: {ThreeStateLabel.Text}");
	}

	void SetCheckedState(bool? value)
	{
		RequestedStateLabel.Text = $"Requested IsChecked={value?.ToString() ?? "null"}";
		ThreeStateCheckBox.IsChecked = value;
		Console.WriteLine($"SANDBOX: {RequestedStateLabel.Text}; actual={ThreeStateCheckBox.IsChecked?.ToString() ?? "null"}; IsThreeState={ThreeStateCheckBox.IsThreeState}");
	}

	void OnSetUnchecked(object? sender, EventArgs e) => SetCheckedState(false);

	void OnSetChecked(object? sender, EventArgs e) => SetCheckedState(true);

	void OnSetIndeterminate(object? sender, EventArgs e) => SetCheckedState(null);

	void OnToggleEnabled(object? sender, EventArgs e) =>
		ThreeStateCheckBox.IsEnabled = !ThreeStateCheckBox.IsEnabled;

	void OnToggleThreeState(object? sender, EventArgs e) =>
		ThreeStateCheckBox.IsThreeState = !ThreeStateCheckBox.IsThreeState;
}
