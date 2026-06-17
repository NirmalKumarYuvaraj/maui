using System.Text;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	readonly StringBuilder _log = new();

	public MainPage()
	{
		InitializeComponent();
	}

	// ── Two-state ──────────────────────────────────────────────────────

	void OnTwoStateCheckedChanged(object? sender, CheckedChangedEventArgs e)
	{
		TwoStateLabel.Text = e.Value ? "Checked" : "Unchecked";
		AppendLog($"[Two-state] CheckedChanged → {e.Value}");
	}

	// ── Three-state (tap cycling) ───────────────────────────────────────

	void OnThreeStateChanged(object? sender, CheckStateChangedEventArgs e)
	{
		ThreeStateLabel.Text = e.CheckState.ToString();
		AppendLog($"[Three-state] CheckStateChanged → {e.CheckState}");
	}

	// ── Programmatic control ────────────────────────────────────────────

	void OnSetUnchecked(object? sender, EventArgs e)
	{
		ProgrammaticCheckBox.CheckState = CheckState.Unchecked;
		AppendLog("[Programmatic] Set → Unchecked");
	}

	void OnSetIndeterminate(object? sender, EventArgs e)
	{
		ProgrammaticCheckBox.CheckState = CheckState.Indeterminate;
		AppendLog("[Programmatic] Set → Indeterminate");
	}

	void OnSetChecked(object? sender, EventArgs e)
	{
		ProgrammaticCheckBox.CheckState = CheckState.Checked;
		AppendLog("[Programmatic] Set → Checked");
	}

	void OnProgrammaticStateChanged(object? sender, CheckStateChangedEventArgs e)
	{
		ProgrammaticLabel.Text = e.CheckState.ToString();
		AppendLog($"[Programmatic] CheckStateChanged → {e.CheckState}");
	}

	// ── Log helpers ─────────────────────────────────────────────────────

	void AppendLog(string message)
	{
		_log.Insert(0, message + "\n");
		EventLog.Text = _log.ToString();
	}

	void OnClearLog(object? sender, EventArgs e)
	{
		_log.Clear();
		EventLog.Text = "(no events yet)";
	}
}
