# Introduction

**DigitalRiseModel.MonoGame** is a data-only 3D model library for MonoGame on .NET 8 and C# 12. It gives you Dr-prefixed model types that follow the shape of the XNA 4.0 model API while keeping rendering entirely in your hands.

## What the library does

- Stores bones and their parent/child hierarchy.
- Stores meshes and their parts.
- Stores vertex and index buffer references and draw ranges.
- Stores glTF-style materials (`DrMaterial`) and skinning data (`DrSkin`).
- Copies local, world-relative and absolute bone transforms.
- Calculates and caches mesh bounding spheres.
- Loads glTF 2.0 scenes, meshes, materials and skins.

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

Materials and skins are represented by `DrMaterial` and `DrSkin`. glTF models are loaded through `AssetManager.LoadDrModel(device, path)`.

MonoGame already provides the XNA types. This library adds a parallel data layer that can evolve independently while still using MonoGame's `Matrix`, `BoundingSphere`, `VertexBuffer` and `IndexBuffer` types.

## Next steps

- [Animate a model](animation.md)
- [Build primitives with MeshPrimitives](mesh-primitives.md)
- [Load a glTF model](gltf-loading.md)
- Browse the API reference.
