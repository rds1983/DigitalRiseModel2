namespace DigitalRiseModel;

/// <summary>
/// Describes the skinning data of a mesh part: the joints and the optional skeleton root.
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
	/// Gets or sets the joints, each pairing a bone with its inverse bind matrix.
	/// </summary>
	public DrSkinJoint[] Joints { get; set; } = [];

	/// <summary>
	/// Gets or sets an object that identifies this skin.
	/// </summary>
	public object? Tag { get; set; }
}
