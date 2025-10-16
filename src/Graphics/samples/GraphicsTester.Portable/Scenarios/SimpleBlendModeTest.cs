using Microsoft.Maui.Graphics;

namespace GraphicsTester.Scenarios
{
    public class SimpleBlendModeTest : AbstractScenario
    {
        public SimpleBlendModeTest() : base(400, 300)
        {
        }

        public override void Draw(ICanvas canvas)
        {
            // Clear background
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(0, 0, Width, Height);

            // Test basic blend modes
            TestBlendMode(canvas, 50, 50, BlendMode.Normal, "Normal");
            TestBlendMode(canvas, 200, 50, BlendMode.Multiply, "Multiply");
            TestBlendMode(canvas, 50, 150, BlendMode.Screen, "Screen");
            TestBlendMode(canvas, 200, 150, BlendMode.Overlay, "Overlay");
        }

        private void TestBlendMode(ICanvas canvas, float x, float y, BlendMode blendMode, string label)
        {
            // Draw first circle (red)
            canvas.FillColor = Colors.Red;
            canvas.FillEllipse(x, y, 60, 60);

            // Draw second circle with blend mode (blue)
            canvas.BlendMode = blendMode;
            canvas.FillColor = Colors.Blue;
            canvas.FillEllipse(x + 30, y + 30, 60, 60);

            // Reset blend mode and draw label
            canvas.BlendMode = BlendMode.Normal;
            canvas.FontColor = Colors.Black;
            canvas.FontSize = 12;
            canvas.DrawString(label, x, y + 120, HorizontalAlignment.Left);
        }
    }
}