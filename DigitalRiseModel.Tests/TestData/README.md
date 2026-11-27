# glTF test assets

Small, generated glTF 2.0 files used by `GltfLoaderTests`. They are checked in so the tests are offline and deterministic.

| File | Contents |
| --- | --- |
| `triangle.gltf` | One triangle, positions and 16-bit indices, embedded buffer |
| `triangle.glb` | Binary GLB version of `triangle.gltf` |
| `two-uvs.gltf` | Positions, normals and two texture coordinate sets |
| `skinned.gltf` | Two-bone hierarchy, `JOINTS_0`, `WEIGHTS_0`, an identity inverse bind matrix |
| `materials.gltf` | Two primitives, two materials, texture references, alpha mask |
| `triangle-strip.gltf` | Four-vertex triangle strip that must convert to two triangles |

All files are generated, not copied from a third party. They are intentionally tiny and contain no external binary dependencies.
