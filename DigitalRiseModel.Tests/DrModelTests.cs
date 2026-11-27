using DigitalRiseModel;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class DrModelTests
{
	[Fact]
	public void Constructor_CollectsBonesAndMeshesFromHierarchy()
	{
		var rootMesh = new DrModelMesh([]);
		var childMesh = new DrModelMesh([]);
		var root = new DrModelBone("Root", rootMesh);
		var child = new DrModelBone("Child", childMesh);
		root.Children = [child];

		var model = new DrModel(root);

		Assert.Same(root, model.Root);
		Assert.Equal(2, model.Bones.Length);
		Assert.Same(root, model.Bones[0]);
		Assert.Same(child, model.Bones[1]);
		Assert.Equal(2, model.Meshes.Length);
		Assert.Same(rootMesh, model.Meshes[0]);
		Assert.Same(childMesh, model.Meshes[1]);
	}

	[Fact]
	public void Constructor_AutoAssignsIndicesInTraversalOrder()
	{
		var root = new DrModelBone("Root") { Index = 99 };
		var first = new DrModelBone("First") { Index = 99 };
		var grandchild = new DrModelBone("Grandchild") { Index = 99 };
		var second = new DrModelBone("Second") { Index = 99 };
		first.Children = [grandchild];
		root.Children = [first, second];

		var model = new DrModel(root);

		Assert.Equal(new[] { 0, 1, 2, 3 }, Array.ConvertAll(model.Bones, bone => bone.Index));
		Assert.Same(root, model.Bones[0]);
		Assert.Same(first, model.Bones[1]);
		Assert.Same(grandchild, model.Bones[2]);
		Assert.Same(second, model.Bones[3]);
	}

	[Fact]
	public void Constructor_PreservesIndicesWhenAutoAssignDisabled()
	{
		var root = new DrModelBone("Root") { Index = 10 };
		var child = new DrModelBone("Child") { Index = 20 };
		root.Children = [child];

		var model = new DrModel(root, autoAssignIndices: false);

		Assert.Equal(10, model.Bones[0].Index);
		Assert.Equal(20, model.Bones[1].Index);
	}

	[Fact]
	public void Constructor_NullRoot_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new DrModel(null!));
	}

	[Fact]
	public void Constructor_CyclicHierarchy_ThrowsInvalidOperationException()
	{
		var root = new DrModelBone("Root");
		var child = new DrModelBone("Child");
		root.Children = [child];
		child.Children = [root];

		Assert.Throws<InvalidOperationException>(() => new DrModel(root));
	}

	[Fact]
	public void CopyBoneTransformsTo_CopiesAllTransforms()
	{
		var root = new DrModelBone("Root") { Transform = Matrix.CreateTranslation(1, 2, 3) };
		var child = new DrModelBone("Child") { Transform = Matrix.CreateScale(4) };
		root.Children = [child];
		var model = new DrModel(root);
		var destination = new Matrix[2];

		model.CopyBoneTransformsTo(destination);

		Assert.Equal(root.Transform, destination[0]);
		Assert.Equal(child.Transform, destination[1]);
	}

	[Fact]
	public void CopyBoneTransformsFrom_UpdatesAllTransforms()
	{
		var root = new DrModelBone("Root");
		var child = new DrModelBone("Child");
		root.Children = [child];
		var model = new DrModel(root);
		var source = new[]
		{
			Matrix.CreateTranslation(1, 0, 0),
			Matrix.CreateTranslation(0, 2, 0)
		};

		model.CopyBoneTransformsFrom(source);

		Assert.Equal(source[0], root.Transform);
		Assert.Equal(source[1], child.Transform);
	}

	[Fact]
	public void CopyAbsoluteBoneTransformsTo_CombinesParentTransforms()
	{
		var root = new DrModelBone("Root")
		{
			Transform = Matrix.CreateTranslation(10, 0, 0)
		};
		var child = new DrModelBone("Child")
		{
			Transform = Matrix.CreateTranslation(0, 5, 0)
		};
		root.Children = [child];
		var model = new DrModel(root);
		var destination = new Matrix[2];

		model.CopyAbsoluteBoneTransformsTo(destination);

		Assert.Equal(root.Transform, destination[0]);
		Assert.Equal(child.Transform * root.Transform, destination[1]);
	}

	[Fact]
	public void CopyAbsoluteBoneTransformsTo_ParentIndexNotBeforeChild_ThrowsInvalidOperationException()
	{
		var root = new DrModelBone("Root") { Index = 1 };
		var child = new DrModelBone("Child") { Index = 0 };
		root.Children = [child];
		var model = new DrModel(root, autoAssignIndices: false);

		Assert.Throws<InvalidOperationException>(() => model.CopyAbsoluteBoneTransformsTo(new Matrix[2]));
	}

	[Fact]
	public void CopyBoneTransformsTo_DestinationTooSmall_ThrowsArgumentOutOfRangeException()
	{
		var root = new DrModelBone("Root");
		root.Children = [new DrModelBone("Child")];
		var model = new DrModel(root);

		Assert.Throws<ArgumentOutOfRangeException>(() => model.CopyBoneTransformsTo(new Matrix[1]));
	}
}
