namespace DigitalRiseModel.Animation;

/// <summary>
/// Animates one bone with a sequence of complete scale, rotation and translation poses.
/// </summary>
public class AnimationChannel
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AnimationChannel"/> class.
	/// </summary>
	/// <param name="boneIndex">The index of the animated bone in <see cref="DrModel.Bones"/>.</param>
	/// <param name="keyframes">The keyframes, ordered by <see cref="AnimationChannelKeyframe.Time"/>.</param>
	public AnimationChannel(int boneIndex, AnimationChannelKeyframe[] keyframes)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(boneIndex);
		ArgumentNullException.ThrowIfNull(keyframes);

		BoneIndex = boneIndex;
		Keyframes = keyframes;
	}

	/// <summary>
	/// Gets the index of the animated bone in <see cref="DrModel.Bones"/>.
	/// </summary>
	public int BoneIndex { get; }

	/// <summary>
	/// Gets the keyframes, ordered by <see cref="AnimationChannelKeyframe.Time"/>.
	/// </summary>
	public AnimationChannelKeyframe[] Keyframes { get; }

	/// <summary>
	/// Gets or sets the interpolation mode used for translation values.
	/// </summary>
	public AnimationInterpolation TranslationInterpolation { get; set; } = AnimationInterpolation.Linear;

	/// <summary>
	/// Gets or sets the interpolation mode used for rotation values.
	/// </summary>
	public AnimationInterpolation RotationInterpolation { get; set; } = AnimationInterpolation.Linear;

	/// <summary>
	/// Gets or sets the interpolation mode used for scale values.
	/// </summary>
	public AnimationInterpolation ScaleInterpolation { get; set; } = AnimationInterpolation.Linear;
}
