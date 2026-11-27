using DigitalRiseModel;

namespace DigitalRiseModel.Tests;

public sealed class DrModelBoneTests
{
	[Fact]
	public void AddChild_ValidBone_SetsParentAndAddsToChildren()
	{
		var parent = new DrModelBone { Name = "Root" };
		var child = new DrModelBone { Name = "Child" };

		parent.AddChild(child);

		Assert.Same(parent, child.Parent);
		Assert.Single(parent.Children);
		Assert.Same(child, parent.Children[0]);
	}

	[Fact]
	public void AddChild_BoneIsAlreadyAChild_ThrowsInvalidOperationException()
	{
		var parent = new DrModelBone();
		var child = new DrModelBone();
		parent.AddChild(child);

		Assert.Throws<InvalidOperationException>(() => parent.AddChild(child));
	}

	[Fact]
	public void AddChild_WouldCreateCycle_ThrowsArgumentException()
	{
		var root = new DrModelBone();
		var child = new DrModelBone();
		root.AddChild(child);

		Assert.Throws<ArgumentException>(() => child.AddChild(root));
	}

	[Fact]
	public void AddChild_Null_ThrowsArgumentNullException()
	{
		var bone = new DrModelBone();

		Assert.Throws<ArgumentNullException>(() => bone.AddChild(null!));
	}
}
