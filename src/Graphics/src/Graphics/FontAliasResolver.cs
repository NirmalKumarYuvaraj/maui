#nullable enable
using System;

namespace Microsoft.Maui.Graphics
{
	/// <summary>
	/// Provides a way to resolve font aliases to their actual font names or paths.
	/// This is used by MAUI to integrate registered fonts with the Graphics library.
	/// </summary>
	public static class FontAliasResolver
	{
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
		public static Func<string, string?>? Resolver { get; set; }

		/// <summary>
		/// Tries to resolve a font alias to its actual font name or path.
		/// </summary>
		/// <param name="fontName">The font name or alias to resolve.</param>
		/// <returns>The resolved font name/path, or null if not resolvable.</returns>
		public static string? Resolve(string fontName)
		{
			try
			{
				return Resolver?.Invoke(fontName);
			}
			catch
			{
				// Silently fail if resolution throws - fall back to original name
				return null;
			}
		}
	}
}
