# Loading glTF

glTF 2.0 files are loaded through the recursive `AssetManager` loader. The public entry point is the `LoadDrModel` extension method:

```csharp
using AssetManagementBase;
using DigitalRiseModel;

var assets = AssetManager.CreateFileAssetManager("Content");
DrModel model = assets.LoadDrModel(graphicsDevice, "models/robot.glb");
```

The graphics device is required because the loader creates MonoGame buffers and textures. The loader itself is internal; use `AssetManager` for loading.

## Recursive assets

`LoadDrModel` uses `AssetManager.UseLoader`, so relative paths inside the glTF resolve against the model's folder:

- External `.bin` buffers.
- External image files referenced by materials.
- Rooted and resource-based asset managers created through XNAssets.

## Readable buffers

For debugging or tests, pass `ModelLoadFlags.ReadableBuffers`:

```csharp
DrModel model = assets.LoadDrModel(
    graphicsDevice,
    "models/robot.glb",
    ModelLoadFlags.ReadableBuffers);
```

The loader then creates vertex and index buffers with `BufferUsage.None`, allowing `GetData` calls. The default `ModelLoadFlags.None` uses write-only buffers.

Use `ModelLoadFlags.KeepSourceGeometry` to keep CPU-side copies:

```csharp
DrModel model = assets.LoadDrModel(
    graphicsDevice,
    "models/robot.glb",
    ModelLoadFlags.KeepSourceGeometry);
```

This fills `DrModelMeshPart.SourceIndices` and `DrModelMeshPart.SourceVertices`; both remain `null` without the flag. Source vertices use `DigitalRiseModel.Vertices.VertexSkinned` with position, normal, UV0, `Byte4` blend indices and `Vector4` blend weights.

`ModelLoadFlags.EnsureUVs` adds a zero-valued `TEXCOORD_0` attribute to parts that do not have one, so a UV channel is always available. `ModelLoadFlags.EnsureUV1` does the same for `TEXCOORD_1`.

## What is loaded

- `.gltf` and `.glb` files.
- The selected scene, or the default scene.
- Scene nodes converted to `DrModelBone` instances under a synthetic root bone.
- Each glTF mesh primitive converted to a `DrModelMeshPart`.
- Triangle lists, triangle strips and triangle fans, converted to triangle lists.
- POSITION, NORMAL, TANGENT, TEXCOORD_n, COLOR_n, JOINTS_n and WEIGHTS_n vertex attributes.
- Metallic-roughness materials as `DrMaterial` with `DrTextureInfo` references.
- `KHR_materials_emissive_strength` as `DrMaterial.EmissiveStrength`.
- Textures as `Texture2D` in `DrTextureInfo.Texture`.
- Node skins as `DrSkin`, assigned to the parts of that node's mesh.
- Animation clips as `AnimationClip` instances in `DrModel.Animations`.

## Vertex layout

Vertex buffers reflect the primitive's actual glTF attributes. The loader creates a `VertexDeclaration` and writes a matching interleaved buffer. Read the layout from `DrModelMeshPart.VertexDeclaration` or `DrModelMeshPart.VertexBuffer.VertexDeclaration`.

| glTF attribute | Vertex element usage |
| --- | --- |
| `POSITION` | `Position`, index 0 |
| `NORMAL` | `Normal`, index 0 |
| `TANGENT` | `Tangent`, index 0 |
| `TEXCOORD_n` | `TextureCoordinate`, index `n` |
| `COLOR_n` | `Color`, index `n` |
| `JOINTS_n` | `BlendIndices`, index `n` |
| `WEIGHTS_n` | `BlendWeight`, index `n` |

All attribute values are stored as 32-bit floats; the component count follows the accessor (`VEC2` becomes `Vector2`, `VEC3` becomes `Vector3`, and so on). Multiple texture coordinate and color sets are preserved. Unknown custom attributes are ignored.

Read the interleaved bytes using the declaration:

```csharp
using Microsoft.Xna.Framework.Graphics;

VertexDeclaration declaration = part.VertexBuffer.VertexDeclaration;
var data = new byte[part.NumVertices * declaration.VertexStride];
part.VertexBuffer.GetData(data);
```

## Materials and textures

`DrModelMeshPart.Material` contains the glTF material data:

- base color factor and texture
- metallic and roughness factors and texture
- normal, occlusion and emissive textures
- emissive factor
- alpha mode and cutoff
- double-sided flag

External image files are loaded with `AssetManager.LoadTexture2D`; embedded images in `.glb` or data URIs are decoded from their buffer views. The result is assigned to `DrTextureInfo.Texture`.

## Skins

`DrModelMeshPart.Skin` contains:

- `Index` — the skin index in the glTF skin collection
- `Joints` — `DrSkinJoint` values, each pairing a joint bone with its inverse bind matrix
- `Skeleton` — the optional skeleton root

A glTF skin belongs to a node, so it is assigned to every part of that node's mesh.

`DrModelInstance.GetSkinTransforms(skin.Index)` returns the joint matrices for the instance's current pose, composed as `inverseBindMatrix * absoluteBoneTransform`.

## Animations

`DrModel.Animations` maps clip names to `DigitalRiseModel.Animation.AnimationClip` values. Each clip contains:

- `Name` — the glTF animation name, or `Animation{n}` when omitted.
- `Duration` — the largest keyframe time in seconds across its channels.
- `Channels` — one channel per animated bone.

Each `AnimationChannel` contains:

- `BoneIndex` — the index of the animated bone in `DrModel.Bones`.
- `Keyframes` — `AnimationChannelKeyframe` values with a `TimeSpan` time and a complete `SrtTransform` pose.
- `TranslationInterpolation`, `RotationInterpolation`, `ScaleInterpolation` — the glTF interpolation mode per component (`Step`, `Linear` or `CubicSpline`).

glTF translation, rotation and scale tracks are merged into one keyframe array per bone. The loader collects the union of all key times for that bone and samples each track at every time. Rotation uses shortest-path slerp for linear interpolation; cubic spline tracks use the glTF in-tangent, value and out-tangent layout while loading. Components without a track use the bone's local transform, so a rotation-only clip does not reset translation or scale. Baked keyframes do not retain cubic tangents, so runtime playback treats `CubicSpline` channels as linear between keyframes.

```csharp
DrModel model = assets.LoadDrModel(graphicsDevice, "models/robot.glb");

if (model.Animations.TryGetValue("Walk", out AnimationClip clip))
{
    AnimationChannel channel = clip.Channels[0];
    DrModelBone bone = model.Bones[channel.BoneIndex];
    SrtTransform pose = channel.Keyframes[0].Pose;
}
```

The loader is data-only: it does not modify bone transforms or drive playback.

Duplicate clip names are made unique by appending `_1`, `_2`, and so on. A channel that targets a node outside the loaded scene is rejected with `InvalidDataException`. Morph target weight animation is not supported and throws `NotSupportedException`.

## Playback

`AnimationController` drives a single clip on a `DrModelInstance`:

```csharp
var instance = new DrModelInstance(model);
var animation = new AnimationController(instance);

if (animation.Play("Walk"))
{
    animation.Update(elapsedTime);
}

animation.Stop();
```

`Play(name)` looks up the clip in `DrModel.Animations`, resets all bones to the bind pose and applies the first frame. `Update(TimeSpan)` advances the clip and writes sampled poses through `DrModelInstance.SetLocalTransform`. Keyframe lookup is handled by `AnimationChannel.Sample`, and pose interpolation by `SrtTransform.Interpolate`, which applies the translation, rotation and scale interpolation modes independently. `Loop` defaults to `true`; set it to `false` for one-shot clips. `Stop` clears the clip and restores the bind pose.

## Limitations

- Sparse accessors are not supported.
- Non-triangle primitive modes are not supported.
- Texture samplers are not represented.
- GPU buffer and texture creation requires a `GraphicsDevice`.
