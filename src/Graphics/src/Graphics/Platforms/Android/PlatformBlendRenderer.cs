using System;
using System.Collections.Generic;
using Android.Graphics;
using APaint = Android.Graphics.Paint;

namespace Microsoft.Maui.Graphics.Platform
{
	internal sealed class PlatformBlendRenderer : IDisposable
	{
		readonly Canvas _target;
		readonly Matrix _initialMatrix;
		readonly bool _deferred;
		readonly int _width;
		readonly int _height;
		readonly List<Action<Canvas>> _stateChanges = new List<Action<Canvas>>();
		readonly List<IDisposable> _resources = new List<IDisposable>();
		Picture _picture;
		Canvas _recording;
		Bitmap _destination;
		Canvas _destinationCanvas;
		Bitmap _source;
		Canvas _sourceCanvas;
		Bitmap _coverage;
		Canvas _coverageCanvas;
		int[] _coveragePixels;
		int[] _sourcePixels;
		int[] _destinationPixels;
		bool _ownsDestination;
		bool _needsIsolation;

		internal PlatformBlendRenderer(Canvas target, Bitmap bitmap = null, bool deferred = false, int width = 0, int height = 0)
		{
			_target = target;
			_deferred = deferred;
			_width = width > 0 ? width : target.Width;
			_height = height > 0 ? height : target.Height;
#pragma warning disable CS0618 // The initial matrix is needed to present the local backing surface.
			_initialMatrix = new Matrix(target.Matrix);
#pragma warning restore CS0618
			if (bitmap != null)
			{
				_destination = bitmap;
				_destinationCanvas = target;
			}
			else
			{
				_picture = new Picture();
				_recording = _picture.BeginRecording(_width, _height);
			}
		}

		internal void ChangeState(Action<Canvas> change)
		{
			if (!_deferred)
				change(_target);
			if (_recording != null)
				change(_recording);
			if (_destinationCanvas != null && _destinationCanvas != _target)
				change(_destinationCanvas);
			if (_sourceCanvas != null)
				change(_sourceCanvas);
			else
				_stateChanges.Add(change);
			if (_coverageCanvas != null)
				change(_coverageCanvas);
		}

		internal void ClipPath(Path path)
		{
			var copy = new Path(path);
			_resources.Add(copy);
			ChangeState(canvas => canvas.ClipPath(copy));
		}

		internal void Draw(Action<Canvas> draw, BlendMode mode, Action<Canvas> drawCoverage = null)
		{
			_needsIsolation |= mode != BlendMode.Normal;
			if (!PlatformBlendModes.RequiresSoftware(mode))
			{
				if (_destinationCanvas != null)
					draw(_destinationCanvas);
				else if (!_deferred)
					draw(_target);
				if (_recording != null)
					draw(_recording);
			}
			else
			{
				EnsureSoftwareSurface();
				_source.EraseColor(global::Android.Graphics.Color.Transparent);
				draw(_sourceCanvas);
				if (mode == BlendMode.PlusDarker && drawCoverage != null)
				{
					_coverage.EraseColor(global::Android.Graphics.Color.Transparent);
					drawCoverage(_coverageCanvas);
					_coverage.GetPixels(_coveragePixels, 0, _width, 0, 0, _width, _height);
				}
				int width = _destination.Width;
				int height = _destination.Height;
				_source.GetPixels(_sourcePixels, 0, width, 0, 0, width, height);
				_destination.GetPixels(_destinationPixels, 0, width, 0, 0, width, height);
				for (int i = 0; i < _sourcePixels.Length; i++)
				{
					float coverage = mode == BlendMode.PlusDarker && drawCoverage != null
						? ((uint)_coveragePixels[i] >> 24) / 255f
						: 1;
					_destinationPixels[i] = BlendModeCompositor.Blend(_sourcePixels[i], _destinationPixels[i], mode, coverage);
				}
				_destination.SetPixels(_destinationPixels, 0, width, 0, 0, width, height);
			}

			if (_ownsDestination && !_deferred)
				Present();
		}

		void EnsureSoftwareSurface()
		{
			if (_sourceCanvas != null)
				return;

			int width = _width;
			int height = _height;
			if (_destination == null)
			{
				_destination = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
				_destinationCanvas = new Canvas(_destination);
				_ownsDestination = true;
				_picture.EndRecording();
				_destinationCanvas.DrawPicture(_picture);
				_recording.Dispose();
				_recording = null;
				_picture.Dispose();
				_picture = null;
				foreach (var change in _stateChanges)
					change(_destinationCanvas);
			}

			_source = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
			_sourceCanvas = new Canvas(_source);
			_coverage = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
			_coverageCanvas = new Canvas(_coverage);
			foreach (var change in _stateChanges)
			{
				change(_sourceCanvas);
				change(_coverageCanvas);
			}
			_stateChanges.Clear();
			_sourcePixels = new int[checked(width * height)];
			_destinationPixels = new int[_sourcePixels.Length];
			_coveragePixels = new int[_sourcePixels.Length];
		}

		void Present()
		{
			int save = _target.Save();
			try
			{
				_target.Matrix = _initialMatrix;
				using var paint = new APaint();
				PlatformBlendModes.Apply(paint, _deferred ? BlendMode.Normal : BlendMode.Copy);
				_target.DrawBitmap(_destination, 0, 0, paint);
			}
			finally
			{
				_target.RestoreToCount(save);
			}
		}

		public void Dispose()
		{
			if (_recording != null)
				_picture.EndRecording();
			if (_deferred)
			{
				if (_ownsDestination)
				{
					Present();
				}
				else
				{
					int save = _needsIsolation ? _target.SaveLayer(0, 0, _width, _height, null) : _target.Save();
					try
					{
						_target.DrawPicture(_picture);
					}
					finally
					{
						_target.RestoreToCount(save);
					}
				}
			}
			_recording?.Dispose();
			_picture?.Dispose();
			_sourceCanvas?.Dispose();
			_source?.Dispose();
			_coverageCanvas?.Dispose();
			_coverage?.Dispose();
			if (_ownsDestination)
			{
				_destinationCanvas.Dispose();
				_destination.Dispose();
			}
			_initialMatrix.Dispose();
			foreach (var resource in _resources)
				resource.Dispose();
		}
	}
}
