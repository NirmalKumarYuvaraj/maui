using Microsoft.Maui.Graphics;

namespace GraphicsTester.Scenarios
{
    public class BlendModes : AbstractScenario
    {
        public BlendModes() : base(720, 1024)
        {
        }

        public override void Draw(ICanvas canvas)
        {
            DrawBlendModeGrid(canvas);
        }

        private static void DrawBlendModeGrid(ICanvas canvas)
        {
            // Define the blend modes to test
            var blendModes = new[]
            {
                (BlendMode.Normal, "Normal"),
                (BlendMode.Multiply, "Multiply"),
                (BlendMode.Screen, "Screen"),
                (BlendMode.Overlay, "Overlay"),
                (BlendMode.Darken, "Darken"),
                (BlendMode.Lighten, "Lighten"),
                (BlendMode.ColorDodge, "ColorDodge"),
                (BlendMode.ColorBurn, "ColorBurn"),
                (BlendMode.SoftLight, "SoftLight"),
                (BlendMode.HardLight, "HardLight"),
                (BlendMode.Difference, "Difference"),
                (BlendMode.Exclusion, "Exclusion"),
                (BlendMode.Hue, "Hue"),
                (BlendMode.Saturation, "Saturation"),
                (BlendMode.Color, "Color"),
                (BlendMode.Luminosity, "Luminosity"),
                (BlendMode.Clear, "Clear"),
                (BlendMode.Copy, "Copy"),
                (BlendMode.SourceIn, "SourceIn"),
                (BlendMode.SourceOut, "SourceOut"),
                (BlendMode.SourceAtop, "SourceAtop"),
                (BlendMode.DestinationOver, "DestinationOver"),
                (BlendMode.DestinationIn, "DestinationIn"),
                (BlendMode.DestinationOut, "DestinationOut"),
                (BlendMode.DestinationAtop, "DestinationAtop"),
                (BlendMode.Xor, "Xor"),
                (BlendMode.PlusDarker, "PlusDarker"),
                (BlendMode.PlusLighter, "PlusLighter")
            };

            const int cols = 4;
            const int rows = 7;
            const float boxWidth = 160;
            const float boxHeight = 120;
            const float padding = 20;

            canvas.FontSize = 10;
            canvas.FontColor = Colors.Black;

            for (int i = 0; i < blendModes.Length && i < cols * rows; i++)
            {
                int col = i % cols;
                int row = i / cols;

                float x = padding + col * (boxWidth + padding);
                float y = padding + row * (boxHeight + padding);

                DrawBlendModeExample(canvas, x, y, boxWidth, boxHeight, blendModes[i].Item1, blendModes[i].Item2);
            }
        }

        private static void DrawBlendModeExample(ICanvas canvas, float x, float y, float width, float height, BlendMode blendMode, string label)
        {
            // Draw background
            canvas.SaveState();
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(x, y, width, height);

            // Draw border
            canvas.StrokeColor = Colors.LightGray;
            canvas.StrokeSize = 1;
            canvas.DrawRectangle(x, y, width, height);

            // Draw label
            canvas.DrawString(label, x + 5, y + height - 15, HorizontalAlignment.Left);

            // Draw blend mode example
            var rectSize = Math.Min(width, height) * 0.4f;
            var centerX = x + width / 2;
            var centerY = y + height / 2 - 10;

            // Draw first rectangle (red)
            canvas.FillColor = Colors.Red;
            canvas.FillRectangle(centerX - rectSize / 2 - 10, centerY - rectSize / 2, rectSize, rectSize);

            // Draw second rectangle with blend mode (blue)
            canvas.BlendMode = blendMode;
            canvas.FillColor = Colors.Blue;
            canvas.FillRectangle(centerX - rectSize / 2 + 10, centerY - rectSize / 2, rectSize, rectSize);

            // Reset blend mode for next example
            canvas.BlendMode = BlendMode.Normal;
            canvas.RestoreState();
        }
    }
}