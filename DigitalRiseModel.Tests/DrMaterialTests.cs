using DigitalRiseModel;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class DrMaterialTests
{
	[Fact]
	public void Defaults_MatchGltfDefaults()
	{
		var material = new DrMaterial();

		Assert.Equal(Vector4.One, material.BaseColorFactor);
		Assert.Equal(1f, material.MetallicFactor);
		Assert.Equal(1f, material.RoughnessFactor);
		Assert.Equal(Vector3.Zero, material.EmissiveFactor);
		Assert.Equal(DrAlphaMode.Opaque, material.AlphaMode);
		Assert.Equal(0.5f, material.AlphaCutoff);
		Assert.False(material.DoubleSided);
	}

	[Fact]
	public void Properties_Set_ReturnsAssignedValues()
	{
		var texture = new DrTextureInfo { TextureIndex = 3, TexCoord = 1 };
		var material = new DrMaterial
		{
			Name = "Metal",
			BaseColorFactor = new Vector4(0.1f, 0.2f, 0.3f, 0.4f),
			BaseColorTexture = texture,
			MetallicFactor = 0.8f,
			RoughnessFactor = 0.2f,
			EmissiveFactor = new Vector3(0.5f, 0.25f, 0f),
			AlphaMode = DrAlphaMode.Blend,
			AlphaCutoff = 0.25f,
			DoubleSided = true
		};

		Assert.Equal("Metal", material.Name);
		Assert.Same(texture, material.BaseColorTexture);
		Assert.Equal(0.8f, material.MetallicFactor);
		Assert.Equal(0.2f, material.RoughnessFactor);
		Assert.Equal(DrAlphaMode.Blend, material.AlphaMode);
		Assert.True(material.DoubleSided);
	}
}
