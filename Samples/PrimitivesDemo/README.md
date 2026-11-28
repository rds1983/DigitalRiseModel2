# PrimitivesDemo

A runnable MonoGame sample that builds a scene entirely from `MeshPrimitives` - no glTF asset required.

```bash
dotnet run --project Samples/PrimitivesDemo
```

Drag to orbit, use the mouse wheel to zoom, press Space to pause the spin and Escape to exit.

## What it shows

The scene contains every primitive, each drawn twice:

- A **solid** part built with `CreatePlane`, `CreateBox`, `CreateSphere`, `CreateCylinder`, `CreateCone`, `CreateCapsule` and `CreateTorus`, shaded with `BasicEffect` per-pixel lighting.
- A **line** part built with the matching `*Lines` factory, drawn with `PrimitiveType.LineList`.

The console prints each part's topology, vertex count, primitive count and bounding radius on startup.

## Code map

| File | Purpose |
| --- | --- |
| `PrimitiveScene.cs` | Builds the `DrModel`: one bone per shape, materials, and the `cullClockwise` example |
| `Graphics/Renderer.cs` | Draws parts with `BasicEffect`, selecting lit vs. unlit and the rasterizer state |
| `PrimitivesGame.cs` | Camera, per-bone spin and frame loop |

## Details worth copying

- **Winding.** Parts are clockwise by default, which matches `RasterizerState.CullCounterClockwise`. The sphere is built with `cullClockwise: true` and tagged with `RasterizerState.CullClockwise` in `Tag`, and the renderer reads `Tag` to pick the state.
- **Topology.** The renderer draws with `part.PrimitiveType`, so solid (`TriangleList`) and line (`LineList`) parts use one code path.
- **Lighting.** Solid parts have normals and light normally. Line parts use the `VertexPosition` layout, so the renderer disables lighting when no normal element is present.
- **Animation.** The spin is applied through `DrModelInstance.SetLocalTransform`, so the shared `DrModel` data is never mutated.
