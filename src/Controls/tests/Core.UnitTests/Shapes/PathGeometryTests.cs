#nullable disable
using System;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class PathGeometryTests : BaseTestFixture
{
	/// <summary>
	/// Verifies clearing a collection with only local figures does not keep the owning geometry alive.
	/// </summary>
	[Fact]
	public void FiguresClear_LocalFigure_DoesNotLeak()
	{
		var geometryReference = CreateClearedLocalFigurePathGeometryReference();

		ForceGC();

		Assert.Null(geometryReference.Target);
	}

	/// <summary>
	/// Verifies clearing a geometry detaches an externally rooted path figure so the geometry can be collected.
	/// </summary>
	[Fact]
	public void FiguresClear_SharedFigure_DoesNotLeakPathGeometry()
	{
		var sharedFigure = new PathFigure
		{
			StartPoint = new Point(10, 20)
		};

		var geometryReference = CreateClearedSharedFigurePathGeometryReference(sharedFigure);

		ForceGC();

		Assert.Null(geometryReference.Target);
	}

	/// <summary>
	/// Verifies clearing a geometry detaches an externally rooted path figure so the owning path can be collected.
	/// </summary>
	[Fact]
	public void FiguresClear_SharedFigure_DoesNotLeakPath()
	{
		var sharedFigure = new PathFigure
		{
			StartPoint = new Point(10, 20)
		};

		var pathReference = CreateClearedSharedFigurePathReference(sharedFigure);

		ForceGC();

		Assert.Null(pathReference.Target);
	}

	/// <summary>
	/// Verifies clearing a figure detaches an externally rooted path segment so the figure can be collected.
	/// </summary>
	[Fact]
	public void SegmentsClear_SharedSegment_DoesNotLeakPathFigure()
	{
		var sharedSegment = new LineSegment
		{
			Point = new Point(30, 40)
		};

		var figureReference = CreateClearedSharedSegmentPathFigureReference(sharedSegment);

		ForceGC();

		Assert.Null(figureReference.Target);
	}

	/// <summary>
	/// Verifies clearing a shared figure releases the owner just like removing the same figure explicitly.
	/// </summary>
	[Fact]
	public void FiguresClear_EquivalentToRemoveAt_BothRelease()
	{
		var sharedFigure = new PathFigure
		{
			StartPoint = new Point(50, 60)
		};

		var (clearReference, removeAtReference) = CreateClearAndRemoveAtReferences(sharedFigure);

		ForceGC();

		Assert.Null(clearReference.Target);
		Assert.Null(removeAtReference.Target);
	}

	static WeakReference CreateClearedLocalFigurePathGeometryReference() =>
		CreateClearedLocalFigurePathGeometryReference(0);

	static WeakReference CreateClearedLocalFigurePathGeometryReference(int depth)
	{
		if (depth < 1024)
			return CreateClearedLocalFigurePathGeometryReference(depth + 1);

		var geometry = new PathGeometry();
		geometry.Figures.Add(new PathFigure
		{
			StartPoint = new Point(1, 2)
		});

		var geometryReference = new WeakReference(geometry);

		geometry.Figures.Clear();

		return geometryReference;
	}

	static WeakReference CreateClearedSharedFigurePathGeometryReference(PathFigure sharedFigure) =>
		CreateClearedSharedFigurePathGeometryReference(sharedFigure, 0);

	static WeakReference CreateClearedSharedFigurePathGeometryReference(PathFigure sharedFigure, int depth)
	{
		if (depth < 1024)
			return CreateClearedSharedFigurePathGeometryReference(sharedFigure, depth + 1);

		var geometry = new PathGeometry();
		geometry.Figures.Add(sharedFigure);

		var geometryReference = new WeakReference(geometry);

		geometry.Figures.Clear();

		return geometryReference;
	}

	static WeakReference CreateClearedSharedFigurePathReference(PathFigure sharedFigure) =>
		CreateClearedSharedFigurePathReference(sharedFigure, 0);

	static WeakReference CreateClearedSharedFigurePathReference(PathFigure sharedFigure, int depth)
	{
		if (depth < 1024)
			return CreateClearedSharedFigurePathReference(sharedFigure, depth + 1);

		var geometry = new PathGeometry();
		geometry.Figures.Add(sharedFigure);

		var path = new Path
		{
			BindingContext = new object(),
			Data = geometry
		};

		var pathReference = new WeakReference(path);

		geometry.Figures.Clear();

		return pathReference;
	}

	static WeakReference CreateClearedSharedSegmentPathFigureReference(LineSegment sharedSegment) =>
		CreateClearedSharedSegmentPathFigureReference(sharedSegment, 0);

	static WeakReference CreateClearedSharedSegmentPathFigureReference(LineSegment sharedSegment, int depth)
	{
		if (depth < 1024)
			return CreateClearedSharedSegmentPathFigureReference(sharedSegment, depth + 1);

		var figure = new PathFigure
		{
			StartPoint = new Point(3, 4)
		};
		figure.Segments.Add(sharedSegment);

		var figureReference = new WeakReference(figure);

		figure.Segments.Clear();

		return figureReference;
	}

	static (WeakReference clearReference, WeakReference removeAtReference) CreateClearAndRemoveAtReferences(PathFigure sharedFigure) =>
		CreateClearAndRemoveAtReferences(sharedFigure, 0);

	static (WeakReference clearReference, WeakReference removeAtReference) CreateClearAndRemoveAtReferences(PathFigure sharedFigure, int depth)
	{
		if (depth < 1024)
			return CreateClearAndRemoveAtReferences(sharedFigure, depth + 1);

		var clearGeometry = new PathGeometry();
		clearGeometry.Figures.Add(sharedFigure);

		var removeAtGeometry = new PathGeometry();
		removeAtGeometry.Figures.Add(sharedFigure);

		var clearReference = new WeakReference(clearGeometry);
		var removeAtReference = new WeakReference(removeAtGeometry);

		clearGeometry.Figures.Clear();
		removeAtGeometry.Figures.RemoveAt(0);

		return (clearReference, removeAtReference);
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
