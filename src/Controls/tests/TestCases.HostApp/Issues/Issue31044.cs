namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31044, "Handlers are not disconnected when a ContentView is unloaded via ControlTemplate change", PlatformAffected.UWP)]
public class Issue31044 : TestContentPage
{
	ContentView _container;
	bool _isTemplate1 = true;
	Label _logLabel;
	ScrollView _logScrollView;
	System.Text.StringBuilder _logBuilder = new System.Text.StringBuilder();

	protected override void Init()
	{
		_container = new ContentView();

		var toggleButton = new Button
		{
			Text = "Toggle Template",
			AutomationId = "ToggleTemplateButton"
		};
		toggleButton.Clicked += OnToggleTemplateClicked;

		// Create log view
		_logLabel = new Label
		{
			FontSize = 12,
			TextColor = Colors.Black,
			BackgroundColor = Colors.LightGray,
			Padding = new Thickness(10),
		};

		_logScrollView = new ScrollView
		{
			Content = _logLabel,
			HeightRequest = 300,
			BackgroundColor = Colors.LightGray
		};

		var layout = new VerticalStackLayout
		{
			Padding = new Thickness(20),
			Spacing = 20,
			Children =
			{
				toggleButton,
				_container,
				new Label
				{
					Text = "Handler Lifecycle Log:",
					FontAttributes = FontAttributes.Bold,
					FontSize = 14
				},
				_logScrollView
			}
		};

		Content = layout;
		ShowTemplateOne();
	}

	void OnToggleTemplateClicked(object sender, EventArgs e)
	{
		LogSeparator();
		LogMessage("========== TOGGLING TEMPLATE ==========");

		if (_isTemplate1)
		{
			LogMessage("Switching from Template1 to Template2...");
			ShowTemplateTwo();
		}
		else
		{
			LogMessage("Switching from Template2 to Template1...");
			ShowTemplateOne();
		}

		_isTemplate1 = !_isTemplate1;
		LogMessage("========================================");
	}

	public void LogMessage(string message)
	{
		_logBuilder.AppendLine(message);
		_logLabel.Text = _logBuilder.ToString();
	}

	void LogSeparator()
	{
		if (_logBuilder.Length > 0)
			_logBuilder.AppendLine();
	}

	void ShowTemplateOne()
	{
		// Setting Container.Content replaces the old content
		// This should trigger handler disconnection on the old content
		var view = new Issue31044_Template1(this);
		_container.Content = view;
	}

	void ShowTemplateTwo()
	{
		// Setting Container.Content replaces the old content
		// This should trigger handler disconnection on the old content
		var view = new Issue31044_Template2(this);
		_container.Content = view;
	}

	void OnOpenPageClicked(object sender, EventArgs e)
	{
		Navigation.PushAsync(new MainPage());
	}
}

public class Issue31044_Template1 : ContentView
{
	readonly Issue31044 _parent;
	CheckBox _checkBox;

	public Issue31044_Template1(Issue31044 parent)
	{
		_parent = parent;
		_checkBox = new CheckBox();

		var label = new Label
		{
			Text = "Template One",
			FontSize = 16,
			TextColor = Colors.Aquamarine
		};

		var stackLayout = new StackLayout
		{
			Children = { label, _checkBox }
		};

		Content = stackLayout;
		HandlerChanged += Template1_HandlerChanged;
		Loaded += Template1_Loaded;
		Unloaded += Template1_Unloaded;
	}

	void Template1_Loaded(object sender, EventArgs e)
	{
		var handlerName = Handler?.GetType().Name ?? "null";
		var checkBoxHandlerName = _checkBox?.Handler?.GetType().Name ?? "null";
		_parent.LogMessage($"[Template1] Loaded - Handler: {handlerName}");
		_parent.LogMessage($"[Template1] CheckBox Handler: {checkBoxHandlerName}");
	}

	void Template1_Unloaded(object sender, EventArgs e)
	{
		var handlerName = Handler?.GetType().Name ?? "null";
		var checkBoxHandlerName = _checkBox?.Handler?.GetType().Name ?? "null";
		_parent.LogMessage($"[Template1] Unloaded - Handler: {handlerName}");
		_parent.LogMessage($"[Template1] CheckBox Handler AFTER Unload: {checkBoxHandlerName}");
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
	}

	void Template1_HandlerChanged(object sender, EventArgs e)
	{
		var handlerName = Handler?.GetType().Name ?? "null";
		_parent.LogMessage($"[Template1] HandlerChanged - New Handler: {handlerName}");
	}
}

public class Issue31044_Template2 : ContentView
{
	readonly Issue31044 _parent;
	CheckBox _checkBox;

	public Issue31044_Template2(Issue31044 parent)
	{
		_parent = parent;
		_checkBox = new CheckBox
		{
			AutomationId = "MyCheckBox1"
		};

		var label = new Label
		{
			Text = "Template Two",
			FontSize = 16,
			TextColor = Colors.Orange
		};

		var stackLayout = new StackLayout
		{
			Children = { label, _checkBox }
		};

		Content = stackLayout;
	}
}