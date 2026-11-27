using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics.Lighting;

/// <summary>
/// Image-based lighting resources generated from the procedural studio environment.
/// Irradiance and prefiltered specular reflections use cubemaps to avoid
/// equirectangular seams; the BRDF LUT remains a 2D texture.
/// </summary>
internal sealed class IblResources : IDisposable
{
	public const int IrradianceSize = 32;
	public const int SpecularSize = 128;
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

	private readonly TextureCube _irradiance;
	private readonly TextureCube _prefilteredSpecular;
	private readonly Texture2D _brdfLut;

	public IblResources(GraphicsDevice device)
	{
		ArgumentNullException.ThrowIfNull(device);

		_irradiance = CreateIrradiance(device);
		_prefilteredSpecular = CreatePrefilteredSpecular(device);
		_brdfLut = CreateBrdfLut(device);
	}

	public TextureCube Irradiance => _irradiance;

	public TextureCube PrefilteredSpecular => _prefilteredSpecular;

	public Texture2D BrdfLut => _brdfLut;

	public void Dispose()
	{
		_irradiance.Dispose();
		_prefilteredSpecular.Dispose();
		_brdfLut.Dispose();
	}

	private static TextureCube CreateIrradiance(GraphicsDevice device)
	{
		var texture = new TextureCube(
			device,
			IrradianceSize,
			false,
			SurfaceFormat.Color);

		for (int face = 0; face < 6; ++face)
		{
			var data = new Color[IrradianceSize * IrradianceSize];
			int currentFace = face;

			Parallel.For(0, IrradianceSize, y =>
			{
				for (int x = 0; x < IrradianceSize; ++x)
				{
					Vector3 normal = CubeFaceDirection(
						currentFace,
						x,
						y,
						IrradianceSize);
					Vector3 up = MathF.Abs(normal.Y) < 0.999f
						? Vector3.UnitY
						: Vector3.UnitX;
					Vector3 right = Vector3.Normalize(
						Vector3.Cross(up, normal));
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
					data[y * IrradianceSize + x] = ToColor(irradiance);
				}
			});

			texture.SetData((CubeMapFace)face, data);
		}

		return texture;
	}

	private static TextureCube CreatePrefilteredSpecular(GraphicsDevice device)
	{
		var texture = new TextureCube(
			device,
			SpecularSize,
			true,
			SurfaceFormat.Color);

		for (int mip = 0; mip < SpecularMipCount; ++mip)
		{
			int size = Math.Max(1, SpecularSize >> mip);
			float roughness = mip / (float)(SpecularMipCount - 1);

			for (int face = 0; face < 6; ++face)
			{
				var data = new Color[size * size];
				int currentFace = face;
				int currentSize = size;
				float currentRoughness = roughness;

				Parallel.For(0, size, y =>
				{
					for (int x = 0; x < currentSize; ++x)
					{
						Vector3 normal = CubeFaceDirection(
							currentFace,
							x,
							y,
							currentSize);
						Vector3 view = normal;
						Vector3 prefiltered = Vector3.Zero;
						float totalWeight = 0f;

						for (int i = 0; i < SpecularSampleCount; ++i)
						{
							Vector2 xi = HammersleySamples[i];
							Vector3 halfVector = ImportanceSampleGGX(
								xi,
								normal,
								currentRoughness);
							Vector3 light = Vector3.Normalize(
								2f * Vector3.Dot(view, halfVector) * halfVector
									- view);
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
						data[y * currentSize + x] = ToColor(prefiltered);
					}
				});

				texture.SetData(
					(CubeMapFace)face,
					mip,
					null,
					data,
					0,
					data.Length);
			}
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

	private static Vector3 CubeFaceDirection(
		int face,
		int x,
		int y,
		int size)
	{
		float u = 2f * (x + 0.5f) / size - 1f;
		float v = 2f * (y + 0.5f) / size - 1f;

		Vector3 direction = face switch
		{
			0 => new Vector3(1f, -v, -u),
			1 => new Vector3(-1f, -v, u),
			2 => new Vector3(u, 1f, v),
			3 => new Vector3(u, -1f, -v),
			4 => new Vector3(u, -v, 1f),
			_ => new Vector3(-u, -v, -1f)
		};

		return Vector3.Normalize(direction);
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
