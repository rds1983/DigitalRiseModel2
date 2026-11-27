using DigitalRiseModel;

namespace DigitalRiseModel.Tests;

public sealed class DrTextureInfoTests
{
	[Fact]
	public void Defaults_AreEmpty()
	{
		var info = new DrTextureInfo();

		Assert.Equal(0, info.TextureIndex);
		Assert.Equal(0, info.TexCoord);
		Assert.Equal(1f, info.Scale);
		Assert.Equal(1f, info.Strength);
		Assert.Null(info.Texture);
	}
}
