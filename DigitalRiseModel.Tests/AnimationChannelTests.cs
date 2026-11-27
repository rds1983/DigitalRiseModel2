using DigitalRiseModel.Animation;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class AnimationChannelTests
{
	[Fact]
	public void Constructor_AssignsBoneIndexAndKeyframes()
	{
		var keyframes = new[]
		{
			new AnimationChannelKeyframe(TimeSpan.Zero, SrtTransform.Identity)
		};

		var channel = new AnimationChannel(2, keyframes);

		Assert.Equal(2, channel.BoneIndex);
		Assert.Same(keyframes, channel.Keyframes);
	}

	[Fact]
	public void Constructor_DefaultsInterpolationToLinear()
	{
		var channel = new AnimationChannel(0, []);

		Assert.Equal(AnimationInterpolation.Linear, channel.TranslationInterpolation);
		Assert.Equal(AnimationInterpolation.Linear, channel.RotationInterpolation);
		Assert.Equal(AnimationInterpolation.Linear, channel.ScaleInterpolation);
	}

	[Fact]
	public void Constructor_NegativeBoneIndex_ThrowsArgumentOutOfRangeException()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationChannel(-1, []));
	}

	[Fact]
	public void Constructor_NullKeyframes_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new AnimationChannel(0, null!));
	}

	[Fact]
	public void Keyframe_Constructor_AssignsTimeAndPose()
	{
		var pose = new SrtTransform(
			new Vector3(1, 2, 3),
			Quaternion.Identity,
			Vector3.One);
		var keyframe = new AnimationChannelKeyframe(TimeSpan.FromSeconds(2), pose);

		Assert.Equal(TimeSpan.FromSeconds(2), keyframe.Time);
		Assert.Equal(pose, keyframe.Pose);
	}
}
