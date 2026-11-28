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
- Building the ground field with `MeshPrimitives.CreatePlane`.
- Applying the character's position and facing to the root bone *after* the controller samples the clip, so movement and animation compose.
- Rendering with stock MonoGame effects only.
- A Myra controls panel, set up the same way as the viewer's UI.

## Code map

| File | Purpose |
| --- | --- |
| `CharacterGame.cs` | Input, clip switching, camera, Myra UI and the frame loop |
| `Scene.cs` | Holds the field and character, plus movement and facing state |
| `Graphics/Renderer.cs` | Low-level stock-effect renderer, scene-agnostic |
| `UI/ControlsPanel.xmmp` | Myra controls overlay; regenerate with `myra-export` |
| `Content/mixamo.gltf`, `Content/mixamo.bin` | Character model with `Idle` and `Run` clips |

## Renderer design

`Renderer` mirrors the viewer's renderer: it owns no scene content, only the effects and
device state. `DrawPart(part, world, instance)` picks the effect per part:

- `part.Skin` is not null → `SkinnedEffect` with the instance's skinning transforms. The world
  matrix stays identity because the bone transforms already include the root motion.
- otherwise → `BasicEffect`, with lighting enabled only when the part has normals. The field uses
  this path.

`CharacterGame.DrawScene` supplies the world matrix for each part and calls the renderer, so all
scene knowledge stays in the sample rather than the renderer.

## Model source

The character model is from [Mixamo](https://www.mixamo.com/).
