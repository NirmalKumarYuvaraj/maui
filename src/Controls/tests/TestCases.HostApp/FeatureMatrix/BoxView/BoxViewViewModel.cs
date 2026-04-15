using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Controls.Sample;

public class BoxViewViewModel : INotifyPropertyChanged
{
	private Color _color = Colors.Blue;
	private CornerRadius _cornerRadius;
	private bool _isRedChecked = false;
	private bool _isBlueChecked = true;
	private bool _isGreenChecked = false;
	private string _cornerRadiusEntryText = string.Empty;

	public void Reset()
	{
		Color = Colors.Blue;
		CornerRadius = default;
		IsRedChecked = false;
		IsBlueChecked = true;
		IsGreenChecked = false;
		CornerRadiusEntryText = string.Empty;
	}

	public string CornerRadiusEntryText
	{
		get => _cornerRadiusEntryText;
		set
		{
			if (_cornerRadiusEntryText != value)
			{
				_cornerRadiusEntryText = value;
				OnPropertyChanged();
			}
		}
	}

	public bool IsRedChecked
	{
		get => _isRedChecked;
		set
		{
			if (_isRedChecked != value)
			{
				_isRedChecked = value;
				if (value)
					SelectColor(Colors.Red, ref _isBlueChecked, nameof(IsBlueChecked), ref _isGreenChecked, nameof(IsGreenChecked));
				OnPropertyChanged();
			}
		}
	}

	public bool IsBlueChecked
	{
		get => _isBlueChecked;
		set
		{
			if (_isBlueChecked != value)
			{
				_isBlueChecked = value;
				if (value)
					SelectColor(Colors.Blue, ref _isRedChecked, nameof(IsRedChecked), ref _isGreenChecked, nameof(IsGreenChecked));
				OnPropertyChanged();
			}
		}
	}

	public bool IsGreenChecked
	{
		get => _isGreenChecked;
		set
		{
			if (_isGreenChecked != value)
			{
				_isGreenChecked = value;
				if (value)
					SelectColor(Colors.Green, ref _isRedChecked, nameof(IsRedChecked), ref _isBlueChecked, nameof(IsBlueChecked));
				OnPropertyChanged();
			}
		}
	}

	private void SelectColor(Color color, ref bool other1, string other1Name, ref bool other2, string other2Name)
	{
		Color = color;
		other1 = false;
		OnPropertyChanged(other1Name);
		other2 = false;
		OnPropertyChanged(other2Name);
	}

	public CornerRadius CornerRadius
	{
		get => _cornerRadius;
		set
		{
			if (_cornerRadius != value)
			{
				_cornerRadius = value;
				OnPropertyChanged();
			}
		}
	}

	public Color Color
	{
		get => _color;
		set
		{
			if (_color != value)
			{
				_color = value;
				OnPropertyChanged();
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}