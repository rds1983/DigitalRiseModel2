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
}
