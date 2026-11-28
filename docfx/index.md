# DigitalRiseModel.MonoGame

A data-only 3D model library for **MonoGame**, targeting **.NET 8** and **C# 12**.

DigitalRiseModel provides `Dr`-prefixed counterparts to the XNA Framework Graphics model classes. It stores bones, meshes, mesh parts, transforms and bounds, but it does not draw anything and does not own effects or rendering state.

## What the library does

- Stores bones and their parent/child hierarchy.
- Stores meshes and their parts.
- Stores vertex and index buffer references and draw ranges.
- Stores glTF-style materials (`DrMaterial`) and skinning data (`DrSkin`).
- Copies local, world-relative and absolute bone transforms.
- Calculates and caches mesh bounding spheres.
- Loads glTF 2.0 scenes, meshes, materials, skins and animations.
- Builds primitive shapes with `MeshPrimitives`.

## What the library does not do

- It does not draw.
- It does not own effects, materials, shaders or render state.
- It does not create a `GraphicsDevice`.
- It does not replace MonoGame's `Model`, `ModelMesh` or `ModelMeshPart`.

## Relationship to XNA and MonoGame

The type names intentionally mirror the XNA model API with a `Dr` prefix:

| XNA | DigitalRiseModel |
| --- | --- |
| `Model` | `DrModel` |
| `ModelBone` | `DrModelBone` |
| `ModelMesh` | `DrModelMesh` |
| `ModelMeshPart` | `DrModelMeshPart` |

MonoGame already provides the XNA types. This library adds a parallel data layer that can evolve independently while still using MonoGame's `Matrix`, `BoundingSphere`, `VertexBuffer` and `IndexBuffer` types.

## Start here

- [Loading glTF](articles/gltf-loading.md) — load a model.
- [Animation](articles/animation.md) — play clips on an instance.
- [Mesh primitives](articles/mesh-primitives.md) — build shapes without an asset.
- [Viewer](articles/viewer.md) — inspect models with `drm-viewer`.
- [API reference](xref:DigitalRiseModel)