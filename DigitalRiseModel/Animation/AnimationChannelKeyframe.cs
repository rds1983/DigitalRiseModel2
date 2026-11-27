namespace DigitalRiseModel.Animation;

/// <summary>
/// A single keyframe of an <see cref="AnimationChannel"/>.
/// </summary>
public struct AnimationChannelKeyframe
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AnimationChannelKeyframe"/> structure.
	/// </summary>
	/// <param name="time">The keyframe time relative to the start of the clip.</param>
	/// <param name="pose">The bone pose at the keyframe time.</param>
	public AnimationChannelKeyframe(TimeSpan time, SrtTransform pose)
	{
		Time = time;
		Pose = pose;
	}

	/// <summary>
	/// Gets the keyframe time relative to the start of the clip.
	/// </summary>
	public TimeSpan Time { get; }

	/// <summary>
	/// Gets the bone pose at the keyframe time.
	/// </summary>
	public SrtTransform Pose { get; }
}
