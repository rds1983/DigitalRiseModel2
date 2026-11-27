using DigitalRiseModel.Animation;
using Microsoft.Xna.Framework;

namespace DigitalRiseModel.Tests;

public sealed class AnimationControllerTests
{
	[Fact]
	public void Constructor_NullInstance_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new AnimationController(null!));
	}

	[Fact]
	public void Play_UnknownClip_ReturnsFalseAndKeepsCurrentState()
	{
		var controller = new AnimationController(CreateInstance());

		Assert.False(controller.Play("Missing"));
		Assert.Null(controller.CurrentClip);
		Assert.False(controller.IsPlaying);
	}

	[Fact]
	public void Play_UnknownClip_DoesNotStopCurrentClip()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance);
		controller.Play("Move");
		controller.Update(TimeSpan.FromMilliseconds(500));

		Assert.False(controller.Play("Missing"));
		Assert.Equal("Move", controller.CurrentClip!.Name);
		Assert.Equal(TimeSpan.FromMilliseconds(500), controller.Time);
	}

	[Fact]
	public void Play_NullClipName_ThrowsArgumentNullException()
	{
		var controller = new AnimationController(CreateInstance());

		Assert.Throws<ArgumentNullException>(() => controller.Play((string)null!));
	}

	[Fact]
	public void Play_KnownClip_AppliesFirstFrameAndResetsBindPose()
	{
		DrModelInstance instance = CreateInstance();
		instance.SetLocalTransform(0, Matrix.CreateTranslation(5, 0, 0));
		var controller = new AnimationController(instance);

		Assert.True(controller.Play("Move"));

		Assert.Equal("Move", controller.CurrentClip!.Name);
		Assert.True(controller.IsPlaying);
		Assert.Equal(TimeSpan.Zero, controller.Time);
		Assert.Equal(Matrix.Identity, instance.GetLocalTransform(0));
		Assert.Equal(Matrix.Identity, instance.GetLocalTransform(1));
	}

	[Fact]
	public void Update_LinearInterpolation_SamplesBetweenKeyframes()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance);
		controller.Play("Move");

		controller.Update(TimeSpan.FromMilliseconds(500));

		Assert.Equal(TimeSpan.FromMilliseconds(500), controller.Time);
		Assert.Equal(new Vector3(1, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Update_StepInterpolation_HoldsPreviousValue()
	{
		DrModelInstance instance = CreateInstance(AnimationInterpolation.Step);
		var controller = new AnimationController(instance)
		{
			Loop = false
		};
		controller.Play("Move");

		controller.Update(TimeSpan.FromMilliseconds(500));
		Assert.Equal(new Vector3(0, 0, 0), instance.GetLocalTransform(1).Translation);

		controller.Update(TimeSpan.FromMilliseconds(500));
		Assert.False(controller.IsPlaying);
		Assert.Equal(new Vector3(2, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Update_Loop_WrapsTimeAndPose()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance);
		controller.Play("Move");

		controller.Update(TimeSpan.FromMilliseconds(1250));

		Assert.Equal(TimeSpan.FromMilliseconds(250), controller.Time);
		Assert.True(controller.IsPlaying);
		Assert.Equal(new Vector3(0.5f, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Update_LoopDisabled_StopsAtClipEnd()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			Loop = false
		};
		controller.Play("Move");

		controller.Update(TimeSpan.FromSeconds(2));

		Assert.False(controller.IsPlaying);
		Assert.Equal(TimeSpan.FromSeconds(1), controller.Time);
		Assert.Equal(new Vector3(2, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Stop_ResetsToBindPose()
	{
		DrModelInstance instance = CreateInstance();
		instance.Model.Bones[1].Transform = Matrix.CreateTranslation(3, 0, 0);
		var controller = new AnimationController(instance);
		controller.Play("Move");
		controller.Update(TimeSpan.FromMilliseconds(500));

		controller.Stop();

		Assert.False(controller.IsPlaying);
		Assert.Null(controller.CurrentClip);
		Assert.Equal(TimeSpan.Zero, controller.Time);
		Assert.Equal(Matrix.CreateTranslation(3, 0, 0), instance.GetLocalTransform(1));
	}

	[Fact]
	public void Duration_NoClip_ReturnsZero()
	{
		var controller = new AnimationController(CreateInstance());

		Assert.Equal(TimeSpan.Zero, controller.Duration);
	}

	[Fact]
	public void Duration_AfterPlay_ReturnsClipDuration()
	{
		var controller = new AnimationController(CreateInstance());

		controller.Play("Move");

		Assert.Equal(TimeSpan.FromSeconds(1), controller.Duration);
	}

	[Fact]
	public void Pause_StopsTimeAdvance()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance);
		controller.Play("Move");
		controller.Update(TimeSpan.FromMilliseconds(500));

		controller.Pause();
		controller.Update(TimeSpan.FromMilliseconds(500));

		Assert.False(controller.IsPlaying);
		Assert.Equal(TimeSpan.FromMilliseconds(500), controller.Time);
	}

	[Fact]
	public void Resume_ContinuesFromCurrentTime()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance);
		controller.Play("Move");
		controller.Update(TimeSpan.FromMilliseconds(250));
		controller.Pause();

		controller.Resume();
		controller.Update(TimeSpan.FromMilliseconds(250));

		Assert.True(controller.IsPlaying);
		Assert.Equal(TimeSpan.FromMilliseconds(500), controller.Time);
		Assert.Equal(new Vector3(1, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Resume_AfterClipFinished_RestartsFromBeginning()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			Loop = false
		};
		controller.Play("Move");
		controller.Update(TimeSpan.FromSeconds(2));
		Assert.False(controller.IsPlaying);

		controller.Resume();

		Assert.True(controller.IsPlaying);
		Assert.Equal(TimeSpan.Zero, controller.Time);
		Assert.Equal(Matrix.Identity, instance.GetLocalTransform(1));
	}

	[Fact]
	public void Seek_AppliesPoseAtRequestedTime()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance);
		controller.Play("Move");

		controller.Seek(TimeSpan.FromMilliseconds(500));

		Assert.Equal(TimeSpan.FromMilliseconds(500), controller.Time);
		Assert.Equal(new Vector3(1, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Seek_ClampsToClipRange()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			Loop = false
		};
		controller.Play("Move");

		controller.Seek(TimeSpan.FromSeconds(5));
		Assert.Equal(TimeSpan.FromSeconds(1), controller.Time);
		Assert.Equal(new Vector3(2, 0, 0), instance.GetLocalTransform(1).Translation);

		controller.Seek(TimeSpan.FromSeconds(-1));
		Assert.Equal(TimeSpan.Zero, controller.Time);
		Assert.Equal(Matrix.Identity, instance.GetLocalTransform(1));
	}

	[Fact]
	public void Seek_WithoutClip_DoesNothing()
	{
		var controller = new AnimationController(CreateInstance());

		controller.Seek(TimeSpan.FromSeconds(1));

		Assert.Equal(TimeSpan.Zero, controller.Time);
	}

	[Fact]
	public void Speed_DefaultsToOne()
	{
		var controller = new AnimationController(CreateInstance());

		Assert.Equal(1f, controller.Speed);
	}

	[Fact]
	public void Speed_Negative_ThrowsArgumentOutOfRangeException()
	{
		var controller = new AnimationController(CreateInstance());

		Assert.Throws<ArgumentOutOfRangeException>(() => controller.Speed = -1f);
	}

	[Fact]
	public void Update_WithSpeedMultiplier_AdvancesFaster()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			Speed = 2f
		};
		controller.Play("Move");

		controller.Update(TimeSpan.FromMilliseconds(250));

		Assert.Equal(TimeSpan.FromMilliseconds(500), controller.Time);
		Assert.Equal(new Vector3(1, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Play_Backwards_StartsAtClipEnd()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			PlayBackwards = true
		};

		controller.Play("Move");

		Assert.Equal(TimeSpan.FromSeconds(1), controller.Time);
		Assert.Equal(new Vector3(2, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Update_Backwards_DecrementsTime()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			PlayBackwards = true
		};
		controller.Play("Move");

		controller.Update(TimeSpan.FromMilliseconds(250));

		Assert.Equal(TimeSpan.FromMilliseconds(750), controller.Time);
		Assert.Equal(new Vector3(1.5f, 0, 0), instance.GetLocalTransform(1).Translation);
	}

	[Fact]
	public void Update_BackwardsLoop_WrapsToEnd()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			PlayBackwards = true
		};
		controller.Play("Move");

		controller.Update(TimeSpan.FromMilliseconds(1250));

		Assert.Equal(TimeSpan.FromMilliseconds(750), controller.Time);
		Assert.True(controller.IsPlaying);
	}

	[Fact]
	public void Update_BackwardsLoopDisabled_StopsAtZero()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			Loop = false,
			PlayBackwards = true
		};
		controller.Play("Move");

		controller.Update(TimeSpan.FromSeconds(2));

		Assert.Equal(TimeSpan.Zero, controller.Time);
		Assert.False(controller.IsPlaying);
		Assert.Equal(Matrix.Identity, instance.GetLocalTransform(1));
	}

	[Fact]
	public void Resume_BackwardsAtStart_RestartsFromEnd()
	{
		DrModelInstance instance = CreateInstance();
		var controller = new AnimationController(instance)
		{
			Loop = false,
			PlayBackwards = true
		};
		controller.Play("Move");
		controller.Update(TimeSpan.FromSeconds(2));
		Assert.False(controller.IsPlaying);

		controller.Resume();

		Assert.True(controller.IsPlaying);
		Assert.Equal(TimeSpan.FromSeconds(1), controller.Time);
	}

	[Fact]
	public void Update_NegativeDeltaTime_ThrowsArgumentOutOfRangeException()
	{
		var controller = new AnimationController(CreateInstance());

		Assert.Throws<ArgumentOutOfRangeException>(
			() => controller.Update(TimeSpan.FromMilliseconds(-1)));
	}

	private static DrModelInstance CreateInstance(
		AnimationInterpolation interpolation = AnimationInterpolation.Linear)
	{
		var root = new DrModelBone("Root");
		var child = new DrModelBone("Child");
		root.Children = [child];
		var model = new DrModel(root);

		var channel = new AnimationChannel(
			1,
			[
				new AnimationChannelKeyframe(
					TimeSpan.Zero,
					new SrtTransform(Vector3.Zero, Quaternion.Identity, Vector3.One)),
				new AnimationChannelKeyframe(
					TimeSpan.FromSeconds(1),
					new SrtTransform(new Vector3(2, 0, 0), Quaternion.Identity, Vector3.One))
			])
		{
			TranslationInterpolation = interpolation
		};

		model.Animations["Move"] = new AnimationClip
		{
			Name = "Move",
			Duration = 1f,
			Channels = [channel]
		};

		return new DrModelInstance(model);
	}
}
