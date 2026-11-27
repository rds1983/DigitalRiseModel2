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
		Assert.Empty(skin.InverseBindMatrices);
	}

	[Fact]
	public void Properties_Set_ReturnsAssignedValues()
	{
		var skeleton = new DrModelBone("Skeleton");
		var joint = new DrModelBone("Joint");
		var inverseBind = new[] { Matrix.Identity };

		var skin = new DrSkin
		{
			Name = "Skin",
			Skeleton = skeleton,
			Joints = [joint],
			InverseBindMatrices = inverseBind
		};

		Assert.Equal("Skin", skin.Name);
		Assert.Same(skeleton, skin.Skeleton);
		Assert.Same(joint, skin.Joints[0]);
		Assert.Same(inverseBind, skin.InverseBindMatrices);
	}
}
