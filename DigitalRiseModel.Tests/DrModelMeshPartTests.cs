using DigitalRiseModel;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class DrModelMeshPartTests
{
	[Fact]
	public void BoundingSphere_Set_ReturnsAssignedValue()
	{
		var bounds = new BoundingSphere(new Vector3(1, 2, 3), 4);
		var part = new DrModelMeshPart { BoundingSphere = bounds };

		Assert.Equal(bounds, part.BoundingSphere);
	}

	[Fact]
	public void MaterialAndSkin_Set_ReturnsAssignedValues()
	{
		var material = new DrMaterial();
		var skin = new DrSkin();
		var part = new DrModelMeshPart { Material = material, Skin = skin };

		Assert.Same(material, part.Material);
		Assert.Same(skin, part.Skin);
	}

	[Fact]
	public void Dispose_ClearsBuffersAndMeshAndIsIdempotent()
	{
		var part = new DrModelMeshPart();
		var mesh = new DrModelMesh(new[] { part });

		part.Dispose();
		part.Dispose();

		Assert.Null(part.VertexBuffer);
		Assert.Null(part.IndexBuffer);
		Assert.Null(part.Mesh);
	}
}
