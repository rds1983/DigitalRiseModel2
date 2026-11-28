# Animation

## How do I animate a model?

Three steps: load a model with animations, create a `DrModelInstance`, and drive an `AnimationController` from your update loop.

```csharp
using DigitalRiseModel;
using DigitalRiseModel.Animation;

// 1. Load. Clips arrive in model.Animations, keyed by name.
DrModel model = assets.LoadDrModel(graphicsDevice, "models/robot.glb");

// 2. One instance owns the pose.
var instance = new DrModelInstance(model);

// 3. Play a clip and advance it each frame.
var animation = new AnimationController(instance);
animation.Play("Walk");

// In Update:
animation.Update(gameTime.ElapsedGameTime);
```

That is the whole loop. The controller writes sampled poses into the instance's local bone transforms, and your renderer draws the instance as usual — if the mesh is skinned, pass `instance.GetSkinTransforms(skin.Index)` to your effect.

```csharp
// In Draw, for each skinned part:
Matrix[] bones = instance.GetSkinTransforms(part.Skin.Index);
```

Nothing about the shared `DrModel` changes, so you can create as many instances as you like from one model, each with its own pose.

### Which clips does my model have?

```csharp
foreach (string name in model.Animations.Keys)
{
    Console.WriteLine(name);
}
```

`Play(name)` returns `false` when the name is missing, so guard on the result if the clip is optional:

```csharp
if (!animation.Play("Walk"))
{
    animation.Play(model.Animations.Keys.First());
}
```

### How do I start at a specific frame or scrub?

Use `Pause`, `Seek` and `Resume`. `Seek` clamps to the clip and applies that pose without changing play state, which is what a timeline slider needs:

```csharp
animation.Pause();
animation.Seek(TimeSpan.FromSeconds(0.75));
animation.Resume();
```

### How do I play once, or in reverse?

```csharp
animation.Loop = false;          // stop on the last frame instead of wrapping
animation.PlayBackwards = true;  // play from the end to the start
animation.Speed = 0.5f;          // half speed; must be non-negative
```

`Speed` combined with `PlayBackwards` gives reverse at any rate. `IsPlaying` becomes `false` when a non-looping clip reaches its end.

### What if I don't want the controller?

Sample the channels yourself and write the transforms wherever you like:

```csharp
foreach (AnimationChannel channel in clip.Channels)
{
    SrtTransform pose = channel.Sample(elapsed);
    instance.SetLocalTransform(channel.BoneIndex, pose.ToMatrix());
}
```

This is the same work the controller does. It is useful when you need blending, layering or a different time source.

## Animation data

`DrModel.Animations` is a `Dictionary<string, AnimationClip>` keyed by clip name. Loaded glTF animations populate it; you can also build clips by hand.

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

## Building a clip by hand

```csharp
var clip = new AnimationClip
{
    Name = "Bob",
    Duration = 2f,
    Channels = [channel]
};

model.Animations["Bob"] = clip;
```

## Sampling rules

`AnimationChannel.Sample(TimeSpan)` evaluates a channel:

- Times at or before the first keyframe return the first pose; times at or after the last return the last pose, so there is no clamping configuration to think about.
- Between keyframes, each component uses its own mode. `Step` holds the earlier value, `Linear` blends, and rotation uses shortest-path spherical interpolation.
- `CubicSpline` channels are linear at runtime: baked keyframes do not keep the cubic tangents. glTF cubic tracks are still converted correctly at load time.
- Sampling a channel with no keyframes throws `InvalidOperationException`.

## Controller reference

`AnimationController` owns single-clip playback for one `DrModelInstance`.

| Member | Meaning |
| --- | --- |
| `Instance` | The animated `DrModelInstance`. |
| `CurrentClip` | The active clip, or `null`. |
| `Time` / `Duration` | Current time and clip length. |
| `IsPlaying` | `true` while the clip is advancing. |
| `Loop` | Wrap at the ends. Defaults to `true`. |
| `Speed` | Non-negative multiplier. Defaults to `1`. |
| `PlayBackwards` | Reverse direction. Defaults to `false`. |

| Method | Behavior |
| --- | --- |
| `Play(string name)` | Looks up the clip in `DrModel.Animations`. Returns `false` if it is missing. |
| `Play(AnimationClip clip)` | Resets every bone to the bind pose, then applies the clip's first frame. |
| `Update(TimeSpan deltaTime)` | Advances time, wraps or stops at the ends, and applies the pose. Negative values throw. |
| `Pause()` | Stops advancing but keeps the current clip and pose. |
| `Resume()` | Continues from the current time, restarting if the clip had finished. |
| `Seek(TimeSpan time)` | Jumps to a clamped time and applies that pose without changing play state. |
| `Stop()` | Clears the clip and restores the bind pose. |

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
