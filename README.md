# DigitalRiseModel.MonoGame

A data-only 3D model library for **MonoGame**, targeting **.NET 8** and **C# 12**. It provides Dr-prefixed counterparts to the XNA Framework Graphics model classes, so model data can be built and inspected with the API shape you already know.

The library does not draw anything and does not own effects or rendering state. You consume the model data and issue draw calls from your own renderer.

The current foundation contains:

- `DrModel`
- `DrModelBone`
- `DrModelMesh`
- `DrModelMeshPart`
- `DrMaterial`
- `DrSkin`
- `DrTextureInfo`

glTF 2.0 loading is available through the recursive `AssetManager.LoadDrModel(device, path)` extension. The `Dr` prefix keeps these types distinct from MonoGame's own `Model`, `ModelMesh` and `ModelMeshPart` classes while preserving familiar XNA member names and behavior.

> **Status:** foundational data classes and glTF 2.0 loading are implemented and covered by unit tests. Animations, OBJ import, mesh processing and serialization are planned.

## Why Dr-prefixed model classes?

MonoGame already ships the XNA `Model` API. This library adds a parallel, data-focused model layer that can evolve without changing MonoGame itself:

- **Familiar API.** Properties follow the XNA Game Studio 4.0 model classes.
- **MonoGame-native data types.** Uses `VertexBuffer`, `IndexBuffer`, `Matrix` and `BoundingSphere` directly.
- **No rendering API.** There is no `Draw`, `Effect`, `GraphicsDevice`, shader or render-state surface in the model classes.
- **Bone hierarchy.** Absolute and relative bone transform copying matches XNA semantics.
- **Simple part storage.** `DrModelMesh.MeshParts` is a `DrModelMeshPart[]` property; its setter clears the old parts' `Mesh` references and sets the new ones.
- **glTF 2.0 import.** Khronos `glTF2Loader` reads `.gltf` and `.glb`; meshes, materials, scenes, bones and skins are converted to Dr types.
- **Testable.** All data-side behavior can be tested without a window.

## Current API

Model types live in the root namespace `DigitalRiseModel`. glTF loading is exposed through the `AssetManager` extension in the same namespace; the loader itself is internal.

| Type | Purpose |
| --- | --- |
| `DrModel` | A model composed of bone and mesh arrays; bone transform helpers |
| `DrModelBone` | A bone with a name, optional mesh, local transform, parent, children array and tag |
| `DrModelMesh` | A mesh with a cached, on-demand bounding sphere, parent bone and parts |
| `DrModelMeshPart` | A draw range with vertex/index buffers, vertex declaration, counts, material, skin, bounding sphere, owning-mesh back-reference and optional `VertexSkinned` source geometry |
| `DrMaterial` | Metallic-roughness material data, including texture references and alpha mode |
| `DrTextureInfo` | A texture index, UV set, normal scale, occlusion strength and loaded `Texture2D` |
| `DrSkin` | Joints, skeleton root and inverse bind matrices |
| `ModelLoadFlags` | Loading options; readable buffers and source geometry for debug/testing |
| `DrModelAssetManagerExtensions` | `AssetManager.LoadDrModel(device, path)` glTF loading |

`DrModelMeshPart` stores the MonoGame `VertexBuffer` and `IndexBuffer` references plus the range to draw. Effects and draw calls belong to your renderer; `DrMaterial` and `DrSkin` describe the data to feed it.

`DrModelMesh` and `DrModelMeshPart` implement `IDisposable`. Disposing a mesh disposes its parts and their buffers; `Dispose()` is idempotent.

## Quick start

```csharp
using DigitalRiseModel;
using Microsoft.Xna.Framework;

var rootBone = new DrModelBone("Root")
{
    Index = 0,
    Transform = Matrix.Identity
};

var part = new DrModelMeshPart
{
    VertexBuffer = vertexBuffer,
    IndexBuffer = indexBuffer,
    NumVertices = vertexCount,
    PrimitiveCount = triangleCount,
    StartIndex = 0,
    VertexOffset = 0
};

var mesh = new DrModelMesh(new[] { part })
{
    Name = "Body"
};

rootBone.Mesh = mesh;

var model = new DrModel(rootBone);
```

Bone transforms can be read and written in bulk, just like XNA:

```csharp
var absoluteTransforms = new Matrix[model.Bones.Length];
model.CopyAbsoluteBoneTransformsTo(absoluteTransforms);

model.CopyBoneTransformsFrom(animatedLocalTransforms);
```

Rendering is then up to you: iterate `model.Meshes`, read each part's buffers and draw range, apply your own effect, and use the bone transforms for skinning.

## Loading glTF

glTF files are loaded through the recursive `AssetManager` loader:

```csharp
using AssetManagementBase;
using DigitalRiseModel;

var assets = AssetManager.CreateFileAssetManager("Content");
DrModel model = assets.LoadDrModel(graphicsDevice, "models/robot.glb");
```

For debugging and tests, `ModelLoadFlags.ReadableBuffers` creates buffers with `BufferUsage.None` so their contents can be retrieved with `GetData`. `ModelLoadFlags.KeepSourceGeometry` also keeps CPU-side source arrays in `DrModelMeshPart.SourceIndices` and `SourceVertices`:

```csharp
DrModel readable = assets.LoadDrModel(
    graphicsDevice,
    "models/robot.glb",
    ModelLoadFlags.ReadableBuffers | ModelLoadFlags.KeepSourceGeometry);
```

`SourceVertices` uses `DigitalRiseModel.Vertices.VertexSkinned`:

```csharp
public Vector3 Position;
public Vector3 Normal;
public Vector2 TextureCoordinate;
public Byte4 BlendIndices;
public Vector4 BlendWeights;
```

`ModelLoadFlags.EnsureUVs` adds a zero-valued `TEXCOORD_0` channel to parts that have no `TEXCOORD_0` attribute, so shaders and tools can rely on a UV channel always being present.

The loader:

- Reads `.gltf` and `.glb` files through the Khronos `glTF2Loader` package.
- Resolves buffers and textures relative to the glTF asset through `AssetManager`.
- Builds a synthetic root bone, then converts scene nodes to `DrModelBone` instances.
- Converts each glTF mesh primitive to a `DrModelMeshPart` with a vertex declaration generated from that primitive's attributes.
- Creates MonoGame `VertexBuffer` and `IndexBuffer` objects on the supplied `GraphicsDevice`.
- Converts metallic-roughness materials to `DrMaterial` and texture references to `DrTextureInfo`.
- Loads referenced textures through XNAssets and assigns them to `DrTextureInfo.Texture`.
- Converts node skins to `DrSkin` and assigns them to the node mesh's parts.
- Loads triangle lists, triangle strips and triangle fans.

Animations are not loaded yet. Sparse accessors and non-triangle primitive modes throw `NotSupportedException`.

## Viewer

`DigitalRiseModel.Viewer` is a small MonoGame window viewer that loads a glTF model through `AssetManager` and renders it with stock effects. It is also packaged as the `drm-viewer` .NET tool.

```bash
dotnet run --project DigitalRiseModel.Viewer -- "path/to/model.glb"
```

Install and run the tool locally:

```bash
dotnet pack DigitalRiseModel.Viewer -c Release -o artifacts
dotnet tool install --global DigitalRiseModel.Viewer --add-source artifacts
drm-viewer "path/to/model.glb"
```

Controls:

- Left-drag: orbit the camera.
- Mouse wheel: zoom.
- Escape: exit.

The controls block is a Myra `VerticalStackPanel` authored in `DigitalRiseModel.Viewer/UI/ControlsPanel.xmmp` and exported to C# with `myra-export`:

```bash
myra-export DigitalRiseModel.Viewer/UI/ControlsPanel.xmmp \
  --namespace DigitalRiseModel.Viewer.UI \
  --class ControlsPanel \
  --output DigitalRiseModel.Viewer/UI
```

Low-level 3D drawing lives in `DigitalRiseModel.Viewer/Graphics/Renderer.cs`. The renderer owns the stock effects, device state, lighting, mesh traversal and draw calls; `ViewerGame` handles loading, camera input, bounds and the Myra UI.

glTF triangle winding depends on the sign of the node's global transform determinant. The loader converts it to MonoGame's clockwise front-face convention, so the viewer renders with `CullCounterClockwise`; double-sided materials use `CullNone`.

## Repository layout

```text
DigitalRiseModel.MonoGame.sln
README.md
AGENTS.md
LICENSE
DigitalRiseModel/
  DigitalRiseModel.MonoGame.csproj
  *.cs                               # root model types (DrModel, DrModelMesh, DrMaterial, ...)
  Gltf/                              # DigitalRiseModel.Gltf (loader)
DigitalRiseModel.Tests/              # xUnit tests
DigitalRiseModel.Viewer/             # MonoGame glTF viewer and drm-viewer tool
docfx/                               # DocFX configuration, articles and API output
.config/dotnet-tools.json            # local DocFX tool manifest
```

## Design principles

1. **Data only.** No `Draw`, `Effect`, shader or render-state APIs in the model classes. Only the glTF loader creates buffers and textures from a caller-supplied `GraphicsDevice`.
2. **MonoGame-native data.** Use MonoGame's `VertexBuffer`, `IndexBuffer`, `Matrix` and `BoundingSphere` types. Do not introduce a parallel math or graphics API.
3. **XNA-shaped, independently implemented.** The public shape follows the XNA 4.0 model classes, but the implementation is written for this project and does not copy upstream source.
4. **Render-ready, not rendering.** The library produces model data and bone transforms; the developer decides how and when to render.
5. **Fail loudly.** Invalid bone ordering and other setup errors throw descriptive exceptions.
6. **Simple arrays where order and ownership are explicit.** `DrModel.Bones`, `DrModel.Meshes`, `DrModelBone.Children` and `DrModelMesh.MeshParts` are arrays.
7. **Stay deterministic.** Bone traversal, array ordering and transform calculations have stable, documented behavior.
8. **Test the data side.** Unit tests must not require a window or GPU device.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MonoGame `MonoGame.Framework.DesktopGL` 3.8.6.1 (managed through NuGet)
- Khronos `glTF2Loader` 2.0.0 (managed through NuGet)
- `XNAssets.MonoGame` 0.8.9 (managed through NuGet)
- `Myra` 1.6.8 for the viewer UI
- An IDE or CLI that can build a `.sln`

## Build and test

```bash
git clone <repository-url>
cd DigitalRiseModel2

dotnet restore DigitalRiseModel.MonoGame.sln
dotnet build DigitalRiseModel.MonoGame.sln
dotnet test DigitalRiseModel.MonoGame.sln
```

The current test suite covers bone hierarchy rules, array back-references, model bone transform copies, bounding-sphere caching, disposal, material/skin/texture defaults, glTF scene/transform conversion, glTF assets with dynamic vertex layouts, multiple UV sets, skinning, materials, triangle strips and GLB files, plus `AssetManager` loading and caching.

## Documentation

The documentation site is built with [DocFX](https://dotnet.github.io/docfx/) from `docfx/`:

```bash
dotnet tool restore
dotnet docfx metadata docfx/docfx.json
dotnet docfx build docfx/docfx.json
```

Delete `docfx/api` before regenerating metadata when public types have been removed, so stale API pages do not survive.

The generated site is written to `docfx/_site`. To preview it locally:

```bash
dotnet docfx serve docfx/_site
```

The site includes a getting-started guide, model-data concepts and the generated API reference.

## Roadmap

- [x] Solution, library project and xUnit test project.
- [x] XNA-shaped `DrModel` data object graph and collections.
- [x] Bone transform copy helpers.
- [x] glTF 2.0 import: scenes, meshes, materials and skins (no animations).
- [ ] Mesh processing: normals, tangents, vertex welding, optimization, simplification.
- [ ] Import/export: OBJ and a versioned native format.
- [ ] Animation: clips, sampling and blending.
- [x] DocFX documentation site with guides and API reference.
- [x] MonoGame viewer packaged as the `drm-viewer` .NET tool.
- [ ] Additional samples and tooling.

## Contributing

Read [AGENTS.md](AGENTS.md) before changing code. In short: keep the library data-only and MonoGame-native, mirror XNA behavior where documented, add tests for behavior changes, and document every public API member.

This is an independent implementation. Do not copy or transliterate source code from MonoGame, XNA or the DigitalRiseModel/DigitalRune projects. Use their documented public APIs as the compatibility target, then write the implementation here.

## License

MIT. See [LICENSE](LICENSE).
