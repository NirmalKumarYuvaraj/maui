#nullable disable
using System;
using System.Linq;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.DependencyInjection;
using AColor = Android.Graphics.Color;
using ADrawableCompat = AndroidX.Core.Graphics.Drawable.DrawableCompat;
using AImageButton = Android.Widget.ImageButton;
using AView = Android.Views.View;
using Color = Microsoft.Maui.Graphics.Color;

namespace Microsoft.Maui.Controls.Platform.Compatibility;

public class SearchHandlerAppearanceTracker : IDisposable
{
	readonly EditText _editText;
	readonly IShellContext _shellContext;

	SearchHandler _searchHandler;
	bool _disposed;
	AView _control;
	InputTypes _inputType;

	IMauiContext MauiContext => _shellContext.Shell.Handler.MauiContext;

	public SearchHandlerAppearanceTracker(IShellSearchView searchView, IShellContext shellContext)
	{
		_shellContext = shellContext;
		_searchHandler = searchView.SearchHandler;
		_control = searchView.View;
		_searchHandler.PropertyChanged += SearchHandlerPropertyChanged;
		_searchHandler.ShowSoftInputRequested += OnShowSoftInputRequested;
		_searchHandler.HideSoftInputRequested += OnHideSoftInputRequested;
		_editText = (_control as ViewGroup).GetChildrenOfType<EditText>().FirstOrDefault();
		_editText.FocusChange += EditTextFocusChange;
		UpdateSearchBarColors();
		UpdateFont();
		UpdateHorizontalTextAlignment();
		UpdateVerticalTextAlignment();
		UpdateInputType();
	}

	protected virtual void SearchHandlerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.Is(SearchHandler.BackgroundColorProperty))
		{
			UpdateBackgroundColor();
		}
		else if (e.Is(SearchHandler.TextColorProperty))
		{
			UpdateTextColor();
		}
		else if (e.Is(SearchHandler.TextTransformProperty))
		{
			UpdateTextTransform();
		}
		else if (e.Is(SearchHandler.PlaceholderProperty))
		{
			UpdatePlaceholder();
		}
		else if (e.Is(SearchHandler.PlaceholderColorProperty))
		{
			UpdatePlaceholderColor();
		}
		else if (e.IsOneOf(SearchHandler.FontFamilyProperty, SearchHandler.FontAttributesProperty, SearchHandler.FontSizeProperty))
		{
			UpdateFont();
		}
		else if (e.Is(SearchHandler.CancelButtonColorProperty))
		{
			UpdateClearIconColor();
		}
		else if (e.Is(SearchHandler.KeyboardProperty))
		{
			UpdateInputType();
		}
		else if (e.Is(SearchHandler.HorizontalTextAlignmentProperty))
		{
			UpdateHorizontalTextAlignment();
		}
		else if (e.Is(SearchHandler.VerticalTextAlignmentProperty))
		{
			UpdateVerticalTextAlignment();
		}
		else if (e.Is(SearchHandler.AutomationIdProperty))
		{
			UpdateAutomationId();
		}
	}

	void EditTextFocusChange(object s, AView.FocusChangeEventArgs args)
	{
		_searchHandler.SetIsFocused(_editText.IsFocused);
	}

	void UpdateSearchBarColors()
	{
		UpdateBackgroundColor();
		UpdateTextColor();
		UpdateTextTransform();
		UpdatePlaceholderColor();
		UpdateClearIconColor();
		UpdateAutomationId();
	}

	void UpdateAutomationId()
	{
		AutomationPropertiesProvider
			.SetAutomationId(_editText, _searchHandler?.AutomationId);

	}

	void UpdateFont()
	{
		var fontManager = MauiContext.Services.GetRequiredService<IFontManager>();
		var font = Font.OfSize(_searchHandler.FontFamily, _searchHandler.FontSize).WithAttributes(_searchHandler.FontAttributes);

		_editText.Typeface = fontManager.GetTypeface(font);
		_editText.SetTextSize(ComplexUnitType.Sp, (float)_searchHandler.FontSize);
	}

	void UpdatePlaceholder()
	{
		_editText.Hint = _searchHandler.Placeholder;
	}

	void UpdatePlaceholderColor()
	{
		if (RuntimeFeature.IsMaterial3Enabled && _searchHandler.PlaceholderColor == null)
		{
			// M3: Use theme's onSurfaceVariant color for hints
			var hintColor = ContextExtensions.GetThemeAttrColor(_editText.Context, Resource.Attribute.colorOnSurfaceVariant);
			_editText.SetHintTextColor(new AColor(hintColor));
		}
		else
		{
			// Custom color or M2 mode
			_editText.UpdatePlaceholderColor(_searchHandler.PlaceholderColor);
		}
	}

	void UpdateHorizontalTextAlignment()
	{
		_editText.UpdateHorizontalAlignment(_searchHandler.HorizontalTextAlignment, TextAlignment.Center.ToVerticalGravityFlags());
	}

	void UpdateVerticalTextAlignment()
	{
		_editText.UpdateVerticalAlignment(_searchHandler.VerticalTextAlignment, TextAlignment.Center.ToVerticalGravityFlags());
	}

	void UpdateTextTransform()
	{
		_editText.Text = _searchHandler.UpdateFormsText(_editText.Text, _searchHandler.TextTransform);
	}

	void UpdateBackgroundColor()
	{
		if (_searchHandler.BackgroundColor == null)
		{
			return;
		}

		var linearLayout = (_control as ViewGroup).GetChildrenOfType<LinearLayout>().FirstOrDefault();
		if (RuntimeFeature.IsMaterial3Enabled)
		{
			// M3: Blend custom color with surface container for proper theming
			linearLayout.SetBackgroundColor(_searchHandler.BackgroundColor.ToPlatform());
		}
		else
		{
			// M2: Direct color application
			linearLayout.SetBackgroundColor(_searchHandler.BackgroundColor.ToPlatform());
		}
	}

	void UpdateClearIconColor()
	{
		UpdateImageButtonIconColor(nameof(SearchHandler.ClearIcon), _searchHandler.CancelButtonColor);
	}

	void UpdateClearPlaceholderIconColor()
	{
		UpdateImageButtonIconColor(nameof(SearchHandler.ClearPlaceholderIcon), _searchHandler.TextColor);
	}

	void UpdateTextColor()
	{
		_editText.UpdateTextColor(_searchHandler.TextColor);
		UpdateImageButtonIconColor("SearchIcon", _searchHandler.TextColor);
		UpdateClearPlaceholderIconColor();
		//we need to set the cursor to
	}
	void UpdateImageButtonIconColor(string tagName, Color toColor)
	{
		var image = _control.FindViewWithTag(tagName) as AImageButton;
		if (image?.Drawable == null)
		{
			return;
		}

		if (toColor != null)
		{
			if (RuntimeFeature.IsMaterial3Enabled)
			{
				// M3: Use DrawableCompat for proper tinting with state support
				ADrawableCompat.SetTint(image.Drawable, toColor.ToPlatform());
			}
			else
			{
				// M2: Use color filter
				image.Drawable.SetColorFilter(toColor, FilterMode.SrcIn);
			}
		}
		else
		{
			if (RuntimeFeature.IsMaterial3Enabled)
			{
				ADrawableCompat.SetTintList(image.Drawable, null);
			}
			else
			{
				image.Drawable.ClearColorFilter();
			}
		}
	}

	void OnShowSoftInputRequested(object sender, EventArgs e)
	{
		_editText?.RequestFocus();
		_control?.ShowSoftInput();
	}

	void OnHideSoftInputRequested(object sender, EventArgs e)
	{
		_control?.HideSoftInput();
	}

	void UpdateInputType()
	{
		var keyboard = _searchHandler.Keyboard;

		_inputType = keyboard.ToInputType();
		bool isSpellCheckEnableSet = false;
		bool isSpellCheckEnable = false;
		// model.IsSet(InputView.IsSpellCheckEnabledProperty)
		if (keyboard is not CustomKeyboard)
		{
			if (isSpellCheckEnableSet)
			{
				if ((_inputType & InputTypes.TextFlagNoSuggestions) != InputTypes.TextFlagNoSuggestions)
				{
					if (!isSpellCheckEnable)
					{
						_inputType |= InputTypes.TextFlagNoSuggestions;
					}
				}
			}
		}
		_editText.InputType = _inputType;

		if (keyboard == Keyboard.Numeric)
		{
			_editText.KeyListener = GetDigitsKeyListener(_inputType);
		}
	}

	protected virtual global::Android.Text.Method.NumberKeyListener GetDigitsKeyListener(InputTypes inputTypes)
	{
		// Override this in a custom renderer to use a different NumberKeyListener
		// or to filter out input types you don't want to allow
		// (e.g., inputTypes &= ~InputTypes.NumberFlagSigned to disallow the sign)
		return LocalizedDigitsKeyListener.Create(inputTypes);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (disposing)
		{
			if (_searchHandler != null)
			{
				_searchHandler.PropertyChanged -= SearchHandlerPropertyChanged;
				_editText.FocusChange -= EditTextFocusChange;
				_searchHandler.ShowSoftInputRequested -= OnShowSoftInputRequested;
				_searchHandler.HideSoftInputRequested -= OnHideSoftInputRequested;
			}
			_searchHandler = null;
			_control = null;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}