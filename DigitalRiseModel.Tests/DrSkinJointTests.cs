using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class DrSkinJointTests
{
	[Fact]
	public void Constructor_AssignsBoneAndInverseBindMatrix()
	{
		var bone = new DrModelBone("Joint");
		Matrix inverseBindMatrix = Matrix.CreateTranslation(1, 2, 3);

		var joint = new DrSkinJoint(bone, inverseBindMatrix);

		Assert.Same(bone, joint.Bone);
		Assert.Equal(inverseBindMatrix, joint.InverseBindMatrix);
	}

	[Fact]
	public void Constructor_NullBone_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(
			() => new DrSkinJoint(null!, Matrix.Identity));
	}
}
