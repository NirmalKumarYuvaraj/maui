using System.Diagnostics;
using Microsoft.Maui.Storage;
namespace Maui.Controls.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		//Maui.Controls.Sample.Sandbox
		var imageSource = new FileImageSource
		{
			CachingEnabled = false,
			File = "avatar.png"
		};
		img.Source = imageSource;
	}

	private void Button_Clicked(object sender, EventArgs e)
	{
		var imageManagerDiskCache = Path.Combine(FileSystem.CacheDirectory, "image_manager_disk_cache");

		if (Directory.Exists(imageManagerDiskCache))
		{
			foreach (var imageCacheFile in Directory.EnumerateFiles(imageManagerDiskCache))
			{
				Debug.WriteLine($"Deleting {imageCacheFile}");
				File.Delete(imageCacheFile);
			}
		}
	}
}