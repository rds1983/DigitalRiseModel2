using DigitalRiseModel.Animation;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class SrtTransformTests
{
	[Fact]
	public void Identity_ReturnsUnitTransform()
	{
		SrtTransform transform = SrtTransform.Identity;

		Assert.Equal(Vector3.Zero, transform.Translation);
		Assert.Equal(Quaternion.Identity, transform.Rotation);
		Assert.Equal(Vector3.One, transform.Scale);
	}

	[Fact]
	public void Equals_SameComponents_ReturnsTrue()
	{
		var left = new SrtTransform(
			new Vector3(1, 2, 3),
			new Quaternion(0, 0.5f, 0, 1),
			new Vector3(2, 2, 2));
		var right = new SrtTransform(
			new Vector3(1, 2, 3),
			new Quaternion(0, 0.5f, 0, 1),
			new Vector3(2, 2, 2));

		Assert.True(left.Equals(right));
		Assert.True(left == right);
		Assert.False(left != right);
		Assert.Equal(left.GetHashCode(), right.GetHashCode());
	}

	[Fact]
	public void Equals_DifferentComponents_ReturnsFalse()
	{
		SrtTransform left = SrtTransform.Identity;
		SrtTransform right = new(
			new Vector3(1, 0, 0),
			Quaternion.Identity,
			Vector3.One);

		Assert.False(left.Equals(right));
		Assert.False(left == right);
		Assert.True(left != right);
	}

	[Fact]
	public void Equals_NullObject_ReturnsFalse()
	{
		SrtTransform transform = SrtTransform.Identity;

		Assert.False(transform.Equals(null));
	}

	[Fact]
	public void Interpolate_Linear_InterpolatesAllComponents()
	{
		SrtTransform start = SrtTransform.Identity;
		var end = new SrtTransform(
			new Vector3(2, 0, 0),
			Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f),
			new Vector3(3, 3, 3));

		SrtTransform result = SrtTransform.Interpolate(
			start,
			end,
			0.5f,
			AnimationInterpolation.Linear,
			AnimationInterpolation.Linear,
			AnimationInterpolation.Linear);

		Assert.Equal(new Vector3(1, 0, 0), result.Translation);
		Assert.Equal(new Vector3(2, 2, 2), result.Scale);
		AssertQuaternionEqual(
			Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4f),
			result.Rotation);
	}

	[Fact]
	public void Interpolate_StepTranslationAndScale_HoldsStartValues()
	{
		var start = new SrtTransform(
			new Vector3(1, 1, 1),
			Quaternion.Identity,
			new Vector3(2, 2, 2));
		var end = new SrtTransform(
			new Vector3(5, 5, 5),
			Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f),
			new Vector3(4, 4, 4));

		SrtTransform result = SrtTransform.Interpolate(
			start,
			end,
			0.5f,
			AnimationInterpolation.Step,
			AnimationInterpolation.Linear,
			AnimationInterpolation.Step);

		Assert.Equal(start.Translation, result.Translation);
		Assert.Equal(start.Scale, result.Scale);
		Assert.NotEqual(start.Rotation, result.Rotation);
	}

	[Fact]
	public void Interpolate_StepRotation_HoldsStartRotation()
	{
		var start = new SrtTransform(
			new Vector3(1, 1, 1),
			Quaternion.Identity,
			new Vector3(2, 2, 2));
		var end = new SrtTransform(
			new Vector3(5, 5, 5),
			Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f),
			new Vector3(4, 4, 4));

		SrtTransform result = SrtTransform.Interpolate(
			start,
			end,
			0.5f,
			AnimationInterpolation.Linear,
			AnimationInterpolation.Step,
			AnimationInterpolation.Linear);

		Assert.Equal(new Vector3(3, 3, 3), result.Translation);
		Assert.Equal(start.Rotation, result.Rotation);
		Assert.Equal(new Vector3(3, 3, 3), result.Scale);
	}

	[Fact]
	public void Interpolate_CubicSpline_TreatsAsLinear()
	{
		var start = new SrtTransform(
			Vector3.Zero,
			Quaternion.Identity,
			Vector3.One);
		var end = new SrtTransform(
			new Vector3(2, 0, 0),
			Quaternion.Identity,
			new Vector3(3, 3, 3));

		SrtTransform result = SrtTransform.Interpolate(
			start,
			end,
			0.5f,
			AnimationInterpolation.CubicSpline,
			AnimationInterpolation.CubicSpline,
			AnimationInterpolation.CubicSpline);

		Assert.Equal(new Vector3(1, 0, 0), result.Translation);
		Assert.Equal(new Vector3(2, 2, 2), result.Scale);
		Assert.Equal(Quaternion.Identity, result.Rotation);
	}

	[Fact]
	public void ToMatrix_ComposesScaleRotationAndTranslation()
	{
		var transform = new SrtTransform(
			new Vector3(1, 2, 3),
			Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f),
			new Vector3(2, 3, 4));

		Matrix expected = Matrix.CreateScale(transform.Scale)
			* Matrix.CreateFromQuaternion(transform.Rotation)
			* Matrix.CreateTranslation(transform.Translation);

		Assert.Equal(expected, transform.ToMatrix());
	}

	[Fact]
	public void ToMatrix_Identity_ReturnsIdentity()
	{
		Assert.Equal(Matrix.Identity, SrtTransform.Identity.ToMatrix());
	}

	private static void AssertQuaternionEqual(Quaternion expected, Quaternion actual)
	{
		Assert.Equal(expected.X, actual.X, 5);
		Assert.Equal(expected.Y, actual.Y, 5);
		Assert.Equal(expected.Z, actual.Z, 5);
		Assert.Equal(expected.W, actual.W, 5);
	}
}
