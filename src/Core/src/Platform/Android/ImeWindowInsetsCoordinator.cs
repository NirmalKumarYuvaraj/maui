using System.Collections.Generic;
using Android.Views;
using AndroidX.Core.View;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform;

internal sealed class ImeWindowInsetsCoordinator
{
	AView? _view;
	float _baseTranslationY;
	float _lastAppliedTranslationY;
	float _translationOffset;
	int _startPaddingBottom;
	bool _isAnimating;

	internal void TrackView(AView view)
	{
		_view = view;
	}

	internal void OnPrepare(WindowInsetsAnimationCompat? animation)
	{
		if (!IsImeAnimation(animation) ||
			_view is not AView view ||
			!SafeAreaExtensions.CanApplyImeInsets(view))
		{
			EndAnimation();
			return;
		}

		EndAnimation();
		_baseTranslationY = view.TranslationY;
		_lastAppliedTranslationY = _baseTranslationY;
		_startPaddingBottom = view.PaddingBottom;
		_translationOffset = 0;
		_isAnimating = true;
	}

	internal WindowInsetsAnimationCompat.BoundsCompat? OnStart(
		WindowInsetsAnimationCompat? animation,
		WindowInsetsAnimationCompat.BoundsCompat? bounds)
	{
		if (!_isAnimating ||
			!IsImeAnimation(animation) ||
			_view is not AView view)
		{
			return bounds;
		}

		_translationOffset = CalculateTranslationOffset(
			_startPaddingBottom,
			view.PaddingBottom);
		_lastAppliedTranslationY = _baseTranslationY + _translationOffset;
		view.TranslationY = _lastAppliedTranslationY;

		return bounds;
	}

	internal WindowInsetsCompat? OnProgress(
		WindowInsetsCompat? insets,
		IList<WindowInsetsAnimationCompat>? runningAnimations)
	{
		if (!_isAnimating ||
			_view is not AView view ||
			runningAnimations is null)
		{
			return insets;
		}

		for (int i = 0; i < runningAnimations.Count; i++)
		{
			var animation = runningAnimations[i];
			if (!IsImeAnimation(animation))
			{
				continue;
			}

			UpdateBaseTranslation(view);
			_lastAppliedTranslationY = CalculateTranslation(
				_baseTranslationY,
				_translationOffset,
				animation.InterpolatedFraction);
			view.TranslationY = _lastAppliedTranslationY;
			break;
		}

		return insets;
	}

	internal void OnEnd(WindowInsetsAnimationCompat? animation)
	{
		if (_isAnimating && IsImeAnimation(animation))
		{
			EndAnimation();
		}
	}

	internal void Reset()
	{
		EndAnimation();
		_view = null;
	}

	void EndAnimation()
	{
		if (_view is AView view && _isAnimating)
		{
			UpdateBaseTranslation(view);
			view.TranslationY = _baseTranslationY;
		}

		_translationOffset = 0;
		_isAnimating = false;
	}

	void UpdateBaseTranslation(AView view)
	{
		_baseTranslationY += view.TranslationY - _lastAppliedTranslationY;
	}

	internal static float CalculateTranslationOffset(
		int startPaddingBottom,
		int endPaddingBottom) =>
		endPaddingBottom - startPaddingBottom;

	internal static float CalculateTranslation(
		float baseTranslationY,
		float translationOffset,
		float interpolatedFraction) =>
		baseTranslationY + (translationOffset * (1 - interpolatedFraction));

	static bool IsImeAnimation(WindowInsetsAnimationCompat? animation) =>
		animation is not null &&
		(animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0;
}
