namespace Maui.Controls.Sample;

internal static class FlyoutDashboard
{
	static readonly Color s_panel = Color.FromArgb("#182334");
	static readonly Color s_muted = Color.FromArgb("#B3C4D8");
	static readonly Color s_accent = Color.FromArgb("#73DCC4");

	internal static View Build(string page)
	{
		// Literal snapshots only: no services, bindings, images, timers or business actions.
		var (subtitle, metrics, columns, rows, facts) = page switch
		{
			"Home" => (
				"Account overview • November 2026",
				new[] { ("Account value", "$284,650"), ("Monthly income", "$1,240"), ("Pending items", "04") },
				new[] { "Activity", "Account / date", "Amount", "Status" },
				new (string, string, string, string)[]
				{
					("Dividend payment", "Income fund • Nov 04", "+$320", "Settled"),
					("Treasury purchase", "Reserve • Nov 03", "$8,000", "Settled"),
					("Monthly contribution", "Growth • Nov 02", "+$750", "Settled"),
					("Draw allocation", "Prize bonds • Nov 02", "120 units", "Pending"),
					("Fund distribution", "Balanced • Nov 01", "+$180", "Settled"),
					("Savings transfer", "Reserve • Oct 31", "+$2,000", "Settled"),
					("Bond maturity", "Fixed income • Oct 30", "$5,000", "Settled"),
					("Reinvestment", "Income fund • Oct 29", "$410", "Pending"),
					("Contribution", "Growth • Oct 28", "+$750", "Settled"),
					("Fee statement", "All accounts • Oct 27", "$12", "Ready"),
					("Account review", "Joint account • Oct 26", "2 holders", "Ready"),
					("Annual statement", "All accounts • Oct 25", "2026", "Ready")
				},
				new[] { ("Next review", "12 November"), ("Risk profile", "Moderate"), ("Plan", "Long-term savings"), ("Statements", "Paperless") }),
			"Draws" => (
				"Prize bond calendar • Scheduled allocations",
				new[] { ("Eligible bonds", "2,480"), ("Next prize pool", "$125,000"), ("Upcoming draws", "12") },
				new[] { "Draw", "Series / date", "Entries", "Status" },
				new (string, string, string, string)[]
				{
					("November premier", "Series A • Nov 08", "240", "Eligible"),
					("November weekly", "Series B • Nov 11", "180", "Eligible"),
					("Autumn bonus", "Series C • Nov 15", "320", "Review"),
					("November classic", "Series A • Nov 18", "240", "Eligible"),
					("Community prize", "Series D • Nov 22", "160", "Eligible"),
					("Month-end special", "Series B • Nov 29", "180", "Review"),
					("December premier", "Series A • Dec 06", "240", "Scheduled"),
					("Winter weekly", "Series C • Dec 09", "320", "Scheduled"),
					("December classic", "Series D • Dec 13", "160", "Scheduled"),
					("Holiday bonus", "Series B • Dec 20", "180", "Scheduled"),
					("Year-end special", "Series A • Dec 27", "240", "Scheduled"),
					("New year preview", "Series C • Jan 03", "320", "Scheduled")
				},
				new[] { ("Allocation", "Automatic"), ("Minimum holding", "30 days"), ("Results", "Published weekly"), ("Prize payment", "Account credit") }),
			"Investments" => (
				"Portfolio snapshot • Diversified long-term holdings",
				new[] { ("Market value", "$248,900"), ("Total return", "+8.42%"), ("Cash available", "$12,450") },
				new[] { "Holding", "Allocation / units", "Value", "Risk" },
				new (string, string, string, string)[]
				{
					("Global equity", "24% • 420 units", "$59,736", "High"),
					("Government bonds", "18% • 360 units", "$44,802", "Low"),
					("Income fund", "12% • 285 units", "$29,868", "Medium"),
					("Balanced index", "10% • 210 units", "$24,890", "Medium"),
					("Treasury reserve", "8% • 160 units", "$19,912", "Low"),
					("Infrastructure", "6% • 125 units", "$14,934", "Medium"),
					("Healthcare", "5% • 110 units", "$12,445", "High"),
					("Technology", "5% • 95 units", "$12,445", "High"),
					("Municipal bonds", "4% • 85 units", "$9,956", "Low"),
					("Property trust", "3% • 70 units", "$7,467", "Medium"),
					("Sustainable fund", "3% • 60 units", "$7,467", "Medium"),
					("Short-term notes", "2% • 50 units", "$4,978", "Low")
				},
				new[] { ("Strategy", "Balanced growth"), ("Rebalance", "Quarterly"), ("Dividends", "Reinvest"), ("Benchmark", "Composite index") }),
			_ => (
				"Registered holders • Ownership and compliance",
				new[] { ("Active holders", "12"), ("Verified accounts", "10"), ("Reviews due", "02") },
				new[] { "Holder", "Reference / ownership", "Units", "Review" },
				new (string, string, string, string)[]
				{
					("Alex Morgan", "H-1001 • Primary", "420", "Verified"),
					("Sam Taylor", "H-1002 • Joint", "360", "Verified"),
					("Jordan Lee", "H-1003 • Primary", "285", "Due"),
					("Casey Brown", "H-1004 • Trustee", "210", "Verified"),
					("Riley Wilson", "H-1005 • Primary", "160", "Verified"),
					("Avery Davis", "H-1006 • Joint", "125", "Verified"),
					("Jamie Clark", "H-1007 • Primary", "110", "Verified"),
					("Quinn Thomas", "H-1008 • Trustee", "95", "Due"),
					("Taylor Evans", "H-1009 • Primary", "85", "Verified"),
					("Robin Scott", "H-1010 • Joint", "70", "Verified"),
					("Drew Walker", "H-1011 • Primary", "60", "Verified"),
					("Cameron Hall", "H-1012 • Trustee", "50", "Verified")
				},
				new[] { ("Register", "Private investors"), ("Ownership", "Nominee accounts"), ("Identity checks", "Annual"), ("Contact", "Secure mailbox") })
		};

		var body = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(16) };
		body.Add(Text(page, 28, bold: true));
		body.Add(Text(subtitle, 14, s_muted));
		body.Add(Text("STATIC DEMONSTRATION • No live account data", 11, s_accent));

		var summary = new Grid { ColumnSpacing = 8, ColumnDefinitions = Columns(3) };
		for (var i = 0; i < metrics.Length; i++)
		{
			var stack = new VerticalStackLayout { Spacing = 8 };
			stack.Add(Text(metrics[i].Item1, 12, s_muted));
			stack.Add(Text(metrics[i].Item2, 19, bold: true));
			stack.Add(new ProgressBar { Progress = (i + 2) / 5.0, ProgressColor = s_accent });
			summary.Add(Card(stack), i, 0);
		}
		body.Add(summary);

		var filters = new Grid { ColumnSpacing = 10, ColumnDefinitions = Columns(2) };
		filters.Add(new Entry
		{
			Placeholder = $"Search {page.ToLowerInvariant()}",
			PlaceholderColor = s_muted,
			TextColor = Colors.White,
			AutomationId = $"{page}Search",
			FontSize = 14
		}, 0, 0);
		filters.Add(new Button
		{
			Text = "All records",
			BackgroundColor = s_panel,
			TextColor = Colors.White,
			AutomationId = $"{page}Filter"
		}, 1, 0);
		body.Add(filters);

		var details = new Grid { ColumnDefinitions = Columns(2), RowSpacing = 12, ColumnSpacing = 12 };
		details.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		details.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		for (var i = 0; i < facts.Length; i++)
		{
			var field = new VerticalStackLayout { Spacing = 4 };
			field.Add(Text(facts[i].Item1, 12, s_muted));
			field.Add(Text(facts[i].Item2, 14, bold: true));
			details.Add(field, i % 2, i / 2);
		}
		body.Add(Card(details));
		body.Add(Text($"{page} register", 20, bold: true));

		// Deliberately non-virtualized: every static row participates in construction/measure.
		var table = new VerticalStackLayout { Spacing = 1 };
		table.Add(TableRow(columns[0], columns[1], columns[2], columns[3], true));
		foreach (var row in rows)
			table.Add(TableRow(row.Item1, row.Item2, row.Item3, row.Item4, false));
		body.Add(table);
		body.Add(new Button
		{
			Text = $"View {page.ToLowerInvariant()} statement (sample)",
			AutomationId = $"{page}Statement",
			BackgroundColor = s_panel,
			TextColor = s_accent
		});
		body.Add(Text("Snapshot prepared 04 Nov 2026. Controls are illustrative; no actions are connected.", 12, s_muted));

		return new ScrollView { Content = body, BackgroundColor = Color.FromArgb("#0B1220") };
	}

	static ColumnDefinitionCollection Columns(int count)
	{
		var columns = new ColumnDefinitionCollection();
		for (var i = 0; i < count; i++)
			columns.Add(new ColumnDefinition(GridLength.Star));
		return columns;
	}

	static Label Text(string text, double size, Color? color = null, bool bold = false) => new()
	{
		Text = text,
		FontSize = size,
		TextColor = color ?? Colors.White,
		FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
		LineBreakMode = LineBreakMode.WordWrap
	};

	static Border Card(View content) => new()
	{
		BackgroundColor = s_panel,
		Stroke = Color.FromArgb("#314158"),
		StrokeThickness = 1,
		Padding = new Thickness(12),
		Content = content
	};

	static Grid TableRow(string name, string description, string value, string status, bool header)
	{
		var row = new Grid { ColumnDefinitions = Columns(3), ColumnSpacing = 8, Padding = new Thickness(10, 12), BackgroundColor = s_panel };
		var identity = new VerticalStackLayout { Spacing = 5 };
		identity.Add(Text(name, 13, bold: true));
		identity.Add(Text(description, 11, s_muted));
		row.Add(identity, 0, 0);
		row.Add(Text(value, 13, bold: header), 1, 0);
		row.Add(new Border
		{
			Padding = new Thickness(4),
			StrokeThickness = 0,
			BackgroundColor = Color.FromArgb("#24394A"),
			VerticalOptions = LayoutOptions.Start,
			Content = Text(status, 11, s_accent, header)
		}, 2, 0);
		return row;
	}
}
