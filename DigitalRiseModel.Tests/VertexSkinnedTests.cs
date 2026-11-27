using DigitalRiseModel.Vertices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace DigitalRiseModel.Tests;

public sealed class VertexSkinnedTests
{
	[Fact]
	public void VertexDeclaration_HasExpectedLayout()
	{
		VertexElement[] elements = VertexSkinned.VertexDeclaration.GetVertexElements();

		Assert.Equal(52, VertexSkinned.VertexDeclaration.VertexStride);
		Assert.Equal(5, elements.Length);
		Assert.Equal(VertexElementUsage.Position, elements[0].VertexElementUsage);
		Assert.Equal(VertexElementUsage.Normal, elements[1].VertexElementUsage);
		Assert.Equal(VertexElementUsage.TextureCoordinate, elements[2].VertexElementUsage);
		Assert.Equal(VertexElementUsage.BlendIndices, elements[3].VertexElementUsage);
		Assert.Equal(VertexElementUsage.BlendWeight, elements[4].VertexElementUsage);
		Assert.Equal(VertexElementFormat.Byte4, elements[3].VertexElementFormat);
	}

	[Fact]
	public void Constructor_AssignsFields()
	{
		var vertex = new VertexSkinned(
			new Vector3(1, 2, 3),
			Vector3.UnitY,
			new Vector2(0.25f, 0.75f),
			new Byte4(1, 2, 3, 4),
			new Vector4(0.1f, 0.2f, 0.3f, 0.4f));

		Assert.Equal(new Vector3(1, 2, 3), vertex.Position);
		Assert.Equal(Vector3.UnitY, vertex.Normal);
		Assert.Equal(new Vector2(0.25f, 0.75f), vertex.TextureCoordinate);
		Assert.Equal(new Byte4(1, 2, 3, 4), vertex.BlendIndices);
		Assert.Equal(new Vector4(0.1f, 0.2f, 0.3f, 0.4f), vertex.BlendWeights);
	}
}
