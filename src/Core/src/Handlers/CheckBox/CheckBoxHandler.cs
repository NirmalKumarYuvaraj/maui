#nullable enable
#if __IOS__ || MACCATALYST
using PlatformView = Microsoft.Maui.Platform.MauiCheckBox;
#elif __ANDROID__
using PlatformView = AndroidX.AppCompat.Widget.AppCompatCheckBox;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.Controls.CheckBox;
#elif (NETSTANDARD || !PLATFORM)
using PlatformView = System.Object;
#endif

namespace Microsoft.Maui.Handlers
{
	/// <summary>
	/// Represents the view handler for the abstract <see cref="ICheckBox"/> view and its platform-specific implementation.
	/// </summary>
	/// <seealso href="https://learn.microsoft.com/dotnet/maui/user-interface/handlers/">Conceptual documentation on handlers</seealso>
	public partial class CheckBoxHandler : ICheckBoxHandler
	{
		public static IPropertyMapper<ICheckBox, ICheckBoxHandler> Mapper = new PropertyMapper<ICheckBox, ICheckBoxHandler>(ViewHandler.ViewMapper)
		{
#if MONOANDROID
			[nameof(ICheckBox.Background)] = MapBackground,
#endif
			[nameof(ICheckBox2.IsThreeState)] = MapIsThreeState,
			[nameof(ICheckBox.IsChecked)] = MapIsChecked,
			[nameof(ICheckBox2.CheckState)] = MapCheckState,
			[nameof(ICheckBox.Foreground)] = MapForeground,
		};

		public static CommandMapper<ICheckBox, CheckBoxHandler> CommandMapper = new(ViewCommandMapper)
		{
		};

		public CheckBoxHandler() : base(Mapper, CommandMapper)
		{

		}

		public CheckBoxHandler(IPropertyMapper? mapper)
			: base(mapper ?? Mapper, CommandMapper)
		{
		}

		public CheckBoxHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
			: base(mapper ?? Mapper, commandMapper ?? CommandMapper)
		{
		}

		ICheckBox ICheckBoxHandler.VirtualView => VirtualView;

		PlatformView ICheckBoxHandler.PlatformView => PlatformView;

		/// <summary>
		/// Maps the <see cref="ICheckBox2.CheckState"/> property to the platform-specific implementations.
		/// </summary>
		/// <param name="handler">The associated handler.</param>
		/// <param name="check">The associated <see cref="ICheckBox"/> instance.</param>
		public static void MapCheckState(ICheckBoxHandler handler, ICheckBox check) =>
			MapIsChecked(handler, check);

		/// <summary>
		/// Maps the <see cref="ICheckBox2.IsThreeState"/> property to the platform-specific implementations.
		/// </summary>
		/// <param name="handler">The associated handler.</param>
		/// <param name="check">The associated <see cref="ICheckBox"/> instance.</param>
		public static partial void MapIsThreeState(ICheckBoxHandler handler, ICheckBox check);

		internal static CheckBoxState GetCheckState(ICheckBox checkBox) =>
			checkBox is ICheckBox2 checkBox2
				? checkBox2.CheckState
				: checkBox.IsChecked ? CheckBoxState.Checked : CheckBoxState.Unchecked;

		internal static bool GetIsThreeState(ICheckBox checkBox) =>
			checkBox is ICheckBox2 { IsThreeState: true };

		// Writes a native state back to the virtual view. Legacy Boolean-only views ignore Indeterminate.
		internal static void SetCheckState(ICheckBox checkBox, CheckBoxState state)
		{
			if (checkBox is ICheckBox2 checkBox2)
			{
				if (checkBox2.CheckState != state)
				{
					checkBox2.CheckState = state;
				}
			}
			else if (state != CheckBoxState.Indeterminate)
			{
				checkBox.IsChecked = state == CheckBoxState.Checked;
			}
		}

#if MONOANDROID
		/// <summary>
		/// Maps the abstract <see cref="IView.Background"/> property to the platform-specific implementations.
		/// </summary>
		/// <param name="handler">The associated handler.</param>
		/// <param name="check">The associated <see cref="ICheckBox"/> instance.</param>
		public static partial void MapBackground(ICheckBoxHandler handler, ICheckBox check);
#endif

		/// <summary>
		/// Maps the abstract <see cref="ICheckBox.IsChecked"/> property to the platform-specific implementations.
		/// </summary>
		/// <param name="handler">The associated handler.</param>
		/// <param name="check">The associated <see cref="ICheckBox"/> instance.</param>
		public static partial void MapIsChecked(ICheckBoxHandler handler, ICheckBox check);

		/// <summary>
		/// Maps the abstract <see cref="ICheckBox.Foreground"/> property to the platform-specific implementations.
		/// </summary>
		/// <param name="handler">The associated handler.</param>
		/// <param name="check">The associated <see cref="ICheckBox"/> instance.</param>
		public static partial void MapForeground(ICheckBoxHandler handler, ICheckBox check);
	}
}