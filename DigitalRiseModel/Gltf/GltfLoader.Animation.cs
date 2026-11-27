using DigitalRiseModel.Animation;
using Microsoft.Xna.Framework;
using GltfAnimation = glTFLoader.Schema.Animation;
using GltfAnimationChannel = glTFLoader.Schema.AnimationChannel;
using GltfAnimationSampler = glTFLoader.Schema.AnimationSampler;
using GltfAnimationTarget = glTFLoader.Schema.AnimationChannelTarget;
using GltfDocument = glTFLoader.Schema.Gltf;

namespace DigitalRiseModel.Gltf;

internal static partial class GltfLoader
{
	private static Dictionary<string, AnimationClip> ConvertAnimations(
		GltfDocument document,
		DrModelBone?[] nodeBones,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		GltfAnimation[] source = document.Animations ?? [];
		var result = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);

		for (int animationIndex = 0; animationIndex < source.Length; animationIndex++)
		{
			GltfAnimation animation = source[animationIndex];
			GltfAnimationChannel[] sourceChannels = animation.Channels ?? [];
			GltfAnimationSampler[] sourceSamplers = animation.Samplers ?? [];
			var boneOrder = new List<int>();
			var boneTracks = new Dictionary<int, BoneTrackSet>();
			float duration = 0f;

			for (int channelIndex = 0; channelIndex < sourceChannels.Length; channelIndex++)
			{
				GltfAnimationChannel sourceChannel = sourceChannels[channelIndex];
				GltfAnimationTarget target = sourceChannel.Target
					?? throw new InvalidDataException(
						$"Animation {animationIndex} channel {channelIndex} has no target.");

				if (target.Node is not int nodeIndex)
				{
					throw new InvalidDataException(
						$"Animation {animationIndex} channel {channelIndex} has no target node.");
				}

				if ((uint)nodeIndex >= (uint)nodeBones.Length || nodeBones[nodeIndex] is null)
				{
					throw new InvalidDataException(
						$"Animation {animationIndex} channel {channelIndex} targets node {nodeIndex}, " +
						"which is not part of the loaded scene.");
				}

				if ((uint)sourceChannel.Sampler >= (uint)sourceSamplers.Length)
				{
					throw new InvalidDataException(
						$"Animation {animationIndex} channel {channelIndex} uses sampler " +
						$"{sourceChannel.Sampler}, which is outside the sampler collection.");
				}

				BoneAnimationPath path = ConvertAnimationPath(target.Path);
				AnimationTrack track = ReadAnimationTrack(
					document,
					sourceSamplers[sourceChannel.Sampler],
					path,
					bufferProvider,
					bufferCache);

				if (!boneTracks.TryGetValue(nodeIndex, out BoneTrackSet? tracks))
				{
					tracks = new BoneTrackSet();
					boneTracks.Add(nodeIndex, tracks);
					boneOrder.Add(nodeIndex);
				}

				int pathIndex = (int)path;

				if (tracks.Tracks[pathIndex] is not null)
				{
					throw new InvalidDataException(
						$"Animation {animationIndex} has more than one {target.Path.Value} channel " +
						$"for node {nodeIndex}.");
				}

				tracks.Tracks[pathIndex] = track;

				if (track.Times.Length > 0)
				{
					duration = MathF.Max(duration, track.Times[^1]);
				}
			}

			var channels = new AnimationChannel[boneOrder.Count];

			for (int i = 0; i < boneOrder.Count; i++)
			{
				int nodeIndex = boneOrder[i];
				channels[i] = CreateBoneChannel(nodeBones[nodeIndex]!, boneTracks[nodeIndex]);
			}

			string name = string.IsNullOrEmpty(animation.Name)
				? $"Animation{animationIndex}"
				: animation.Name;

			name = GetUniqueAnimationName(result, name);

			result[name] = new AnimationClip
			{
				Name = name,
				Duration = duration,
				Channels = channels
			};
		}

		return result;
	}

	private static AnimationChannel CreateBoneChannel(DrModelBone bone, BoneTrackSet tracks)
	{
		AnimationTrack? translationTrack = tracks.Tracks[(int)BoneAnimationPath.Translation];
		AnimationTrack? rotationTrack = tracks.Tracks[(int)BoneAnimationPath.Rotation];
		AnimationTrack? scaleTrack = tracks.Tracks[(int)BoneAnimationPath.Scale];

		var times = new SortedSet<float>();

		foreach (AnimationTrack? track in tracks.Tracks)
		{
			if (track is null)
			{
				continue;
			}

			foreach (float time in track.Times)
			{
				times.Add(time);
			}
		}

		SrtTransform basePose = DecomposeBoneTransform(bone.Transform);
		var keyframes = new AnimationChannelKeyframe[times.Count];
		int keyframeIndex = 0;

		foreach (float time in times)
		{
			var pose = new SrtTransform(
				translationTrack is null
					? basePose.Translation
					: SampleVector3(translationTrack, time),
				rotationTrack is null
					? basePose.Rotation
					: SampleQuaternion(rotationTrack, time),
				scaleTrack is null
					? basePose.Scale
					: SampleVector3(scaleTrack, time));

			keyframes[keyframeIndex++] = new AnimationChannelKeyframe(
				TimeSpan.FromSeconds(time),
				pose);
		}

		return new AnimationChannel(bone.Index, keyframes)
		{
			TranslationInterpolation = translationTrack?.Interpolation ?? AnimationInterpolation.Linear,
			RotationInterpolation = rotationTrack?.Interpolation ?? AnimationInterpolation.Linear,
			ScaleInterpolation = scaleTrack?.Interpolation ?? AnimationInterpolation.Linear
		};
	}

	private static AnimationTrack ReadAnimationTrack(
		GltfDocument document,
		GltfAnimationSampler sourceSampler,
		BoneAnimationPath path,
		Func<int, byte[]> bufferProvider,
		Dictionary<int, byte[]> bufferCache)
	{
		(float[] times, int timeComponents) = ReadFloats(
			document,
			sourceSampler.Input,
			bufferProvider,
			bufferCache);

		RequireComponents(timeComponents, 1, "animation time");

		(float[] values, int components) = ReadFloats(
			document,
			sourceSampler.Output,
			bufferProvider,
			bufferCache);

		int expectedComponents = path == BoneAnimationPath.Rotation ? 4 : 3;

		RequireComponents(components, expectedComponents, $"animation {path}");

		AnimationInterpolation interpolation = ConvertAnimationInterpolation(sourceSampler.Interpolation);
		int valuesPerKey = interpolation == AnimationInterpolation.CubicSpline
			? components * 3
			: components;
		long expectedValueCount = (long)times.Length * valuesPerKey;

		if (values.Length != expectedValueCount)
		{
			throw new InvalidDataException(
				$"Animation {path} sampler has {values.Length} values; expected " +
				$"{expectedValueCount} for {times.Length} keyframes with {interpolation} interpolation.");
		}

		for (int i = 0; i < times.Length; i++)
		{
			float time = times[i];

			if (!float.IsFinite(time) || time < 0f)
			{
				throw new InvalidDataException(
					$"Animation {path} sampler has an invalid time at index {i}.");
			}

			if (i > 0 && time <= times[i - 1])
			{
				throw new InvalidDataException(
					$"Animation {path} sampler times must be strictly increasing.");
			}
		}

		return new AnimationTrack
		{
			Times = times,
			Values = values,
			Components = components,
			Interpolation = interpolation
		};
	}

	private static SrtTransform DecomposeBoneTransform(Matrix transform)
	{
		if (transform.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation))
		{
			return new SrtTransform(translation, rotation, scale);
		}

		return SrtTransform.Identity;
	}

	private static Vector3 SampleVector3(AnimationTrack track, float time)
	{
		Vector4 value = Sample(track, time);
		return new Vector3(value.X, value.Y, value.Z);
	}

	private static Quaternion SampleQuaternion(AnimationTrack track, float time)
	{
		float[] times = track.Times;

		if (times.Length == 0)
		{
			return Quaternion.Identity;
		}

		if (times.Length == 1 || time <= times[0])
		{
			return ReadQuaternion(track, 0);
		}

		if (time >= times[^1])
		{
			return ReadQuaternion(track, times.Length - 1);
		}

		int index = FindKeyIndex(times, time);

		if (track.Interpolation == AnimationInterpolation.Step)
		{
			return ReadQuaternion(track, index);
		}

		if (track.Interpolation == AnimationInterpolation.CubicSpline)
		{
			Vector4 value = SampleCubic(track, index, time);
			return Quaternion.Normalize(new Quaternion(value.X, value.Y, value.Z, value.W));
		}

		float t = (time - times[index]) / (times[index + 1] - times[index]);
		Quaternion previous = ReadQuaternion(track, index);
		Quaternion next = ReadQuaternion(track, index + 1);

		return Quaternion.Normalize(Quaternion.Slerp(previous, next, t));
	}

	private static Vector4 Sample(AnimationTrack track, float time)
	{
		float[] times = track.Times;

		if (times.Length == 0)
		{
			return Vector4.Zero;
		}

		if (times.Length == 1 || time <= times[0])
		{
			return ReadValue(track, 0);
		}

		if (time >= times[^1])
		{
			return ReadValue(track, times.Length - 1);
		}

		int index = FindKeyIndex(times, time);

		if (track.Interpolation == AnimationInterpolation.Step)
		{
			return ReadValue(track, index);
		}

		if (track.Interpolation == AnimationInterpolation.CubicSpline)
		{
			return SampleCubic(track, index, time);
		}

		float t = (time - times[index]) / (times[index + 1] - times[index]);

		return Vector4.Lerp(
			ReadValue(track, index),
			ReadValue(track, index + 1),
			t);
	}

	private static Vector4 SampleCubic(AnimationTrack track, int index, float time)
	{
		float[] times = track.Times;
		int components = track.Components;
		int stride = components * 3;
		int currentOffset = index * stride;
		int nextOffset = (index + 1) * stride;

		Vector4 value0 = ReadComponents(track.Values, currentOffset + components, components);
		Vector4 outTangent0 = ReadComponents(track.Values, currentOffset + 2 * components, components);
		Vector4 inTangent1 = ReadComponents(track.Values, nextOffset, components);
		Vector4 value1 = ReadComponents(track.Values, nextOffset + components, components);

		float deltaTime = times[index + 1] - times[index];
		float t = (time - times[index]) / deltaTime;
		float t2 = t * t;
		float t3 = t2 * t;

		float h00 = 2f * t3 - 3f * t2 + 1f;
		float h10 = t3 - 2f * t2 + t;
		float h01 = -2f * t3 + 3f * t2;
		float h11 = t3 - t2;

		return h00 * value0
			+ h10 * deltaTime * outTangent0
			+ h01 * value1
			+ h11 * deltaTime * inTangent1;
	}

	private static Quaternion ReadQuaternion(AnimationTrack track, int keyIndex)
	{
		Vector4 value = ReadValue(track, keyIndex);
		return new Quaternion(value.X, value.Y, value.Z, value.W);
	}

	private static Vector4 ReadValue(AnimationTrack track, int keyIndex)
	{
		int stride = track.Interpolation == AnimationInterpolation.CubicSpline
			? track.Components * 3
			: track.Components;
		int offset = keyIndex * stride;

		if (track.Interpolation == AnimationInterpolation.CubicSpline)
		{
			offset += track.Components;
		}

		return ReadComponents(track.Values, offset, track.Components);
	}

	private static Vector4 ReadComponents(float[] values, int offset, int components)
	{
		return components switch
		{
			4 => new Vector4(
				values[offset],
				values[offset + 1],
				values[offset + 2],
				values[offset + 3]),
			3 => new Vector4(
				values[offset],
				values[offset + 1],
				values[offset + 2],
				0f),
			2 => new Vector4(
				values[offset],
				values[offset + 1],
				0f,
				0f),
			1 => new Vector4(values[offset], 0f, 0f, 0f),
			_ => throw new InvalidDataException(
				$"An animation value has {components} components; expected 1 to 4.")
		};
	}

	private static int FindKeyIndex(float[] times, float time)
	{
		int low = 0;
		int high = times.Length - 1;

		while (low < high)
		{
			int middle = (low + high + 1) / 2;

			if (times[middle] <= time)
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

	private static BoneAnimationPath ConvertAnimationPath(GltfAnimationTarget.PathEnum path)
	{
		return path.Value switch
		{
			"translation" => BoneAnimationPath.Translation,
			"rotation" => BoneAnimationPath.Rotation,
			"scale" => BoneAnimationPath.Scale,
			"weights" => throw new NotSupportedException(
				"Animation channels that target morph target weights are not supported."),
			_ => throw new InvalidDataException($"Unknown animation target path '{path.Value}'.")
		};
	}

	private static AnimationInterpolation ConvertAnimationInterpolation(
		GltfAnimationSampler.InterpolationEnum interpolation)
	{
		return interpolation.Value switch
		{
			"STEP" => AnimationInterpolation.Step,
			"CUBICSPLINE" => AnimationInterpolation.CubicSpline,
			_ => AnimationInterpolation.Linear
		};
	}

	private static string GetUniqueAnimationName(
		Dictionary<string, AnimationClip> animations,
		string name)
	{
		if (!animations.ContainsKey(name))
		{
			return name;
		}

		int suffix = 1;

		while (animations.ContainsKey($"{name}_{suffix}"))
		{
			suffix++;
		}

		return $"{name}_{suffix}";
	}

	private enum BoneAnimationPath
	{
		Translation,
		Rotation,
		Scale
	}

	private sealed class BoneTrackSet
	{
		public AnimationTrack?[] Tracks { get; } = new AnimationTrack?[3];
	}

	private sealed class AnimationTrack
	{
		public float[] Times { get; init; } = [];

		public float[] Values { get; init; } = [];

		public int Components { get; init; }

		public AnimationInterpolation Interpolation { get; init; }
	}
}
