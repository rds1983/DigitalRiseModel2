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

	/// <summary>
	/// Samples the channel at the specified time.
	/// </summary>
	/// <param name="time">The time within the clip.</param>
	/// <returns>The sampled pose.</returns>
	/// <exception cref="InvalidOperationException">The channel has no keyframes.</exception>
	public SrtTransform Sample(TimeSpan time)
	{
		AnimationChannelKeyframe[] keyframes = Keyframes;

		if (keyframes.Length == 0)
		{
			throw new InvalidOperationException("The animation channel has no keyframes.");
		}

		if (time <= keyframes[0].Time)
		{
			return keyframes[0].Pose;
		}

		if (time >= keyframes[^1].Time)
		{
			return keyframes[^1].Pose;
		}

		int index = FindKeyframeIndex(keyframes, time);
		AnimationChannelKeyframe current = keyframes[index];
		AnimationChannelKeyframe next = keyframes[index + 1];
		long segmentTicks = (next.Time - current.Time).Ticks;
		float amount = (float)((time - current.Time).Ticks / (double)segmentTicks);

		return SrtTransform.Interpolate(
			current.Pose,
			next.Pose,
			amount,
			TranslationInterpolation,
			RotationInterpolation,
			ScaleInterpolation);
	}

	private static int FindKeyframeIndex(AnimationChannelKeyframe[] keyframes, TimeSpan time)
	{
		int low = 0;
		int high = keyframes.Length - 1;

		while (low < high)
		{
			int middle = (low + high + 1) / 2;

			if (keyframes[middle].Time <= time)
			{
				low = middle;
			}
			else
			{
				high = middle - 1;
			}
		}

		return low;
	}
}
