using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;

namespace Microsoft.Maui.Controls.Hosting;

static class Material3HandlerRegistration
{
	public static void AddAndroidHandlers(IMauiHandlersCollection handlersCollection)
	{
		if (Material3Configuration.Enabled)
		{
			AddMaterial3ReplacementHandlers(handlersCollection);
		}
		else
		{
			AddLegacyHandlers(handlersCollection);
		}
	}

	static void AddMaterial3ReplacementHandlers(IMauiHandlersCollection handlersCollection)
	{
		// Keep this list limited to controls whose native hierarchy, event model,
		// dialog behavior, or measurement contract differs under Material 3.
		handlersCollection.AddHandler<Label, LabelHandler2>();
		handlersCollection.AddHandler<Editor, EditorHandler2>();
		handlersCollection.AddHandler<Picker, PickerHandler2>();
		handlersCollection.AddHandler<RadioButton, RadioButtonHandler2>();
		handlersCollection.AddHandler<TimePicker, TimePickerHandler2>();
		handlersCollection.AddHandler<Switch, SwitchHandler2>();
		handlersCollection.AddHandler<ProgressBar, ProgressBarHandler2>();
		handlersCollection.AddHandler<ActivityIndicator, ActivityIndicatorHandler2>();
		handlersCollection.AddHandler<Image, ImageHandler2>();
		handlersCollection.AddHandler<SearchBar, SearchBarHandler2>();
		handlersCollection.AddHandler<Slider, SliderHandler2>();
		handlersCollection.AddHandler<DatePicker, DatePickerHandler2>();
		handlersCollection.AddHandler<Entry, EntryHandler2>();
	}

	static void AddLegacyHandlers(IMauiHandlersCollection handlersCollection)
	{
		handlersCollection.AddHandler<Label, LabelHandler>();
		handlersCollection.AddHandler<Editor, EditorHandler>();
		handlersCollection.AddHandler<Picker, PickerHandler>();
		handlersCollection.AddHandler<RadioButton, RadioButtonHandler>();
		handlersCollection.AddHandler<TimePicker, TimePickerHandler>();
		handlersCollection.AddHandler<Switch, SwitchHandler>();
		handlersCollection.AddHandler<ProgressBar, ProgressBarHandler>();
		handlersCollection.AddHandler<ActivityIndicator, ActivityIndicatorHandler>();
		handlersCollection.AddHandler<Image, ImageHandler>();
		handlersCollection.AddHandler<SearchBar, SearchBarHandler>();
		handlersCollection.AddHandler<Slider, SliderHandler>();
		handlersCollection.AddHandler<DatePicker, DatePickerHandler>();
		handlersCollection.AddHandler<Entry, EntryHandler>();
	}
}
