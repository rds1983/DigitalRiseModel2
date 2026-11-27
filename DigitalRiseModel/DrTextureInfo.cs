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
	/// Gets or sets the normal-map scale. This value is used only for normal textures.
	/// </summary>
	public float Scale { get; set; } = 1f;

	/// <summary>
	/// Gets or sets the occlusion strength. This value is used only for occlusion textures.
	/// </summary>
	public float Strength { get; set; } = 1f;

	/// <summary>
	/// Gets or sets an object that identifies this texture reference.
	/// </summary>
	public object? Tag { get; set; }
}
