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
	public void Sample_EmptyKeyframes_ThrowsInvalidOperationException()
	{
		var channel = new AnimationChannel(0, []);

		Assert.Throws<InvalidOperationException>(() => channel.Sample(TimeSpan.Zero));
	}

	[Fact]
	public void Sample_BeforeFirstKeyframe_ReturnsFirstPose()
	{
		AnimationChannel channel = CreateChannel();

		SrtTransform pose = channel.Sample(TimeSpan.FromSeconds(-1));

		Assert.Equal(Vector3.Zero, pose.Translation);
	}

	[Fact]
	public void Sample_AfterLastKeyframe_ReturnsLastPose()
	{
		AnimationChannel channel = CreateChannel();

		SrtTransform pose = channel.Sample(TimeSpan.FromSeconds(2));

		Assert.Equal(new Vector3(2, 0, 0), pose.Translation);
		Assert.Equal(new Vector3(3, 3, 3), pose.Scale);
	}

	[Fact]
	public void Sample_Linear_InterpolatesPose()
	{
		AnimationChannel channel = CreateChannel();

		SrtTransform pose = channel.Sample(TimeSpan.FromMilliseconds(500));

		Assert.Equal(new Vector3(1, 0, 0), pose.Translation);
		Assert.Equal(Quaternion.Identity, pose.Rotation);
		Assert.Equal(new Vector3(2, 2, 2), pose.Scale);
	}

	[Fact]
	public void Sample_Step_HoldsPreviousPose()
	{
		AnimationChannel channel = CreateChannel();
		channel.TranslationInterpolation = AnimationInterpolation.Step;
		channel.ScaleInterpolation = AnimationInterpolation.Step;

		SrtTransform pose = channel.Sample(TimeSpan.FromMilliseconds(500));

		Assert.Equal(Vector3.Zero, pose.Translation);
		Assert.Equal(Vector3.One, pose.Scale);
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

	private static AnimationChannel CreateChannel()
	{
		return new AnimationChannel(
			0,
			[
				new AnimationChannelKeyframe(
					TimeSpan.Zero,
					new SrtTransform(Vector3.Zero, Quaternion.Identity, Vector3.One)),
				new AnimationChannelKeyframe(
					TimeSpan.FromSeconds(1),
					new SrtTransform(
						new Vector3(2, 0, 0),
						Quaternion.Identity,
						new Vector3(3, 3, 3)))
			]);
	}
}
