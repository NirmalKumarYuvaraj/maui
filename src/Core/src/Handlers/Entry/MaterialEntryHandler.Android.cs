using System;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.Core.Content;
using Google.Android.Material.TextField;
using Microsoft.Maui.Graphics;
using static Android.Views.View;
using static Android.Widget.TextView;

namespace Microsoft.Maui.Handlers
{
	internal partial class MaterialEntryHandler : ViewHandler<IEntry, TextInputLayout>
	{
		public static PropertyMapper<IEntry, MaterialEntryHandler> Mapper =
		  new(ElementMapper)
		  {
			  [nameof(IEntry.Placeholder)] = MapPlaceholder,
		  };

		private static void MapPlaceholder(MaterialEntryHandler handler, IEntry entry)
		{
			handler.PlatformView.Hint = entry.Placeholder;
		}

		public static CommandMapper<IEntry, MaterialEntryHandler> CommandMapper =
		  new(ViewCommandMapper);

		public MaterialEntryHandler() : base(Mapper, CommandMapper)
		{
		}

		protected override TextInputLayout CreatePlatformView()
		{
			var inflater = LayoutInflater.FromContext(Context)!;

			var view = inflater.Inflate(Resource.Layout.textinputlayout, null);
			var textInputLayout =
				view?.FindViewById<TextInputLayout>(Resource.Id.input_outline);
			//textInputLayout?.SetBoxBackgroundColorResource(Colors.Transparent.ToPlatform());
			return textInputLayout!;
		}

	}
}
