using DigitalRiseModel;

namespace DigitalRiseModel.Tests;

public sealed class DrModelBoneTests
{
	[Fact]
	public void Constructor_SetsNameAndOptionalMesh()
	{
		var mesh = new DrModelMesh([]);

		var bone = new DrModelBone("Root", mesh);

		Assert.Equal("Root", bone.Name);
		Assert.Same(mesh, bone.Mesh);
		Assert.Same(bone, mesh.ParentBone);
		Assert.Empty(bone.Children);
	}

	[Fact]
	public void Constructor_WithoutMesh_LeavesMeshNull()
	{
		var bone = new DrModelBone("Root");

		Assert.Null(bone.Mesh);
	}

	[Fact]
	public void Mesh_Set_SetsMeshParentBone()
	{
		var bone = new DrModelBone("Root");
		var mesh = new DrModelMesh([]);

		bone.Mesh = mesh;

		Assert.Same(mesh, bone.Mesh);
		Assert.Same(bone, mesh.ParentBone);
	}

	[Fact]
	public void Mesh_Replace_ClearsOldMeshParentBoneAndSetsNewMeshParentBone()
	{
		var bone = new DrModelBone("Root");
		var oldMesh = new DrModelMesh([]);
		var newMesh = new DrModelMesh([]);
		bone.Mesh = oldMesh;

		bone.Mesh = newMesh;

		Assert.Null(oldMesh.ParentBone);
		Assert.Same(bone, newMesh.ParentBone);
	}

	[Fact]
	public void Mesh_MoveToAnotherBone_ClearsPreviousBoneMesh()
	{
		var first = new DrModelBone("First");
		var second = new DrModelBone("Second");
		var mesh = new DrModelMesh([]);
		first.Mesh = mesh;

		second.Mesh = mesh;

		Assert.Null(first.Mesh);
		Assert.Same(mesh, second.Mesh);
		Assert.Same(second, mesh.ParentBone);
	}

	[Fact]
	public void Mesh_SetNull_ClearsMeshParentBone()
	{
		var bone = new DrModelBone("Root");
		var mesh = new DrModelMesh([]);
		bone.Mesh = mesh;

		bone.Mesh = null;

		Assert.Null(mesh.ParentBone);
	}

	[Fact]
	public void Tag_Set_ReturnsAssignedValue()
	{
		var tag = new object();
		var bone = new DrModelBone("Root") { Tag = tag };

		Assert.Same(tag, bone.Tag);
	}

	[Fact]
	public void Constructor_NullName_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new DrModelBone(null!));
	}

	[Fact]
	public void Children_Set_SetsParentOfEachChild()
	{
		var parent = new DrModelBone("Root");
		var first = new DrModelBone("First");
		var second = new DrModelBone("Second");

		parent.Children = [first, second];

		Assert.Same(parent, first.Parent);
		Assert.Same(parent, second.Parent);
		Assert.Equal(2, parent.Children.Length);
	}

	[Fact]
	public void Children_Replace_ClearsOldParentsAndSetsNewParents()
	{
		var parent = new DrModelBone("Root");
		var oldChild = new DrModelBone("Old");
		var newChild = new DrModelBone("New");
		parent.Children = [oldChild];

		parent.Children = [newChild];

		Assert.Null(oldChild.Parent);
		Assert.Same(parent, newChild.Parent);
	}

	[Fact]
	public void Children_SetNull_ThrowsArgumentNullException()
	{
		var bone = new DrModelBone("Root");

		Assert.Throws<ArgumentNullException>(() => bone.Children = null!);
	}

	[Fact]
	public void Children_SetNullChild_ThrowsArgumentNullException()
	{
		var bone = new DrModelBone("Root");

		Assert.Throws<ArgumentNullException>(() => bone.Children = [null!]);
	}
}
