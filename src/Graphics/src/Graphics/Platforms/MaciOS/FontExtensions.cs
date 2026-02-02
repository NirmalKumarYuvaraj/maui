using CoreGraphics;
using CoreText;
using ObjCRuntime;
using System;
#if IOS || MACCATALYST || __IOS__
using PlatformFont = UIKit.UIFont;
#else
using PlatformFont = AppKit.NSFont;
#endif

namespace Microsoft.Maui.Graphics.Platform;

public static class FontExtensions
{
	public static CGFont ToCGFont(this IFont font)
	{
		if (string.IsNullOrEmpty(font?.Name))
		{
			return GetDefaultCGFont();
		}

		// Try to resolve font alias via FontAliasResolver (for MAUI registered fonts)
		var resolvedFontName = FontAliasResolver.Resolve(font.Name) ?? font.Name;
		return CGFont.CreateWithFontName(resolvedFontName) ?? GetDefaultCGFont();
	}

	public static CTFont ToCTFont(this IFont font, nfloat? size = null)
	{
		if (string.IsNullOrEmpty(font?.Name))
		{
			return GetDefaultCTFont(size);
		}

		// Try to resolve font alias via FontAliasResolver (for MAUI registered fonts)
		var resolvedFontName = FontAliasResolver.Resolve(font.Name) ?? font.Name;
		return new CTFont(resolvedFontName, size ?? PlatformFont.SystemFontSize, CTFontOptions.Default);
	}

	public static PlatformFont ToPlatformFont(this IFont font, nfloat? size = null)
	{
		// Try to resolve font alias via FontAliasResolver (for MAUI registered fonts)
		var resolvedFontName = font?.Name;
		if (!string.IsNullOrEmpty(resolvedFontName))
		{
			resolvedFontName = FontAliasResolver.Resolve(resolvedFontName) ?? resolvedFontName;
		}

#if IOS || MACCATALYST || __IOS__
		return PlatformFont.FromName(resolvedFontName ?? DefaultFontName, size ?? PlatformFont.SystemFontSize);
#else
			return PlatformFont.FromFontName(resolvedFontName ?? DefaultFontName, size ?? PlatformFont.SystemFontSize);
#endif
	}

	static string _defaultFontName;

	static string DefaultFontName
	{
		get
		{
			if (_defaultFontName == null)
			{
				using var defaultFont = PlatformFont.SystemFontOfSize(PlatformFont.SystemFontSize);
#if IOS || MACCATALYST || __IOS__
				_defaultFontName ??= defaultFont.Name;
#else
					_defaultFontName ??= defaultFont.FamilyName;
#endif
			}

			return _defaultFontName ?? string.Empty;
		}
	}

	internal static CGFont GetDefaultCGFont()
		=> CGFont.CreateWithFontName(DefaultFontName);

	internal static PlatformFont GetDefaultPlatformFont(nfloat? size = null)
		=> PlatformFont.SystemFontOfSize(size ?? PlatformFont.SystemFontSize);

	public static CTFont GetDefaultCTFont(nfloat? size = null)
		=> new CTFont(CTFontUIFontType.System, size ?? PlatformFont.SystemFontSize, Foundation.NSLocale.CurrentLocale.Identifier);

}