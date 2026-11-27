namespace DigitalRiseModel.Animation;

/// <summary>
/// Represents a full animation clip: its name, duration and animated channels.
/// </summary>
public sealed class AnimationClip
{
	/// <summary>
	/// Gets or sets the clip name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the clip duration in seconds.
	/// </summary>
	public float Duration { get; set; }

	/// <summary>
	/// Gets or sets the animation channels.
	/// </summary>
	public AnimationChannel[] Channels { get; set; } = [];

	/// <summary>
	/// Gets or sets an object that identifies this clip.
	/// </summary>
	public object? Tag { get; set; }
}
