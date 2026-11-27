# Loading glTF

glTF 2.0 files are loaded through `DigitalRiseModel.Gltf.GltfLoader`, which uses the Khronos **glTF2Loader** package for parsing.

```csharp
using DigitalRiseModel.Gltf;

DrModel model = GltfLoader.Load(graphicsDevice, "models/robot.glb");
```

The graphics device is required only when the loader should create MonoGame `VertexBuffer` and `IndexBuffer` objects.

## Data-only loading

Omit the graphics device to load the model data without creating GPU resources:

```csharp
DrModel model = GltfLoader.Load("models/robot.glb");
```

`VertexBuffer` and `IndexBuffer` are `null` in this mode, but `VertexDeclaration`, counts, bounds, materials and skins are still populated. The unit tests use this path to load the bundled glTF fixtures without a graphics device.

## What is loaded

- `.gltf` and `.glb` files.
- The selected scene, or the default scene.
- Scene nodes converted to `DrModelBone` instances under a synthetic root bone.
- Each glTF mesh primitive converted to a `DrModelMeshPart`.
- Triangle lists, triangle strips and triangle fans, converted to triangle lists.
- POSITION, NORMAL, TANGENT, TEXCOORD_0, COLOR_0, JOINTS_0 and WEIGHTS_0 vertex attributes.
- Metallic-roughness materials as `DrMaterial` with `DrTextureInfo` references.
- Node skins as `DrSkin`, assigned to the parts of that node's mesh.

## Vertex layout

Vertex buffers reflect the primitive's actual glTF attributes. The loader creates a `VertexDeclaration` and writes a matching interleaved buffer. Read the layout from `DrModelMeshPart.VertexBuffer.VertexDeclaration`.

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

## Materials

`DrModelMeshPart.Material` contains the glTF material data:

- base color factor and texture
- metallic and roughness factors and texture
- normal, occlusion and emissive textures
- emissive factor
- alpha mode and cutoff
- double-sided flag

`DrTextureInfo.TextureIndex` refers to the glTF texture index. The loader does not decode image files; texture loading belongs to the renderer.

## Skins

`DrModelMeshPart.Skin` contains:

- `Joints` — the joint bones
- `Skeleton` — the optional skeleton root
- `InverseBindMatrices` — one matrix per joint

A glTF skin belongs to a node, so it is assigned to every part of that node's mesh.

## Limitations

- Animations are not loaded.
- Sparse accessors are not supported.
- Non-triangle primitive modes are not supported.
- Image files are not decoded.
- GPU buffer creation requires a `GraphicsDevice`. Use the data-only overload when only the model data is needed.
