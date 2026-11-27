using Microsoft.Xna.Framework;

namespace DigitalRiseModel;

/// <summary>
/// Describes one joint of a <see cref="DrSkin"/>: the joint bone and its inverse bind matrix.
/// </summary>
public sealed class DrSkinJoint
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DrSkinJoint"/> class.
	/// </summary>
	/// <param name="bone">The joint bone.</param>
	/// <param name="inverseBindMatrix">The inverse bind matrix of the joint.</param>
	/// <exception cref="ArgumentNullException"><paramref name="bone"/> is <see langword="null"/>.</exception>
	public DrSkinJoint(DrModelBone bone, Matrix inverseBindMatrix)
	{
		ArgumentNullException.ThrowIfNull(bone);

		Bone = bone;
		InverseBindMatrix = inverseBindMatrix;
	}

	/// <summary>
	/// Gets or sets the joint bone.
	/// </summary>
	public DrModelBone Bone { get; set; }

	/// <summary>
	/// Gets or sets the inverse bind matrix of the joint.
	/// </summary>
	public Matrix InverseBindMatrix { get; set; }
}
