using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics.Materials;

/// <summary>
/// Immutable snapshot of a glTF material's renderer-facing state.
/// </summary>
internal sealed class MaterialBinding
{
	public MaterialBinding(DrMaterial? material)
	{
		Material = material;
		BaseColorFactor = material?.BaseColorFactor ?? Vector4.One;
		BaseColorTexture = material?.BaseColorTexture?.Texture;
		MetallicFactor = material?.MetallicFactor ?? 1f;
		RoughnessFactor = material?.RoughnessFactor ?? 1f;
		MetallicRoughnessTexture = material?.MetallicRoughnessTexture?.Texture;
		NormalTexture = material?.NormalTexture?.Texture;
		NormalScale = material?.NormalTexture?.Scale ?? 1f;
		OcclusionTexture = material?.OcclusionTexture?.Texture;
		OcclusionStrength = material?.OcclusionTexture?.Strength ?? 1f;
		EmissiveTexture = material?.EmissiveTexture?.Texture;
		EmissiveFactor = material?.EmissiveFactor ?? Vector3.Zero;
		AlphaMode = material?.AlphaMode ?? DrAlphaMode.Opaque;
		AlphaCutoff = material?.AlphaCutoff ?? 0.5f;
		DoubleSided = material?.DoubleSided ?? false;
	}

	public static MaterialBinding Default { get; } = new(null);

	public DrMaterial? Material { get; }

	public Vector4 BaseColorFactor { get; }

	public Texture2D? BaseColorTexture { get; }

	public float MetallicFactor { get; }

	public float RoughnessFactor { get; }

	public Texture2D? MetallicRoughnessTexture { get; }

	public Texture2D? NormalTexture { get; }

	public float NormalScale { get; }

	public Texture2D? OcclusionTexture { get; }

	public float OcclusionStrength { get; }

	public Texture2D? EmissiveTexture { get; }

	public Vector3 EmissiveFactor { get; }

	public DrAlphaMode AlphaMode { get; }

	public float AlphaCutoff { get; }

	public bool DoubleSided { get; }

	public bool UseBaseColorAlpha => AlphaMode == DrAlphaMode.Blend;
}
