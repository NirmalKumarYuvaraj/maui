using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;

namespace Microsoft.Maui.Handlers
{
	public partial class RadioButtonHandler : ViewHandler<IRadioButton, View>
	{
		Drawable? _defaultBackground;
		BorderDrawable? _backgroundDrawable;

		internal static AppCompatRadioButton? GetPlatformRadioButton(IRadioButtonHandler handler) => handler.PlatformView as AppCompatRadioButton;

		public override void PlatformArrange(Graphics.Rect frame)
		{
			this.PrepareForTextViewArrange(frame);
			base.PlatformArrange(frame);
		}

		protected override AppCompatRadioButton CreatePlatformView()
		{
			return new AppCompatRadioButton(Context)
			{
				SoundEffectsEnabled = false
			};
		}

		protected override void ConnectHandler(View platformView)
		{
			AppCompatRadioButton? platformRadioButton = GetPlatformRadioButton(this);
			if (platformRadioButton != null)
			{
				_defaultBackground = platformRadioButton.Background;
				base.ConnectHandler(platformView);
				platformRadioButton.CheckedChange += OnCheckChanged;
			}
			else
			{
				base.ConnectHandler(platformView);
			}
		}

		protected override void DisconnectHandler(View platformView)
		{
			if (platformView is AppCompatRadioButton platformRadioButton)
			{
				platformRadioButton.CheckedChange -= OnCheckChanged;
				platformRadioButton.ResetBackground(_defaultBackground, ref _backgroundDrawable);
			}

			_defaultBackground = null;

			base.DisconnectHandler(platformView);
		}

		public static void MapBackground(IRadioButtonHandler handler, IRadioButton radioButton)
		{
			if (handler is RadioButtonHandler platformHandler)
				GetPlatformRadioButton(handler)?.UpdateBackground(radioButton, platformHandler._defaultBackground, ref platformHandler._backgroundDrawable);
			else
				GetPlatformRadioButton(handler)?.UpdateBackground(radioButton);
		}

		public static void MapIsChecked(IRadioButtonHandler handler, IRadioButton radioButton)
		{
			GetPlatformRadioButton(handler)?.UpdateIsChecked(radioButton);
		}

		public static void MapContent(IRadioButtonHandler handler, IRadioButton radioButton)
		{
			GetPlatformRadioButton(handler)?.UpdateContent(radioButton);
		}

		public static void MapTextColor(IRadioButtonHandler handler, ITextStyle textStyle)
		{
			GetPlatformRadioButton(handler)?.UpdateTextColor(textStyle);
		}

		public static void MapCharacterSpacing(IRadioButtonHandler handler, ITextStyle textStyle)
		{
			GetPlatformRadioButton(handler)?.UpdateCharacterSpacing(textStyle);
		}

		public static void MapFont(IRadioButtonHandler handler, ITextStyle textStyle)
		{
			var fontManager = handler.GetRequiredService<IFontManager>();

			GetPlatformRadioButton(handler)?.UpdateFont(textStyle, fontManager);
		}

		public static void MapStrokeColor(IRadioButtonHandler handler, IRadioButton radioButton)
		{
			if (handler is RadioButtonHandler)
				MapBackground(handler, radioButton);
			else
				GetPlatformRadioButton(handler)?.UpdateStrokeColor(radioButton);
		}

		public static void MapStrokeThickness(IRadioButtonHandler handler, IRadioButton radioButton)
		{
			if (handler is RadioButtonHandler)
				MapBackground(handler, radioButton);
			else
				GetPlatformRadioButton(handler)?.UpdateStrokeThickness(radioButton);
		}

		public static void MapCornerRadius(IRadioButtonHandler handler, IRadioButton radioButton)
		{
			if (handler is RadioButtonHandler)
				MapBackground(handler, radioButton);
			else
				GetPlatformRadioButton(handler)?.UpdateCornerRadius(radioButton);
		}

		void OnCheckChanged(object? sender, CompoundButton.CheckedChangeEventArgs e)
		{
			if (VirtualView == null)
				return;

			VirtualView.IsChecked = e.IsChecked;
		}
	}
}