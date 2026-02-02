#nullable enable
using System;
using System.Threading;

namespace Microsoft.Maui.Graphics
{
	/// <summary>
	/// Provides a way to resolve font aliases to their actual font names or paths.
	/// This is used by MAUI to integrate registered fonts with the Graphics library.
	/// </summary>
	internal static class FontAliasResolver
	{
		static Func<string, string?>? s_resolver;

		/// <summary>
		/// Gets or sets the function used to resolve font aliases.
		/// When set, this function is called by Graphics FontExtensions to resolve
		/// font names registered via fonts.AddFont() in MAUI applications.
		/// </summary>
		/// <remarks>
		/// The function takes a font name or alias as input and returns:
		/// - On Android: The filename (e.g., "MyFont.ttf")
		/// - On iOS/MacCatalyst: The PostScript name of the font
		/// - On Windows: The full path with family name (e.g., "ms-appx:///Fonts/MyFont.ttf#MyFont")
		/// - null if the font is not registered or cannot be resolved
		/// </remarks>
		public static Func<string, string?>? Resolver
		{
			get => Volatile.Read(ref s_resolver);
			set => Volatile.Write(ref s_resolver, value);
		}

		/// <summary>
		/// Tries to resolve a font alias to its actual font name or path.
		/// </summary>
		/// <param name="fontName">The font name or alias to resolve.</param>
		/// <returns>The resolved font name/path, or null if not resolvable.</returns>
		public static string? Resolve(string fontName)
		{
			var resolver = Resolver; // Single read for thread safety
			if (resolver is null)
			{
				return null;
			}

			try
			{
				return resolver(fontName);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"FontAliasResolver: Failed to resolve font '{fontName}': {ex.Message}");
				return null;
			}
		}
	}
}
