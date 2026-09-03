using System.Globalization;

namespace Maui.Controls.Sample;

public partial class Issue37892View : ContentView
{
	readonly Dictionary<string, SizeHistory> _sizeHistories = [];

	public Issue37892View()
	{
		InitializeComponent();

		TrackSize("Outer", ReproRoot);
		TrackSize("Grid", ReproGrid);
		TrackSize("Scroll", ReproScrollView);
		TrackSize("Label", WrappingLabel);
	}

	void TrackSize(string name, VisualElement element)
	{
		_sizeHistories[name] = new SizeHistory();
		element.SizeChanged += (_, _) => RecordSize(name, element.Width, element.Height);
	}

	void RecordSize(string name, double width, double height)
	{
		SizeHistory history = _sizeHistories[name];
		history.Previous = history.Current;
		history.Current = new Size(width, height);
		history.ChangeCount++;

		if (history.ChangeCount <= 20 || history.ChangeCount % 100 == 0)
		{
			Console.WriteLine(
				$"SANDBOX: ISSUE37892 SIZE {name} #{history.ChangeCount} " +
				$"{FormatSize(history.Previous)} -> {FormatSize(history.Current)} " +
				$"delta={FormatSize(history.Current - history.Previous)}");

			if (name == "Outer")
				LogPlatformState(history.ChangeCount);
		}
	}

	static string FormatSize(Size size) =>
		string.Create(CultureInfo.InvariantCulture, $"{size.Width:R}x{size.Height:R}");

	partial void LogPlatformState(long changeCount);

	sealed class SizeHistory
	{
		public long ChangeCount { get; set; }
		public Size Current { get; set; }
		public Size Previous { get; set; }
	}
}
