namespace Maui.Controls.Sample;

sealed class SharedTransitionDemoPage : ContentPage
{
	static readonly (string Name, string Group, Color Color)[] Items =
	{
		("Coral", "coral", Color.FromArgb("#D96850")),
		("Ocean", "ocean", Color.FromArgb("#268A9B")),
		("Violet", "violet", Color.FromArgb("#8062B7"))
	};

	readonly Switch _enabled = new() { IsToggled = true, AutomationId = "SharedTransitionEnabled" };
	readonly Slider _duration = new() { Minimum = 100, Maximum = 1500, Value = 650, AutomationId = "SharedTransitionDuration" };
	readonly Label _durationLabel = new();
	readonly Picker _easing = new()
	{
		ItemsSource = new[] { "Cubic in/out", "Linear", "Bounce out" },
		SelectedIndex = 0,
		AutomationId = "SharedTransitionEasing"
	};
	readonly Label _navigationMode = new();
	readonly Button _switchNavigation = new() { AutomationId = "SharedTransitionSwitchHost" };
	bool _navigating;

	public SharedTransitionDemoPage()
	{
		Title = "Shared Transitions";

		_durationLabel.Text = $"Duration: {_duration.Value:F0} ms";
		_duration.ValueChanged += (_, args) => _durationLabel.Text = $"Duration: {args.NewValue:F0} ms";
		_switchNavigation.Clicked += SwitchNavigation;

		var existingSandbox = new Button
		{
			Text = "Open existing Sandbox page",
			AutomationId = "SharedTransitionOpenSandbox"
		};
		existingSandbox.Clicked += async (_, _) =>
		{
			Console.WriteLine("SANDBOX: Opening existing Sandbox page");
			await Navigation.PushAsync(new MainPage());
		};

		var content = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 12,
			Children =
			{
				new Label { Text = "Shared-element transitions", FontSize = 24, FontAttributes = FontAttributes.Bold },
				new Label { Text = "Android prototype. Other platforms use normal navigation." },
				_navigationMode,
				_switchNavigation,
				new HorizontalStackLayout
				{
					Spacing = 12,
					Children =
					{
						new Label { Text = "Enable shared transition", VerticalOptions = LayoutOptions.Center },
						_enabled
					}
				},
				_durationLabel,
				_duration,
				new Label { Text = "Easing" },
				_easing,
				new Label { Text = "Open a color, then navigate back. Each swatch uses the same name with a different group." }
			}
		};

		foreach (var item in Items)
			content.Children.Add(CreateItem(item));

		content.Children.Add(existingSandbox);
		Content = new ScrollView { Content = content };
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		bool isShell = Window?.Page is SharedTransitionDemoShell;
		_navigationMode.Text = $"Navigation host: {(isShell ? "Shell" : "NavigationPage")}";
		_switchNavigation.Text = isShell ? "Switch to NavigationPage" : "Switch to Shell";
	}

	void SwitchNavigation(object? sender, EventArgs args)
	{
		var window = Window ?? throw new InvalidOperationException("The demo must be attached to a window.");
		bool useShell = window.Page is not SharedTransitionDemoShell;
		Console.WriteLine($"SANDBOX: Switching to {(useShell ? "Shell" : "NavigationPage")}");
		window.Page = useShell
			? new SharedTransitionDemoShell()
			: new NavigationPage(new SharedTransitionDemoPage());
	}

	View CreateItem((string Name, string Group, Color Color) item)
	{
		var swatch = new BoxView
		{
			Color = item.Color,
			CornerRadius = 12,
			HeightRequest = 64,
			WidthRequest = 64
		};
		SharedTransition.SetName(swatch, "color");
		SharedTransition.SetGroup(swatch, item.Group);

		var label = new Label { Text = item.Name, VerticalOptions = LayoutOptions.Center };
		var open = new Button { Text = "Open", AutomationId = $"SharedTransitionOpen_{item.Group}" };
		open.Clicked += async (_, _) =>
		{
			if (_navigating)
				return;

			_navigating = true;
			try
			{
				var easing = _easing.SelectedIndex switch
				{
					1 => Easing.Linear,
					2 => Easing.BounceOut,
					_ => Easing.CubicInOut
				};
				Console.WriteLine($"SANDBOX: Opening {item.Name}, shared transition enabled: {_enabled.IsToggled}");
				await Navigation.PushAsync(new DetailPage(item, (uint)Math.Round(_duration.Value), easing, _enabled.IsToggled));
			}
			finally
			{
				_navigating = false;
			}
		};

		var row = new Grid
		{
			ColumnSpacing = 12,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = 64 },
				new ColumnDefinition { Width = GridLength.Star },
				new ColumnDefinition { Width = GridLength.Auto }
			},
			Children = { swatch, label, open }
		};
		Grid.SetColumn(label, 1);
		Grid.SetColumn(open, 2);
		return row;
	}

	sealed class DetailPage : ContentPage
	{
		public DetailPage((string Name, string Group, Color Color) item, uint duration, Easing easing, bool enabled)
		{
			Title = item.Name;
			SharedTransition.SetIsEnabled(this, enabled);

			var swatch = new BoxView
			{
				Color = item.Color,
				CornerRadius = 20,
				HeightRequest = 240,
				HorizontalOptions = LayoutOptions.Fill
			};
			SharedTransition.SetName(swatch, "color");
			SharedTransition.SetGroup(swatch, item.Group);
			SharedTransition.SetDuration(swatch, duration);
			SharedTransition.SetEasing(swatch, easing);

			var back = new Button { Text = "Back", AutomationId = "SharedTransitionBack" };
			back.Clicked += async (_, _) =>
			{
				Console.WriteLine("SANDBOX: Returning from shared transition detail");
				await Navigation.PopAsync();
			};

			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Padding = 20,
					Spacing = 16,
					Children =
					{
						swatch,
						new Label { Text = item.Name, FontSize = 24, FontAttributes = FontAttributes.Bold },
						new Label { Text = $"Name: color, group: {item.Group}, duration: {duration} ms" },
						new Label { Text = "Use Back or the navigation bar to return to the matching swatch." },
						back
					}
				}
			};
		}
	}
}

sealed class SharedTransitionDemoShell : Shell
{
	public SharedTransitionDemoShell()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;
		Items.Add(new FlyoutItem
		{
			Title = "Shared Transitions",
			Items =
			{
				new ShellSection
				{
					Items =
					{
						new ShellContent
						{
							Title = "Shared Transitions",
							Route = "shared-transitions",
							Content = new SharedTransitionDemoPage()
						}
					}
				}
			}
		});
	}
}
