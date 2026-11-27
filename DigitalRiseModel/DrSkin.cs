namespace DigitalRiseModel;

/// <summary>
/// Describes the skinning data of a mesh part: the joints and the optional skeleton root.
/// </summary>
public sealed class DrSkin
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DrSkin"/> class.
	/// </summary>
	/// <param name="index">The skin index in the model's skin collection.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
	public DrSkin(int index = 0)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);

		Index = index;
	}

	/// <summary>
	/// Gets or sets the skin name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets the skin index in the model's skin collection.
	/// </summary>
	public int Index { get; private set; }

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
