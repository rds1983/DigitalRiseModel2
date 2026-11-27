using DigitalRiseModel;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class DrModelTests
{
	[Fact]
	public void Constructor_UsesFirstRootBoneAsRoot()
	{
		var firstRoot = new DrModelBone("FirstRoot") { Index = 0 };
		var child = new DrModelBone("Child") { Index = 1 };
		firstRoot.Children = [child];
		var secondRoot = new DrModelBone("SecondRoot") { Index = 2 };
		var model = new DrModel(
			new List<DrModelBone> { firstRoot, child, secondRoot },
			new List<DrModelMesh>());

		Assert.Same(firstRoot, model.Root);
		Assert.Equal(3, model.Bones.Length);
	}

	[Fact]
	public void CopyBoneTransformsTo_CopiesAllTransforms()
	{
		var first = new DrModelBone("First") { Transform = Matrix.CreateTranslation(1, 2, 3) };
		var second = new DrModelBone("Second") { Transform = Matrix.CreateScale(4) };
		var model = new DrModel(new List<DrModelBone> { first, second }, new List<DrModelMesh>());
		var destination = new Matrix[2];

		model.CopyBoneTransformsTo(destination);

		Assert.Equal(first.Transform, destination[0]);
		Assert.Equal(second.Transform, destination[1]);
	}

	[Fact]
	public void CopyBoneTransformsFrom_UpdatesAllTransforms()
	{
		var first = new DrModelBone("First");
		var second = new DrModelBone("Second");
		var model = new DrModel(new List<DrModelBone> { first, second }, new List<DrModelMesh>());
		var source = new[]
		{
			Matrix.CreateTranslation(1, 0, 0),
			Matrix.CreateTranslation(0, 2, 0)
		};

		model.CopyBoneTransformsFrom(source);

		Assert.Equal(source[0], first.Transform);
		Assert.Equal(source[1], second.Transform);
	}

	[Fact]
	public void CopyAbsoluteBoneTransformsTo_CombinesParentTransforms()
	{
		var root = new DrModelBone("Root")
		{
			Index = 0,
			Transform = Matrix.CreateTranslation(10, 0, 0)
		};
		var child = new DrModelBone("Child")
		{
			Index = 1,
			Transform = Matrix.CreateTranslation(0, 5, 0)
		};
		root.Children = [child];
		var model = new DrModel(new List<DrModelBone> { root, child }, new List<DrModelMesh>());
		var destination = new Matrix[2];

		model.CopyAbsoluteBoneTransformsTo(destination);

		Assert.Equal(root.Transform, destination[0]);
		Assert.Equal(child.Transform * root.Transform, destination[1]);
	}

	[Fact]
	public void CopyAbsoluteBoneTransformsTo_ParentAfterChild_ThrowsInvalidOperationException()
	{
		var child = new DrModelBone("Child") { Index = 0 };
		var parent = new DrModelBone("Parent") { Index = 1 };
		parent.Children = [child];
		var model = new DrModel(new List<DrModelBone> { child, parent }, new List<DrModelMesh>());

		Assert.Throws<InvalidOperationException>(() => model.CopyAbsoluteBoneTransformsTo(new Matrix[2]));
	}

	[Fact]
	public void CopyBoneTransformsTo_DestinationTooSmall_ThrowsArgumentOutOfRangeException()
	{
		var model = new DrModel(
			new List<DrModelBone> { new DrModelBone("First"), new DrModelBone("Second") },
			new List<DrModelMesh>());

		Assert.Throws<ArgumentOutOfRangeException>(() => model.CopyBoneTransformsTo(new Matrix[1]));
	}
}
