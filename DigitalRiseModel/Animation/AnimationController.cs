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
	private float _speed = 1f;

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
	/// Gets the duration of <see cref="CurrentClip"/>, or <see cref="TimeSpan.Zero"/>
	/// when no clip is active.
	/// </summary>
	public TimeSpan Duration => _currentClip is null
		? TimeSpan.Zero
		: TimeSpan.FromSeconds(_currentClip.Duration);

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
	/// Gets or sets the playback speed multiplier. The value must be non-negative;
	/// use <see cref="PlayBackwards"/> for reverse playback. Defaults to 1.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">The assigned value is negative.</exception>
	public float Speed
	{
		get => _speed;
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			_speed = value;
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the clip plays in reverse.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	public bool PlayBackwards { get; set; }

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
		_time = PlayBackwards
			? TimeSpan.FromSeconds(clip.Duration)
			: TimeSpan.Zero;
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
	/// Pauses playback without clearing the current clip.
	/// </summary>
	public void Pause()
	{
		_isPlaying = false;
	}

	/// <summary>
	/// Resumes the current clip from its current time. If the clip has finished,
	/// playback restarts from its beginning.
	/// </summary>
	public void Resume()
	{
		if (_currentClip is null)
		{
			return;
		}

		if (PlayBackwards)
		{
			if (_time <= TimeSpan.Zero)
			{
				_time = Duration;
			}
		}
		else if (_time >= Duration)
		{
			_time = TimeSpan.Zero;
		}

		_isPlaying = true;
		ApplyPose();
	}

	/// <summary>
	/// Moves playback to the specified time and applies that pose. The time is clamped
	/// to the clip's duration. Playback state is not changed.
	/// </summary>
	/// <param name="time">The time within the current clip.</param>
	public void Seek(TimeSpan time)
	{
		if (_currentClip is null)
		{
			return;
		}

		TimeSpan duration = Duration;

		if (time < TimeSpan.Zero)
		{
			time = TimeSpan.Zero;
		}
		else if (time > duration)
		{
			time = duration;
		}

		_time = time;
		ApplyPose();
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

		TimeSpan duration = Duration;

		if (duration <= TimeSpan.Zero)
		{
			_time = TimeSpan.Zero;
			_isPlaying = false;
			ApplyPose();
			return;
		}

		double ticks = deltaTime.Ticks * (double)Speed * (PlayBackwards ? -1.0 : 1.0);
		_time += TimeSpan.FromTicks((long)ticks);

		if (_time < TimeSpan.Zero)
		{
			if (Loop)
			{
				_time = TimeSpan.FromTicks(NormalizeTicks(_time.Ticks, duration.Ticks));
			}
			else
			{
				_time = TimeSpan.Zero;
				_isPlaying = false;
			}
		}
		else if (_time >= duration)
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

	private static long NormalizeTicks(long ticks, long period)
	{
		long remainder = ticks % period;
		return remainder < 0 ? remainder + period : remainder;
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
