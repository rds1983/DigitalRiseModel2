# DigitalRiseModel.MonoGame

Data-only 3D model library for **MonoGame** on **.NET 8**. Provides XNA-shaped `DrModel`, `DrModelBone`, `DrModelMesh`, `DrModelMeshPart`, materials, skins, animation clips and model instances. It does not render; draw calls belong to your renderer.

## Overview

Features:

- **XNA-shaped model types** — `DrModel`, `DrModelBone`, `DrModelMesh`, `DrModelMeshPart` backed by MonoGame's own math and buffer types.
- **glTF 2.0 loading** — scenes, meshes, materials, skins and animations through `AssetManager.LoadDrModel`.
- **Metallic-roughness materials** — base color, metallic-roughness, normal, occlusion and emissive maps, plus `KHR_materials_emissive_strength`, `KHR_texture_transform` and `KHR_materials_unlit`.
- **Skinning** — joints, inverse bind matrices and per-instance skinning transforms.
- **Animation** — clips, channels and keyframes with step, linear and cubic-spline interpolation.
- **Instancing** — independent poses per `DrModelInstance` over shared model data.
- **Mesh primitives** — plane, box, sphere, cylinder, cone, capsule and torus, with solid and wireframe variants.
- **Cached bounds** — per-part, per-mesh and per-instance bounding spheres.
- **Viewer** — a PBR glTF viewer with IBL, HDR, ACES tone mapping, bloom and FXAA.

## Documentation

Full documentation, including API reference, is published at **https://rds1983.github.io/DigitalRiseModel2/**.

## Code Metrics

| Area | Files | Lines | Code | Size |
| --- | ---: | ---: | ---: | ---: |
| Library | 35 | 5,531 | 3,672 | 155.3 KB |
| Unit tests | 17 | 3,245 | 2,640 | 87.6 KB |
| Viewer | 18 | 2,774 | 2,262 | 75.5 KB |
| Samples | 11 | 1,421 | 1,124 | 35.0 KB |
| **Total** | **81** | **12,971** | **9,698** | **353.5 KB** |

22 public types, 222 tests.

## License

MIT. See [LICENSE](LICENSE).
