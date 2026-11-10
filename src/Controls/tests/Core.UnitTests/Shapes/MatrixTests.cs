using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests.Shapes;

public class MatrixTests : BaseTestFixture
{
	const double Tolerance = 1e-4;

	// One representative of every internal MatrixTypes fast path.
	public static readonly string[] Kinds = { "Identity", "Default", "Translation", "Scaling", "ScalingTranslation", "Unknown" };

	static Matrix Create(string kind) => kind switch
	{
		"Identity" => Matrix.Identity,
		"Default" => new Matrix(),
		"Translation" => new Matrix(1, 0, 0, 1, 3, -4),
		"Scaling" => new Matrix(2, 0, 0, -0.5, 0, 0),
		"ScalingTranslation" => new Matrix(2, 0, 0, 3, 4, 5),
		"Unknown" => new Matrix(1, 2, 3, 4, 5, 6),
		_ => throw new ArgumentOutOfRangeException(nameof(kind)),
	};

	static Matrix3x2 CreateExpected(string kind) => kind switch
	{
		"Identity" or "Default" => Matrix3x2.Identity,
		"Translation" => new Matrix3x2(1, 0, 0, 1, 3, -4),
		"Scaling" => new Matrix3x2(2, 0, 0, -0.5f, 0, 0),
		"ScalingTranslation" => new Matrix3x2(2, 0, 0, 3, 4, 5),
		"Unknown" => new Matrix3x2(1, 2, 3, 4, 5, 6),
		_ => throw new ArgumentOutOfRangeException(nameof(kind)),
	};

	public static IEnumerable<object[]> AllKinds()
	{
		foreach (var kind in Kinds)
			yield return new object[] { kind };
	}

	public static IEnumerable<object[]> KindPairs()
	{
		foreach (var a in Kinds)
			foreach (var b in Kinds)
				yield return new object[] { a, b };
	}

	static void AssertMatrix(Matrix3x2 expected, Matrix actual)
	{
		Assert.Equal(expected.M11, actual.M11, Tolerance);
		Assert.Equal(expected.M12, actual.M12, Tolerance);
		Assert.Equal(expected.M21, actual.M21, Tolerance);
		Assert.Equal(expected.M22, actual.M22, Tolerance);
		Assert.Equal(expected.M31, actual.OffsetX, Tolerance);
		Assert.Equal(expected.M32, actual.OffsetY, Tolerance);

		// Transforming points exercises the type-specialised MultiplyPoint path,
		// which must agree with the public element values.
		foreach (var p in new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(-2.5, 7) })
		{
			var e = Vector2.Transform(new Vector2((float)p.X, (float)p.Y), expected);
			var a = actual.Transform(p);
			Assert.Equal(e.X, a.X, Tolerance);
			Assert.Equal(e.Y, a.Y, Tolerance);
		}
	}

	static void AssertMatrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY, Matrix actual) =>
		AssertMatrix(new Matrix3x2((float)m11, (float)m12, (float)m21, (float)m22, (float)offsetX, (float)offsetY), actual);

	static float Radians(double degrees) => (float)(degrees * Math.PI / 180.0);

	[Fact]
	public void ConstructorStoresAllElements()
	{
		var m = new Matrix(1.5, -2, 3.25, 4, -5, 6.75);

		Assert.Equal(1.5, m.M11);
		Assert.Equal(-2, m.M12);
		Assert.Equal(3.25, m.M21);
		Assert.Equal(4, m.M22);
		Assert.Equal(-5, m.OffsetX);
		Assert.Equal(6.75, m.OffsetY);
		Assert.False(m.IsIdentity);
	}

	[Fact]
	public void IdentityHasIdentityElements()
	{
		var m = Matrix.Identity;

		Assert.True(m.IsIdentity);
		AssertMatrix(Matrix3x2.Identity, m);
		Assert.Equal(1, m.Determinant);
		Assert.True(m.HasInverse);
	}

	[Fact]
	public void DefaultMatrixBehavesAsIdentity()
	{
		var m = new Matrix();

		Assert.True(m.IsIdentity);
		AssertMatrix(Matrix3x2.Identity, m);
		Assert.Equal(1, m.Determinant);
	}

	[Fact]
	public void ConstructingWithIdentityValuesIsIdentityAndEqualsIdentity()
	{
		var m = new Matrix(1, 0, 0, 1, 0, 0);

		Assert.True(m.IsIdentity);
		Assert.Equal(Matrix.Identity, m);
	}

	[Theory]
	[InlineData(1, 0, 0, 1, 1, 0)]
	[InlineData(1, 0, 0, 1, 0, 1)]
	[InlineData(2, 0, 0, 1, 0, 0)]
	[InlineData(1, 0, 0, 2, 0, 0)]
	[InlineData(1, 1, 0, 1, 0, 0)]
	[InlineData(1, 0, 1, 1, 0, 0)]
	public void AnyNonIdentityElementMakesMatrixNonIdentity(double m11, double m12, double m21, double m22, double dx, double dy)
	{
		Assert.False(new Matrix(m11, m12, m21, m22, dx, dy).IsIdentity);
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void ElementsMatchConstructionForEveryMatrixType(string kind)
	{
		AssertMatrix(CreateExpected(kind), Create(kind));
	}

	[Fact]
	public void SetIdentityResetsMatrixToIdentity()
	{
		var m = new Matrix(1, 2, 3, 4, 5, 6);

		m.SetIdentity();

		Assert.True(m.IsIdentity);
		AssertMatrix(Matrix3x2.Identity, m);
		Assert.Equal(1, m.Determinant);
	}

	[Fact(Skip = "Product bug: SetIdentity only changes the internal type flag; Equals compares stale raw fields so the result is not equal to Matrix.Identity")]
	public void SetIdentityMatrixEqualsIdentity()
	{
		var m = new Matrix(1, 2, 3, 4, 5, 6);

		m.SetIdentity();

		Assert.Equal(Matrix.Identity, m);
		Assert.True(m == Matrix.Identity);
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void DeterminantMatchesGeneralFormula(string kind)
	{
		Assert.Equal(CreateExpected(kind).GetDeterminant(), Create(kind).Determinant, Tolerance);
	}

	[Theory]
	[InlineData(0, 0, 0, 1, 0, 0)]
	[InlineData(1, 0, 0, 0, 5, 5)]
	[InlineData(1, 2, 2, 4, 0, 0)]
	[InlineData(2, 4, 1, 2, 3, 4)]
	public void SingularMatrixHasNoInverseAndInvertThrows(double m11, double m12, double m21, double m22, double dx, double dy)
	{
		var m = new Matrix(m11, m12, m21, m22, dx, dy);

		Assert.Equal(0, m.Determinant);
		Assert.False(m.HasInverse);
		Assert.Throws<InvalidOperationException>(() => m.Invert());
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void InvertProducesMathematicalInverse(string kind)
	{
		var m = Create(kind);
		Assert.True(m.HasInverse);
		Assert.True(Matrix3x2.Invert(CreateExpected(kind), out var expected));

		var inverse = m;
		inverse.Invert();

		AssertMatrix(expected, inverse);
		AssertMatrix(Matrix3x2.Identity, m * inverse);
		AssertMatrix(Matrix3x2.Identity, inverse * m);
	}

	[Fact]
	public void InvertScaleTranslationHandComputed()
	{
		// x' = 2x + 4 -> x = 0.5x' - 2 ; y' = 4y - 8 -> y = 0.25y' + 2
		var m = new Matrix(2, 0, 0, 4, 4, -8);

		m.Invert();

		AssertMatrix(0.5, 0, 0, 0.25, -2, 2, m);
	}

	[Theory]
	[MemberData(nameof(KindPairs))]
	public void MultiplyMatchesGeneralMatrixProductForAllTypeCombinations(string left, string right)
	{
		var expected = CreateExpected(left) * CreateExpected(right);

		AssertMatrix(expected, Create(left) * Create(right));
		AssertMatrix(expected, Matrix.Multiply(Create(left), Create(right)));
	}

	[Theory]
	[MemberData(nameof(KindPairs))]
	public void AppendAndPrependMatchGeneralMatrixProduct(string target, string other)
	{
		var appended = Create(target);
		appended.Append(Create(other));
		AssertMatrix(CreateExpected(target) * CreateExpected(other), appended);

		var prepended = Create(target);
		prepended.Prepend(Create(other));
		AssertMatrix(CreateExpected(other) * CreateExpected(target), prepended);
	}

	[Fact]
	public void MultiplyDoesNotMutateOperands()
	{
		var a = new Matrix(1, 2, 3, 4, 5, 6);
		var b = new Matrix(2, 0, 0, 3, 4, 5);

		_ = a * b;

		AssertMatrix(1, 2, 3, 4, 5, 6, a);
		AssertMatrix(2, 0, 0, 3, 4, 5, b);
	}

	[Fact]
	public void MultiplyHandComputed()
	{
		// Scale by 2, then translate by (10, 20): (1,1) -> (12, 22)
		var m = new Matrix(2, 0, 0, 2, 0, 0) * new Matrix(1, 0, 0, 1, 10, 20);

		AssertMatrix(2, 0, 0, 2, 10, 20, m);
		Assert.Equal(new Point(12, 22), m.Transform(new Point(1, 1)));
	}

	[Theory]
	[InlineData("Identity", 90)]
	[InlineData("Translation", 30)]
	[InlineData("Scaling", -45)]
	[InlineData("ScalingTranslation", 180)]
	[InlineData("Unknown", 400)]
	[InlineData("Unknown", -720)]
	public void RotateAndRotatePrependMatchRotationMatrix(string kind, double angle)
	{
		var rotation = Matrix3x2.CreateRotation(Radians(angle % 360));

		var appended = Create(kind);
		appended.Rotate(angle);
		AssertMatrix(CreateExpected(kind) * rotation, appended);

		var prepended = Create(kind);
		prepended.RotatePrepend(angle);
		AssertMatrix(rotation * CreateExpected(kind), prepended);
	}

	[Fact]
	public void Rotate90DegreesMapsXAxisToYAxis()
	{
		var m = Matrix.Identity;
		m.Rotate(90);

		var p = m.Transform(new Point(1, 0));

		Assert.Equal(0, p.X, Tolerance);
		Assert.Equal(1, p.Y, Tolerance);
	}

	[Theory]
	[InlineData("Identity", 90, 10, 20)]
	[InlineData("Translation", 45, -3, 7)]
	[InlineData("ScalingTranslation", 370, 1, 1)]
	[InlineData("Unknown", -60, 5, -5)]
	public void RotateAtAndRotateAtPrependMatchRotationAboutCenter(string kind, double angle, double cx, double cy)
	{
		var rotation = Matrix3x2.CreateRotation(Radians(angle % 360), new Vector2((float)cx, (float)cy));

		var appended = Create(kind);
		appended.RotateAt(angle, cx, cy);
		AssertMatrix(CreateExpected(kind) * rotation, appended);

		var prepended = Create(kind);
		prepended.RotateAtPrepend(angle, cx, cy);
		AssertMatrix(rotation * CreateExpected(kind), prepended);
	}

	[Fact]
	public void RotateAtKeepsCenterFixed()
	{
		var m = Matrix.Identity;
		m.RotateAt(73, 10, 20);

		var center = m.Transform(new Point(10, 20));

		Assert.Equal(10, center.X, Tolerance);
		Assert.Equal(20, center.Y, Tolerance);
	}

	[Theory]
	[InlineData("Identity", 2, 3)]
	[InlineData("Translation", 2, 3)]
	[InlineData("Scaling", 4, 0.5)]
	[InlineData("ScalingTranslation", -1, 2)]
	[InlineData("Unknown", 0.5, -2)]
	public void ScaleAndScalePrependMatchScaleMatrix(string kind, double sx, double sy)
	{
		var scale = Matrix3x2.CreateScale((float)sx, (float)sy);

		var appended = Create(kind);
		appended.Scale(sx, sy);
		AssertMatrix(CreateExpected(kind) * scale, appended);

		var prepended = Create(kind);
		prepended.ScalePrepend(sx, sy);
		AssertMatrix(scale * CreateExpected(kind), prepended);
	}

	[Theory]
	[InlineData("Identity", 2, 3, 10, 20)]
	[InlineData("Translation", 2, 3, -1, 1)]
	[InlineData("Scaling", 4, 0.5, 2, 2)]
	[InlineData("ScalingTranslation", -1, 2, 3, 4)]
	[InlineData("Unknown", 0.5, -2, 6, -6)]
	public void ScaleAtAndScaleAtPrependMatchScaleAboutCenter(string kind, double sx, double sy, double cx, double cy)
	{
		var scale = Matrix3x2.CreateScale((float)sx, (float)sy, new Vector2((float)cx, (float)cy));

		var appended = Create(kind);
		appended.ScaleAt(sx, sy, cx, cy);
		AssertMatrix(CreateExpected(kind) * scale, appended);

		var prepended = Create(kind);
		prepended.ScaleAtPrepend(sx, sy, cx, cy);
		AssertMatrix(scale * CreateExpected(kind), prepended);
	}

	[Fact]
	public void ScaleAtKeepsCenterFixed()
	{
		var m = Matrix.Identity;
		m.ScaleAt(3, 5, 10, 20);

		Assert.Equal(new Point(10, 20), m.Transform(new Point(10, 20)));
		Assert.Equal(new Point(13, 25), m.Transform(new Point(11, 21)));
	}

	[Theory]
	[InlineData("Identity", 30, 0)]
	[InlineData("Translation", 0, 45)]
	[InlineData("Scaling", 20, -10)]
	[InlineData("ScalingTranslation", 15, 25)]
	[InlineData("Unknown", 405, -370)]
	public void SkewAndSkewPrependMatchSkewMatrix(string kind, double skewX, double skewY)
	{
		var skew = Matrix3x2.CreateSkew(Radians(skewX % 360), Radians(skewY % 360));

		var appended = Create(kind);
		appended.Skew(skewX, skewY);
		AssertMatrix(CreateExpected(kind) * skew, appended);

		var prepended = Create(kind);
		prepended.SkewPrepend(skewX, skewY);
		AssertMatrix(skew * CreateExpected(kind), prepended);
	}

	[Fact]
	public void Skew45DegreesHorizontallyShiftsXByY()
	{
		var m = Matrix.Identity;
		m.Skew(45, 0);

		var p = m.Transform(new Point(0, 2));

		Assert.Equal(2, p.X, Tolerance);
		Assert.Equal(2, p.Y, Tolerance);
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void TranslateAndTranslatePrependMatchTranslationMatrix(string kind)
	{
		var translation = Matrix3x2.CreateTranslation(7, -9);

		var appended = Create(kind);
		appended.Translate(7, -9);
		AssertMatrix(CreateExpected(kind) * translation, appended);

		var prepended = Create(kind);
		prepended.TranslatePrepend(7, -9);
		AssertMatrix(translation * CreateExpected(kind), prepended);
	}

	[Fact]
	public void TranslateThenScaleDiffersFromScaleThenTranslate()
	{
		var translateFirst = Matrix.Identity;
		translateFirst.Translate(10, 0);
		translateFirst.Scale(2, 2);

		var scaleFirst = Matrix.Identity;
		scaleFirst.Scale(2, 2);
		scaleFirst.Translate(10, 0);

		Assert.Equal(new Point(20, 0), translateFirst.Transform(new Point(0, 0)));
		Assert.Equal(new Point(10, 0), scaleFirst.Transform(new Point(0, 0)));
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void TransformPointArrayTransformsEveryPointInPlace(string kind)
	{
		var m = Create(kind);
		var expected = CreateExpected(kind);
		var points = new[] { new Point(0, 0), new Point(1, 2), new Point(-3, 4.5) };
		var originals = (Point[])points.Clone();

		m.Transform(points);

		for (int i = 0; i < points.Length; i++)
		{
			var e = Vector2.Transform(new Vector2((float)originals[i].X, (float)originals[i].Y), expected);
			Assert.Equal(e.X, points[i].X, Tolerance);
			Assert.Equal(e.Y, points[i].Y, Tolerance);
		}
	}

	[Fact]
	public void TransformNullPointArrayDoesNothing()
	{
		var m = new Matrix(1, 2, 3, 4, 5, 6);
		m.Transform((Point[])null);
	}

	[Fact]
	public void TransformPointHandComputed()
	{
		// x' = 1*x + 3*y + 5, y' = 2*x + 4*y + 6
		var m = new Matrix(1, 2, 3, 4, 5, 6);

		Assert.Equal(new Point(5 + 1 + 6, 6 + 2 + 8), m.Transform(new Point(1, 2)));
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void MultiplyVectorIgnoresTranslation(string kind)
	{
		var m = Create(kind);
		var expected = Vector2.TransformNormal(new Vector2(3, -2), CreateExpected(kind));
		double x = 3, y = -2;

		m.MultiplyVector(ref x, ref y);

		Assert.Equal(expected.X, x, Tolerance);
		Assert.Equal(expected.Y, y, Tolerance);
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void ElementSettersProduceMatchingMatrixFromEveryStartingType(string kind)
	{
		var source = CreateExpected(kind);

		var m11 = Create(kind);
		m11.M11 = 7;
		AssertMatrix(source with { M11 = 7 }, m11);

		var m12 = Create(kind);
		m12.M12 = 7;
		AssertMatrix(source with { M12 = 7 }, m12);

		var m21 = Create(kind);
		m21.M21 = 7;
		AssertMatrix(source with { M21 = 7 }, m21);

		var m22 = Create(kind);
		m22.M22 = 7;
		AssertMatrix(source with { M22 = 7 }, m22);

		var offsetX = Create(kind);
		offsetX.OffsetX = 7;
		AssertMatrix(source with { M31 = 7 }, offsetX);

		var offsetY = Create(kind);
		offsetY.OffsetY = 7;
		AssertMatrix(source with { M32 = 7 }, offsetY);
	}

	[Fact]
	public void SettingAllElementsOnDefaultMatrixProducesThatMatrix()
	{
		var m = new Matrix
		{
			M11 = 1,
			M12 = 2,
			M21 = 3,
			M22 = 4,
			OffsetX = 5,
			OffsetY = 6,
		};

		AssertMatrix(1, 2, 3, 4, 5, 6, m);
		Assert.Equal(-2, m.Determinant, Tolerance);
	}

	[Fact]
	public void EqualityOperatorsAndEqualsCompareValues()
	{
		var a = new Matrix(1, 2, 3, 4, 5, 6);
		var b = new Matrix(1, 2, 3, 4, 5, 6);
		var c = new Matrix(1, 2, 3, 4, 5, 7);

		Assert.True(a == b);
		Assert.False(a != b);
		Assert.True(a.Equals(b));
		Assert.True(a.Equals((object)b));
		Assert.Equal(a.GetHashCode(), b.GetHashCode());

		Assert.False(a == c);
		Assert.True(a != c);
		Assert.False(a.Equals(c));
		Assert.False(a.Equals((object)c));
	}

	[Fact]
	public void EqualsObjectReturnsFalseForNullAndOtherTypes()
	{
		var m = Matrix.Identity;

		Assert.False(m.Equals(null));
		Assert.False(m.Equals("1,0,0,1,0,0"));
		Assert.False(m.Equals(new MatrixTransform()));
	}

	[Fact]
	public void ToMatrix3X2CopiesAllElements()
	{
		var result = new Matrix(1.5, -2, 3, 4.25, -5, 6).ToMatrix3X2();

		Assert.Equal(new Matrix3x2(1.5f, -2, 3, 4.25f, -5, 6), result);
	}

	[Fact]
	public void ToMatrix3X2OfIdentityIsIdentity()
	{
		Assert.True(Matrix.Identity.ToMatrix3X2().IsIdentity);
		Assert.True(new Matrix().ToMatrix3X2().IsIdentity);
	}

	[Fact]
	public void ToMatrixCopiesAllElements()
	{
		var source = new Matrix3x2(1.5f, -2, 3, 4.25f, -5, 6);

		var result = source.ToMatrix();

		AssertMatrix(source, result);
		Assert.Equal(source.GetDeterminant(), result.Determinant, Tolerance);
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void ToMatrixRoundTripsThroughMatrix3X2(string kind)
	{
		var roundTripped = Create(kind).ToMatrix3X2().ToMatrix();

		AssertMatrix(CreateExpected(kind), roundTripped);
		Assert.Equal(Create(kind).IsIdentity, roundTripped.IsIdentity);
	}

	[Fact]
	public void ToMatrixOfComposedTransformBehavesLikeSource()
	{
		var source = Matrix3x2.CreateRotation(0.5f) * Matrix3x2.CreateTranslation(3, 4);

		var result = source.ToMatrix();
		result.Invert();

		Assert.True(Matrix3x2.Invert(source, out var expected));
		AssertMatrix(expected, result);
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void PrependOffsetMatchesTranslationPrepend(string kind)
	{
		var m = Create(kind);

		MatrixUtil.PrependOffset(ref m, 7, -9);

		AssertMatrix(Matrix3x2.CreateTranslation(7, -9) * CreateExpected(kind), m);
	}

	[Fact]
	public void TransformRectIgnoresEmptyRect()
	{
		var rect = Rect.Zero;
		var m = new Matrix(2, 0, 0, 2, 5, 5);

		MatrixUtil.TransformRect(ref rect, ref m);

		Assert.Equal(Rect.Zero, rect);
	}

	[Fact]
	public void TransformRectWithIdentityLeavesRectUnchanged()
	{
		var rect = new Rect(1, 2, 3, 4);
		var m = Matrix.Identity;

		MatrixUtil.TransformRect(ref rect, ref m);

		Assert.Equal(new Rect(1, 2, 3, 4), rect);
	}

	[Fact]
	public void TransformRectWithScalingScalesAndNormalizesNegativeSize()
	{
		var rect = new Rect(1, 2, 3, 4);
		var m = new Matrix(-2, 0, 0, 3, 0, 0);

		MatrixUtil.TransformRect(ref rect, ref m);

		// Corners x: -2, -8 ; y: 6, 18
		Assert.Equal(new Rect(-8, 6, 6, 12), rect);
	}

	[Fact]
	public void TransformRectWithTranslationOffsetsX()
	{
		var rect = new Rect(1, 2, 3, 4);
		var m = new Matrix(1, 0, 0, 1, 10, 0);

		MatrixUtil.TransformRect(ref rect, ref m);

		Assert.Equal(new Rect(11, 2, 3, 4), rect);
	}

	[Fact(Skip = "Product bug: MatrixUtil.TransformRect adds OffsetY to rect.X instead of rect.Y (Matrix.cs translation branch)")]
	public void TransformRectWithTranslationOffsetsY()
	{
		var rect = new Rect(1, 2, 3, 4);
		var m = new Matrix(1, 0, 0, 1, 10, 20);

		MatrixUtil.TransformRect(ref rect, ref m);

		Assert.Equal(new Rect(11, 22, 3, 4), rect);
	}

	[Fact(Skip = "Product bug: MatrixUtil.TransformRect transforms (Right, Top) twice and never (Left, Top), so the bounds can miss a corner")]
	public void TransformRectWithRotationReturnsBoundingBoxOfAllCorners()
	{
		var rect = new Rect(0, 0, 10, 10);
		var m = Matrix.Identity;
		m.Rotate(45);

		MatrixUtil.TransformRect(ref rect, ref m);

		var half = 10 / Math.Sqrt(2);
		Assert.Equal(-half, rect.X, Tolerance);
		Assert.Equal(0, rect.Y, Tolerance);
		Assert.Equal(2 * half, rect.Width, Tolerance);
		Assert.Equal(2 * half, rect.Height, Tolerance);
	}

	[Fact]
	public void MatrixTransformDefaultsToIdentity()
	{
		var transform = new MatrixTransform();

		Assert.True(transform.Matrix.IsIdentity);
		Assert.True(transform.Value.IsIdentity);
	}

	[Fact]
	public void SettingMatrixUpdatesValueAndRaisesPropertyChanged()
	{
		var transform = new MatrixTransform();
		var changed = new List<string>();
		transform.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		var matrix = new Matrix(1, 2, 3, 4, 5, 6);

		transform.Matrix = matrix;

		Assert.Equal(matrix, transform.Matrix);
		Assert.Equal(matrix, transform.Value);
		Assert.Contains(nameof(MatrixTransform.Matrix), changed);
		Assert.Contains(nameof(Transform.Value), changed);
	}

	[Fact]
	public void SettingSameMatrixDoesNotRaisePropertyChanged()
	{
		var transform = new MatrixTransform { Matrix = new Matrix(1, 2, 3, 4, 5, 6) };
		int count = 0;
		transform.PropertyChanged += (_, _) => count++;

		transform.Matrix = new Matrix(1, 2, 3, 4, 5, 6);

		Assert.Equal(0, count);
	}

	[Fact]
	public void MatrixTransformValueTracksSubsequentMatrixChanges()
	{
		var transform = new MatrixTransform { Matrix = new Matrix(2, 0, 0, 2, 0, 0) };

		transform.Matrix = new Matrix(1, 0, 0, 1, 10, 20);

		Assert.Equal(new Point(11, 21), transform.Value.Transform(new Point(1, 1)));
	}

	[Fact]
	public void MatrixTransformMatrixCanBeSetFromBinding()
	{
		var transform = new MatrixTransform();
		transform.SetBinding(MatrixTransform.MatrixProperty, new Binding("M"));

		transform.BindingContext = new { M = new Matrix(1, 0, 0, 1, 3, 4) };

		Assert.Equal(new Point(3, 4), transform.Value.Transform(Point.Zero));
	}

	[Fact]
	public void MatrixIsDecoratedWithMatrixTypeConverter()
	{
		Assert.IsType<MatrixTypeConverter>(TypeDescriptor.GetConverter(typeof(Matrix)));
	}

	[Theory]
	[InlineData(typeof(string), true)]
	[InlineData(typeof(Matrix), false)]
	[InlineData(typeof(double), false)]
	public void ConverterCanConvertFromOnlyString(Type type, bool expected)
	{
		Assert.Equal(expected, new MatrixTypeConverter().CanConvertFrom(null, type));
	}

	[Theory]
	[InlineData(typeof(string), true)]
	[InlineData(typeof(Matrix), false)]
	[InlineData(null, false)]
	public void ConverterCanConvertToOnlyString(Type type, bool expected)
	{
		Assert.Equal(expected, new MatrixTypeConverter().CanConvertTo(null, type));
	}

	[Theory]
	[InlineData("1,2,3,4,5,6")]
	[InlineData("1 2 3 4 5 6")]
	[InlineData("1, 2, 3, 4, 5, 6")]
	[InlineData("  1 ,2  3,,4 , 5   6  ")]
	[InlineData("+1 2.0 3.00 4 5 6")]
	public void ConvertFromParsesSixNumbersWithAnySeparators(string input)
	{
		var m = (Matrix)new MatrixTypeConverter().ConvertFromInvariantString(input);

		AssertMatrix(1, 2, 3, 4, 5, 6, m);
	}

	[Fact]
	public void ConvertFromParsesDecimalsAndNegativesWithInvariantCulture()
	{
		var m = (Matrix)new MatrixTypeConverter().ConvertFrom(null, new CultureInfo("de-DE"), "-1.5 0.25 -.5 2 -100.75 0");

		AssertMatrix(-1.5, 0.25, -0.5, 2, -100.75, 0, m);
	}

	[Fact]
	public void ConvertFromIdentityStringProducesIdentity()
	{
		var m = (Matrix)new MatrixTypeConverter().ConvertFromInvariantString("1 0 0 1 0 0");

		Assert.True(m.IsIdentity);
		Assert.Equal(Matrix.Identity, m);
	}

	[Theory]
	[InlineData("")]
	[InlineData("1 2 3 4 5")]
	[InlineData("1 2 3 4 5 6 7")]
	[InlineData(" , , ")]
	[InlineData("1 2 3 4 5 a")]
	[InlineData("1 2 3 4 5 1e3")]
	[InlineData("1;2;3;4;5;6")]
	public void ConvertFromInvalidStringThrows(string input)
	{
		Assert.Throws<ArgumentException>(() => new MatrixTypeConverter().ConvertFromInvariantString(input));
	}

	[Fact]
	public void ConvertToProducesInvariantCommaSeparatedString()
	{
		var converter = new MatrixTypeConverter();

		var result = converter.ConvertTo(null, new CultureInfo("de-DE"), new Matrix(1.5, -2, 0.25, 4, -5.5, 6), typeof(string));

		Assert.Equal("1.5, -2, 0.25, 4, -5.5, 6", result);
	}

	[Fact]
	public void ConvertToOfDefaultMatrixProducesIdentityString()
	{
		Assert.Equal("1, 0, 0, 1, 0, 0", new MatrixTypeConverter().ConvertToInvariantString(new Matrix()));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("1 2 3 4 5 6")]
	[InlineData(42)]
	public void ConvertToNonMatrixThrows(object value)
	{
		Assert.Throws<NotSupportedException>(() => new MatrixTypeConverter().ConvertTo(null, CultureInfo.InvariantCulture, value, typeof(string)));
	}

	[Theory]
	[MemberData(nameof(AllKinds))]
	public void ConvertToAndBackRoundTrips(string kind)
	{
		var converter = new MatrixTypeConverter();
		var original = Create(kind);
		original.Rotate(33);

		var text = converter.ConvertToInvariantString(original);
		var parsed = (Matrix)converter.ConvertFromInvariantString(text);

		Assert.Equal(original.M11, parsed.M11);
		Assert.Equal(original.M12, parsed.M12);
		Assert.Equal(original.M21, parsed.M21);
		Assert.Equal(original.M22, parsed.M22);
		Assert.Equal(original.OffsetX, parsed.OffsetX);
		Assert.Equal(original.OffsetY, parsed.OffsetY);
	}
}
