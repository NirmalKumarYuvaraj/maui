#nullable disable
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.CardView.Widget;
using AndroidX.Core.Content;
using Google.Android.Material.Card;
using Google.Android.Material.TextField;
using Java.Lang;
using AColor = Android.Graphics.Color;
using AImageButton = Android.Widget.ImageButton;
using ASupportDrawable = AndroidX.AppCompat.Graphics.Drawable;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;

namespace Microsoft.Maui.Controls.Platform.Compatibility
{
	public class ShellSearchView : FrameLayout, IShellSearchView, TextView.IOnEditorActionListener, ITextWatcher
	{
		#region IShellSearchView

		public event EventHandler SearchConfirmed;

		public SearchHandler SearchHandler { get; set; }

		AView IShellSearchView.View
		{
			get
			{
				if (_searchButton == null)
					throw new InvalidOperationException("LoadView must be called before accessing View");
				return this;
			}
		}

		void IShellSearchView.LoadView()
		{
			LoadView(SearchHandler);
			if (_searchHandlerAppearanceTracker == null)
				_searchHandlerAppearanceTracker = CreateSearchHandlerAppearanceTracker();
		}

		protected virtual SearchHandlerAppearanceTracker CreateSearchHandlerAppearanceTracker()
		{
			return new SearchHandlerAppearanceTracker(this, _shellContext);
		}

		#endregion IShellSearchView

		#region ITextWatcher

		void ITextWatcher.AfterTextChanged(IEditable s)
		{
			var text = _textBlock.Text;

			if (text == ShellSearchViewAdapter.DoNotUpdateMarker)
			{
				return;
			}

			UpdateClearButtonState();

			SearchHandler.SetValue(SearchHandler.QueryProperty, text);

			if (SearchHandler.ShowsResults)
			{
				if (string.IsNullOrEmpty(text))
				{
					_textBlock.DismissDropDown();
				}
				else
				{
					_textBlock.ShowDropDown();
				}
			}
		}

		void ITextWatcher.BeforeTextChanged(ICharSequence s, int start, int count, int after)
		{
		}

		void ITextWatcher.OnTextChanged(ICharSequence s, int start, int before, int count)
		{
		}

		#endregion ITextWatcher

		IMauiContext MauiContext => _shellContext.Shell.Handler.MauiContext;
		IShellContext _shellContext;
		FrameLayout _cardView;
		AImageButton _clearButton;
		AImageButton _clearPlaceholderButton;
		AImageButton _searchButton;
		AutoCompleteTextView _textBlock;
		bool _disposed;
		SearchHandlerAppearanceTracker _searchHandlerAppearanceTracker;

		public ShellSearchView(Context context, IShellContext shellContext) : base(context)
		{
			_shellContext = shellContext;
		}

		ISearchHandlerController Controller => SearchHandler;

		bool TextView.IOnEditorActionListener.OnEditorAction(TextView v, ImeAction actionId, KeyEvent e)
		{
			// Fire Completed and dismiss keyboard for hardware / physical keyboards
			if (actionId == ImeAction.Done || (actionId == ImeAction.ImeNull && e.KeyCode == Keycode.Enter && e.Action == KeyEventActions.Up))
			{
				SearchConfirmed?.Invoke(this, EventArgs.Empty);
				Controller.QueryConfirmed();
			}

			return true;
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				_disposed = true;

				SearchHandler.PropertyChanged -= OnSearchHandlerPropertyChanged;

				_textBlock.ItemClick -= OnTextBlockItemClicked;
				_textBlock.RemoveTextChangedListener(this);
				_textBlock.SetOnEditorActionListener(null);
				_textBlock.DropDownBackground.Dispose();
				_textBlock.SetDropDownBackgroundDrawable(null);

				_clearButton.Click -= OnClearButtonClicked;
				_clearPlaceholderButton.Click -= OnClearPlaceholderButtonClicked;
				_searchButton.Click -= OnSearchButtonClicked;

				_textBlock.Adapter.Dispose();
				_textBlock.Adapter = null;
				_searchHandlerAppearanceTracker?.Dispose();
				_textBlock.Dispose();
				_clearButton.Dispose();
				_searchButton.Dispose();
				_cardView.Dispose();
				_clearPlaceholderButton.Dispose();
			}

			_textBlock = null;
			_clearButton = null;
			_searchButton = null;
			_cardView = null;
			_clearPlaceholderButton = null;
			_shellContext = null;
			_searchHandlerAppearanceTracker = null;

			SearchHandler = null;

			base.Dispose(disposing);
		}

		protected virtual void LoadView(SearchHandler searchHandler)
		{
			var query = searchHandler.Query;
			var placeholder = searchHandler.Placeholder;

			LP lp;
			var context = Context;
			_cardView = RuntimeFeature.IsMaterial3Enabled
				? new MaterialCardView(context)
				: new CardView(context);
			using (lp = new LayoutParams(LP.MatchParent, LP.MatchParent))
			{
				_cardView.LayoutParameters = lp;
			}

			// Apply M3-specific card styling
			if (RuntimeFeature.IsMaterial3Enabled && _cardView is MaterialCardView materialCard)
			{
				materialCard.StrokeWidth = 0;
				// M3 uses surface container color with elevation overlay
				var surfaceColor = ContextExtensions.GetThemeAttrColor(context, Resource.Attribute.colorSurfaceContainerHigh);
				materialCard.SetCardBackgroundColor(new AColor(surfaceColor));
				// M3 elevation - use level 2 for search bar prominence
				materialCard.CardElevation = context.ToPixels(3);
				// M3 corner radius - medium (12dp)
				materialCard.Radius = context.ToPixels(12);
			}

			var linearLayout = new LinearLayout(context);
			using (lp = new LayoutParams(LP.MatchParent, LP.MatchParent))
			{
				linearLayout.LayoutParameters = lp;
			}
			linearLayout.Orientation = Orientation.Horizontal;

			_cardView.AddView(linearLayout);

			// M3: Use 16dp spacing (M3 component padding), M2: Use 8dp
			int padding = RuntimeFeature.IsMaterial3Enabled
				? (int)context.ToPixels(16)
				: (int)context.ToPixels(8);

			// M3: Use system ic_menu_search, M2: Use AppCompat search icon
			var searchIconResource = RuntimeFeature.IsMaterial3Enabled
				? global::Android.Resource.Drawable.IcMenuSearch
				: Resource.Drawable.abc_ic_search_api_material;
			_searchButton = CreateImageButton(context, searchHandler, SearchHandler.QueryIconProperty, searchIconResource, padding, 0, "SearchIcon");

			lp = new LinearLayout.LayoutParams(0, LP.MatchParent)
			{
				Gravity = GravityFlags.Fill,
				Weight = 1
			};
			_textBlock = RuntimeFeature.IsMaterial3Enabled
				? new MaterialAutoCompleteTextView(context)
				: new AppCompatAutoCompleteTextView(context);
			_textBlock.LayoutParameters = lp;
			_textBlock.Text = query;
			_textBlock.Hint = placeholder;
			_textBlock.ImeOptions = ImeAction.Done;
			lp.Dispose();
			_textBlock.Enabled = searchHandler.IsSearchEnabled;
			_textBlock.SetBackground(null);
			_textBlock.SetPadding(padding, 0, padding, 0);
			_textBlock.SetSingleLine(true);
			_textBlock.Threshold = 1;
			_textBlock.Adapter = new ShellSearchViewAdapter(SearchHandler, _shellContext);
			_textBlock.ItemClick += OnTextBlockItemClicked;
			_textBlock.SetDropDownBackgroundDrawable(new ClipDrawableWrapper(_textBlock.DropDownBackground, context));

			// A note on accessibility. The _textBlocks hint is what android defaults to reading in the screen
			// reader. Therefore, we do not need to set something else.

			// M3: Use system ic_menu_close_clear_cancel, M2: Use AppCompat clear icon
			var clearIconResource = RuntimeFeature.IsMaterial3Enabled
				? global::Android.Resource.Drawable.IcMenuCloseClearCancel
				: Resource.Drawable.abc_ic_clear_material;
			_clearButton = CreateImageButton(context, searchHandler, SearchHandler.ClearIconProperty, clearIconResource, 0, padding, nameof(SearchHandler.ClearIcon));
			_clearPlaceholderButton = CreateImageButton(context, searchHandler, SearchHandler.ClearPlaceholderIconProperty, -1, 0, padding, nameof(SearchHandler.ClearPlaceholderIcon));

			linearLayout.AddView(_searchButton);
			linearLayout.AddView(_textBlock);
			linearLayout.AddView(_clearButton);
			linearLayout.AddView(_clearPlaceholderButton);

			// hook all events down here to avoid getting events while doing setup
			searchHandler.PropertyChanged += OnSearchHandlerPropertyChanged;
			_textBlock.AddTextChangedListener(this);
			_textBlock.SetOnEditorActionListener(this);
			_clearButton.Click += OnClearButtonClicked;
			_clearPlaceholderButton.Click += OnClearPlaceholderButtonClicked;
			_searchButton.Click += OnSearchButtonClicked;

			AddView(_cardView);

			linearLayout.Dispose();
		}

		protected virtual void OnSearchHandlerPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == SearchHandler.IsSearchEnabledProperty.PropertyName)
			{
				_textBlock.Enabled = SearchHandler.IsSearchEnabled;
			}
		}

		protected override async void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			Alpha = 0;
			Animate().Alpha(1).SetDuration(200).SetListener(null);

			// need to wait so keyboard will show
			await Task.Delay(200);

			if (_disposed)
				return;
		}

		protected virtual void OnClearButtonClicked(object sender, EventArgs e)
		{
			_textBlock.Text = "";
		}

		protected virtual void OnClearPlaceholderButtonClicked(object sender, EventArgs e)
		{
			Controller.ClearPlaceholderClicked();
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			var width = right - left;
			// M3: Account for larger padding (16dp * 2 + icon adjustments ≈ 40dp), M2: Use 25dp
			var widthAdjustment = RuntimeFeature.IsMaterial3Enabled ? 40 : 25;
			width -= (int)Context.ToPixels(widthAdjustment);
			var height = bottom - top;
			for (int i = 0; i < ChildCount; i++)
			{
				var child = GetChildAt(i);
				child.Measure(MakeMeasureSpec(width, MeasureSpecMode.Exactly),
							  MakeMeasureSpec(height, MeasureSpecMode.Exactly));
				child.Layout(0, 0, width, height);
			}

			_textBlock.DropDownHorizontalOffset = -_textBlock.Left;
			var radius = _cardView is CardView cardView ? cardView.Radius : 0;
			_textBlock.DropDownVerticalOffset = -(int)System.Math.Ceiling(radius);
			_textBlock.DropDownWidth = width;
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			var measureWidth = GetSize(widthMeasureSpec);
			var measureHeight = GetSize(heightMeasureSpec);

			// M3: Use 56dp (standard M3 search bar height), M2: Use 35dp
			var height = RuntimeFeature.IsMaterial3Enabled ? 56 : 35;
			SetMeasuredDimension(measureWidth, (int)Context.ToPixels(height));
		}

		int GetSize(int measureSpec)
		{
			const int modeMask = 0x3 << 30;
			return measureSpec & ~modeMask;
		}

		int MakeMeasureSpec(int size, MeasureSpecMode mode)
		{
			return size + (int)mode;
		}

		protected virtual void OnSearchButtonClicked(object sender, EventArgs e)
		{
		}

		AImageButton CreateImageButton(Context context, BindableObject bindable, BindableProperty property, int defaultImage, int leftMargin, int rightMargin, string tag)
		{
			var result = new AImageButton(context);
			result.Tag = tag;
			result.SetPadding(0, 0, 0, 0);
			result.Focusable = false;
			result.SetScaleType(ImageView.ScaleType.FitCenter);

			if (bindable.GetValue(property) is ImageSource image)
			{
				AutomationPropertiesProvider.SetContentDescription(result, image, null, null);

				image.LoadImage(MauiContext, (r) =>
				{
					result.SetImageDrawable(r?.Value);
				});
			}
			else if (defaultImage > 0 && ContextCompat.GetDrawable(Context, defaultImage) is Drawable defaultDrawable)
			{
				result.SetImageDrawable(defaultDrawable);
			}
			else
			{
				result.SetImageDrawable(null);
			}

			// M3: Use 24dp (standard M3 icon size), M2: Use 22dp
			var iconSize = RuntimeFeature.IsMaterial3Enabled ? 24 : 22;
			var lp = new LinearLayout.LayoutParams((int)Context.ToPixels(iconSize), LP.MatchParent)
			{
				LeftMargin = leftMargin,
				RightMargin = rightMargin
			};

			result.LayoutParameters = lp;
			lp.Dispose();
			result.SetBackground(null);

			return result;
		}

		void OnTextBlockItemClicked(object sender, AdapterView.ItemClickEventArgs e)
		{
			var index = e.Position;
			var item = Controller.ListProxy[index];

			_textBlock.Text = "";
			SearchConfirmed?.Invoke(this, EventArgs.Empty);
			Controller.ItemSelected(item);
		}

		void UpdateClearButtonState()
		{
			if (string.IsNullOrEmpty(_textBlock.Text))
			{
				_clearButton.Visibility = ViewStates.Gone;
				if (SearchHandler.ClearPlaceholderIcon != null && SearchHandler.ClearPlaceholderEnabled)
					_clearPlaceholderButton.Visibility = ViewStates.Visible;
				else
					_clearPlaceholderButton.Visibility = ViewStates.Gone;
			}
			else
			{
				_clearPlaceholderButton.Visibility = ViewStates.Gone;
				_clearButton.Visibility = ViewStates.Visible;
			}
		}

		class ClipDrawableWrapper : ASupportDrawable.DrawableWrapperCompat
		{
			readonly Context _context;

			public ClipDrawableWrapper(Drawable dr, Context context) : base(dr)
			{
				_context = context;
			}

			public override void Draw(Canvas canvas)
			{
				base.Draw(canvas);

				// Step 1: Clip out the top shadow that was drawn as it wont look right when lined up
#pragma warning disable CA1416 // https://github.com/xamarin/xamarin-android/issues/6962
				var paint = new Paint
				{
					Color = AColor.Black
				};
				paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));

				canvas.DrawRect(0, -100, canvas.Width, 0, paint);

				// Step 2: Draw separator line
				paint = new Paint();
				if (RuntimeFeature.IsMaterial3Enabled)
				{
					// M3: Use theme-aware outline variant color
					var outlineColor = ContextExtensions.GetThemeAttrColor(_context, Resource.Attribute.colorOutlineVariant);
					paint.Color = new AColor(outlineColor);
				}
				else
				{
					// M2: Use hardcoded light gray
					paint.Color = AColor.LightGray;
				}
#pragma warning restore CA1416
				canvas.DrawLine(0, 0, canvas.Width, 0, paint);
			}
		}
	}
}