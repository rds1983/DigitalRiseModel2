# Animation

The `DigitalRiseModel.Animation` namespace holds animation data and a single-clip player. Like the rest of the library it is data-only: clips are stored as keyframes, and applying a pose is up to `AnimationController` or your own code.

## Animation data

`DrModel.Animations` is a `Dictionary<string, AnimationClip>` keyed by clip name. Loaded glTF animations populate it; you can also build clips by hand.

```csharp
using DigitalRiseModel.Animation;

foreach (var pair in model.Animations)
{
    Console.WriteLine($"{pair.Key}: {pair.Value.Duration:0.##}s, {pair.Value.Channels.Length} channels");
}
```

### AnimationClip

| Member | Meaning |
| --- | --- |
| `Name` | Clip name. Duplicate glTF names are made unique with `_1`, `_2`, suffixes. |
| `Duration` | Clip length in seconds. For glTF this is the largest keyframe time. |
| `Channels` | One `AnimationChannel` per animated bone. |
| `Tag` | Free-form identifier for your own use. |

### AnimationChannel

A channel animates one bone with a sequence of complete scale-rotation-translation poses.

| Member | Meaning |
| --- | --- |
| `BoneIndex` | Index into `DrModel.Bones` of the animated bone. |
| `Keyframes` | `AnimationChannelKeyframe[]`, ordered by time. |
| `TranslationInterpolation` | `Step`, `Linear` or `CubicSpline`. Defaults to `Linear`. |
| `RotationInterpolation` | Same modes; rotation interpolates with slerp. Defaults to `Linear`. |
| `ScaleInterpolation` | Same modes. Defaults to `Linear`. |

Interpolation is stored per component, so a channel can step its translation while interpolating rotation.

### AnimationChannelKeyframe

Each keyframe is a `TimeSpan` time relative to the clip start and a full `SrtTransform` pose.

```csharp
var channel = new AnimationChannel(
    boneIndex: 1,
    keyframes:
    [
        new AnimationChannelKeyframe(TimeSpan.Zero, SrtTransform.Identity),
        new AnimationChannelKeyframe(
            TimeSpan.FromSeconds(1),
            new SrtTransform(
                new Vector3(0f, 1f, 0f),
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.PiOver2),
                Vector3.One))
    ])
{
    RotationInterpolation = AnimationInterpolation.Linear
};
```

## Sampling poses

`AnimationChannel.Sample(TimeSpan)` evaluates the channel without touching any model data:

```csharp
SrtTransform pose = channel.Sample(TimeSpan.FromSeconds(0.5f));
Matrix local = pose.ToMatrix();
```

Sampling rules:

- Times at or before the first keyframe return the first pose; times at or after the last return the last pose, so there is no clamping configuration to think about.
- Between keyframes, each component interpolates with its own mode. `Step` holds the earlier value, `Linear` blends, and rotation uses shortest-path spherical interpolation.
- `CubicSpline` channels are treated as linear at runtime: baked keyframes do not keep the cubic tangents. glTF cubic tracks are still converted correctly at load time.
- Sampling a channel with no keyframes throws `InvalidOperationException`.

A custom player can sample channels directly and write the result wherever it likes:

```csharp
foreach (AnimationChannel channel in clip.Channels)
{
    SrtTransform pose = channel.Sample(elapsed);
    instance.SetLocalTransform(channel.BoneIndex, pose.ToMatrix());
}
```

## Playback

`AnimationController` owns single-clip playback for one `DrModelInstance`. It records the current time and writes sampled poses into the instance's local transforms; shared `DrModel` data is never modified.

```csharp
using DigitalRiseModel.Animation;

var instance = new DrModelInstance(model);
var animation = new AnimationController(instance)
{
    Loop = true,
    Speed = 1f
};

if (animation.Play("Walk"))
{
    // In your update loop:
    animation.Update(gameTime.ElapsedGameTime);
}
```

### Playback state

| Member | Meaning |
| --- | --- |
| `Instance` | The animated `DrModelInstance`. |
| `CurrentClip` | The active clip, or `null`. |
| `Time` | Current time within the clip. |
| `Duration` | Current clip's duration, or `TimeSpan.Zero`. |
| `IsPlaying` | `true` while the clip is advancing. |
| `Loop` | Wrap at the ends. Defaults to `true`. |
| `Speed` | Non-negative multiplier. Defaults to `1`. |
| `PlayBackwards` | Reverse direction. Defaults to `false`. |

### Methods

| Method | Behavior |
| --- | --- |
| `Play(string name)` | Looks up the clip in `DrModel.Animations`. Returns `false` if it is missing. |
| `Play(AnimationClip clip)` | Resets every bone to the bind pose, then applies the clip's first frame. |
| `Update(TimeSpan deltaTime)` | Advances time, wraps or stops at the ends, and applies the pose. Negative values throw. |
| `Pause()` | Stops advancing but keeps the current clip and pose. |
| `Resume()` | Continues from the current time, restarting if the clip had finished. |
| `Seek(TimeSpan time)` | Jumps to a clamped time and applies that pose without changing play state. |
| `Stop()` | Clears the clip and restores the bind pose. |

`speed` multiplies the elapsed time and `PlayBackwards` negates it, so `Speed = 2` with `PlayBackwards = true` plays at double speed in reverse. With `Loop = false`, playback stops at the last frame; with `Loop = true`, time wraps in either direction.

Binding `Seek` to a slider and `Pause`/`Resume` to a button is enough to build a scrubber. `Samples/SimpleViewer` and the viewer both do this in their animation panel.

## glTF mapping

When a model is loaded, glTF translation, rotation and scale tracks are merged into one keyframe array per bone:

- The loader takes the union of all key times for the bone and samples each track there.
- Rotation-only clips keep the bone's translation and scale, because components without a track fall back to the bone's local transform.
- Missing animation names become `Animation{n}`.
- Morph target weight animation is not supported and throws `NotSupportedException`.

See [Loading glTF](gltf-loading.md) for the loader-side details.

## Limitations

- One clip plays at a time; there is no blending or cross-fading.
- Playback writes local transforms only. Skinning still happens through `DrModelInstance.GetSkinTransforms` in your renderer.
- `CubicSpline` is linear between baked keyframes at runtime.
