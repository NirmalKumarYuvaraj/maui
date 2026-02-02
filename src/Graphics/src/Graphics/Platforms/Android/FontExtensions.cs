using System;
using System.Collections.Generic;
using System.IO;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using AApplication = Android.App.Application;

namespace Microsoft.Maui.Graphics.Platform;

public static class FontExtensions
{
	public static Typeface ToTypeface(this IFont font)
	{
		TypefaceStyle GetStyle(IFont font)
		{
			if (font.Weight >= FontWeights.Bold)
			{
				return font.StyleType == FontStyleType.Normal ? TypefaceStyle.Bold : TypefaceStyle.BoldItalic;
			}
			else
			{
				return font.StyleType == FontStyleType.Normal ? TypefaceStyle.Normal : TypefaceStyle.Italic;
			}
		}

		if (font == null)
		{
			return Typeface.Default;
		}

		if (string.IsNullOrEmpty(font.Name))
		{
			return Typeface.DefaultFromStyle(GetStyle(font));
		}

		var context = AApplication.Context;
		Typeface typeface = null;

		// Try to resolve font alias via FontAliasResolver (for MAUI registered fonts)
		var resolvedFontName = FontAliasResolver.Resolve(font.Name) ?? font.Name;

		// Check if the resolved font name is a filesystem path (embedded fonts)
		// Filesystem paths start with '/' on Android
		if (resolvedFontName.StartsWith("/", StringComparison.Ordinal) && File.Exists(resolvedFontName))
		{
			// Load font from filesystem path (for embedded fonts extracted to cache)
			typeface = Typeface.CreateFromFile(resolvedFontName);
			if (typeface != null)
			{
				return typeface;
			}
		}

		// Fonts can be resources in API 26+
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			var id = context.Resources.GetIdentifier(resolvedFontName, "font", context.PackageName);

			if (id > 0)
			{
				typeface = context.Resources?.GetFont(id);
			}
		}

		// Next, we can try to load from an asset
		if (typeface == null)
		{
			// First check filename as is (using resolved name)
			if (!TryLoadTypefaceFromAsset(resolvedFontName, out typeface))
			{
				var sepChar = Java.IO.File.PathSeparatorChar;

				// Also try any *fonts*/ subfolders
				foreach (var a in context.Assets.List(""))
				{
					var file = new Java.IO.File(a);
					if (file.IsDirectory && file.Name.Contains("fonts", StringComparison.InvariantCultureIgnoreCase))
					{
						if (TryLoadTypefaceFromAsset(file.AbsolutePath.TrimEnd(sepChar) + resolvedFontName.TrimStart(sepChar), out typeface))
						{
							break;
						}
					}
				}
			}
		}

		if (typeface == null)
		{
			return Typeface.Create(resolvedFontName, GetStyle(font));
		}

		return typeface;
	}

	static bool TryLoadTypefaceFromAsset(string filename, out Typeface typeface)
	{
		try
		{
			typeface = Typeface.CreateFromAsset(AApplication.Context.Assets, filename);

			return typeface != null;
		}
		catch
		{
			typeface = null;
		}

		return false;
	}
}
