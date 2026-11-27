using DigitalRiseModel;
using DigitalRiseModel.Gltf;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Tests;

public sealed class ModelLoadFlagsTests
{
	[Fact]
	public void Values_AreStable()
	{
		Assert.Equal(0, (int)ModelLoadFlags.None);
		Assert.Equal(1, (int)ModelLoadFlags.ReadableBuffers);
		Assert.Equal(2, (int)ModelLoadFlags.KeepSourceGeometry);
		Assert.Equal(4, (int)ModelLoadFlags.EnsureUVs);
	}

	[Fact]
	public void None_UsesWriteOnlyBuffers()
	{
		Assert.Equal(BufferUsage.WriteOnly, GltfLoader.GetBufferUsage(ModelLoadFlags.None));
	}

	[Fact]
	public void ReadableBuffers_AllowsGetDataBuffers()
	{
		Assert.Equal(
			BufferUsage.None,
			GltfLoader.GetBufferUsage(ModelLoadFlags.ReadableBuffers));
	}

	[Fact]
	public void KeepSourceGeometry_DoesNotChangeBufferUsage()
	{
		Assert.Equal(
			BufferUsage.WriteOnly,
			GltfLoader.GetBufferUsage(ModelLoadFlags.KeepSourceGeometry));
	}
}
