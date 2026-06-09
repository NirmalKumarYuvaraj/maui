#nullable disable
using System;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class PathGeometryTests : BaseTestFixture
{
	/// <summary>
	/// Control case: clearing a collection that holds only locally-rooted figures should release the geometry.
	/// </summary>
	[Fact]
	public void FiguresClear_LocalFigure_DoesNotLeak()
	{
		AllocateClearedLocalFigurePathGeometry(out var geometryReference);

		ForceGC();

		Assert.Null(geometryReference.Target);
	}

	/// <summary>
	/// Regression test for https://github.com/dotnet/maui/issues/35809.
	/// Clearing the figures collection must detach an externally-rooted figure so the owning
	/// PathGeometry can be collected.
	/// </summary>
	[Fact]
	public void FiguresClear_SharedFigure_DoesNotLeakPathGeometry()
	{
		var sharedFigure = new PathFigure { StartPoint = new Point(10, 20) };

		AllocateClearedSharedFigurePathGeometry(sharedFigure, out var geometryReference);

		ForceGC();

		Assert.Null(geometryReference.Target);
	}

	/// <summary>
	/// Extends the #35809 regression: the leak chain goes Figure → Geometry → Path → BindingContext.
	/// After Clear() the entire chain must be collectible.
	/// </summary>
	[Fact]
	public void FiguresClear_SharedFigure_DoesNotLeakPath()
	{
		var sharedFigure = new PathFigure { StartPoint = new Point(10, 20) };

		AllocateClearedSharedFigurePath(sharedFigure, out var pathReference);

		ForceGC();

		Assert.Null(pathReference.Target);
	}

	/// <summary>
	/// Analogous to the #35809 PathFigureCollection bug — applied to PathSegmentCollection.
	/// PathFigure.Segments.Clear() must detach a shared segment so the PathFigure can be collected.
	/// </summary>
	[Fact]
	public void SegmentsClear_SharedSegment_DoesNotLeakPathFigure()
	{
		var sharedSegment = new LineSegment { Point = new Point(30, 40) };

		AllocateClearedSharedSegmentPathFigure(sharedSegment, out var figureReference);

		ForceGC();

		Assert.Null(figureReference.Target);
	}

	/// <summary>
	/// Analogous to the #35809 PathFigureCollection bug — applied to GeometryCollection.
	/// GeometryGroup.Children.Clear() must detach a shared geometry so the GeometryGroup can be collected.
	/// </summary>
	[Fact]
	public void ChildrenClear_SharedGeometry_DoesNotLeakGeometryGroup()
	{
		var sharedGeometry = new RectangleGeometry { Rect = new Rect(0, 0, 10, 10) };

		AllocateClearedSharedGeometryGroup(sharedGeometry, out var groupReference);

		ForceGC();

		Assert.Null(groupReference.Target);
	}

	/// <summary>
	/// Analogous to the #35809 PathFigureCollection bug — applied to TransformCollection.
	/// TransformGroup.Children.Clear() must detach a shared transform so the TransformGroup can be collected.
	/// </summary>
	[Fact]
	public void ChildrenClear_SharedTransform_DoesNotLeakTransformGroup()
	{
		var sharedTransform = new RotateTransform { Angle = 45 };

		AllocateClearedSharedTransformGroup(sharedTransform, out var groupReference);

		ForceGC();

		Assert.Null(groupReference.Target);
	}

	/// <summary>
	/// Regression guard: Clear() and RemoveAt(0) must both release the owner when a shared figure is present.
	/// </summary>
	[Fact]
	public void FiguresClear_EquivalentToRemoveAt_BothRelease()
	{
		var sharedFigure = new PathFigure { StartPoint = new Point(50, 60) };

		AllocateClearedAndRemoveAtPathGeometries(sharedFigure, out var clearReference, out var removeAtReference);

		ForceGC();

		Assert.Null(clearReference.Target);
		Assert.Null(removeAtReference.Target);
	}

	// Allocator helpers — keep all locals in a separate stack frame from the caller so the JIT
	// releases them when the helper returns. Standard pattern used by other MAUI memory-leak unit
	// tests (see BindingUnitTests.HackAroundMonoSucking).

	static void AllocateClearedLocalFigurePathGeometry(out WeakReference geometryReference)
	{
		var geometry = new PathGeometry();
		geometry.Figures.Add(new PathFigure { StartPoint = new Point(1, 2) });

		geometryReference = new WeakReference(geometry);

		geometry.Figures.Clear();
	}

	static void AllocateClearedSharedFigurePathGeometry(PathFigure sharedFigure, out WeakReference geometryReference)
	{
		var geometry = new PathGeometry();
		geometry.Figures.Add(sharedFigure);

		geometryReference = new WeakReference(geometry);

		geometry.Figures.Clear();
	}

	static void AllocateClearedSharedFigurePath(PathFigure sharedFigure, out WeakReference pathReference)
	{
		var geometry = new PathGeometry();
		geometry.Figures.Add(sharedFigure);

		var path = new Path
		{
			BindingContext = new object(),
			Data = geometry
		};

		pathReference = new WeakReference(path);

		geometry.Figures.Clear();
	}

	static void AllocateClearedSharedSegmentPathFigure(LineSegment sharedSegment, out WeakReference figureReference)
	{
		var figure = new PathFigure { StartPoint = new Point(3, 4) };
		figure.Segments.Add(sharedSegment);

		figureReference = new WeakReference(figure);

		figure.Segments.Clear();
	}

	static void AllocateClearedSharedGeometryGroup(Geometry sharedGeometry, out WeakReference groupReference)
	{
		var group = new GeometryGroup();
		group.Children.Add(sharedGeometry);

		groupReference = new WeakReference(group);

		group.Children.Clear();
	}

	static void AllocateClearedSharedTransformGroup(Transform sharedTransform, out WeakReference groupReference)
	{
		var group = new TransformGroup();
		group.Children.Add(sharedTransform);

		groupReference = new WeakReference(group);

		group.Children.Clear();
	}

	static void AllocateClearedAndRemoveAtPathGeometries(
		PathFigure sharedFigure,
		out WeakReference clearReference,
		out WeakReference removeAtReference)
	{
		var clearGeometry = new PathGeometry();
		clearGeometry.Figures.Add(sharedFigure);

		var removeAtGeometry = new PathGeometry();
		removeAtGeometry.Figures.Add(sharedFigure);

		clearReference = new WeakReference(clearGeometry);
		removeAtReference = new WeakReference(removeAtGeometry);

		clearGeometry.Figures.Clear();
		removeAtGeometry.Figures.RemoveAt(0);
	}

	static void ForceGC()
	{
		for (int i = 0; i < 3; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}
	}
}
