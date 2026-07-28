using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using AView = Android.Views.View;

namespace Microsoft.Maui.Platform;

internal sealed class ImeWindowInsetsCoordinator
{
	AView? _view;
	int _startPaddingBottom;
	int _targetPaddingBottom;
	bool _isAnimating;
	int _animationSequence;
	int _frame;
	long _lastFrameUptimeMillis;

	internal void TrackView(AView view)
	{
		if (!ReferenceEquals(_view, view))
		{
			MauiWindowInsetDebug.WriteImeAnimation(
				nameof(TrackView),
				"TrackView",
				_animationSequence,
				_frame,
				SystemClock.UptimeMillis(),
				0,
				view,
				SafeAreaExtensions.CanApplyImeInsets(view),
				0,
				0,
				view.PaddingBottom,
				0,
				view.TranslationY,
				view.TranslationY);
		}

		_view = view;
	}

	internal void OnPrepare(WindowInsetsAnimationCompat? animation)
	{
		var view = _view;
		var eligible = view is not null && SafeAreaExtensions.CanApplyImeInsets(view);
		if (!IsImeAnimation(animation) || view is null || !eligible)
		{
			MauiWindowInsetDebug.WriteImeAnimation(
				nameof(OnPrepare),
				"Skipped",
				_animationSequence,
				_frame,
				SystemClock.UptimeMillis(),
				0,
				view,
				eligible,
				animation?.InterpolatedFraction ?? 0,
				0,
				view?.PaddingBottom ?? 0,
				0,
				view?.TranslationY ?? 0,
				view?.TranslationY ?? 0);
			EndAnimation();
			return;
		}

		EndAnimation();
		_animationSequence++;
		_frame = 0;
		_lastFrameUptimeMillis = SystemClock.UptimeMillis();
		_startPaddingBottom = view.PaddingBottom;
		_targetPaddingBottom = _startPaddingBottom;
		_isAnimating = true;

		MauiWindowInsetDebug.WriteImeAnimation(
			nameof(OnPrepare),
			"Prepared",
			_animationSequence,
			_frame,
			_lastFrameUptimeMillis,
			0,
			view,
			eligible,
			animation?.InterpolatedFraction ?? 0,
			0,
			_startPaddingBottom,
			0,
			view.TranslationY,
			view.TranslationY);
	}

	internal WindowInsetsAnimationCompat.BoundsCompat? OnStart(
		WindowInsetsAnimationCompat? animation,
		WindowInsetsAnimationCompat.BoundsCompat? bounds)
	{
		if (!_isAnimating ||
			!IsImeAnimation(animation) ||
			_view is not AView view)
		{
			MauiWindowInsetDebug.WriteImeAnimation(
				nameof(OnStart),
				"Skipped",
				_animationSequence,
				_frame,
				SystemClock.UptimeMillis(),
				0,
				_view,
				false,
				animation?.InterpolatedFraction ?? 0,
				0,
				_startPaddingBottom,
				0,
				_view?.TranslationY ?? 0,
				_view?.TranslationY ?? 0);
			return bounds;
		}

		MauiWindowInsetDebug.WriteImeAnimation(
			nameof(OnStart),
			"Started",
			_animationSequence,
			_frame,
			SystemClock.UptimeMillis(),
			0,
			view,
			true,
			animation?.InterpolatedFraction ?? 0,
			0,
			_startPaddingBottom,
			_targetPaddingBottom,
			view.TranslationY,
			view.TranslationY);

		return bounds;
	}

	internal void OnInsetsApplied(AView view)
	{
		if (!_isAnimating || !ReferenceEquals(_view, view))
		{
			return;
		}

		_targetPaddingBottom = view.PaddingBottom;
		if (_targetPaddingBottom != _startPaddingBottom)
		{
			view.SetPadding(
				view.PaddingLeft,
				view.PaddingTop,
				view.PaddingRight,
				_startPaddingBottom);
		}

		MauiWindowInsetDebug.WriteImeAnimation(
			nameof(OnInsetsApplied),
			"CapturedTarget",
			_animationSequence,
			_frame,
			SystemClock.UptimeMillis(),
			0,
			view,
			true,
			0,
			0,
			_startPaddingBottom,
			_targetPaddingBottom,
			view.TranslationY,
			view.TranslationY);
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

			var uptimeMillis = SystemClock.UptimeMillis();
			var frameDeltaMillis = uptimeMillis - _lastFrameUptimeMillis;
			var translationBefore = view.TranslationY;
			var imeBottom = insets?.GetInsets(WindowInsetsCompat.Type.Ime())?.Bottom ?? 0;

			var animatedPaddingBottom = CalculateAnimatedPadding(
				_startPaddingBottom,
				_targetPaddingBottom,
				animation.InterpolatedFraction);
			if (view.PaddingBottom != animatedPaddingBottom)
			{
				view.SetPadding(
					view.PaddingLeft,
					view.PaddingTop,
					view.PaddingRight,
					animatedPaddingBottom);
			}
			_frame++;

			MauiWindowInsetDebug.WriteImeAnimation(
				nameof(OnProgress),
				"Frame",
				_animationSequence,
				_frame,
				uptimeMillis,
				frameDeltaMillis,
				view,
				true,
				animation.InterpolatedFraction,
				imeBottom,
				_startPaddingBottom,
				_targetPaddingBottom,
				translationBefore,
				view.TranslationY);
			_lastFrameUptimeMillis = uptimeMillis;
			break;
		}

		return insets;
	}

	internal void OnEnd(WindowInsetsAnimationCompat? animation)
	{
		if (_isAnimating && IsImeAnimation(animation))
		{
			MauiWindowInsetDebug.WriteImeAnimation(
				nameof(OnEnd),
				"Ending",
				_animationSequence,
				_frame,
				SystemClock.UptimeMillis(),
				SystemClock.UptimeMillis() - _lastFrameUptimeMillis,
				_view,
				true,
				animation?.InterpolatedFraction ?? 1,
				0,
				_startPaddingBottom,
				_targetPaddingBottom,
				_view?.TranslationY ?? 0,
				_view?.TranslationY ?? 0);
			EndAnimation();
			if (_view is AView view)
			{
				ViewCompat.RequestApplyInsets(view);
			}
		}
	}

	internal void Reset()
	{
		MauiWindowInsetDebug.WriteImeAnimation(
			nameof(Reset),
			"Reset",
			_animationSequence,
			_frame,
			SystemClock.UptimeMillis(),
			0,
			_view,
			_view is not null && SafeAreaExtensions.CanApplyImeInsets(_view),
			0,
			0,
			_startPaddingBottom,
			_targetPaddingBottom,
			_view?.TranslationY ?? 0,
			_view?.TranslationY ?? 0);
		EndAnimation();
		_view = null;
	}

	void EndAnimation()
	{
		if (_view is AView view && _isAnimating)
		{
			MauiWindowInsetDebug.WriteImeAnimation(
				nameof(EndAnimation),
				"Completed",
				_animationSequence,
				_frame,
				SystemClock.UptimeMillis(),
				0,
				view,
				SafeAreaExtensions.CanApplyImeInsets(view),
				1,
				0,
				_startPaddingBottom,
				_targetPaddingBottom,
				view.TranslationY,
				view.TranslationY);
		}

		_isAnimating = false;
	}

	internal static int CalculateAnimatedPadding(
		int startPadding,
		int targetPadding,
		float interpolatedFraction) =>
		(int)Math.Round(
			startPadding + ((targetPadding - startPadding) * interpolatedFraction),
			MidpointRounding.AwayFromZero);

	static bool IsImeAnimation(WindowInsetsAnimationCompat? animation) =>
		animation is not null &&
		(animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0;
}
