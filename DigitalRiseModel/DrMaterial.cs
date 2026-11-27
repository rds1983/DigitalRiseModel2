using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Describes a material using the metallic-roughness model from glTF 2.0.
/// </summary>
public sealed class DrMaterial
{
	/// <summary>
	/// Gets or sets the material name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the base color factor.
	/// </summary>
	public Vector4 BaseColorFactor { get; set; } = Vector4.One;

	/// <summary>
	/// Gets or sets the base color texture.
	/// </summary>
	public DrTextureInfo? BaseColorTexture { get; set; }

	/// <summary>
	/// Gets or sets the metallic factor.
	/// </summary>
	public float MetallicFactor { get; set; } = 1f;

	/// <summary>
	/// Gets or sets the roughness factor.
	/// </summary>
	public float RoughnessFactor { get; set; } = 1f;

	/// <summary>
	/// Gets or sets the metallic-roughness texture.
	/// </summary>
	public DrTextureInfo? MetallicRoughnessTexture { get; set; }

	/// <summary>
	/// Gets or sets the normal texture.
	/// </summary>
	public DrTextureInfo? NormalTexture { get; set; }

	/// <summary>
	/// Gets or sets the occlusion texture.
	/// </summary>
	public DrTextureInfo? OcclusionTexture { get; set; }

	/// <summary>
	/// Gets or sets the emissive texture.
	/// </summary>
	public DrTextureInfo? EmissiveTexture { get; set; }

	/// <summary>
	/// Gets or sets the emissive color factor.
	/// </summary>
	public Vector3 EmissiveFactor { get; set; }

	/// <summary>
	/// Gets or sets the emissive strength multiplier from
	/// <c>KHR_materials_emissive_strength</c>. The default is 1.
	/// </summary>
	public float EmissiveStrength { get; set; } = 1f;

	/// <summary>
	/// Gets or sets the alpha mode.
	/// </summary>
	public DrAlphaMode AlphaMode { get; set; } = DrAlphaMode.Opaque;

	/// <summary>
	/// Gets or sets the alpha cutoff used by <see cref="DrAlphaMode.Mask"/>.
	/// </summary>
	public float AlphaCutoff { get; set; } = 0.5f;

	/// <summary>
	/// Gets or sets whether the material is rendered double-sided.
	/// </summary>
	public bool DoubleSided { get; set; }

	/// <summary>
	/// Gets or sets whether the material uses <c>KHR_materials_unlit</c> and
	/// should bypass lighting.
	/// </summary>
	public bool Unlit { get; set; }

	/// <summary>
	/// Gets or sets an object that identifies this material.
	/// </summary>
	public object? Tag { get; set; }
}
