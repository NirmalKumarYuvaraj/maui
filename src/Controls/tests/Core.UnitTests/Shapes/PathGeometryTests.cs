using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class PathGeometryTests : BaseTestFixture
{
	[Fact]
	public void AppendPathPreservesFigureBoundariesClosureAndExistingPoints()
	{
		var geometry = new PathGeometry
		{
			Figures =
			{
				new PathFigure { StartPoint = new Point(10, 20), IsClosed = true, Segments = { new LineSegment(new Point(30, 40)) } },
				new PathFigure { StartPoint = new Point(-10, -20), Segments = { new LineSegment(new Point(-30, -40)) } }
			}
		};
		var path = new PathF();
		path.MoveTo(1, 2);
		path.LineTo(3, 4);

		geometry.AppendPath(path);

		Assert.Equal(new[] { PathOperation.Move, PathOperation.Line, PathOperation.Move, PathOperation.Line, PathOperation.Close, PathOperation.Move, PathOperation.Line }, path.SegmentTypes);
		Assert.Equal(new[] { new PointF(1, 2), new PointF(3, 4), new PointF(10, 20), new PointF(30, 40), new PointF(-10, -20), new PointF(-30, -40) }, path.Points);
		Assert.Equal(3, path.SubPathCount);
		Assert.False(path.IsSubPathClosed(0));
		Assert.True(path.IsSubPathClosed(1));
		Assert.False(path.IsSubPathClosed(2));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void EmptyFigureStillEmitsStartPointAndOptionalClosure(bool closed)
	{
		var geometry = new PathGeometry { Figures = { new PathFigure { StartPoint = new Point(12, 34), IsClosed = closed } } };
		var path = new PathF();

		geometry.AppendPath(path);

		Assert.Equal(new PointF(12, 34), Assert.Single(path.Points));
		Assert.Equal(closed ? new[] { PathOperation.Move, PathOperation.Close } : new[] { PathOperation.Move }, path.SegmentTypes);
	}

	[Fact]
	public void EmptyGeometryDoesNotModifyExistingPath()
	{
		var path = new PathF();
		path.MoveTo(1, 2);
		path.LineTo(3, 4);

		new PathGeometry().AppendPath(path);

		Assert.Equal(new[] { PathOperation.Move, PathOperation.Line }, path.SegmentTypes);
		Assert.Equal(new[] { new PointF(1, 2), new PointF(3, 4) }, path.Points);
	}

	[Fact]
	public void ArcStartsAtPreviousSegmentEndpointRatherThanFigureStart()
	{
		var geometry = new PathGeometry
		{
			Figures =
			{
				new PathFigure
				{
					StartPoint = new Point(-50, -60),
					Segments =
					{
						new LineSegment(new Point(10, 0)),
						new ArcSegment(new Point(0, 10), new Size(10, 10), 0, SweepDirection.Clockwise, false)
					}
				}
			}
		};
		var path = new PathF();

		geometry.AppendPath(path);

		Assert.Equal(new PointF(-50, -60), path.FirstPoint);
		Assert.Equal(new PointF(10, 0), path[1]);
		Assert.Equal(0, path.LastPoint.X, 4);
		Assert.Equal(10, path.LastPoint.Y, 4);
		for (var i = 2; i < path.Count; i++)
		{
			Assert.InRange(path[i].X, -0.0001f, 10.0001f);
			Assert.InRange(path[i].Y, -0.0001f, 10.0001f);
			Assert.Equal(100, path[i].X * path[i].X + path[i].Y * path[i].Y, 3);
		}
	}

	[Fact]
	public void ReplacingFiguresThroughNullMovesBothFigureAndSegmentSubscriptions()
	{
		var oldFigure = new PathFigure();
		var geometry = new PathGeometry { Figures = { oldFigure } };
		var oldCollection = geometry.Figures;
		var invalidations = 0;
		geometry.InvalidatePathGeometryRequested += (_, _) => invalidations++;

		geometry.Figures = null;
		oldFigure.StartPoint = new Point(1, 2);
		oldFigure.Segments.Add(new LineSegment());
		oldCollection.Add(new PathFigure());
		Assert.Equal(0, invalidations);

		var newFigure = new PathFigure();
		geometry.Figures = new PathFigureCollection { newFigure };
		newFigure.StartPoint = new Point(3, 4);
		newFigure.Segments.Add(new LineSegment());
		Assert.Equal(2, invalidations);
	}

	[Fact]
	public void ReplacingRemovingAndMovingFiguresMaintainsBothSubscriptions()
	{
		var oldFigure = new PathFigure();
		var replacement = new PathFigure();
		var other = new PathFigure();
		var geometry = new PathGeometry { Figures = { oldFigure, other } };
		var invalidations = 0;
		geometry.InvalidatePathGeometryRequested += (_, _) => invalidations++;

		geometry.Figures[0] = replacement;
		Assert.Equal(1, invalidations);
		invalidations = 0;
		oldFigure.StartPoint = new Point(1, 2);
		oldFigure.Segments.Add(new LineSegment());
		Assert.Equal(0, invalidations);
		replacement.StartPoint = new Point(1, 2);
		replacement.Segments.Add(new LineSegment());
		Assert.Equal(2, invalidations);

		geometry.Figures.Move(0, 1);
		invalidations = 0;
		replacement.StartPoint = new Point(3, 4);
		replacement.Segments.Add(new LineSegment());
		other.StartPoint = new Point(3, 4);
		Assert.Equal(3, invalidations);

		geometry.Figures.Remove(replacement);
		invalidations = 0;
		replacement.StartPoint = new Point(5, 6);
		replacement.Segments.Add(new LineSegment());
		Assert.Equal(0, invalidations);
	}

	[Fact]
	public void ClearingFiguresAllowsReaddingAndResubscribingSameFigure()
	{
		var figure = new PathFigure();
		var geometry = new PathGeometry { Figures = { figure } };
		geometry.Figures.Clear();
		var invalidations = 0;
		geometry.InvalidatePathGeometryRequested += (_, _) => invalidations++;

		geometry.Figures.Add(figure);
		figure.StartPoint = new Point(1, 2);
		figure.Segments.Add(new LineSegment());

		Assert.Equal(3, invalidations);
	}

	[Fact]
	public void RemovingOneDuplicateFigureKeepsRemainingOccurrenceSubscribed()
	{
		var figure = new PathFigure();
		var geometry = new PathGeometry { Figures = { figure, figure } };
		geometry.Figures.RemoveAt(0);
		var invalidations = 0;
		geometry.InvalidatePathGeometryRequested += (_, _) => invalidations++;

		figure.StartPoint = new Point(1, 2);
		figure.Segments.Add(new LineSegment());
		Assert.Equal(2, invalidations);

		geometry.Figures.RemoveAt(0);
		invalidations = 0;
		figure.StartPoint = new Point(3, 4);
		figure.Segments.Add(new LineSegment());
		Assert.Equal(0, invalidations);
	}

	[Fact]
	public async Task SharedFiguresDoesNotRetainPathGeometry()
	{
		var sharedFigures = new PathFigureCollection();
		var geometryReference = CreateGeometryWithSharedFigures(sharedFigures);

		Assert.False(await geometryReference.WaitForCollect(),
			"PathGeometry should not be retained by a shared PathFigureCollection.");
		GC.KeepAlive(sharedFigures);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference CreateGeometryWithSharedFigures(PathFigureCollection figures)
	{
		var geometry = new PathGeometry { Figures = figures };
		return new WeakReference(geometry);
	}

	/// <summary>
	/// Figures.Clear() must unsubscribe the cleared PathFigure from the PathGeometry,
	/// otherwise the figure retains the geometry alive via its PropertyChanged delegate.
	/// </summary>
	[Fact]
	public void FiguresClear_UnsubscribesFigurePropertyChangedHandler()
	{
		var geometry = new PathGeometry();
		var sharedFigure = new PathFigure { StartPoint = new Point(0, 0) };
		geometry.Figures.Add(sharedFigure);

		int invalidateCount = 0;
		geometry.InvalidatePathGeometryRequested += (s, e) => invalidateCount++;

		// Sanity-check: mutating the figure before Clear should trigger invalidation.
		sharedFigure.StartPoint = new Point(10, 10);
		Assert.Equal(1, invalidateCount);

		// Act - Clear() fires CollectionChanged (Reset), which itself calls Invalidate() once.
		geometry.Figures.Clear();
		int countAfterClear = invalidateCount;

		// After Clear, mutating the figure must NOT trigger any further invalidation on the geometry.
		sharedFigure.StartPoint = new Point(20, 20);
		Assert.Equal(countAfterClear, invalidateCount);
	}

	/// <summary>
	/// Figures.Clear() must unsubscribe the cleared PathFigure's segment-invalidation event
	/// from the PathGeometry.
	/// </summary>
	[Fact]
	public void FiguresClear_UnsubscribesFigureSegmentInvalidateHandler()
	{
		var geometry = new PathGeometry();
		var sharedFigure = new PathFigure { StartPoint = new Point(0, 0) };
		geometry.Figures.Add(sharedFigure);

		int invalidateCount = 0;
		geometry.InvalidatePathGeometryRequested += (s, e) => invalidateCount++;

		// Sanity-check: adding a segment before Clear should trigger invalidation.
		sharedFigure.Segments.Add(new LineSegment { Point = new Point(100, 100) });
		Assert.Equal(1, invalidateCount);

		// Act - Clear() fires CollectionChanged (Reset), which itself calls Invalidate() once.
		geometry.Figures.Clear();
		int countAfterClear = invalidateCount;

		// After Clear, adding segments to the cleared figure must NOT trigger any further invalidation.
		sharedFigure.Segments.Add(new LineSegment { Point = new Point(200, 200) });
		Assert.Equal(countAfterClear, invalidateCount);
	}

	/// <summary>
	/// After Figures.Clear(), the PathGeometry must be eligible for garbage collection
	/// even when the cleared PathFigure is still alive (shared/rooted elsewhere).
	/// </summary>
	[Fact]
	public void FiguresClear_AllowsPathGeometryToBeGarbageCollected()
	{
		var sharedFigure = new PathFigure { StartPoint = new Point(0, 0) };
		var weakRef = CreateGeometryAndClear(sharedFigure);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// If the bug is present, sharedFigure still holds the geometry alive via
		// its PropertyChanged delegate chain, so TryGetTarget would return true.
		Assert.False(weakRef.TryGetTarget(out _),
			"PathGeometry was retained by the cleared PathFigure (event-handler leak in Figures.Clear()).");
	}

	// Factored out so the JIT cannot inline the PathGeometry local onto the caller's frame.
	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference<PathGeometry> CreateGeometryAndClear(PathFigure figure)
	{
		var geometry = new PathGeometry();
		geometry.Figures.Add(figure);
		geometry.Figures.Clear();
		return new WeakReference<PathGeometry>(geometry);
	}
}
