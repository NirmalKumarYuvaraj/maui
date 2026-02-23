using Microsoft.Extensions.FileProviders;
using Microsoft.Maui.Media;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33162, "MediaPicker.CapturePhotoAsync calls OnAppearing after capturing image in iOS", PlatformAffected.iOS)]
public class Issue33162 : ContentPage
{
	int _onAppearingCount = 0;
	Label _resultLabel;

	public Issue33162()
	{
		_resultLabel = new Label
		{
			Text = "OnAppearing count: 0",
			AutomationId = "ResultLabel"
		};

		var triggerButton = new Button
		{
			Text = "Present Native Modal",
			AutomationId = "TriggerButton"
		};

		triggerButton.Clicked += async (s, e) =>
		{
			var file = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
			{
				Title = "Take a photo",
			});

			if (file is not null)
			{
				await DisplayAlertAsync(null, "Image Captured", "Ok");
			}
		};

		Content = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 20,
			Children =
			{
				new Label
				{
					Text = "Test Case: OnAppearing should only be called once when the page appears, NOT when native iOS modals (like camera) are dismissed.",
					FontSize = 14
				},
				_resultLabel,
				triggerButton
			}
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_onAppearingCount++;
		_resultLabel.Text = $"OnAppearing count: {_onAppearingCount}";
	}
}
