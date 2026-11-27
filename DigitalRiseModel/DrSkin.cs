using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Describes the skinning data of a mesh part: the joint bones, the optional skeleton
/// root and the inverse bind matrices.
/// </summary>
public sealed class DrSkin
{
	/// <summary>
	/// Gets or sets the skin name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the skeleton root bone, if one is specified.
	/// </summary>
	public DrModelBone? Skeleton { get; set; }

	/// <summary>
	/// Gets or sets the joint bones.
	/// </summary>
	public DrModelBone[] Joints { get; set; } = [];

	/// <summary>
	/// Gets or sets the inverse bind matrices, one per joint.
	/// </summary>
	public Matrix[] InverseBindMatrices { get; set; } = [];

	/// <summary>
	/// Gets or sets an object that identifies this skin.
	/// </summary>
	public object? Tag { get; set; }
}
