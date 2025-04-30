using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Numerics;
using WRect = Windows.Foundation.Rect;
#if NETFX_CORE
using WColors = global::Windows.UI.Colors;
#else
using WColors = global::Microsoft.UI.Colors;
#endif

#if MAUI_GRAPHICS_WIN2D
namespace Microsoft.Maui.Graphics.Win2D
#else
namespace Microsoft.Maui.Graphics.Platform
#endif
{
	/// <summary>
	/// A Windows platform implementation of <see cref="CanvasState"/>.
	/// </summary>
#if MAUI_GRAPHICS_WIN2D
	public class W2DCanvasState
#else
	public class PlatformCanvasState
#endif
		: CanvasState
	{
		static readonly float[] _emptyFloatArray = Array.Empty<float>();
		readonly PlatformCanvas _owner;
		readonly PlatformCanvasState _parentState;
		float _alpha = 1;
		float[] _dashes;
		float _dashOffset;
		ICanvasBrush _fillBrush;
		bool _fillBrushValid;
		CanvasSolidColorBrush _fontBrush;
		bool _fontBrushValid;
		float _fontSize;
		Vector2 _linearGradientStartPoint;
		Vector2 _linearGradientEndPoint;
		Vector2 _radialGradientCenter;
		float _radialGradientRadius;
		//private GradientStopCollection _gradientStopCollection;
		CanvasGeometry _layerBounds;
		CanvasGeometry _layerClipBounds;
		CanvasGeometry _layerMask;
		CanvasActiveLayer _layer;
		bool _needsStrokeStyle;
		float _scale;
		global::Windows.UI.Color _shadowColor;
		bool _shadowColorValid;
		Color _sourceFillColor;
		Paint _sourceFillpaint;
		Paint _sourceStrokepaint;
		Color _sourceFontColor;
		Color _sourceShadowColor;
		Color _sourceStrokeColor;
		ICanvasBrush _strokeBrush;
		bool _strokeBrushValid;
		CanvasStrokeStyle _strokeStyle;
		float _miterLimit;
		CanvasCapStyle _lineCap;
		CanvasLineJoin _lineJoin;
		//private CanvasStrokeStyleProperties strokeStyleProperties;
		int _layerCount = 0;
		readonly float _dpi = 96;

		public IFont Font { get; set; }

		public float BlurRadius { get; private set; }
		public bool IsShadowed { get; private set; }
		public bool IsBlurred { get; private set; }
		public Vector2 ShadowOffset { get; private set; }
		public float ShadowBlur { get; set; }
		public Matrix3x2 Matrix { get; private set; }

#if MAUI_GRAPHICS_WIN2D
		public W2DCanvasState(
#else
		public PlatformCanvasState(
#endif
			PlatformCanvas owner)
		{
			_owner = owner;
			_parentState = null;
			SetToDefaults();
		}

#if MAUI_GRAPHICS_WIN2D
		public W2DCanvasState(
#else
		public PlatformCanvasState(
#endif
			PlatformCanvasState prototype) : base(prototype)
		{
			_dpi = prototype.Dpi;
			_owner = prototype._owner;
			_parentState = prototype;

			_strokeBrush = prototype._strokeBrush;
			_fillBrush = prototype._fillBrush;
			_fontBrush = prototype._fontBrush;
			_shadowColor = prototype._shadowColor;

			_sourceStrokeColor = prototype._sourceStrokeColor;
			_sourceFillpaint = prototype._sourceFillpaint;
			_sourceFillColor = prototype._sourceFillColor;
			_sourceFontColor = prototype._sourceFontColor;
			_sourceShadowColor = prototype._sourceShadowColor;

			_strokeBrushValid = prototype._strokeBrushValid;
			_fillBrushValid = prototype._fillBrushValid;
			_fontBrushValid = prototype._fontBrushValid;
			_shadowColorValid = prototype._shadowColorValid;

			_dashes = prototype._dashes;
			_dashOffset = prototype._dashOffset;
			_strokeStyle = prototype._strokeStyle;
			_lineJoin = prototype._lineJoin;
			_lineCap = prototype._lineCap;
			_miterLimit = prototype._miterLimit;
			_needsStrokeStyle = prototype._needsStrokeStyle;

			IsShadowed = prototype.IsShadowed;
			ShadowOffset = prototype.ShadowOffset;
			ShadowBlur = prototype.ShadowBlur;

			Matrix = new Matrix3x2(prototype.Matrix.M11, prototype.Matrix.M12, prototype.Matrix.M21, prototype.Matrix.M22, prototype.Matrix.M31, prototype.Matrix.M32);

			Font = prototype.Font;
			FontSize = prototype.FontSize;

			_alpha = prototype._alpha;
			_scale = prototype._scale;

			IsBlurred = prototype.IsBlurred;
			BlurRadius = prototype.BlurRadius;
		}

		public void SetToDefaults()
		{
			_sourceStrokeColor = Colors.Black;
			_strokeBrushValid = false;
			_needsStrokeStyle = false;
			_strokeStyle = null;

			_dashes = null;
			_dashOffset = 1;
			_miterLimit = CanvasDefaults.DefaultMiterLimit;
			_lineCap = CanvasCapStyle.Flat;
			_lineJoin = CanvasLineJoin.Miter;

			_sourceFillpaint = Colors.White.AsPaint();
			_fillBrushValid = false;

			Matrix = Matrix3x2.Identity;

			IsShadowed = false;
			_sourceShadowColor = CanvasDefaults.DefaultShadowColor;

			Font = null;
			FontSize = 12;

			_sourceFontColor = Colors.Black;
			_fontBrushValid = false;

			_alpha = 1;
			_scale = 1;

			IsBlurred = false;
			BlurRadius = 0;
		}

		public float FontSize
		{
			get => _fontSize;
			set => _fontSize = value;
		}

		public float Dpi => _dpi;

		public float Alpha
		{
			get => _alpha;

			set
			{
				if (_alpha != value)
				{
					_alpha = value;
					InvalidateBrushes();
				}
			}
		}

		public Color StrokeColor
		{
			set
			{
				var finalValue = value ?? Colors.Black;

				if (!finalValue.Equals(_sourceStrokeColor))
				{
					ReleaseStrokeBrush();
					_sourceStrokeColor = finalValue;
					_strokeBrushValid = false;
					_sourceStrokepaint = null;
				}
			}
		}

#pragma warning disable RS0016

		public Paint Stroke
		{
			set
			{
				var finalValue = value;

				if (!finalValue.Equals(_sourceStrokepaint))
				{
					_sourceStrokepaint = finalValue;
					_strokeBrushValid = true;
				}
			}
		}

		public float MiterLimit
		{
			set
			{
				_miterLimit = value;
				InvalidateStrokeStyle();
				_needsStrokeStyle = true;
			}
		}

		public LineCap StrokeLineCap
		{
			set
			{
				switch (value)
				{
					case LineCap.Butt:
						_lineCap = CanvasCapStyle.Flat;
						break;
					case LineCap.Round:
						_lineCap = CanvasCapStyle.Round;
						break;
					default:
						_lineCap = CanvasCapStyle.Square;
						break;
				}

				InvalidateStrokeStyle();
				_needsStrokeStyle = true;
			}
		}

		public LineJoin StrokeLineJoin
		{
			set
			{
				switch (value)
				{
					case LineJoin.Bevel:
						_lineJoin = CanvasLineJoin.Bevel;
						break;
					case LineJoin.Round:
						_lineJoin = CanvasLineJoin.Round;
						break;
					default:
						_lineJoin = CanvasLineJoin.Miter;
						break;
				}

				InvalidateStrokeStyle();
				_needsStrokeStyle = true;
			}
		}

		public void SetStrokeDashPattern(float[] pattern, float strokeDashOffset, float strokeSize)
		{
			if (pattern is null || pattern.Length == 0)
			{
				if (_needsStrokeStyle == false)
				{
					return;
				}
				_dashes = null;
			}
			else
			{
				_dashes = pattern;
			}

			_dashOffset = strokeDashOffset;

			InvalidateStrokeStyle();
			_needsStrokeStyle = true;
		}

		public Color FillColor
		{
			set
			{
				ReleaseFillBrush();
				_fillBrushValid = false;
				_sourceFillColor = value;
				_sourceFillpaint = null;
			}
		}

		public void SetLinearGradient(Paint aPaint, Vector2 startPoint, Vector2 endPoint)
		{
			ReleaseFillBrush();
			_fillBrushValid = false;
			_sourceFillColor = null;
			_sourceFillpaint = aPaint;
			_linearGradientStartPoint = startPoint;
			_linearGradientEndPoint = endPoint;
		}

		public void SetRadialGradient(Paint aPaint, Vector2 center, float radius)
		{
			ReleaseFillBrush();
			_fillBrushValid = false;
			_sourceFillColor = null;
			_sourceFillpaint = aPaint;
			_radialGradientCenter = center;
			_radialGradientRadius = radius;
		}

		public void SetBitmapBrush(CanvasImageBrush bitmapBrush)
		{
			_fillBrush = bitmapBrush;
			_fillBrushValid = true;
		}

		public ICanvasBrush PlatformFillBrush
		{
			get
			{
				if (_fillBrush is null || !_fillBrushValid)
				{
					if (_sourceFillColor is not null)
					{
						_fillBrush = new CanvasSolidColorBrush(_owner.Session, _sourceFillColor.AsColor(_alpha));
						_fillBrushValid = true;
					}
					else if (_sourceFillpaint is not null)
					{
						if (_sourceFillpaint is LinearGradientPaint linearGradientPaint)
						{
							var gradientStops = new CanvasGradientStop[linearGradientPaint.GradientStops.Length];
							for (int i = 0; i < linearGradientPaint.GradientStops.Length; i++)
							{
								gradientStops[i] = new CanvasGradientStop()
								{
									Position = linearGradientPaint.GradientStops[i].Offset,
									Color = linearGradientPaint.GradientStops[i].Color.AsColor(Colors.White, _alpha)
								};
							}

							_fillBrush = new CanvasLinearGradientBrush(_owner.Session, gradientStops);
							((CanvasLinearGradientBrush)_fillBrush).StartPoint = _linearGradientStartPoint;
							((CanvasLinearGradientBrush)_fillBrush).EndPoint = _linearGradientEndPoint;
						}
						else if (_sourceFillpaint is RadialGradientPaint radialGradientPaint)
						{
							//float radius = GeometryUtil.GetDistance(_gradientPoint1.X, _gradientPoint1.Y, _gradientPoint2.X, _gradientPoint2.Y);

							var gradientStops = new CanvasGradientStop[radialGradientPaint.GradientStops.Length];
							for (int i = 0; i < radialGradientPaint.GradientStops.Length; i++)
							{
								gradientStops[i] = new CanvasGradientStop
								{
									Position = radialGradientPaint.GradientStops[i].Offset,
									Color = radialGradientPaint.GradientStops[i].Color.AsColor(Colors.White, _alpha)
								};
							}
							_fillBrush = new CanvasRadialGradientBrush(_owner.Session, gradientStops);
							((CanvasRadialGradientBrush)_fillBrush).Center = _radialGradientCenter;
							((CanvasRadialGradientBrush)_fillBrush).RadiusX = _radialGradientRadius;
							((CanvasRadialGradientBrush)_fillBrush).RadiusY = _radialGradientRadius;
						}
						_fillBrushValid = true;
					}
					else
					{
						_fillBrush = new CanvasSolidColorBrush(_owner.Session, WColors.White);
						_fillBrushValid = true;
					}
				}

				return _fillBrush;
			}
		}

		public Color FontColor
		{
			set
			{
				var finalValue = value ?? Colors.Black;

				if (!finalValue.Equals(_sourceFontColor))
				{
					_sourceFontColor = finalValue;
					_fontBrushValid = false;
				}
			}
		}

		public void SetShadow(SizeF offset, float blur, Color color)
		{
			if (blur > 0)
			{
				IsShadowed = true;
				ShadowOffset = new Vector2(offset.Width, offset.Height);
				ShadowBlur = blur;
				_sourceShadowColor = color;
				_shadowColorValid = false;
			}
			else
			{
				IsShadowed = false;
			}
		}

		public global::Windows.UI.Color ShadowColor
		{
			get
			{
				if (!_shadowColorValid)
				{
					_shadowColor = _sourceShadowColor?.AsColor(_alpha) ?? CanvasDefaults.DefaultShadowColor.AsColor(_alpha);
				}

				return _shadowColor;
			}
		}

		public void SetBlur(float aRadius)
		{
			if (aRadius > 0)
			{
				IsBlurred = true;
				BlurRadius = aRadius;
			}
			else
			{
				IsBlurred = false;
				BlurRadius = 0;
			}
		}

		public float ActualScale => _scale;

		public float ActualShadowBlur => ShadowBlur * Math.Abs(_scale);

		public Matrix3x2 AppendTranslate(float tx, float ty)
		{
			//Matrix = Matrix * Matrix3x2.Translation(tx, ty);
			Matrix = Matrix.Translate(tx, ty);
			return Matrix;
		}

		public Matrix3x2 AppendConcatenateTransform(Matrix3x2 transform)
		{
			return Matrix = Matrix3x2.Multiply(Matrix, transform);
		}

		public Matrix3x2 AppendScale(float tx, float ty)
		{
			_scale *= tx;
			Matrix = Matrix.Scale(tx, ty);
			return Matrix;
		}

		public Matrix3x2 AppendRotate(float aAngle)
		{
			float radians = GeometryUtil.DegreesToRadians(aAngle);
			Matrix = Matrix.Rotate(radians);
			return Matrix;
		}

		public Matrix3x2 AppendRotate(float aAngle, float x, float y)
		{
			float radians = GeometryUtil.DegreesToRadians(aAngle);
			Matrix = Matrix.Translate(x, y);
			Matrix = Matrix.Rotate(radians);
			Matrix = Matrix.Translate(-x, -y);
			return Matrix;
		}

		public void ClipPath(PathF path, WindingMode windingMode)
		{
			if (_layerMask is not null)
			{
				throw new Exception("Only one clip operation currently supported.");
			}


			/* Unmerged change from project 'Microsoft.Maui.Graphics.Win2D.WinUI.Desktop'
			Before:
						var layerRect = new Rect(0, 0, _owner.CanvasSize.Width, _owner.CanvasSize.Height);
			After:
						var layerRect = new global::Windows.Foundation.Rect(0, 0, _owner.CanvasSize.Width, _owner.CanvasSize.Height);
			*/
			var layerRect = new WRect(0, 0, _owner.CanvasSize.Width, _owner.CanvasSize.Height);
			_layerBounds = CanvasGeometry.CreateRectangle(_owner.Session, layerRect);
			var clipGeometry = path.AsPath(_owner.Session, windingMode == WindingMode.NonZero ? CanvasFilledRegionDetermination.Winding : CanvasFilledRegionDetermination.Alternate);

			_layerMask = _layerBounds.CombineWith(clipGeometry, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);

			_layer = _owner.Session.CreateLayer(1, _layerMask);
			_layerCount++;
		}

		public void ClipRectangle(float x, float y, float width, float height)
		{
			var path = new PathF();
			path.AppendRectangle(x, y, width, height);
			ClipPath(path, WindingMode.NonZero);
		}

		public void SubtractFromClip(float x, float y, float width, float height)
		{
			if (_layerMask is not null)
			{
				throw new Exception("Only one subtraction currently supported.");
			}


			/* Unmerged change from project 'Microsoft.Maui.Graphics.Win2D.WinUI.Desktop'
			Before:
						var layerRect = new Rect(0, 0, _owner.CanvasSize.Width, _owner.CanvasSize.Height);
			After:
						var layerRect = new global::Windows.Foundation.Rect(0, 0, _owner.CanvasSize.Width, _owner.CanvasSize.Height);
			*/
			var layerRect = new WRect(0, 0, _owner.CanvasSize.Width, _owner.CanvasSize.Height);
			_layerBounds = CanvasGeometry.CreateRectangle(_owner.Session, layerRect);


			/* Unmerged change from project 'Microsoft.Maui.Graphics.Win2D.WinUI.Desktop'
			Before:
						var boundsToSubtract = new Rect(x, y, width, height);
			After:
						var boundsToSubtract = new global::Windows.Foundation.Rect(x, y, width, height);
			*/
			var boundsToSubtract = new WRect(x, y, width, height);
			_layerClipBounds = CanvasGeometry.CreateRectangle(_owner.Session, boundsToSubtract);

			_layerMask = _layerBounds.CombineWith(_layerClipBounds, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);

			_layer = _owner.Session.CreateLayer(1, _layerMask);
			_layerCount++;
		}
		public void SaveRenderTargetState()
		{

		}

		public void RestoreRenderTargetState()
		{
			_owner.Session.Transform = Matrix;
			//needsStrokeStyle = true;
		}

		public ICanvasBrush PlatformFontBrush
		{
			get
			{
				if (_fontBrush is null || (!_fontBrushValid && _parentState is not null && _fontBrush == _parentState._fontBrush))
				{
					_fontBrush = new CanvasSolidColorBrush(_owner.Session, _sourceFontColor.AsColor(Colors.Black, _alpha));
				}
				else if (!_fontBrushValid)
				{
					_fontBrush.Color = _sourceFontColor.AsColor(Colors.Black, _alpha);
				}

				return _fontBrush;
			}
		}

		public ICanvasBrush PlatformStrokeBrush
		{
			get
			{
				if (_strokeBrush is null || !_strokeBrushValid)	
				{
					if (_sourceStrokeColor is not null)
					{
						_strokeBrush = new CanvasSolidColorBrush(_owner.Session, _sourceStrokeColor.AsColor(_alpha));
						_strokeBrushValid = true;
					}
					else if (_sourceStrokepaint is not null)
					{
						if (_sourceStrokepaint is LinearGradientPaint linearGradientPaint)
						{
							var gradientStops = new CanvasGradientStop[linearGradientPaint.GradientStops.Length];
							for (int i = 0; i < linearGradientPaint.GradientStops.Length; i++)
							{
								gradientStops[i] = new CanvasGradientStop()
								{
									Position = linearGradientPaint.GradientStops[i].Offset,
									Color = linearGradientPaint.GradientStops[i].Color.AsColor(Colors.White, _alpha)
								};
							}

							float x1 = (float)(linearGradientPaint.StartPoint.X);
							float y1 = (float)(linearGradientPaint.StartPoint.Y);

							float x2 = (float)(linearGradientPaint.EndPoint.X);
							float y2 = (float)(linearGradientPaint.EndPoint.Y);

							_linearGradientStartPoint.X = x1;
							_linearGradientStartPoint.Y = y1;
							_linearGradientEndPoint.X = x2;
							_linearGradientEndPoint.Y = y2;

							_strokeBrush = new CanvasLinearGradientBrush(_owner.Session, gradientStops);
							((CanvasLinearGradientBrush)_strokeBrush).StartPoint = _linearGradientStartPoint;
							((CanvasLinearGradientBrush)_strokeBrush).EndPoint = _linearGradientEndPoint;
						}
						else if (_sourceFillpaint is RadialGradientPaint radialGradientPaint)
						{
							//float radius = GeometryUtil.GetDistance(_gradientPoint1.X, _gradientPoint1.Y, _gradientPoint2.X, _gradientPoint2.Y);

							var gradientStops = new CanvasGradientStop[radialGradientPaint.GradientStops.Length];
							for (int i = 0; i < radialGradientPaint.GradientStops.Length; i++)
							{
								gradientStops[i] = new CanvasGradientStop
								{
									Position = radialGradientPaint.GradientStops[i].Offset,
									Color = radialGradientPaint.GradientStops[i].Color.AsColor(Colors.White, _alpha)
								};
							}
							_strokeBrush = new CanvasRadialGradientBrush(_owner.Session, gradientStops);
							((CanvasRadialGradientBrush)_strokeBrush).Center = _radialGradientCenter;
							((CanvasRadialGradientBrush)_strokeBrush).RadiusX = _radialGradientRadius;
							((CanvasRadialGradientBrush)_strokeBrush).RadiusY = _radialGradientRadius;
						}
						_strokeBrushValid = true;
					}
					else
					{
						_strokeBrush = new CanvasSolidColorBrush(_owner.Session, WColors.White);
						_strokeBrushValid = true;
					}
				}

				return _strokeBrush;
			}
		}


		public CanvasStrokeStyle PlatformStrokeStyle
		{
			get
			{
				if (_needsStrokeStyle)
				{
					if (_strokeStyle is null)
					{
						_strokeStyle = new CanvasStrokeStyle();
					}

					if (_dashes is not null)
					{
						_strokeStyle.CustomDashStyle = _dashes;
						_strokeStyle.DashCap = _lineCap;
					}
					else
					{
						_strokeStyle.CustomDashStyle = _emptyFloatArray;
					}

					_strokeStyle.DashOffset = _dashOffset;
					_strokeStyle.MiterLimit = _miterLimit;
					_strokeStyle.StartCap = _lineCap;
					_strokeStyle.EndCap = _lineCap;
					_strokeStyle.LineJoin = _lineJoin;


					return _strokeStyle;
				}

				return _strokeStyle;
			}
		}

		void InvalidateBrushes()
		{
			_strokeBrushValid = false;
			_fillBrushValid = false;
			_shadowColorValid = false;
			_fontBrushValid = false;
		}

		void ReleaseFillBrush()
		{
			if (_fillBrush is not null)
			{
				if (_parentState is null || _fillBrush != _parentState._fillBrush)
				{
					_fillBrush.Dispose();
				}
				_fillBrush = null;
			}
		}

		void ReleaseStrokeBrush()
		{
			if (_strokeBrush is not null)
			{
				if (_parentState is null || _strokeBrush != _parentState._strokeBrush)
				{
					_strokeBrush.Dispose();
				}
				_strokeBrush = null;
			}
		}

		void InvalidateStrokeStyle()
		{
			if (_strokeStyle is not null)
			{
				if (_parentState is null || _strokeStyle != _parentState._strokeStyle)
				{	
					_strokeStyle.Dispose();
				}
				_strokeStyle = null;
			}
		}

		public override void Dispose()
		{
			base.Dispose();

			if (_layer is not null)
			{
				_layer.Dispose();
				_layer = null;
			}

			if (_layerMask is not null)
			{
				_layerMask.Dispose();
				_layerMask = null;
			}

			if (_layerBounds is not null)
			{
				_layerBounds.Dispose();
				_layerBounds = null;
			}

			if (_layerClipBounds is not null)
			{
				_layerClipBounds.Dispose();
				_layerClipBounds = null;
			}
		}
	}
}
