using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel;

/// <summary>
/// Describes a texture reference used by a <see cref="DrMaterial"/>.
/// </summary>
public sealed class DrTextureInfo
{
	/// <summary>
	/// Gets or sets the texture index.
	/// </summary>
	public int TextureIndex { get; set; }

	/// <summary>
	/// Gets or sets the texture coordinate set index.
	/// </summary>
	public int TexCoord { get; set; }

	/// <summary>
	/// Gets or sets the <c>KHR_texture_transform</c> applied to the texture
	/// coordinates, or <see langword="null"/> when no transform is defined.
	/// </summary>
	public DrTextureTransform? TextureTransform { get; set; }

	/// <summary>
	/// Gets or sets the sampler settings, or <see langword="null"/> when the
	/// texture uses glTF default sampler behavior.
	/// </summary>
	public DrTextureSampler? Sampler { get; set; }

	/// <summary>
	/// Gets or sets the normal-map scale. This value is used only for normal textures.
	/// </summary>
	public float Scale { get; set; } = 1f;

	/// <summary>
	/// Gets or sets the occlusion strength. This value is used only for occlusion textures.
	/// </summary>
	public float Strength { get; set; } = 1f;

	/// <summary>
	/// Gets or sets the loaded texture, or <see langword="null"/> when the model was loaded
	/// without a graphics device.
	/// </summary>
	public Texture2D? Texture { get; set; }

	/// <summary>
	/// Gets or sets an object that identifies this texture reference.
	/// </summary>
	public object? Tag { get; set; }
}
