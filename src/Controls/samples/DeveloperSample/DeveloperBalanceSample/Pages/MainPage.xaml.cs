using DeveloperBalanceSample.Models;
using DeveloperBalanceSample.PageModels;

namespace DeveloperBalanceSample.Pages;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageModel model)
	{
		InitializeComponent();
		BindingContext = model;
	}
}