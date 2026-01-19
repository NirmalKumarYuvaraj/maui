using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Issues
{
	[Issue(IssueTracker.Github, 26426, "Grid invalid layout when using GridLength.Auto", PlatformAffected.Android | PlatformAffected.UWP)]
	public partial class Issue26426 : ContentPage
	{
		public Issue26426()
		{
			InitializeComponent();

			// Populate ListView with enough data to demonstrate the issue
			var items = new List<string>();
			for (int i = 0; i < 20; i++)
			{
				items.Add($"Item {i + 1} - This is test content");
			}

			_grid.ItemsSource = items;

			// Log layout measurements for debugging
			_grid.SizeChanged += (s, e) =>
			{
				System.Diagnostics.Debug.WriteLine($"[Issue26426] ListView Size: {_grid.Width} x {_grid.Height}");
			};

			this.SizeChanged += (s, e) =>
			{
				System.Diagnostics.Debug.WriteLine($"[Issue26426] Page Size: {this.Width} x {this.Height}");
			};
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			System.Diagnostics.Debug.WriteLine($"[Issue26426] OnAppearing - Page: {this.Width} x {this.Height}, ListView: {_grid.Width} x {_grid.Height}");
		}
	}
}
