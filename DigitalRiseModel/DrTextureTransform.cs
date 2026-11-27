using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Describes a <c>KHR_texture_transform</c> applied to texture coordinates.
/// </summary>
public sealed class DrTextureTransform
{
	/// <summary>
	/// Gets or sets the translation applied after scaling and rotation.
	/// </summary>
	public Vector2 Offset { get; set; }

	/// <summary>
	/// Gets or sets the rotation in radians.
	/// </summary>
	public float Rotation { get; set; }

	/// <summary>
	/// Gets or sets the scale factor.
	/// </summary>
	public Vector2 Scale { get; set; } = Vector2.One;
}
