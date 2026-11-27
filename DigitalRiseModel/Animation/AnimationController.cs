using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Animation;

/// <summary>
/// Plays animation clips from a <see cref="DrModelInstance"/> model by writing keyframe
/// poses into the instance's local bone transforms.
/// </summary>
public sealed class AnimationController
{
	private readonly DrModelInstance _instance;
	private AnimationClip? _currentClip;
	private TimeSpan _time;
	private bool _isPlaying;

	/// <summary>
	/// Initializes a new instance of the <see cref="AnimationController"/> class.
	/// </summary>
	/// <param name="instance">The model instance to animate.</param>
	/// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
	public AnimationController(DrModelInstance instance)
	{
		ArgumentNullException.ThrowIfNull(instance);

		_instance = instance;
	}

	/// <summary>
	/// Gets the animated model instance.
	/// </summary>
	public DrModelInstance Instance => _instance;

	/// <summary>
	/// Gets the clip that is currently playing, or <see langword="null"/> when no clip is active.
	/// </summary>
	public AnimationClip? CurrentClip => _currentClip;

	/// <summary>
	/// Gets the current time within <see cref="CurrentClip"/>.
	/// </summary>
	public TimeSpan Time => _time;

	/// <summary>
	/// Gets a value indicating whether a clip is currently advancing.
	/// </summary>
	public bool IsPlaying => _isPlaying;

	/// <summary>
	/// Gets or sets a value indicating whether playback loops when it reaches the end
	/// of the clip. Defaults to <see langword="true"/>.
	/// </summary>
	public bool Loop { get; set; } = true;

	/// <summary>
	/// Plays a clip by name from <see cref="DrModel.Animations"/>.
	/// </summary>
	/// <param name="clipName">The clip name.</param>
	/// <returns>
	/// <see langword="true"/> if the clip was found and started; otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="clipName"/> is <see langword="null"/>.</exception>
	public bool Play(string clipName)
	{
		ArgumentNullException.ThrowIfNull(clipName);

		if (!_instance.Model.Animations.TryGetValue(clipName, out AnimationClip? clip))
		{
			return false;
		}

		Play(clip);
		return true;
	}

	/// <summary>
	/// Plays a clip from its beginning. All bones are first reset to the model's bind pose,
	/// then the clip's first frame is applied.
	/// </summary>
	/// <param name="clip">The clip to play.</param>
	/// <exception cref="ArgumentNullException"><paramref name="clip"/> is <see langword="null"/>.</exception>
	public void Play(AnimationClip clip)
	{
		ArgumentNullException.ThrowIfNull(clip);

		ResetToBindPose();
		_currentClip = clip;
		_time = TimeSpan.Zero;
		_isPlaying = true;
		ApplyPose();
	}

	/// <summary>
	/// Stops playback, clears the current clip and resets the instance to the bind pose.
	/// </summary>
	public void Stop()
	{
		_currentClip = null;
		_time = TimeSpan.Zero;
		_isPlaying = false;
		ResetToBindPose();
	}

	/// <summary>
	/// Advances the current clip and applies its pose to the instance.
	/// </summary>
	/// <param name="deltaTime">The elapsed time.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="deltaTime"/> is negative.
	/// </exception>
	public void Update(TimeSpan deltaTime)
	{
		if (deltaTime < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(
				nameof(deltaTime),
				deltaTime,
				"The elapsed time must not be negative.");
		}

		if (!_isPlaying || _currentClip is null)
		{
			return;
		}

		TimeSpan duration = TimeSpan.FromSeconds(_currentClip.Duration);

		if (duration <= TimeSpan.Zero)
		{
			_time = TimeSpan.Zero;
			_isPlaying = false;
			ApplyPose();
			return;
		}

		_time += deltaTime;

		if (_time >= duration)
		{
			if (Loop)
			{
				_time = TimeSpan.FromTicks(_time.Ticks % duration.Ticks);
			}
			else
			{
				_time = duration;
				_isPlaying = false;
			}
		}

		ApplyPose();
	}

	private void ResetToBindPose()
	{
		for (int i = 0; i < _instance.BoneCount; i++)
		{
			_instance.SetLocalTransform(i, _instance.Model.Bones[i].Transform);
		}
	}

	private void ApplyPose()
	{
		if (_currentClip is null)
		{
			return;
		}

		foreach (AnimationChannel channel in _currentClip.Channels)
		{
			if (channel.Keyframes.Length == 0)
			{
				continue;
			}

			SrtTransform pose = channel.Sample(_time);
			_instance.SetLocalTransform(channel.BoneIndex, pose.ToMatrix());
		}
	}
}
