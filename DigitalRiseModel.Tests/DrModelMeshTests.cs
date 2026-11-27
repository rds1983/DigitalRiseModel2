using DigitalRiseModel;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class DrModelMeshTests
{
	[Fact]
	public void Constructor_WithEmptyParts_HasEmptyMeshParts()
	{
		var mesh = new DrModelMesh([]);

		Assert.Empty(mesh.MeshParts);
	}

	[Fact]
	public void Constructor_WithParts_SetsMeshPartsAndBackReferences()
	{
		var first = new DrModelMeshPart();
		var second = new DrModelMeshPart();
		var parts = new[] { first, second };

		var mesh = new DrModelMesh(parts);

		Assert.Same(parts, mesh.MeshParts);
		Assert.Same(mesh, first.Mesh);
		Assert.Same(mesh, second.Mesh);
	}

	[Fact]
	public void Constructor_WithNullParts_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new DrModelMesh((DrModelMeshPart[])null!));
	}

	[Fact]
	public void MeshParts_Set_ReturnsAssignedArrayAndSetsPartMeshes()
	{
		var first = new DrModelMeshPart();
		var second = new DrModelMeshPart();
		var parts = new[] { first, second };

		var mesh = new DrModelMesh([])
		{
			MeshParts = parts
		};

		Assert.Same(parts, mesh.MeshParts);
		Assert.Same(mesh, first.Mesh);
		Assert.Same(mesh, second.Mesh);
	}

	[Fact]
	public void MeshParts_Set_ReplacementClearsOldPartsAndSetsNewParts()
	{
		var oldPart = new DrModelMeshPart();
		var newPart = new DrModelMeshPart();
		var mesh = new DrModelMesh(new[] { oldPart });

		mesh.MeshParts = new[] { newPart };

		Assert.Null(oldPart.Mesh);
		Assert.Same(mesh, newPart.Mesh);
	}

	[Fact]
	public void MeshParts_SetNull_ThrowsArgumentNullException()
	{
		var mesh = new DrModelMesh([]);

		Assert.Throws<ArgumentNullException>(() => mesh.MeshParts = null!);
	}

	[Fact]
	public void BoundingSphere_NoParts_ReturnsDefault()
	{
		var mesh = new DrModelMesh([]);

		BoundingSphere bounds = mesh.BoundingSphere;

		Assert.Equal(Vector3.Zero, bounds.Center);
		Assert.Equal(0f, bounds.Radius);
	}

	[Fact]
	public void BoundingSphere_SinglePart_ReturnsPartSphere()
	{
		var expected = new BoundingSphere(new Vector3(1, 2, 3), 4);
		var mesh = new DrModelMesh(new[] { new DrModelMeshPart { BoundingSphere = expected } });

		BoundingSphere bounds = mesh.BoundingSphere;

		Assert.Equal(expected.Center, bounds.Center);
		Assert.Equal(expected.Radius, bounds.Radius);
	}

	[Fact]
	public void BoundingSphere_MultipleParts_ReturnsMergedSphere()
	{
		var first = new BoundingSphere(new Vector3(0, 0, 0), 1);
		var second = new BoundingSphere(new Vector3(10, 0, 0), 2);
		var expected = BoundingSphere.CreateMerged(first, second);
		var mesh = new DrModelMesh(
		[
			new DrModelMeshPart { BoundingSphere = first },
			new DrModelMeshPart { BoundingSphere = second }
		]);

		BoundingSphere bounds = mesh.BoundingSphere;

		Assert.Equal(expected.Center, bounds.Center);
		Assert.Equal(expected.Radius, bounds.Radius);
	}

	[Fact]
	public void BoundingSphere_AfterPartBoundsChange_Recalculates()
	{
		var part = new DrModelMeshPart
		{
			BoundingSphere = new BoundingSphere(Vector3.Zero, 1)
		};
		var mesh = new DrModelMesh(new[] { part });
		_ = mesh.BoundingSphere;
		var updated = new BoundingSphere(new Vector3(5, 0, 0), 2);

		part.BoundingSphere = updated;

		BoundingSphere bounds = mesh.BoundingSphere;
		Assert.Equal(updated.Center, bounds.Center);
		Assert.Equal(updated.Radius, bounds.Radius);
	}

	[Fact]
	public void BoundingSphere_AfterMeshPartsChange_Recalculates()
	{
		var first = new DrModelMeshPart
		{
			BoundingSphere = new BoundingSphere(Vector3.Zero, 1)
		};
		var mesh = new DrModelMesh(new[] { first });
		_ = mesh.BoundingSphere;
		var second = new DrModelMeshPart
		{
			BoundingSphere = new BoundingSphere(new Vector3(10, 0, 0), 2)
		};

		mesh.MeshParts = new[] { second };

		BoundingSphere bounds = mesh.BoundingSphere;
		Assert.Equal(second.BoundingSphere.Center, bounds.Center);
		Assert.Equal(second.BoundingSphere.Radius, bounds.Radius);
	}

	[Fact]
	public void Dispose_DisposesPartsAndClearsMeshParts()
	{
		var first = new DrModelMeshPart();
		var second = new DrModelMeshPart();
		var mesh = new DrModelMesh(new[] { first, second });

		mesh.Dispose();

		Assert.Empty(mesh.MeshParts);
		Assert.Null(first.Mesh);
		Assert.Null(second.Mesh);

		mesh.Dispose();
	}
}
