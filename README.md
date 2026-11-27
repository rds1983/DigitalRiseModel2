# DigitalRiseModel.MonoGame

A data-only 3D model library for **MonoGame**, targeting **.NET 8** and **C# 12**. It provides Dr-prefixed counterparts to the XNA Framework Graphics model classes, so model data can be built and inspected with the API shape you already know.

The library does not draw anything and does not own effects or rendering state. You consume the model data and issue draw calls from your own renderer.

The current foundation contains:

- `DrModel`
- `DrModelBone`
- `DrModelMesh`
- `DrModelMeshPart`

The `Dr` prefix keeps these types distinct from MonoGame's own `Model`, `ModelMesh` and `ModelMeshPart` classes while preserving familiar XNA member names and behavior.

> **Status:** foundational data classes implemented and covered by unit tests. Mesh processing, content import/export and animation are planned.

## Why Dr-prefixed model classes?

MonoGame already ships the XNA `Model` API. This library adds a parallel, data-focused model layer that can evolve without changing MonoGame itself:

- **Familiar API.** Properties follow the XNA Game Studio 4.0 model classes.
- **MonoGame-native data types.** Uses `VertexBuffer`, `IndexBuffer`, `Matrix` and `BoundingSphere` directly.
- **No rendering API.** There is no `Draw`, `Effect`, `GraphicsDevice`, shader or render-state surface in the model classes.
- **Bone hierarchy.** Absolute and relative bone transform copying matches XNA semantics.
- **Simple part storage.** `DrModelMesh.MeshParts` is a `DrModelMeshPart[]` property; its setter clears the old parts' `Mesh` references and sets the new ones.
- **Testable.** All behavior can be tested without a graphics device or window.

## Current API

All model types live in the root namespace `DigitalRiseModel`.

| Type | Purpose |
| --- | --- |
| `DrModel` | A model composed of bone and mesh arrays; bone transform helpers |
| `DrModelBone` | A bone with a name, optional mesh, local transform, parent, children array and tag |
| `DrModelMesh` | A mesh with a cached, on-demand bounding sphere, parent bone and parts |
| `DrModelMeshPart` | A draw range with vertex/index buffers, counts, a bounding sphere and an owning-mesh back-reference |

`DrModelMeshPart` stores the MonoGame `VertexBuffer` and `IndexBuffer` references plus the range to draw. Effects, materials and draw calls belong to your renderer.

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

## Repository layout

```text
DigitalRiseModel.MonoGame.sln
README.md
AGENTS.md
LICENSE
DigitalRiseModel/
  DigitalRiseModel.MonoGame.csproj
  *.cs                               # library sources (DrModel, DrModelMesh, ...)
DigitalRiseModel.Tests/              # xUnit tests
docfx/                               # DocFX configuration and articles (planned)
Samples/                             # one folder per sample (planned)
  <SampleName>/
```

## Design principles

1. **Data only.** No `Draw`, `Effect`, `GraphicsDevice`, render-state or shader APIs in the model classes.
2. **MonoGame-native data.** Use MonoGame's `VertexBuffer`, `IndexBuffer`, `Matrix` and `BoundingSphere` types. Do not introduce a parallel math or graphics API.
3. **XNA-shaped, independently implemented.** The public shape follows the XNA 4.0 model classes, but the implementation is written for this project and does not copy upstream source.
4. **Render-ready, not rendering.** The library produces model data and bone transforms; the developer decides how and when to render.
5. **Fail loudly.** Invalid bone ordering and other setup errors throw descriptive exceptions.
6. **Simple arrays where order and ownership are explicit.** `DrModel.Bones`, `DrModel.Meshes`, `DrModelBone.Children` and `DrModelMesh.MeshParts` are arrays.
7. **Stay deterministic.** Bone transform ordering and name lookup have stable, documented behavior.
8. **Test the data side.** Unit tests must not require a window or GPU device.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MonoGame `MonoGame.Framework.DesktopGL` 3.8.6.1 (managed through NuGet)
- An IDE or CLI that can build a `.sln`

## Build and test

```bash
git clone <repository-url>
cd DigitalRiseModel2

dotnet restore DigitalRiseModel.MonoGame.sln
dotnet build DigitalRiseModel.MonoGame.sln
dotnet test DigitalRiseModel.MonoGame.sln
```

The current test suite covers bone hierarchy rules, name-based lookups, model bone transform copies and mesh part storage.

## Roadmap

- [x] Solution, library project and xUnit test project.
- [x] XNA-shaped `DrModel` data object graph and collections.
- [x] Bone transform copy helpers.
- [ ] Mesh processing: normals, tangents, vertex welding, optimization, simplification.
- [ ] Import/export: glTF 2.0, OBJ and a versioned native format.
- [ ] Animation: skeletons, clips, sampling and blending.
- [ ] DocFX documentation site and runnable samples.

## Contributing

Read [AGENTS.md](AGENTS.md) before changing code. In short: keep the library data-only and MonoGame-native, mirror XNA behavior where documented, add tests for behavior changes, and document every public API member.

This is an independent implementation. Do not copy or transliterate source code from MonoGame, XNA or the DigitalRiseModel/DigitalRune projects. Use their documented public APIs as the compatibility target, then write the implementation here.

## License

MIT. See [LICENSE](LICENSE).
