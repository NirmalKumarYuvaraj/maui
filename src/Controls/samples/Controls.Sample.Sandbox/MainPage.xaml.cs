using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}
	protected override void OnAppearing()
	{
		Pin pin = new()
		{
			Label = "Melbourne",
			Type = PinType.Place,
			Location = new(-37.84, 144.94),
			Address = "123 Elizabeth St\nMelbourne, Victoria\n3000"
		};

		map.Pins.Add(pin);

		pin.InfoWindowClicked += (s, args) =>
		{
			System.Diagnostics.Trace.WriteLine("Pin info clicked");
		};

		MapSpan span = MapSpan.FromCenterAndRadius(new(-37.02, 144.96), Distance.FromKilometers(250));
		map.MoveToRegion(span);
	}
}