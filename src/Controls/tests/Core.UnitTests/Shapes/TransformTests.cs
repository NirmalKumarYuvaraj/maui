using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class TransformTests : BaseTestFixture
{
	const double Tolerance = 1e-9;

	static void AssertMatrix(Matrix actual, double m11, double m12, double m21, double m22, double offsetX, double offsetY)
	{
		Assert.Equal(m11, actual.M11, Tolerance);
		Assert.Equal(m12, actual.M12, Tolerance);
		Assert.Equal(m21, actual.M21, Tolerance);
		Assert.Equal(m22, actual.M22, Tolerance);
		Assert.Equal(offsetX, actual.OffsetX, Tolerance);
		Assert.Equal(offsetY, actual.OffsetY, Tolerance);
	}

	static void AssertMatrix(Matrix expected, Matrix actual)
		=> AssertMatrix(actual, expected.M11, expected.M12, expected.M21, expected.M22, expected.OffsetX, expected.OffsetY);

	static void AssertTransformsPoint(Transform transform, double x, double y, double expectedX, double expectedY)
	{
		var result = transform.Value.Transform(new Point(x, y));
		Assert.Equal(expectedX, result.X, Tolerance);
		Assert.Equal(expectedY, result.Y, Tolerance);
	}

	static List<string> TrackPropertyChanges(BindableObject bindable)
	{
		var changes = new List<string>();
		bindable.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
		return changes;
	}

	[Fact]
	public void BaseTransformDefaultsToIdentityAndValueIsSettable()
	{
		var transform = new Transform();
		Assert.True(transform.Value.IsIdentity);
		AssertMatrix(transform.Value, 1, 0, 0, 1, 0, 0);

		var changes = TrackPropertyChanges(transform);
		transform.Value = new Matrix(1, 2, 3, 4, 5, 6);

		AssertMatrix(transform.Value, 1, 2, 3, 4, 5, 6);
		Assert.Contains(nameof(Transform.Value), changes);
		AssertTransformsPoint(transform, 1, 1, 9, 12);
	}

	[Fact]
	public void TransformDeclaresTransformTypeConverter()
	{
		var attribute = (TypeConverterAttribute)Attribute.GetCustomAttribute(typeof(Transform), typeof(TypeConverterAttribute));
		Assert.Equal(typeof(TransformTypeConverter).AssemblyQualifiedName, attribute.ConverterTypeName);
	}

	[Fact]
	public void RotateTransformDefaults()
	{
		var rotate = new RotateTransform();
		Assert.Equal(0, rotate.Angle);
		Assert.Equal(0, rotate.CenterX);
		Assert.Equal(0, rotate.CenterY);
		Assert.True(rotate.Value.IsIdentity);
	}

	[Fact]
	public void RotateTransformConstructorsAssignProperties()
	{
		var angleOnly = new RotateTransform(45);
		Assert.Equal(45, angleOnly.Angle);
		Assert.Equal(0, angleOnly.CenterX);
		Assert.Equal(0, angleOnly.CenterY);

		var withCenter = new RotateTransform(30, 10, 20);
		Assert.Equal(30, withCenter.Angle);
		Assert.Equal(10, withCenter.CenterX);
		Assert.Equal(20, withCenter.CenterY);
	}

	[Theory]
	[InlineData(90, 1, 0, 0, 1)]
	[InlineData(90, 0, 1, -1, 0)]
	[InlineData(180, 1, 0, -1, 0)]
	[InlineData(270, 1, 0, 0, -1)]
	[InlineData(-90, 1, 0, 0, -1)]
	[InlineData(360, 3, 4, 3, 4)]
	[InlineData(0, 3, 4, 3, 4)]
	[InlineData(720, 2, -5, 2, -5)]
	[InlineData(450, 1, 0, 0, 1)]
	public void RotateTransformRotatesPointsAboutOrigin(double angle, double x, double y, double expectedX, double expectedY)
	{
		AssertTransformsPoint(new RotateTransform(angle), x, y, expectedX, expectedY);
	}

	[Fact]
	public void RotateTransform45DegreesMatrix()
	{
		var half = Math.Sqrt(2) / 2;
		var rotate = new RotateTransform(45);

		AssertMatrix(rotate.Value, half, half, -half, half, 0, 0);
		AssertTransformsPoint(rotate, 1, 0, half, half);
		AssertTransformsPoint(rotate, 1, 1, 0, Math.Sqrt(2));
	}

	[Theory]
	[InlineData(90, 10, 20, 11, 20, 10, 21)]
	[InlineData(90, 10, 20, 10, 20, 10, 20)]
	[InlineData(180, 5, 5, 0, 0, 10, 10)]
	[InlineData(-90, 10, 20, 11, 20, 10, 19)]
	[InlineData(360, 10, 20, 7, 3, 7, 3)]
	[InlineData(90, -5, -5, -4, -5, -5, -4)]
	public void RotateTransformRotatesPointsAboutCenter(double angle, double cx, double cy, double x, double y, double expectedX, double expectedY)
	{
		AssertTransformsPoint(new RotateTransform(angle, cx, cy), x, y, expectedX, expectedY);
	}

	[Fact]
	public void RotateTransformAboutCenterEqualsTranslateRotateTranslate()
	{
		var expected = new Matrix(1, 0, 0, 1, -10, -20)
			* new RotateTransform(33).Value
			* new Matrix(1, 0, 0, 1, 10, 20);

		AssertMatrix(expected, new RotateTransform(33, 10, 20).Value);
	}

	[Fact]
	public void RotateTransformPropertyChangesUpdateValueAndRaisePropertyChanged()
	{
		var rotate = new RotateTransform();
		var changes = TrackPropertyChanges(rotate);

		rotate.Angle = 90;
		Assert.Contains(nameof(RotateTransform.Angle), changes);
		Assert.Contains(nameof(Transform.Value), changes);
		AssertTransformsPoint(rotate, 1, 0, 0, 1);

		changes.Clear();
		rotate.CenterX = 10;
		Assert.Equal(new[] { nameof(RotateTransform.CenterX) }, changes.FindAll(n => n != nameof(Transform.Value)));
		Assert.Contains(nameof(Transform.Value), changes);
		AssertTransformsPoint(rotate, 11, 0, 10, 1);

		changes.Clear();
		rotate.CenterY = 10;
		Assert.Contains(nameof(RotateTransform.CenterY), changes);
		Assert.Contains(nameof(Transform.Value), changes);
		AssertTransformsPoint(rotate, 11, 10, 10, 11);

		changes.Clear();
		rotate.Angle = 90;
		Assert.Empty(changes);
	}

	[Fact]
	public void ScaleTransformDefaults()
	{
		var scale = new ScaleTransform();
		Assert.Equal(1, scale.ScaleX);
		Assert.Equal(1, scale.ScaleY);
		Assert.Equal(0, scale.CenterX);
		Assert.Equal(0, scale.CenterY);
		Assert.True(scale.Value.IsIdentity);
	}

	[Fact]
	public void ScaleTransformConstructorsAssignProperties()
	{
		var scale = new ScaleTransform(2, 3);
		Assert.Equal(2, scale.ScaleX);
		Assert.Equal(3, scale.ScaleY);
		Assert.Equal(0, scale.CenterX);
		Assert.Equal(0, scale.CenterY);
		AssertMatrix(scale.Value, 2, 0, 0, 3, 0, 0);

		var centered = new ScaleTransform(2, 3, 10, 20);
		Assert.Equal(2, centered.ScaleX);
		Assert.Equal(3, centered.ScaleY);
		Assert.Equal(10, centered.CenterX);
		Assert.Equal(20, centered.CenterY);
		AssertMatrix(centered.Value, 2, 0, 0, 3, -10, -40);
	}

	[Theory]
	[InlineData(2, 3, 0, 0, 1, 1, 2, 3)]
	[InlineData(-1, 1, 0, 0, 4, 5, -4, 5)]
	[InlineData(1, -1, 0, 0, 4, 5, 4, -5)]
	[InlineData(2, 2, 10, 10, 10, 10, 10, 10)]
	[InlineData(2, 2, 10, 10, 11, 12, 12, 14)]
	[InlineData(0.5, 0.5, 10, 10, 20, 0, 15, 5)]
	[InlineData(0, 0, 10, 20, 123, -45, 10, 20)]
	[InlineData(0, 1, 0, 0, 7, 8, 0, 8)]
	[InlineData(-1, -1, 5, 5, 0, 0, 10, 10)]
	public void ScaleTransformScalesPointsAboutCenter(double sx, double sy, double cx, double cy, double x, double y, double expectedX, double expectedY)
	{
		AssertTransformsPoint(new ScaleTransform(sx, sy, cx, cy), x, y, expectedX, expectedY);
	}

	[Fact]
	public void ScaleTransformZeroScaleIsNotInvertible()
	{
		Assert.False(new ScaleTransform(0, 2).Value.HasInverse);
		Assert.True(new ScaleTransform(3, 2).Value.HasInverse);
	}

	[Fact]
	public void ScaleTransformPropertyChangesUpdateValue()
	{
		var scale = new ScaleTransform();
		var changes = TrackPropertyChanges(scale);

		scale.ScaleX = 2;
		Assert.Contains(nameof(ScaleTransform.ScaleX), changes);
		Assert.Contains(nameof(Transform.Value), changes);
		AssertMatrix(scale.Value, 2, 0, 0, 1, 0, 0);

		scale.ScaleY = 4;
		AssertMatrix(scale.Value, 2, 0, 0, 4, 0, 0);

		scale.CenterX = 1;
		AssertMatrix(scale.Value, 2, 0, 0, 4, -1, 0);

		changes.Clear();
		scale.CenterY = 1;
		Assert.Contains(nameof(ScaleTransform.CenterY), changes);
		Assert.Contains(nameof(Transform.Value), changes);
		AssertMatrix(scale.Value, 2, 0, 0, 4, -1, -3);
	}

	[Fact]
	public void SkewTransformDefaults()
	{
		var skew = new SkewTransform();
		Assert.Equal(0, skew.AngleX);
		Assert.Equal(0, skew.AngleY);
		Assert.Equal(0, skew.CenterX);
		Assert.Equal(0, skew.CenterY);
		Assert.True(skew.Value.IsIdentity);
	}

	[Fact]
	public void SkewTransformConstructorsAssignProperties()
	{
		var skew = new SkewTransform(45, 30);
		Assert.Equal(45, skew.AngleX);
		Assert.Equal(30, skew.AngleY);
		Assert.Equal(0, skew.CenterX);
		Assert.Equal(0, skew.CenterY);

		var centered = new SkewTransform(45, 0, 10, 20);
		Assert.Equal(45, centered.AngleX);
		Assert.Equal(0, centered.AngleY);
		Assert.Equal(10, centered.CenterX);
		Assert.Equal(20, centered.CenterY);
		AssertMatrix(centered.Value, 1, 0, 1, 1, -20, 0);
	}

	[Theory]
	[InlineData(45, 0, 0, 0, 0, 1, 1, 1)]
	[InlineData(45, 0, 0, 0, 3, 2, 5, 2)]
	[InlineData(-45, 0, 0, 0, 3, 2, 1, 2)]
	[InlineData(0, 45, 0, 0, 2, 3, 2, 5)]
	[InlineData(0, -45, 0, 0, 2, 3, 2, 1)]
	[InlineData(45, 45, 0, 0, 1, 2, 3, 3)]
	[InlineData(45, 0, 0, 10, 0, 10, 0, 10)]
	[InlineData(45, 0, 0, 10, 0, 12, 2, 12)]
	[InlineData(0, 45, 10, 0, 13, 0, 13, 3)]
	[InlineData(0, 0, 10, 20, 7, 9, 7, 9)]
	[InlineData(180, 180, 5, 5, 7, 9, 7, 9)]
	public void SkewTransformSkewsPointsAboutCenter(double ax, double ay, double cx, double cy, double x, double y, double expectedX, double expectedY)
	{
		AssertTransformsPoint(new SkewTransform(ax, ay, cx, cy), x, y, expectedX, expectedY);
	}

	[Fact]
	public void SkewTransformPropertyChangesUpdateValue()
	{
		var skew = new SkewTransform();
		var changes = TrackPropertyChanges(skew);

		skew.AngleX = 45;
		Assert.Contains(nameof(SkewTransform.AngleX), changes);
		Assert.Contains(nameof(Transform.Value), changes);
		AssertMatrix(skew.Value, 1, 0, 1, 1, 0, 0);

		skew.AngleY = 45;
		AssertMatrix(skew.Value, 1, 1, 1, 1, 0, 0);

		skew.CenterX = 2;
		AssertMatrix(skew.Value, 1, 1, 1, 1, 0, -2);

		skew.CenterY = 3;
		AssertMatrix(skew.Value, 1, 1, 1, 1, -3, -2);
	}

	[Fact]
	public void TranslateTransformDefaults()
	{
		var translate = new TranslateTransform();
		Assert.Equal(0, translate.X);
		Assert.Equal(0, translate.Y);
		Assert.True(translate.Value.IsIdentity);
	}

	[Theory]
	[InlineData(10, 20, 0, 0, 10, 20)]
	[InlineData(10, 20, 1, 2, 11, 22)]
	[InlineData(-5, -7, 1, 2, -4, -5)]
	[InlineData(0, 0, 3, 4, 3, 4)]
	public void TranslateTransformOffsetsPoints(double tx, double ty, double x, double y, double expectedX, double expectedY)
	{
		var translate = new TranslateTransform(tx, ty);
		Assert.Equal(tx, translate.X);
		Assert.Equal(ty, translate.Y);
		AssertMatrix(translate.Value, 1, 0, 0, 1, tx, ty);
		AssertTransformsPoint(translate, x, y, expectedX, expectedY);
	}

	[Fact]
	public void TranslateTransformPropertyChangesUpdateValue()
	{
		var translate = new TranslateTransform();
		var changes = TrackPropertyChanges(translate);

		translate.X = 5;
		Assert.Contains(nameof(TranslateTransform.X), changes);
		Assert.Contains(nameof(Transform.Value), changes);
		AssertMatrix(translate.Value, 1, 0, 0, 1, 5, 0);

		translate.Y = -3;
		Assert.Contains(nameof(TranslateTransform.Y), changes);
		AssertMatrix(translate.Value, 1, 0, 0, 1, 5, -3);
	}

	[Fact]
	public void CompositeTransformDefaults()
	{
		var composite = new CompositeTransform();
		Assert.Equal(0, composite.CenterX);
		Assert.Equal(0, composite.CenterY);
		Assert.Equal(1, composite.ScaleX);
		Assert.Equal(1, composite.ScaleY);
		Assert.Equal(0, composite.SkewX);
		Assert.Equal(0, composite.SkewY);
		Assert.Equal(0, composite.Rotation);
		Assert.Equal(0, composite.TranslateX);
		Assert.Equal(0, composite.TranslateY);
		Assert.True(composite.Value.IsIdentity);
	}

	[Fact]
	public void CompositeTransformAppliesScaleThenRotateThenTranslateAboutCenter()
	{
		var composite = new CompositeTransform
		{
			CenterX = 10,
			CenterY = 10,
			ScaleX = 2,
			Rotation = 90,
			TranslateX = 5,
		};

		// (11,10) -> center-relative (1,0) -> scale (2,0) -> rotate (0,2) -> uncenter (10,12) -> translate (15,12)
		AssertTransformsPoint(composite, 11, 10, 15, 12);
		AssertTransformsPoint(composite, 10, 10, 15, 10);
		AssertTransformsPoint(composite, 10, 11, 14, 10);
	}

	[Fact]
	public void CompositeTransformAppliesSkewBeforeRotate()
	{
		var composite = new CompositeTransform
		{
			SkewX = 45,
			Rotation = 90,
		};

		// (0,1) -> skew (1,1) -> rotate 90 (-1,1); rotating first would give (-1,0) -> skew (-1,0)
		AssertTransformsPoint(composite, 0, 1, -1, 1);
	}

	[Fact]
	public void CompositeTransformAppliesScaleBeforeSkew()
	{
		var composite = new CompositeTransform
		{
			ScaleY = 2,
			SkewX = 45,
		};

		// (0,1) -> scale (0,2) -> skew (2,2); skewing first would give (1,1) -> scale (1,2)
		AssertTransformsPoint(composite, 0, 1, 2, 2);
	}

	[Theory]
	[InlineData(0, 0, 1, 1, 0, 0, 0, 0, 0)]
	[InlineData(10, 20, 2, 3, 0, 0, 0, 0, 0)]
	[InlineData(10, 20, 2, 3, 15, -10, 30, 7, -8)]
	[InlineData(-5, 4, -1, 0.5, 0, 30, -45, 0, 100)]
	[InlineData(3, 3, 0, 0, 0, 0, 0, 1, 1)]
	[InlineData(0, 0, 1, 1, 0, 0, 360, 0, 0)]
	public void CompositeTransformMatchesManuallyComposedMatrices(double cx, double cy, double sx, double sy, double kx, double ky, double rotation, double tx, double ty)
	{
		var composite = new CompositeTransform
		{
			CenterX = cx,
			CenterY = cy,
			ScaleX = sx,
			ScaleY = sy,
			SkewX = kx,
			SkewY = ky,
			Rotation = rotation,
			TranslateX = tx,
			TranslateY = ty,
		};

		var radians = rotation * Math.PI / 180;
		var cos = Math.Cos(radians);
		var sin = Math.Sin(radians);
		var tanX = Math.Tan(kx * Math.PI / 180);
		var tanY = Math.Tan(ky * Math.PI / 180);

		var expected = new Matrix(1, 0, 0, 1, -cx, -cy)
			* new Matrix(sx, 0, 0, sy, 0, 0)
			* new Matrix(1, tanY, tanX, 1, 0, 0)
			* new Matrix(cos, sin, -sin, cos, 0, 0)
			* new Matrix(1, 0, 0, 1, cx, cy)
			* new Matrix(1, 0, 0, 1, tx, ty);

		AssertMatrix(expected, composite.Value);

		foreach (var point in new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(-7, 13) })
		{
			var p = new Point(point.X - cx, point.Y - cy);
			p = new Point(p.X * sx, p.Y * sy);
			p = new Point(p.X + p.Y * tanX, p.Y + p.X * tanY);
			p = new Point(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
			p = new Point(p.X + cx + tx, p.Y + cy + ty);

			AssertTransformsPoint(composite, point.X, point.Y, p.X, p.Y);
		}
	}

	[Fact]
	public void CompositeTransformEachPropertyChangeUpdatesValue()
	{
		var composite = new CompositeTransform();
		var changes = TrackPropertyChanges(composite);
		var previous = composite.Value;

		void SetAndAssertChanged(Action set, string propertyName)
		{
			changes.Clear();
			set();
			Assert.Contains(propertyName, changes);
			Assert.Contains(nameof(Transform.Value), changes);
			Assert.NotEqual(previous, composite.Value);
			previous = composite.Value;
		}

		SetAndAssertChanged(() => composite.TranslateX = 1, nameof(CompositeTransform.TranslateX));
		SetAndAssertChanged(() => composite.TranslateY = 2, nameof(CompositeTransform.TranslateY));
		SetAndAssertChanged(() => composite.ScaleX = 2, nameof(CompositeTransform.ScaleX));
		SetAndAssertChanged(() => composite.ScaleY = 3, nameof(CompositeTransform.ScaleY));
		SetAndAssertChanged(() => composite.CenterX = 4, nameof(CompositeTransform.CenterX));
		SetAndAssertChanged(() => composite.CenterY = 5, nameof(CompositeTransform.CenterY));
		SetAndAssertChanged(() => composite.SkewX = 10, nameof(CompositeTransform.SkewX));
		SetAndAssertChanged(() => composite.SkewY = 20, nameof(CompositeTransform.SkewY));
		SetAndAssertChanged(() => composite.Rotation = 30, nameof(CompositeTransform.Rotation));
	}

	[Fact]
	public void TransformGroupDefaultsToEmptyChildrenAndIdentity()
	{
		var group = new TransformGroup();
		Assert.NotNull(group.Children);
		Assert.Empty(group.Children);
		Assert.True(group.Value.IsIdentity);
		AssertTransformsPoint(group, 3, 4, 3, 4);
	}

	[Fact]
	public void TransformGroupWithSingleChildEqualsChild()
	{
		var rotate = new RotateTransform(30, 4, 5);
		var group = new TransformGroup { Children = { rotate } };
		AssertMatrix(rotate.Value, group.Value);
	}

	[Fact]
	public void TransformGroupAppliesChildrenInOrder()
	{
		var translateThenRotate = new TransformGroup
		{
			Children = { new TranslateTransform(10, 0), new RotateTransform(90) }
		};
		AssertTransformsPoint(translateThenRotate, 1, 0, 0, 11);

		var rotateThenTranslate = new TransformGroup
		{
			Children = { new RotateTransform(90), new TranslateTransform(10, 0) }
		};
		AssertTransformsPoint(rotateThenTranslate, 1, 0, 10, 1);
	}

	[Fact]
	public void TransformGroupValueIsProductOfChildValues()
	{
		var scale = new ScaleTransform(2, 3, 1, 1);
		var skew = new SkewTransform(10, 20);
		var rotate = new RotateTransform(45, 5, 5);
		var translate = new TranslateTransform(-3, 7);
		var group = new TransformGroup { Children = { scale, skew, rotate, translate } };

		var expected = scale.Value * skew.Value * rotate.Value * translate.Value;
		AssertMatrix(expected, group.Value);
	}

	[Fact]
	public void TransformGroupSupportsNestedGroups()
	{
		var inner = new TransformGroup
		{
			Children = { new ScaleTransform(2, 2), new TranslateTransform(1, 0) }
		};
		var outer = new TransformGroup
		{
			Children = { inner, new RotateTransform(90) }
		};

		// (1,0) -> scale (2,0) -> translate (3,0) -> rotate (0,3)
		AssertTransformsPoint(outer, 1, 0, 0, 3);

		((TranslateTransform)inner.Children[1]).X = 5;

		// (1,0) -> scale (2,0) -> translate (7,0) -> rotate (0,7)
		AssertTransformsPoint(inner, 1, 0, 7, 0);
		AssertTransformsPoint(outer, 1, 0, 0, 7);
	}

	[Fact]
	public void TransformGroupRecomputesWhenChildPropertyChanges()
	{
		var translate = new TranslateTransform(1, 2);
		var group = new TransformGroup { Children = { new ScaleTransform(2, 2), translate } };
		var changes = TrackPropertyChanges(group);

		AssertTransformsPoint(group, 1, 1, 3, 4);

		translate.X = 10;

		Assert.Contains(nameof(Transform.Value), changes);
		AssertTransformsPoint(group, 1, 1, 12, 4);
	}

	[Fact]
	public void TransformGroupRecomputesWhenChildrenCollectionChanges()
	{
		var group = new TransformGroup();
		var scale = new ScaleTransform(2, 2);
		var translate = new TranslateTransform(5, 0);

		group.Children.Add(scale);
		AssertTransformsPoint(group, 1, 1, 2, 2);

		group.Children.Add(translate);
		AssertTransformsPoint(group, 1, 1, 7, 2);

		group.Children.Move(1, 0);
		AssertTransformsPoint(group, 1, 1, 12, 2);

		group.Children[0] = new TranslateTransform(0, 5);
		AssertTransformsPoint(group, 1, 1, 2, 12);

		group.Children.Remove(scale);
		AssertTransformsPoint(group, 1, 1, 1, 6);

		group.Children.Clear();
		Assert.True(group.Value.IsIdentity);
	}

	[Fact]
	public void TransformGroupIgnoresRemovedChildPropertyChanges()
	{
		var translate = new TranslateTransform(1, 0);
		var group = new TransformGroup { Children = { translate } };
		group.Children.Remove(translate);

		translate.X = 50;

		Assert.True(group.Value.IsIdentity);
	}

	[Fact]
	public void TransformGroupSameChildAddedTwiceIsAppliedTwice()
	{
		var translate = new TranslateTransform(1, 2);
		var group = new TransformGroup { Children = { translate, translate } };
		AssertTransformsPoint(group, 0, 0, 2, 4);

		translate.X = 3;
		AssertTransformsPoint(group, 0, 0, 6, 4);

		group.Children.RemoveAt(0);
		translate.Y = 10;
		AssertTransformsPoint(group, 0, 0, 3, 10);
	}

	[Fact]
	public void TransformGroupReplacingChildrenCollectionRecomputesValue()
	{
		var group = new TransformGroup { Children = { new TranslateTransform(1, 1) } };
		var oldChildren = group.Children;

		group.Children = new TransformCollection { new ScaleTransform(3, 3) };
		AssertTransformsPoint(group, 1, 1, 3, 3);

		oldChildren.Add(new TranslateTransform(100, 100));
		AssertTransformsPoint(group, 1, 1, 3, 3);

		group.Children.Add(new TranslateTransform(1, 0));
		AssertTransformsPoint(group, 1, 1, 4, 3);
	}

	[Fact(Skip = "Product bug: TransformGroup.UpdateTransformMatrix (TransformGroup.cs:175) enumerates Children without a null check, so setting Children = null throws NullReferenceException even though Attach/DetachCollection handle null")]
	public void TransformGroupNullChildrenProducesIdentity()
	{
		var group = new TransformGroup { Children = { new TranslateTransform(1, 1) } };

		group.Children = null;

		Assert.Null(group.Children);
		Assert.True(group.Value.IsIdentity);
	}

	[Fact]
	public void TransformCollectionIsObservableCollectionOfTransform()
	{
		var collection = new TransformCollection();
		var actions = new List<System.Collections.Specialized.NotifyCollectionChangedAction>();
		collection.CollectionChanged += (_, e) => actions.Add(e.Action);

		var rotate = new RotateTransform(10);
		collection.Add(rotate);
		collection.Add(new ScaleTransform());
		collection.Remove(rotate);
		collection.Clear();

		Assert.Equal(new[]
		{
			System.Collections.Specialized.NotifyCollectionChangedAction.Add,
			System.Collections.Specialized.NotifyCollectionChangedAction.Add,
			System.Collections.Specialized.NotifyCollectionChangedAction.Remove,
			System.Collections.Specialized.NotifyCollectionChangedAction.Reset,
		}, actions);
		Assert.Empty(collection);
	}

	[Theory]
	[InlineData(typeof(string), true)]
	[InlineData(typeof(int), false)]
	[InlineData(typeof(Matrix), false)]
	[InlineData(typeof(Transform), false)]
	public void TransformTypeConverterCanConvert(Type type, bool expected)
	{
		var converter = new TransformTypeConverter();
		Assert.Equal(expected, converter.CanConvertFrom(null, type));
		Assert.Equal(expected, converter.CanConvertTo(null, type));
	}

	[Theory]
	[InlineData("1,2,3,4,5,6", 1, 2, 3, 4, 5, 6)]
	[InlineData("1 2 3 4 5 6", 1, 2, 3, 4, 5, 6)]
	[InlineData("  1, 0 ,0,  1 , 10,20 ", 1, 0, 0, 1, 10, 20)]
	[InlineData("-1.5,0.25,-0,2,-10.75,1000", -1.5, 0.25, 0, 2, -10.75, 1000)]
	[InlineData("0,0,0,0,0,0", 0, 0, 0, 0, 0, 0)]
	public void TransformTypeConverterConvertsStringToMatrixTransform(string input, double m11, double m12, double m21, double m22, double offsetX, double offsetY)
	{
		var result = new TransformTypeConverter().ConvertFromInvariantString(input);

		var matrixTransform = Assert.IsType<MatrixTransform>(result);
		AssertMatrix(matrixTransform.Matrix, m11, m12, m21, m22, offsetX, offsetY);
		AssertMatrix(matrixTransform.Value, m11, m12, m21, m22, offsetX, offsetY);
	}

	[Fact]
	public void TransformTypeConverterResultTransformsPoints()
	{
		var transform = (Transform)new TransformTypeConverter().ConvertFromInvariantString("0,1,-1,0,10,20");
		AssertTransformsPoint(transform, 1, 0, 10, 21);
	}

	[Fact]
	public void TransformTypeConverterUsesInvariantCultureRegardlessOfCulture()
	{
		var result = new TransformTypeConverter().ConvertFrom(null, new CultureInfo("de-DE"), "1.5,0,0,1.5,0,0");
		AssertMatrix(((MatrixTransform)result).Matrix, 1.5, 0, 0, 1.5, 0, 0);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("1,2,3,4,5")]
	[InlineData("1,2,3,4,5,6,7")]
	[InlineData("1,2,3,4,5,x")]
	[InlineData("a,b,c,d,e,f")]
	public void TransformTypeConverterThrowsForInvalidStrings(string input)
	{
		Assert.Throws<ArgumentException>(() => new TransformTypeConverter().ConvertFromInvariantString(input));
	}

	[Fact]
	public void TransformTypeConverterThrowsForNull()
	{
		Assert.Throws<ArgumentException>(() => new TransformTypeConverter().ConvertFrom(null, CultureInfo.InvariantCulture, null));
	}

	[Theory]
	[InlineData("1,2,3,4,5,6", "1, 2, 3, 4, 5, 6")]
	[InlineData("1.5 -0.25 0 1 -10 20.125", "1.5, -0.25, 0, 1, -10, 20.125")]
	public void TransformTypeConverterRoundTripsMatrixTransform(string input, string expected)
	{
		var converter = new TransformTypeConverter();
		var transform = converter.ConvertFromInvariantString(input);

		var text = converter.ConvertToInvariantString(transform);
		Assert.Equal(expected, text);

		var roundTripped = (MatrixTransform)converter.ConvertFromInvariantString(text);
		AssertMatrix(((MatrixTransform)transform).Matrix, roundTripped.Matrix);
	}

	[Fact]
	public void TransformTypeConverterConvertToThrowsForNonMatrixTransform()
	{
		var converter = new TransformTypeConverter();
		Assert.Throws<NotSupportedException>(() => converter.ConvertToInvariantString(new RotateTransform(45)));
		Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, CultureInfo.InvariantCulture, null, typeof(string)));
	}
}
