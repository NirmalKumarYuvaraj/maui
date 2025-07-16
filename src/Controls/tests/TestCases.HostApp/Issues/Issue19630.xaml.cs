using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 19630, "TapGestureRecognizer in SwipeItemView not working", PlatformAffected.Android | PlatformAffected.iOS)]
public partial class Issue19630 : ContentPage
{
	public Issue19630()
	{
		InitializeComponent();
		BindingContext = new Issue19630ViewModel(StatusLabel);
	}

	private void OnEditLabelTapped(object sender, EventArgs e)
	{
		// This helps us verify that touch events are reaching the gesture recognizer
		StatusLabel.Text = "Status: Edit label tapped (touch handling works!)";
		System.Diagnostics.Debug.WriteLine("Edit label tapped - touch handling is working!");
	}

	private void OnDeleteLabelTapped(object sender, EventArgs e)
	{
		// This helps us verify that touch events are reaching the gesture recognizer
		StatusLabel.Text = "Status: Delete label tapped (touch handling works!)";
		System.Diagnostics.Debug.WriteLine("Delete label tapped - touch handling is working!");
	}
}

public class Issue19630ViewModel : INotifyPropertyChanged
{
	private readonly Label _statusLabel;

	public Issue19630ViewModel(Label statusLabel)
	{
		_statusLabel = statusLabel;
		TestItems = new ObservableCollection<TestItem>
		{
			new TestItem { Name = "Item 1" },
			new TestItem { Name = "Item 2" },
			new TestItem { Name = "Item 3" }
		};

		EditCommand = new Command<TestItem>(OnEdit);
		DeleteCommand = new Command<TestItem>(OnDelete);
		WorkingCommand = new Command<TestItem>(OnWorking);
	}

	public ObservableCollection<TestItem> TestItems { get; }

	public ICommand EditCommand { get; }
	public ICommand DeleteCommand { get; }
	public ICommand WorkingCommand { get; }

	private void OnEdit(TestItem item)
	{
		System.Diagnostics.Debug.WriteLine($"Edit command executed for {item.Name}");
		UpdateStatus($"Edit command executed for {item.Name}");
	}

	private void OnDelete(TestItem item)
	{
		System.Diagnostics.Debug.WriteLine($"Delete command executed for {item.Name}");
		UpdateStatus($"Delete command executed for {item.Name}");
	}

	private void OnWorking(TestItem item)
	{
		System.Diagnostics.Debug.WriteLine($"Working command executed for {item.Name}");
		UpdateStatus($"Working command executed for {item.Name}");
	}

	private void UpdateStatus(string message)
	{
		_statusLabel.Text = $"Status: {message}";
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public class TestItem
{
	public string Name { get; set; }
}