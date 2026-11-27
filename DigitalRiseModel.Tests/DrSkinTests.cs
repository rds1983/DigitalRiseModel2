using DigitalRiseModel;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class DrSkinTests
{
	[Fact]
	public void Defaults_AreEmpty()
	{
		var skin = new DrSkin();

		Assert.Null(skin.Skeleton);
		Assert.Empty(skin.Joints);
	}

	[Fact]
	public void Properties_Set_ReturnsAssignedValues()
	{
		var skeleton = new DrModelBone("Skeleton");
		var bone = new DrModelBone("Joint");
		var joint = new DrSkinJoint(bone, Matrix.Identity);
		var joints = new[] { joint };

		var skin = new DrSkin
		{
			Name = "Skin",
			Skeleton = skeleton,
			Joints = joints
		};

		Assert.Equal("Skin", skin.Name);
		Assert.Same(skeleton, skin.Skeleton);
		Assert.Same(joints, skin.Joints);
		Assert.Same(joint, skin.Joints[0]);
	}
}
