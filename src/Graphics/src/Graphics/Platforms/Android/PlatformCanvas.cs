using System;
using System.Numerics;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Microsoft.Maui.Graphics.Platform.Text;
using Microsoft.Maui.Graphics.Text;
using APaint = Android.Graphics.Paint;

namespace Microsoft.Maui.Graphics.Platform
{
	public class PlatformCanvas : AbstractCanvas<PlatformCanvasState>, IBlurrableCanvas
	{
		private Canvas _canvas;
		private PlatformBlendRenderer _blendRenderer;
		private Shader _shader;

		private readonly Matrix _shaderMatrix = new Matrix();
		private readonly PlatformCanvasStateService _stateService;

		public PlatformCanvas(Context context = null)
			: base(CreateStateService(out var stateService), new PlatformStringSizeService())
		{
			_stateService = stateService;
			DisplayScale = (context ?? global::Android.App.Application.Context)?.Resources?.DisplayMetrics?.Density ?? 1;
		}

		static PlatformCanvasStateService CreateStateService(out PlatformCanvasStateService stateService) =>
			stateService = new PlatformCanvasStateService();

		public override void Dispose()
		{
			_blendRenderer?.Dispose();
			_blendRenderer = null;
			_stateService.Dispose();
			base.Dispose();
		}

		public Canvas Canvas
		{
			get => _canvas;
			set
			{
				_blendRenderer?.Dispose();
				_blendRenderer = null;
				_canvas = null;
				ResetState();
				_canvas = value;
				if (value != null)
					_blendRenderer = new PlatformBlendRenderer(value);
			}
		}

		internal void SetBitmap(Bitmap bitmap)
		{
			_blendRenderer?.Dispose();
			_blendRenderer = new PlatformBlendRenderer(_canvas, bitmap);
		}

		internal void BeginDraw(Canvas canvas, int width, int height)
		{
			Canvas = canvas;
			_blendRenderer.Dispose();
			_blendRenderer = new PlatformBlendRenderer(canvas, deferred: true, width: width, height: height);
		}

		public override bool Antialias
		{
			set => CurrentState.AntiAlias = value;
		}

		protected override float PlatformStrokeSize
		{
			set => CurrentState.PlatformStrokeSize = value;
		}

		public override float MiterLimit
		{
			set => CurrentState.MiterLimit = value;
		}

		public override float Alpha
		{
			set => CurrentState.Alpha = value;
		}

		public override LineCap StrokeLineCap
		{
			set => CurrentState.StrokeLineCap = value;
		}

		public override LineJoin StrokeLineJoin
		{
			set => CurrentState.StrokeLineJoin = value;
		}

		public override Color StrokeColor
		{
			set => CurrentState.StrokeColor = value ?? Colors.Black;
		}

		public override Color FontColor
		{
			set => CurrentState.FontColor = value ?? Colors.Black;
		}

		public override IFont Font
		{
			set => CurrentState.Font = value;
		}

		public override float FontSize
		{
			set => CurrentState.FontSize = value;
		}

		public override Color FillColor
		{
			set => CurrentState.FillColor = value ?? Colors.White;
		}

		public override BlendMode BlendMode
		{
			set
			{
				if ((uint)value > (uint)BlendMode.PlusLighter)
					throw new ArgumentOutOfRangeException(nameof(value));
				CurrentState.BlendMode = value;
			}
		}

		void DrawLayout(StaticLayout layout)
		{
			_blendRenderer.Draw(canvas => layout.Draw(canvas), CurrentState.BlendMode, canvas =>
			{
				var paint = layout.Paint;
				using var previous = paint.ColorFilter;
				using var coverage = new ColorMatrixColorFilter(new float[]
				{
					0, 0, 0, 0, 255,
					0, 0, 0, 0, 255,
					0, 0, 0, 0, 255,
					0, 0, 0, 0, 255
				});
				paint.SetColorFilter(coverage);
				try
				{
					layout.Draw(canvas);
				}
				finally
				{
					paint.SetColorFilter(previous);
				}
			});
		}

		void Draw(Action<Canvas, APaint> draw, APaint paint, Action<Canvas, APaint> drawCoverage = null)
		{
			_blendRenderer.Draw(canvas => draw(canvas, paint), CurrentState.BlendMode, canvas =>
			{
				using var coveragePaint = new APaint(paint);
				coveragePaint.Color = global::Android.Graphics.Color.White;
				coveragePaint.SetShader(null);
				coveragePaint.SetColorFilter(null);
				PlatformBlendModes.Apply(coveragePaint, BlendMode.Normal);
				(drawCoverage ?? draw)(canvas, coveragePaint);
			});
		}

		void ChangeCanvasState(Action<Canvas> change) => _blendRenderer.ChangeState(change);

		protected override void PlatformSetStrokeDashPattern(float[] strokePattern, float strokeDashOffset, float strokeSize)
		{
			CurrentState.SetStrokeDashPattern(strokePattern, strokeDashOffset, strokeSize);
		}

		public override void SetFillPaint(Paint paint, RectF rectangle)
		{
			if (paint == null)
				paint = Colors.White.AsPaint();

			if (_shader != null)
			{
				CurrentState.SetFillPaintShader(null);
				_shader.Dispose();
				_shader = null;
			}

			if (paint is SolidPaint solidPaint)
			{
				FillColor = solidPaint.Color;
			}
			else if (paint is LinearGradientPaint linearGradientPaint)
			{
				var colors = new int[linearGradientPaint.GradientStops.Length];
				var stops = new float[colors.Length];

				PaintGradientStop[] vStops = linearGradientPaint.GetSortedStops();

				for (int i = 0; i < vStops.Length; i++)
				{
					colors[i] = vStops[i].Color.MultiplyAlpha(CurrentState.Alpha).ToInt();
					stops[i] = vStops[i].Offset;
				}

				try
				{
					CurrentState.FillColor = Colors.White;

					float x1 = (float)(linearGradientPaint.StartPoint.X * rectangle.Width) + rectangle.X;
					float y1 = (float)(linearGradientPaint.StartPoint.Y * rectangle.Height) + rectangle.Y;

					float x2 = (float)(linearGradientPaint.EndPoint.X * rectangle.Width) + rectangle.X;
					float y2 = (float)(linearGradientPaint.EndPoint.Y * rectangle.Height) + rectangle.Y;

					_shader = new LinearGradient(x1, y1, x2, y2, colors, stops, Shader.TileMode.Clamp);
					CurrentState.SetFillPaintShader(_shader);
				}
				catch (Exception exc)
				{
					System.Diagnostics.Debug.WriteLine(exc);
					FillColor = linearGradientPaint.BlendStartAndEndColors();
				}
			}
			else if (paint is RadialGradientPaint radialGradientPaint)
			{
				var colors = new int[radialGradientPaint.GradientStops.Length];
				var stops = new float[colors.Length];

				PaintGradientStop[] vStops = radialGradientPaint.GetSortedStops();

				for (int i = 0; i < vStops.Length; i++)
				{
					colors[i] = vStops[i].Color.MultiplyAlpha(CurrentState.Alpha).ToInt();
					stops[i] = vStops[i].Offset;
				}

				float centerX = (float)(radialGradientPaint.Center.X * rectangle.Width) + rectangle.X;
				float centerY = (float)(radialGradientPaint.Center.Y * rectangle.Height) + rectangle.Y;
				float radius = (float)radialGradientPaint.Radius * Math.Max(rectangle.Height, rectangle.Width);

				if (radius == 0)
					radius = GeometryUtil.GetDistance(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);

				try
				{
					CurrentState.FillColor = Colors.White;
					_shader = new RadialGradient(centerX, centerY, radius, colors, stops, Shader.TileMode.Clamp);
					CurrentState.SetFillPaintShader(_shader);
				}
				catch (Exception exc)
				{
					System.Diagnostics.Debug.WriteLine(exc);
					FillColor = radialGradientPaint.BlendStartAndEndColors();
				}
			}
			else if (paint is PatternPaint patternPaint)
			{
				var bitmap = patternPaint.GetPatternBitmap(DisplayScale);

				if (bitmap != null)
				{
					try
					{
						CurrentState.FillColor = Colors.White;
						CurrentState.SetFillPaintFilterBitmap(true);

						_shader = new BitmapShader(bitmap, Shader.TileMode.Repeat, Shader.TileMode.Repeat);
						_shaderMatrix.Reset();
						_shaderMatrix.PreScale(CurrentState.ScaleX, CurrentState.ScaleY);
						_shader.SetLocalMatrix(_shaderMatrix);

						CurrentState.SetFillPaintShader(_shader);
					}
					catch (Exception exc)
					{
						System.Diagnostics.Debug.WriteLine(exc);
						FillColor = patternPaint.BackgroundColor;
					}
				}
				else
				{
					FillColor = patternPaint.BackgroundColor;
				}
			}
			else if (paint is ImagePaint imagePaint)
			{
				if (imagePaint.Image is PlatformImage image)
				{
					var bitmap = image.PlatformRepresentation;

					if (bitmap != null)
					{
						try
						{
							CurrentState.FillColor = Colors.White;
							CurrentState.SetFillPaintFilterBitmap(true);

							_shader = new BitmapShader(bitmap, Shader.TileMode.Repeat, Shader.TileMode.Repeat);
							_shaderMatrix.Reset();
							_shaderMatrix.PreScale(CurrentState.ScaleX, CurrentState.ScaleY);
							_shader.SetLocalMatrix(_shaderMatrix);

							CurrentState.SetFillPaintShader(_shader);
						}
						catch (Exception exc)
						{
							System.Diagnostics.Debug.WriteLine(exc);
							FillColor = paint.BackgroundColor;
						}
					}
					else
						FillColor = Colors.White;
				}
				else
				{
					FillColor = Colors.White;
				}
			}
			else
				FillColor = paint.BackgroundColor;

			//System.Diagnostics.Debug.WriteLine("Gradient Set To: "+aPaint.PaintType);
		}

		protected override void PlatformDrawLine(float x1, float y1, float x2, float y2)
		{
			//canvas.DrawLine (x1, y1, x2, y2, CurrentState.StrokePaintWithAlpha);

			var platformPath = new Path();
			platformPath.MoveTo(x1, y1);
			platformPath.LineTo(x2, y2);
			Draw((canvas, paint) => canvas.DrawPath(platformPath, paint), CurrentState.StrokePaintWithAlpha);
			platformPath.Dispose();
		}

		protected override void PlatformDrawArc(
			float x,
			float y,
			float width,
			float height,
			float startAngle,
			float endAngle,
			bool clockwise,
			bool closed)
		{
			while (startAngle < 0)
			{
				startAngle += 360;
			}

			while (endAngle < 0)
			{
				endAngle += 360;
			}

			var rectX = x;
			var rectY = y;
			var rectWidth = width;
			var rectHeight = height;

			float sweep = GeometryUtil.GetSweep(startAngle, endAngle, clockwise);

			var rect = new global::Android.Graphics.RectF(rectX, rectY, rectX + rectWidth, rectY + rectHeight);

			startAngle *= -1;
			if (!clockwise)
			{
				sweep *= -1;
			}

			if (closed)
			{
				var platformPath = new Path();
				platformPath.AddArc(rect, startAngle, sweep);
				platformPath.Close();
				Draw((canvas, paint) => canvas.DrawPath(platformPath, paint), CurrentState.StrokePaintWithAlpha);
				platformPath.Dispose();
			}
			else
			{
				Draw((canvas, paint) => canvas.DrawArc(rect, startAngle, sweep, false, paint), CurrentState.StrokePaintWithAlpha);
			}

			rect.Dispose();
		}

		public override void FillArc(
			float x,
			float y,
			float width,
			float height,
			float startAngle,
			float endAngle,
			bool clockwise)
		{
			while (startAngle < 0)
			{
				startAngle += 360;
			}

			while (endAngle < 0)
			{
				endAngle += 360;
			}

			var sweep = GeometryUtil.GetSweep(startAngle, endAngle, clockwise);
			var rect = new global::Android.Graphics.RectF(x, y, x + width, y + height);

			startAngle *= -1;
			if (!clockwise)
			{
				sweep *= -1;
			}

			Draw((canvas, paint) => canvas.DrawArc(rect, startAngle, sweep, false, paint), CurrentState.FillPaintWithAlpha);
			rect.Dispose();
		}

		protected override void PlatformDrawRectangle(float x, float y, float width, float height)
		{
			float rectX = 0, rectY = 0, rectWidth = 0, rectHeight = 0;

			float strokeSize = CurrentState.ScaledStrokeSize;
			if (strokeSize == 0)
				return;

			rectX = x;
			rectY = y;
			rectWidth = width;
			rectHeight = height;

			Draw((canvas, paint) => canvas.DrawRect(rectX, rectY, rectX + rectWidth, rectY + rectHeight, paint), CurrentState.StrokePaintWithAlpha);
		}

		public override void FillRectangle(float x, float y, float width, float height)
		{
			var rectX = x;
			var rectY = y;
			var rectWidth = width;
			var rectHeight = height;

			Draw((canvas, paint) => canvas.DrawRect(rectX, rectY, rectX + rectWidth, rectY + rectHeight, paint), CurrentState.FillPaintWithAlpha);
		}

		protected override void PlatformDrawRoundedRectangle(
			float x,
			float y,
			float width,
			float height,
			float aCornerRadius)
		{
			// These values work for a stroke location of center.
			float strokeSize = CurrentState.ScaledStrokeSize;

			var rectX = x;
			var rectY = y;
			var rectWidth = width;
			var rectHeight = height;
			var radius = aCornerRadius;

			var rect = new global::Android.Graphics.RectF(rectX, rectY, rectX + rectWidth, rectY + rectHeight);
			Draw((canvas, paint) => canvas.DrawRoundRect(rect, radius, radius, paint), CurrentState.StrokePaintWithAlpha);
			rect.Dispose();
		}

		public override void FillRoundedRectangle(float x, float y, float width, float height, float aCornerRadius)
		{
			var rectX = x;
			var rectY = y;
			var rectWidth = width;
			var rectHeight = height;
			var radius = aCornerRadius;

			var rect = new global::Android.Graphics.RectF(rectX, rectY, rectX + rectWidth, rectY + rectHeight);
			Draw((canvas, paint) => canvas.DrawRoundRect(rect, radius, radius, paint), CurrentState.FillPaintWithAlpha);
			rect.Dispose();
		}

		protected override void PlatformDrawEllipse(float x, float y, float width, float height)
		{
			// These values work for a stroke location of center.
			float strokeSize = CurrentState.ScaledStrokeSize;

			var rectX = x;
			var rectY = y;
			var rectWidth = width;
			var rectHeight = height;

			var rect = new global::Android.Graphics.RectF(rectX, rectY, rectX + rectWidth, rectY + rectHeight);
			Draw((canvas, paint) => canvas.DrawOval(rect, paint), CurrentState.StrokePaintWithAlpha);
			rect.Dispose();
		}

		public override void FillEllipse(float x, float y, float width, float height)
		{
			/* todo: support gradients here */

			// These values work for a stroke location of center.
			var rectX = x;
			var rectY = y;
			var rectWidth = width;
			var rectHeight = height;

			var rect = new global::Android.Graphics.RectF(rectX, rectY, rectX + rectWidth, rectY + rectHeight);
			Draw((canvas, paint) => canvas.DrawOval(rect, paint), CurrentState.FillPaintWithAlpha);
			rect.Dispose();
		}

		public override void SubtractFromClip(float x, float y, float width, float height)
		{
#pragma warning disable CS0618, CA1416, CA1422 // clipRect(Region.Op) method deprecated in API 26 https://developer.android.com/reference/android/graphics/Canvas#clipRect(float,%20float,%20float,%20float,%20android.graphics.Region.Op)
			ChangeCanvasState(canvas => canvas.ClipRect(x, y, x + width, y + height, Region.Op.Difference));
#pragma warning restore CS0618, CA1416, CA1422 // clipRect(Region.Op) method deprecated
		}

		protected override void PlatformDrawPath(PathF aPath)
		{
			var platformPath = aPath.AsAndroidPath();

			if (_shader != null)
			{
				// Mirror iOS ReplacePathWithStrokedPath: convert stroke geometry into a fill path
				using var strokedOutline = new Path();
				CurrentState.StrokePaintWithAlpha.GetFillPath(platformPath, strokedOutline);
				// FillPaintWithAlpha already has the gradient shader from SetFillPaint
				Draw((canvas, paint) => canvas.DrawPath(strokedOutline, paint), CurrentState.FillPaintWithAlpha);
			}
			else
			{
				Draw((canvas, paint) => canvas.DrawPath(platformPath, paint), CurrentState.StrokePaintWithAlpha);
			}

			platformPath.Dispose();
		}

		public override void ClipPath(PathF path, WindingMode windingMode = WindingMode.NonZero)
		{
			var platformPath = path.AsAndroidPath();
			platformPath.SetFillType(windingMode == WindingMode.NonZero ? Path.FillType.Winding : Path.FillType.EvenOdd);
			_blendRenderer.ClipPath(platformPath);
			platformPath.Dispose();
		}

		public override void ClipRectangle(float x, float y, float width, float height)
		{
			ChangeCanvasState(canvas => canvas.ClipRect(x, y, x + width, y + height));
		}

		public override void FillPath(PathF path, WindingMode windingMode)
		{
			var platformPath = path.AsAndroidPath();
			platformPath.SetFillType(windingMode == WindingMode.NonZero ? Path.FillType.Winding : Path.FillType.EvenOdd);
			Draw((canvas, paint) => canvas.DrawPath(platformPath, paint), CurrentState.FillPaintWithAlpha);
			platformPath.Dispose();
		}

		public override void DrawString(string value, float x, float y, HorizontalAlignment horizAlignment)
		{
			if (horizAlignment == HorizontalAlignment.Left)
				DrawString(value, x, y);
			else if (horizAlignment == HorizontalAlignment.Right)
			{
				SizeF vSize = GetStringSize(
					value,
					CurrentState.Font,
					CurrentState.ScaledFontSize);
				x -= vSize.Width;
				DrawString(value, x, y);
			}
			else
			{
				SizeF vSize = GetStringSize(
					value,
					CurrentState.Font,
					CurrentState.ScaledFontSize);
				x -= vSize.Width / 2f;
				DrawString(value, x, y);
			}
		}

		private void DrawString(string value, float x, float y)
		{
			if (value == null)
				return;

			ChangeCanvasState(canvas => canvas.Save());
			float top = y - CurrentState.ScaledFontSize;
			ChangeCanvasState(canvas => canvas.Translate(x, top));
			StaticLayout layout = TextLayoutUtils.CreateLayout(value, CurrentState.FontPaint, 512, Layout.Alignment.AlignNormal);
			DrawLayout(layout);
			layout.Dispose();
			ChangeCanvasState(canvas => canvas.Restore());
		}

		public override void DrawString(
			string value,
			float x,
			float y,
			float width,
			float height,
			HorizontalAlignment horizAlignment,
			VerticalAlignment vertAlignment,
			TextFlow textFlow = TextFlow.ClipBounds,
			float lineSpacingAdjustment = 0)
		{
			if (value == null || value.Length == 0 || width == 0 || height == 0)
				return;

			ChangeCanvasState(canvas => canvas.Save());

			var alignment = Layout.Alignment.AlignNormal;
			if (horizAlignment == HorizontalAlignment.Center)
			{
				alignment = Layout.Alignment.AlignCenter;
			}
			else if (horizAlignment == HorizontalAlignment.Right)
			{
				alignment = Layout.Alignment.AlignOpposite;
			}

			var layout = TextLayoutUtils.CreateLayout(value, CurrentState.FontPaint, (int)width, alignment);
			var offset = layout.GetOffsetsToDrawText(x, y, width, height, horizAlignment, vertAlignment);
			ChangeCanvasState(canvas => canvas.Translate(offset.Width, offset.Height));
			DrawLayout(layout);
			layout.Dispose();
			ChangeCanvasState(canvas => canvas.Restore());
		}

		public override void DrawText(IAttributedText value, float x, float y, float width, float height)
		{
			ChangeCanvasState(canvas => canvas.Save());
			var span = value.AsSpannableString();
			var layout = TextLayoutUtils.CreateLayoutForSpannedString(span, CurrentState.FontPaint, (int)width, Layout.Alignment.AlignNormal);
			var offset = layout.GetOffsetsToDrawText(x, y, width, height, HorizontalAlignment.Left, VerticalAlignment.Top);
			ChangeCanvasState(canvas => canvas.Translate(offset.Width, offset.Height));
			DrawLayout(layout);
			layout.Dispose();
			span.Dispose();
			ChangeCanvasState(canvas => canvas.Restore());
		}

		public override void ResetState()
		{
			base.ResetState();

			_shader?.Dispose();
			_shader = null;

			_stateService.Reset(CurrentState);
		}

		public override bool RestoreState()
		{
			_shader?.Dispose();
			_shader = null;

			return base.RestoreState();
		}

		protected override void StateRestored(PlatformCanvasState state)
		{
			_blendRenderer?.ChangeState(canvas => canvas.Restore());
		}

		public override void SetShadow(SizeF offset, float blur, Color color)
		{
			SizeF actualOffset = offset;
			var sx = actualOffset.Width;
			var sy = actualOffset.Height;

			if (color == null)
			{
				var actualColor = Colors.Black.AsColorMultiplyAlpha(CurrentState.Alpha);
				CurrentState.SetShadow(blur, sx, sy, actualColor);
			}
			else
			{
				var actualColor = color.AsColorMultiplyAlpha(CurrentState.Alpha);
				CurrentState.SetShadow(blur, sx, sy, actualColor);
			}
		}

		protected override void PlatformRotate(float degrees, float radians, float x, float y)
		{
			ChangeCanvasState(canvas => canvas.Rotate(degrees, x, y));
		}

		protected override void PlatformRotate(float degrees, float radians)
		{
			ChangeCanvasState(canvas => canvas.Rotate(degrees));
		}

		protected override void PlatformScale(float xFactor, float yFactor)
		{
			CurrentState.SetScale(Math.Abs(xFactor), Math.Abs(yFactor));
			if (xFactor < 0 || yFactor < 0)
				ChangeCanvasState(canvas => canvas.Scale(xFactor < 0 ? -1 : 1, yFactor < 0 ? -1 : 1));
		}

		protected override void PlatformTranslate(float tx, float ty)
		{
			float x = tx * CurrentState.ScaleX;
			float y = ty * CurrentState.ScaleY;
			ChangeCanvasState(canvas => canvas.Translate(x, y));
		}

		protected override void PlatformConcatenateTransform(Matrix3x2 transform)
		{
			ChangeCanvasState(canvas =>
			{
#pragma warning disable CS0618 // getMatrix() method deprecated in API 16 https://developer.android.com/reference/android/graphics/Canvas#getMatrix()
				using var matrix = new Matrix(canvas.Matrix);
#pragma warning restore CS0618
				using var addition = transform.AsMatrix();
				matrix.PostConcat(addition);
				canvas.Matrix = matrix;
			});
		}

		public override void SaveState()
		{
			ChangeCanvasState(canvas => canvas.Save());
			base.SaveState();
		}

		public void SetBlur(float radius)
		{
			CurrentState.SetBlur(radius);
		}

		public override void DrawImage(IImage image, float x, float y, float width, float height)
		{
			var mdimage = image.ToPlatformImage() as PlatformImage;

			if (mdimage != null)
			{
				var bitmap = mdimage.PlatformRepresentation;
				if (bitmap != null)
				{
					var scaleX = CurrentState.ScaleX < 0 ? -1 : 1;
					var scaleY = CurrentState.ScaleY < 0 ? -1 : 1;

					ChangeCanvasState(canvas => canvas.Save());
					//canvas.Scale (scaleX, scaleY);
					var srcRect = new global::Android.Graphics.Rect(0, 0, bitmap.Width, bitmap.Height);

					x *= scaleX;
					y *= scaleY;
					width *= scaleX;
					height *= scaleY;

					var rx1 = Math.Min(x, x + width);
					var ry1 = Math.Min(y, y + height);
					var rx2 = Math.Max(x, x + width);
					var ry2 = Math.Max(y, y + height);

					var destRect = new global::Android.Graphics.RectF(rx1, ry1, rx2, ry2);
					var paint = CurrentState.GetImagePaint(1, 1);
					Draw((canvas, imagePaint) => canvas.DrawBitmap(bitmap, srcRect, destRect, imagePaint), paint,
						(canvas, coveragePaint) => canvas.DrawRect(destRect, coveragePaint));
					paint?.Dispose();
					srcRect.Dispose();
					destRect.Dispose();
					ChangeCanvasState(canvas => canvas.Restore());
				}
			}
		}
	}
}
