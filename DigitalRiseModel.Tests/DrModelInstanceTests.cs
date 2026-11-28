using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class DrModelInstanceTests
{
	[Fact]
	public void Constructor_NullModel_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new DrModelInstance(null!));
	}

	[Fact]
	public void Constructor_CopiesBindPoseLocalTransforms()
	{
		DrModel model = CreateModel();

		var instance = new DrModelInstance(model);

		Assert.Equal(model.Bones.Length, instance.BoneCount);

		for (int i = 0; i < model.Bones.Length; i++)
		{
			Assert.Equal(model.Bones[i].Transform, instance.GetLocalTransform(i));
		}
	}

	[Fact]
	public void AbsoluteBoneTransforms_ComputesAbsoluteTransforms()
	{
		DrModel model = CreateModel();
		var instance = new DrModelInstance(model);

		Matrix rootAbsolute = model.Bones[0].Transform;
		Matrix childAbsolute = model.Bones[1].Transform * rootAbsolute;
		Matrix grandchildAbsolute = model.Bones[2].Transform * childAbsolute;
		Matrix[] absolute = instance.AbsoluteBoneTransforms;

		Assert.Equal(rootAbsolute, absolute[0]);
		Assert.Equal(childAbsolute, absolute[1]);
		Assert.Equal(grandchildAbsolute, absolute[2]);
	}

	[Fact]
	public void SetLocalTransform_MarksAbsoluteTransformsDirty()
	{
		DrModel model = CreateModel();
		var instance = new DrModelInstance(model);
		Matrix newLocal = Matrix.CreateTranslation(5, 0, 0);

		instance.SetLocalTransform(2, newLocal);

		Assert.Equal(newLocal, instance.GetLocalTransform(2));
		Assert.Equal(
			newLocal * instance.AbsoluteBoneTransforms[1],
			instance.AbsoluteBoneTransforms[2]);
	}

	[Fact]
	public void AbsoluteBoneTransforms_SecondQuery_ReturnsCachedArray()
	{
		var instance = new DrModelInstance(CreateModel());

		Matrix[] first = instance.AbsoluteBoneTransforms;
		Matrix[] second = instance.AbsoluteBoneTransforms;

		Assert.Same(first, second);
	}

	[Fact]
	public void GetLocalTransform_NegativeIndex_ThrowsArgumentOutOfRangeException()
	{
		var instance = new DrModelInstance(CreateModel());

		Assert.Throws<ArgumentOutOfRangeException>(() => instance.GetLocalTransform(-1));
	}

	[Fact]
	public void SetLocalTransform_NegativeIndex_ThrowsArgumentOutOfRangeException()
	{
		var instance = new DrModelInstance(CreateModel());

		Assert.Throws<ArgumentOutOfRangeException>(
			() => instance.SetLocalTransform(-1, Matrix.Identity));
	}

	[Fact]
	public void GetLocalTransform_IndexTooLarge_ThrowsArgumentOutOfRangeException()
	{
		var instance = new DrModelInstance(CreateModel());

		Assert.Throws<ArgumentOutOfRangeException>(
			() => instance.GetLocalTransform(instance.BoneCount));
	}

	[Fact]
	public void SetLocalTransform_IndexTooLarge_ThrowsArgumentOutOfRangeException()
	{
		var instance = new DrModelInstance(CreateModel());

		Assert.Throws<ArgumentOutOfRangeException>(
			() => instance.SetLocalTransform(instance.BoneCount, Matrix.Identity));
	}

	[Fact]
	public void AbsoluteBoneTransforms_ParentAfterChild_ThrowsInvalidOperationException()
	{
		DrModel model = CreateModel();
		model.Bones[1].Index = 2;
		var instance = new DrModelInstance(model);

		Assert.Throws<InvalidOperationException>(() => instance.AbsoluteBoneTransforms);
	}

	[Fact]
	public void BoundingSphere_NoMeshes_ReturnsDefault()
	{
		var instance = new DrModelInstance(CreateModel());

		BoundingSphere bounds = instance.BoundingSphere;

		Assert.Equal(Vector3.Zero, bounds.Center);
		Assert.Equal(0f, bounds.Radius);
	}

	[Fact]
	public void BoundingSphere_TransformsMeshByBoneAbsoluteTransform()
	{
		var instance = new DrModelInstance(CreateModelWithMesh());

		BoundingSphere bounds = instance.BoundingSphere;

		Assert.Equal(new Vector3(12, 0, 0), bounds.Center);
		Assert.Equal(1f, bounds.Radius);
	}

	[Fact]
	public void BoundingSphere_AfterSetLocalTransform_Recalculates()
	{
		var instance = new DrModelInstance(CreateModelWithMesh());
		_ = instance.BoundingSphere;

		instance.SetLocalTransform(0, Matrix.CreateTranslation(20, 0, 0));

		BoundingSphere bounds = instance.BoundingSphere;

		Assert.Equal(new Vector3(22, 0, 0), bounds.Center);
	}

	[Fact]
	public void BoundingSphere_MultipleMeshes_MergesTransformedSpheres()
	{
		var root = new DrModelBone("Root");
		var child = new DrModelBone("Child")
		{
			Transform = Matrix.CreateTranslation(10, 0, 0)
		};
		root.Children = [child];

		var rootPart = new DrModelMeshPart
		{
			BoundingSphere = new BoundingSphere(Vector3.Zero, 1f)
		};
		var childPart = new DrModelMeshPart
		{
			BoundingSphere = new BoundingSphere(Vector3.Zero, 1f)
		};
		root.Mesh = new DrModelMesh([rootPart]);
		child.Mesh = new DrModelMesh([childPart]);

		var instance = new DrModelInstance(new DrModel(root));

		BoundingSphere bounds = instance.BoundingSphere;

		Assert.Equal(new Vector3(5, 0, 0), bounds.Center);
		Assert.Equal(6f, bounds.Radius);
	}

	[Fact]
	public void BoundingSphere_SkinnedMesh_UsesSkinTransform()
	{
		var root = new DrModelBone("Root")
		{
			Transform = Matrix.CreateScale(0.01f)
		};
		var part = new DrModelMeshPart
		{
			BoundingSphere = new BoundingSphere(new Vector3(0, 1, 0), 1f),
			Skin = new DrSkin(0)
			{
				Joints = [new DrSkinJoint(root, Matrix.CreateScale(100f))]
			}
		};
		root.Mesh = new DrModelMesh([part]);

		var instance = new DrModelInstance(new DrModel(root));

		BoundingSphere bounds = instance.BoundingSphere;

		Assert.Equal(new Vector3(0, 1, 0), bounds.Center);
		Assert.Equal(1f, bounds.Radius);
	}

	[Fact]
	public void GetSkinTransforms_ReturnsInverseBindTimesAbsoluteTransform()
	{
		var instance = new DrModelInstance(CreateSkinnedModel(
			inverseBindMatrix: Matrix.CreateTranslation(-1, 0, 0)));

		Matrix[] transforms = instance.GetSkinTransforms(0);

		Assert.Single(transforms);
		Assert.Equal(Matrix.CreateTranslation(1, 0, 0), transforms[0]);
	}

	[Fact]
	public void GetSkinTransforms_AfterPoseChange_Recalculates()
	{
		var instance = new DrModelInstance(CreateSkinnedModel(Matrix.Identity));
		Matrix[] transforms = instance.GetSkinTransforms(0);
		Assert.Equal(Matrix.CreateTranslation(2, 0, 0), transforms[0]);

		instance.SetLocalTransform(0, Matrix.CreateTranslation(5, 0, 0));

		Assert.Same(transforms, instance.GetSkinTransforms(0));
		Assert.Equal(Matrix.CreateTranslation(5, 0, 0), transforms[0]);
	}

	[Fact]
	public void GetSkinTransforms_UnknownIndex_ThrowsArgumentOutOfRangeException()
	{
		var instance = new DrModelInstance(CreateSkinnedModel(Matrix.Identity));

		Assert.Throws<ArgumentOutOfRangeException>(
			() => instance.GetSkinTransforms(1));
	}

	private static DrModel CreateSkinnedModel(Matrix inverseBindMatrix)
	{
		var root = new DrModelBone("Root")
		{
			Transform = Matrix.CreateTranslation(2, 0, 0)
		};
		var part = new DrModelMeshPart();
		var skin = new DrSkin(0)
		{
			Joints = [new DrSkinJoint(root, inverseBindMatrix)]
		};
		part.Skin = skin;
		root.Mesh = new DrModelMesh([part]);

		return new DrModel(root);
	}

	private static DrModel CreateModel()
	{
		var root = new DrModelBone("Root")
		{
			Transform = Matrix.CreateTranslation(1, 2, 3)
		};
		var child = new DrModelBone("Child")
		{
			Transform = Matrix.CreateScale(2)
		};
		var grandchild = new DrModelBone("Grandchild")
		{
			Transform = Matrix.CreateTranslation(0, 1, 0)
		};

		root.Children = [child];
		child.Children = [grandchild];

		return new DrModel(root);
	}

	private static DrModel CreateModelWithMesh()
	{
		var root = new DrModelBone("Root")
		{
			Transform = Matrix.CreateTranslation(10, 0, 0)
		};
		var child = new DrModelBone("Child")
		{
			Transform = Matrix.CreateTranslation(1, 0, 0)
		};
		root.Children = [child];

		var part = new DrModelMeshPart
		{
			BoundingSphere = new BoundingSphere(new Vector3(1, 0, 0), 1f)
		};
		child.Mesh = new DrModelMesh([part]);

		return new DrModel(root);
	}
}
