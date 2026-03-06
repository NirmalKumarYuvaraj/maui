#nullable disable
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Controls.Platform
{
	public static class LabelExtensions
	{
		public static void UpdateText(this UILabel platformLabel, Label label)
		{
			var text = TextTransformUtilities.GetTransformedText(label.Text, label.TextTransform);

			switch (label.TextType)
			{
				case TextType.Html:
					platformLabel.UpdateTextHtml(text);

					if (label.Handler is LabelHandler labelHandler)
						Label.MapFormatting(labelHandler, label);

					label.InvalidateMeasure();
					break;

				default:
					if (label.FormattedText != null)
						platformLabel.AttributedText = label.ToNSAttributedString();
					else
					{
						if (platformLabel.AttributedText is not null)
						{
							platformLabel.AttributedText = null;
						}

						platformLabel.Text = text;
					}
					break;
			}
		}
	}
}
