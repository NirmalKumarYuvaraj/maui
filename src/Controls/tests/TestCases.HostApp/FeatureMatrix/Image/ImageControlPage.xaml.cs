using System;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample
{
	public class ImageControlPage : NavigationPage
	{
		private ImageViewModel _viewModel;
		public ImageControlPage()
		{
			_viewModel = new ImageViewModel();

			PushAsync(new ImageControlMainPage(_viewModel));
		}
	}

	public partial class ImageControlMainPage : ContentPage
	{
		private ImageViewModel _viewModel;

		public ImageControlMainPage(ImageViewModel viewModel)
		{
			InitializeComponent();
			_viewModel = viewModel;
			BindingContext = _viewModel;
		}

		private async void NavigateToOptionsPage_Clicked(object sender, EventArgs e)
		{
			BindingContext = _viewModel = new ImageViewModel();
			await Navigation.PushAsync(new ImageOptionsPage(_viewModel));
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			if (_viewModel.RecreateOnApply)
			{
				_viewModel.RecreateOnApply = false;
				RecreateImage();
				_viewModel.RecreationCount++;
			}
		}

		private void RecreateImage()
		{
			var image = new Image
			{
				AutomationId = "ImageControl"
			};

			image.SetBinding(Image.AspectProperty, nameof(ImageViewModel.Aspect));
			image.SetBinding(Image.IsAnimationPlayingProperty, nameof(ImageViewModel.IsAnimationPlaying));
			image.SetBinding(Image.IsOpaqueProperty, nameof(ImageViewModel.IsOpaque));
			image.SetBinding(Image.SourceProperty, nameof(ImageViewModel.Source));
			image.SetBinding(Image.IsEnabledProperty, nameof(ImageViewModel.IsEnabled));
			image.SetBinding(Image.IsVisibleProperty, nameof(ImageViewModel.IsVisible));
			image.SetBinding(Image.FlowDirectionProperty, nameof(ImageViewModel.FlowDirection));
			image.SetBinding(Image.ShadowProperty, nameof(ImageViewModel.ImageShadow));

			ImageContainer.Children.Remove(TestImage);
			ImageContainer.Children.Insert(0, image);
			TestImage = image;
		}
	}
}