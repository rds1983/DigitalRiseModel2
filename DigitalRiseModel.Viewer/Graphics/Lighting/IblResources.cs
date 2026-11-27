using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics.Lighting;

/// <summary>
/// Image-based lighting textures generated from the procedural studio environment.
/// Irradiance and the BRDF LUT are 2D textures using a spherical parameterization;
/// specular reflections are prefiltered into a mip chain with the same parameterization.
/// </summary>
internal sealed class IblResources : IDisposable
{
	public const int IrradianceWidth = 64;
	public const int IrradianceHeight = 32;
	public const int SpecularWidth = 256;
	public const int SpecularHeight = 128;
	public const int SpecularMipCount = 6;
	public const int BrdfLutSize = 128;

	private const int IrradianceSampleCount = 128;
	private const int SpecularSampleCount = 64;
	private const int BrdfSampleCount = 128;
	private const int HammersleyCount = 128;

	private static readonly Vector2[] HammersleySamples =
		CreateHammersleySamples(HammersleyCount);

	private static readonly Vector3 KeyDirection =
		Vector3.Normalize(new Vector3(0.45f, 0.8f, 0.65f));
	private static readonly Vector3 FillDirection =
		Vector3.Normalize(new Vector3(-0.75f, 0.35f, 0.4f));
	private static readonly Vector3 RimDirection =
		Vector3.Normalize(new Vector3(0.1f, 0.45f, -0.9f));

	private readonly Texture2D _irradiance;
	private readonly Texture2D _prefilteredSpecular;
	private readonly Texture2D _brdfLut;

	public IblResources(GraphicsDevice device)
	{
		ArgumentNullException.ThrowIfNull(device);

		_irradiance = CreateIrradiance(device);
		_prefilteredSpecular = CreatePrefilteredSpecular(device);
		_brdfLut = CreateBrdfLut(device);
	}

	public Texture2D Irradiance => _irradiance;

	public Texture2D PrefilteredSpecular => _prefilteredSpecular;

	public Texture2D BrdfLut => _brdfLut;

	public void Dispose()
	{
		_irradiance.Dispose();
		_prefilteredSpecular.Dispose();
		_brdfLut.Dispose();
	}

	private static Texture2D CreateIrradiance(GraphicsDevice device)
	{
		var data = new Color[IrradianceWidth * IrradianceHeight];

		for (int y = 0; y < IrradianceHeight; ++y)
		{
			float v = (y + 0.5f) / IrradianceHeight;

			for (int x = 0; x < IrradianceWidth; ++x)
			{
				float u = (x + 0.5f) / IrradianceWidth;
				Vector3 normal = EquirectUVToDirection(u, v);
				Vector3 up = MathF.Abs(normal.Y) < 0.999f
					? Vector3.UnitY
					: Vector3.UnitX;
				Vector3 right = Vector3.Normalize(Vector3.Cross(up, normal));
				up = Vector3.Cross(normal, right);

				Vector3 irradiance = Vector3.Zero;

				for (int i = 0; i < IrradianceSampleCount; ++i)
				{
					Vector2 xi = HammersleySamples[i];
					float phi = 2f * MathF.PI * xi.X;
					float cosTheta = MathF.Sqrt(1f - xi.Y);
					float sinTheta = MathF.Sqrt(xi.Y);
					Vector3 tangent = new(
						MathF.Cos(phi) * sinTheta,
						MathF.Sin(phi) * sinTheta,
						cosTheta);
					Vector3 sampleDirection = tangent.X * right
						+ tangent.Y * up
						+ tangent.Z * normal;

					irradiance += EnvironmentRadiance(sampleDirection);
				}

				irradiance /= IrradianceSampleCount;
				data[y * IrradianceWidth + x] = ToColor(irradiance);
			}
		}

		var texture = new Texture2D(
			device,
			IrradianceWidth,
			IrradianceHeight,
			false,
			SurfaceFormat.Color);
		texture.SetData(data);
		return texture;
	}

	private static Texture2D CreatePrefilteredSpecular(GraphicsDevice device)
	{
		var texture = new Texture2D(
			device,
			SpecularWidth,
			SpecularHeight,
			true,
			SurfaceFormat.Color);

		for (int mip = 0; mip < SpecularMipCount; ++mip)
		{
			int width = Math.Max(1, SpecularWidth >> mip);
			int height = Math.Max(1, SpecularHeight >> mip);
			float roughness = mip / (float)(SpecularMipCount - 1);
			var data = new Color[width * height];
			int sampleCount = Math.Min(SpecularSampleCount, HammersleySamples.Length);

			for (int y = 0; y < height; ++y)
			{
				float v = (y + 0.5f) / height;

				for (int x = 0; x < width; ++x)
				{
					float u = (x + 0.5f) / width;
					Vector3 normal = EquirectUVToDirection(u, v);
					Vector3 view = normal;
					Vector3 prefiltered = Vector3.Zero;
					float totalWeight = 0f;

					for (int i = 0; i < sampleCount; ++i)
					{
						Vector2 xi = HammersleySamples[i];
						Vector3 halfVector = ImportanceSampleGGX(
							xi,
							normal,
							roughness);
						Vector3 light = Vector3.Normalize(
							2f * Vector3.Dot(view, halfVector) * halfVector - view);
						float nDotL = Math.Clamp(
							Vector3.Dot(normal, light),
							0f,
							1f);

						if (nDotL > 0f)
						{
							prefiltered += EnvironmentRadiance(light) * nDotL;
							totalWeight += nDotL;
						}
					}

					prefiltered /= MathF.Max(totalWeight, 0.0001f);
					data[y * width + x] = ToColor(prefiltered);
				}
			}

			texture.SetData(mip, null, data, 0, data.Length);
		}

		return texture;
	}

	private static Texture2D CreateBrdfLut(GraphicsDevice device)
	{
		var data = new Color[BrdfLutSize * BrdfLutSize];

		for (int y = 0; y < BrdfLutSize; ++y)
		{
			float roughness = (y + 0.5f) / BrdfLutSize;

			for (int x = 0; x < BrdfLutSize; ++x)
			{
				float nDotV = (x + 0.5f) / BrdfLutSize;
				Vector2 brdf = IntegrateBrdf(nDotV, roughness);
				data[y * BrdfLutSize + x] = new Color(
					brdf.X,
					brdf.Y,
					0f,
					1f);
			}
		}

		var texture = new Texture2D(
			device,
			BrdfLutSize,
			BrdfLutSize,
			false,
			SurfaceFormat.Color);
		texture.SetData(data);
		return texture;
	}

	private static Vector2 IntegrateBrdf(float nDotV, float roughness)
	{
		float safeNDotV = MathF.Max(nDotV, 1e-4f);
		Vector3 view = new(
			MathF.Sqrt(1f - safeNDotV * safeNDotV),
			0f,
			safeNDotV);
		Vector3 normal = Vector3.UnitZ;
		float a = roughness * roughness;
		float scale = 0f;
		float bias = 0f;

		for (int i = 0; i < BrdfSampleCount; ++i)
		{
			Vector2 xi = HammersleySamples[i];
			Vector3 halfVector = ImportanceSampleGGX(xi, normal, roughness);
			Vector3 light = Vector3.Normalize(
				2f * Vector3.Dot(view, halfVector) * halfVector - view);
			float nDotL = Math.Clamp(light.Z, 0f, 1f);
			float nDotH = Math.Clamp(halfVector.Z, 0f, 1f);
			float vDotH = Math.Clamp(Vector3.Dot(view, halfVector), 0f, 1f);

			if (nDotL > 0f)
			{
				float k = a * 0.5f;
				float geometryView = safeNDotV / (safeNDotV * (1f - k) + k);
				float geometryLight = nDotL / (nDotL * (1f - k) + k);
				float visibility = geometryView * geometryLight
					* vDotH / (nDotH * safeNDotV);
				float fresnel = MathF.Pow(1f - vDotH, 5f);

				scale += (1f - fresnel) * visibility;
				bias += fresnel * visibility;
			}
		}

		return new Vector2(scale, bias) / BrdfSampleCount;
	}

	private static Vector3 EnvironmentRadiance(Vector3 direction)
	{
		direction = Vector3.Normalize(direction);

		float up = Math.Clamp(direction.Y * 0.5f + 0.5f, 0f, 1f);
		Vector3 color = Vector3.Lerp(
			new Vector3(0.02f, 0.018f, 0.016f),
			new Vector3(0.16f, 0.20f, 0.28f),
			up);

		color += new Vector3(0.55f, 0.52f, 0.48f)
			* MathF.Pow(Saturate(Vector3.Dot(direction, KeyDirection)), 32f);
		color += new Vector3(0.12f, 0.15f, 0.22f)
			* MathF.Pow(Saturate(Vector3.Dot(direction, FillDirection)), 16f);
		color += new Vector3(0.20f, 0.23f, 0.30f)
			* MathF.Pow(Saturate(Vector3.Dot(direction, RimDirection)), 24f);

		return color;
	}

	private static Vector3 ImportanceSampleGGX(
		Vector2 xi,
		Vector3 normal,
		float roughness)
	{
		float a = roughness * roughness;
		float phi = 2f * MathF.PI * xi.X;
		float cosTheta = MathF.Sqrt(
			(1f - xi.Y) / (1f + (a * a - 1f) * xi.Y));
		float sinTheta = MathF.Sqrt(
			MathF.Max(0f, 1f - cosTheta * cosTheta));
		Vector3 halfVector = new(
			MathF.Cos(phi) * sinTheta,
			MathF.Sin(phi) * sinTheta,
			cosTheta);
		Vector3 up = MathF.Abs(normal.Y) < 0.999f
			? Vector3.UnitY
			: Vector3.UnitX;
		Vector3 tangent = Vector3.Normalize(Vector3.Cross(up, normal));
		Vector3 bitangent = Vector3.Cross(normal, tangent);

		return tangent * halfVector.X
			+ bitangent * halfVector.Y
			+ normal * halfVector.Z;
	}

	private static Vector3 EquirectUVToDirection(float u, float v)
	{
		return EquirectUVToDirection(new Vector2(u, v));
	}

	private static Vector3 EquirectUVToDirection(Vector2 uv)
	{
		float y = 1f - 2f * Math.Clamp(uv.Y, 0f, 1f);
		float radius = MathF.Sqrt(MathF.Max(1f - y * y, 0f));
		float phi = (uv.X - 0.5f) * 2f * MathF.PI;

		return new Vector3(
			radius * MathF.Cos(phi),
			y,
			radius * MathF.Sin(phi));
	}

	private static Vector2[] CreateHammersleySamples(int count)
	{
		var samples = new Vector2[count];

		for (int i = 0; i < count; ++i)
		{
			samples[i] = new Vector2(
				(i + 0.5f) / count,
				RadicalInverse((uint)i));
		}

		return samples;
	}

	private static float RadicalInverse(uint bits)
	{
		bits = (bits << 16) | (bits >> 16);
		bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
		bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
		bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
		bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
		return bits * 2.3283064365386963e-10f;
	}

	private static Color ToColor(Vector3 value)
	{
		return new Color(
			Math.Clamp(value.X, 0f, 1f),
			Math.Clamp(value.Y, 0f, 1f),
			Math.Clamp(value.Z, 0f, 1f),
			1f);
	}

	private static float Saturate(float value)
	{
		return Math.Clamp(value, 0f, 1f);
	}
}
