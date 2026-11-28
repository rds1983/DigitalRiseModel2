using DigitalRiseModel.Viewer.Graphics.Lighting;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class LightPackerTests
{
	[Fact]
	public void Pack_NormalizesDirectionAndPacksIntensity()
	{
		var lights = new LightSet();
		lights.Add(new DirectionalLight
		{
			Direction = new Vector3(0f, 2f, 0f),
			Color = new Vector3(1f, 0.5f, 0.25f),
			Intensity = 3f
		});

		var directions = new Vector4[LightSet.MaxDirectionalLights];
		var colors = new Vector4[LightSet.MaxDirectionalLights];

		int count = LightPacker.Pack(lights, directions, colors);

		Assert.Equal(1, count);
		AssertVector(new Vector3(0f, 1f, 0f), directions[0]);
		Assert.Equal(3f, directions[0].W, 5);
		Assert.Equal(new Vector4(1f, 0.5f, 0.25f, 0f), colors[0]);
	}

	[Fact]
	public void Pack_SkipsDisabledAndZeroDirectionLights()
	{
		var lights = new LightSet();
		lights.Add(new DirectionalLight
		{
			Direction = Vector3.UnitY,
			Intensity = 0f
		});
		lights.Add(new DirectionalLight
		{
			Direction = Vector3.UnitY,
			Intensity = -1f
		});
		lights.Add(new DirectionalLight
		{
			Direction = Vector3.Zero,
			Intensity = 1f
		});
		lights.Add(new DirectionalLight
		{
			Direction = Vector3.UnitX,
			Intensity = 2f
		});

		var directions = new Vector4[LightSet.MaxDirectionalLights];
		var colors = new Vector4[LightSet.MaxDirectionalLights];

		int count = LightPacker.Pack(lights, directions, colors);

		Assert.Equal(1, count);
		AssertVector(Vector3.UnitX, directions[0]);
		Assert.Equal(2f, directions[0].W, 5);
	}

	[Fact]
	public void Pack_ZeroesUnusedEntries()
	{
		var lights = new LightSet();
		lights.Add(new DirectionalLight
		{
			Direction = Vector3.UnitZ,
			Intensity = 1f
		});

		var directions = new Vector4[LightSet.MaxDirectionalLights];
		var colors = new Vector4[LightSet.MaxDirectionalLights];
		directions[1] = Vector4.One;
		colors[2] = Vector4.One;

		int count = LightPacker.Pack(lights, directions, colors);

		Assert.Equal(1, count);

		for (int i = 1; i < LightSet.MaxDirectionalLights; ++i)
		{
			Assert.Equal(Vector4.Zero, directions[i]);
			Assert.Equal(Vector4.Zero, colors[i]);
		}
	}

	[Fact]
	public void Pack_TooShortDirectionArray_ThrowsArgumentException()
	{
		var lights = new LightSet();
		var directions = new Vector4[LightSet.MaxDirectionalLights - 1];
		var colors = new Vector4[LightSet.MaxDirectionalLights];

		Assert.Throws<ArgumentException>(
			() => LightPacker.Pack(lights, directions, colors));
	}

	[Fact]
	public void Pack_TooShortColorArray_ThrowsArgumentException()
	{
		var lights = new LightSet();
		var directions = new Vector4[LightSet.MaxDirectionalLights];
		var colors = new Vector4[LightSet.MaxDirectionalLights - 1];

		Assert.Throws<ArgumentException>(
			() => LightPacker.Pack(lights, directions, colors));
	}

	private static void AssertVector(Vector3 expected, Vector4 actual)
	{
		Assert.Equal(expected.X, actual.X, 5);
		Assert.Equal(expected.Y, actual.Y, 5);
		Assert.Equal(expected.Z, actual.Z, 5);
	}
}
