using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.Maui.Platform;

public static class TransformationExtensions
{
	const double EPSILON = 1e-10;
	const double FULL_ROTATION = 360.0;

	public static void UpdateTransformation(this FrameworkElement frameworkElement, IView view)
	{
		// Cache transformation values to avoid multiple property accesses
		var transformation = new ViewTransformation
		{
			RotationX = view.RotationX,
			RotationY = view.RotationY,
			Rotation = view.Rotation,
			TranslationX = view.TranslationX,
			TranslationY = view.TranslationY,
			ScaleX = view.Scale * view.ScaleX,
			ScaleY = view.Scale * view.ScaleY,
			AnchorX = view.AnchorX,
			AnchorY = view.AnchorY
		};

		if (IsIdentityTransformation(transformation))
		{
			ClearTransformations(frameworkElement, view);
		}
		else
		{
			ApplyTransformations(frameworkElement, transformation);
		}
	}

	static bool IsIdentityTransformation(ViewTransformation transformation)
	{
		return IsZeroRotation(transformation.RotationX) &&
			   IsZeroRotation(transformation.RotationY) &&
			   IsZeroRotation(transformation.Rotation) &&
			   IsZeroTranslation(transformation.TranslationX) &&
			   IsZeroTranslation(transformation.TranslationY) &&
			   IsIdentityScale(transformation.ScaleX) &&
			   IsIdentityScale(transformation.ScaleY);
	}

	static void ClearTransformations(FrameworkElement frameworkElement, IView view)
	{
		// Only clear transforms when not connecting handler to avoid interfering
		// with the initialization process
		if (!view.IsConnectingHandler())
		{
			frameworkElement.Projection = null;
			frameworkElement.RenderTransform = null;
		}
	}

	static void ApplyTransformations(FrameworkElement frameworkElement, ViewTransformation transformation)
	{
		frameworkElement.RenderTransformOrigin = new global::Windows.Foundation.Point(
			transformation.AnchorX,
			transformation.AnchorY);

		// Use PlaneProjection for 3D rotations, CompositeTransform for 2D transformations
		if (HasThreeDimensionalRotation(transformation))
		{
			ApplyPlaneProjection(frameworkElement, transformation);
		}
		else
		{
			ApplyCompositeTransform(frameworkElement, transformation);
		}
	}

	static void ApplyPlaneProjection(FrameworkElement frameworkElement, ViewTransformation transformation)
	{
		// PlaneProjection handles 3D transformations but may affect touch/scroll functionality
		// on scrollable views like ScrollView, ListView, and TableView
		frameworkElement.Projection = new PlaneProjection
		{
			CenterOfRotationX = transformation.AnchorX,
			CenterOfRotationY = transformation.AnchorY,
			GlobalOffsetX = transformation.TranslationX,
			GlobalOffsetY = transformation.TranslationY,
			RotationX = -transformation.RotationX, // Negated for correct direction
			RotationY = -transformation.RotationY, // Negated for correct direction
			RotationZ = -transformation.Rotation    // Negated for correct direction
		};

		// Apply scaling separately since PlaneProjection doesn't handle it
		if (!IsIdentityScale(transformation.ScaleX) || !IsIdentityScale(transformation.ScaleY))
		{
			frameworkElement.RenderTransform = new ScaleTransform
			{
				ScaleX = transformation.ScaleX,
				ScaleY = transformation.ScaleY
			};
		}
	}

	static void ApplyCompositeTransform(FrameworkElement frameworkElement, ViewTransformation transformation)
	{
		// CompositeTransform handles all 2D transformations in one object
		// and preserves touch/scroll functionality
		frameworkElement.RenderTransform = new CompositeTransform
		{
			Rotation = transformation.Rotation,
			ScaleX = transformation.ScaleX,
			ScaleY = transformation.ScaleY,
			TranslateX = transformation.TranslationX,
			TranslateY = transformation.TranslationY
		};
	}

	static bool HasThreeDimensionalRotation(ViewTransformation transformation)
	{
		return !IsZeroRotation(transformation.RotationX) || !IsZeroRotation(transformation.RotationY);
	}

	static bool IsZeroRotation(double rotation)
	{
		return Math.Abs(rotation % FULL_ROTATION) < EPSILON;
	}

	static bool IsZeroTranslation(double translation)
	{
		return Math.Abs(translation) < EPSILON;
	}

	static bool IsIdentityScale(double scale)
	{
		return Math.Abs(scale - 1.0) < EPSILON;
	}

	/// <summary>
	/// Value object to hold transformation parameters
	/// </summary>
	readonly struct ViewTransformation
	{
		public double RotationX { get; init; }
		public double RotationY { get; init; }
		public double Rotation { get; init; }
		public double TranslationX { get; init; }
		public double TranslationY { get; init; }
		public double ScaleX { get; init; }
		public double ScaleY { get; init; }
		public double AnchorX { get; init; }
		public double AnchorY { get; init; }
	}
}