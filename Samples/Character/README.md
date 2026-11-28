# Character

A runnable MonoGame sample that drives an animated character across a flat field with WASD movement, mouse turning and a chase camera.

```bash
dotnet run --project Samples/Character
```

## Controls

- `W` / `A` / `S` / `D`: move the character relative to its facing.
- Mouse (horizontal): turn the character. The cursor is re-centred each frame.
- `Escape`: exit.

The camera always sits behind the character's back and looks slightly past it.

## What it shows

- Loading a skinned glTF model through `AssetManager.LoadDrModel`.
- Driving `Run` and `Idle` clips with `AnimationController`, switching on movement input.
- Building the ground field with `MeshPrimitives.CreatePlaneLines`.
- Applying the character's position and facing to the root bone *after* the controller samples the clip, so movement and animation compose.
- Rendering with stock MonoGame effects only, through a `BeginFrame`/`Draw`/`EndFrame` renderer.
- Myra controls and stats panels, set up the same way as the viewer's UI.

## Code map

| File | Purpose |
| --- | --- |
| `CharacterGame.cs` | Input, clip switching, camera, Myra UI and the frame loop |
| `Scene.cs` | Holds the field and character, movement and facing state, and `Draw(Renderer)` |
| `Graphics/Renderer.cs` | Low-level stock-effect renderer, scene-agnostic |
| `Graphics/RenderStatistics.cs` | Per-frame draw call and triangle counters |
| `UI/ControlsPanel.xmmp` | Myra controls overlay; regenerate with `myra-export` |
| `Content/mixamo.gltf`, `Content/mixamo.bin` | Character model with `Idle` and `Run` clips |

## Renderer design

`Renderer` mirrors the viewer's renderer: it owns no scene content, only the effects, device
state and statistics. A frame looks like this:

```csharp
_renderer.BeginFrame(view, projection);
_scene.Draw(_renderer);
_renderer.EndFrame();
```

`DrawPart(part, world, boneMatrices)` picks the effect per part:

- `boneMatrices` is not null → `SkinnedEffect`. The world matrix stays identity because the bone
  transforms already include the root motion.
- `boneMatrices` is null → `BasicEffect`, with lighting enabled only when the part has normals.
  The field uses this path.

A part that has `Skin` but receives `null` bone matrices throws `InvalidOperationException`, so a
forgotten skinning setup fails loudly instead of drawing the bind pose.

`DrawInstance(instance)` draws every part of a `DrModelInstance`, resolving the world matrix and
skin transforms itself.


## Model source

The character model is from [Mixamo](https://www.mixamo.com/).
