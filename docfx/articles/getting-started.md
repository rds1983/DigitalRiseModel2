# Getting started

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer.
- MonoGame `MonoGame.Framework.DesktopGL` `3.8.6.1` (restored through NuGet).

## Add a reference

Reference the library project from your MonoGame project:

```xml
<ItemGroup>
  <ProjectReference Include="..\DigitalRiseModel\DigitalRiseModel.MonoGame.csproj" />
</ItemGroup>
```

## Create a model

```csharp
using DigitalRiseModel;
using Microsoft.Xna.Framework;

var root = new DrModelBone("Root")
{
    Transform = Matrix.Identity
};

var child = new DrModelBone("Child")
{
    Transform = Matrix.CreateTranslation(0, 1, 0)
};

root.Children = [child];

var model = new DrModel(root);
```

`DrModel` walks the hierarchy from `root`, fills `Bones` and `Meshes`, and assigns sequential bone indices by default:

```csharp
Console.WriteLine(model.Bones.Length); // 2
Console.WriteLine(root.Index);         // 0
Console.WriteLine(child.Index);        // 1
```

Pass `autoAssignIndices: false` when the bones already have indices that must be preserved.

## Attach a mesh

```csharp
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

root.Mesh = mesh;
```

Assigning `root.Mesh` also sets `mesh.ParentBone` to `root`.

## Read bone transforms

```csharp
var absolute = new Matrix[model.Bones.Length];
model.CopyAbsoluteBoneTransformsTo(absolute);

model.CopyBoneTransformsFrom(animatedLocalTransforms);
```

The absolute-transform helper requires each parent bone to appear before its children in `model.Bones`. The default `autoAssignIndices: true` traversal guarantees that ordering.

## Render

The library stops at data. To render:

1. Iterate `model.Bones` and decide where each bone is in the world.
2. Iterate `model.Meshes` and read `mesh.MeshParts`.
3. Bind each part's `VertexBuffer` and `IndexBuffer` with your own `GraphicsDevice`.
4. Apply your own effect and call `DrawIndexedPrimitives`.
5. Use the bone transforms for skinning if the mesh is skinned.

## Dispose model data

`DrModelMesh` and `DrModelMeshPart` implement `IDisposable`. Disposing a mesh disposes its parts and their buffers:

```csharp
using var mesh = new DrModelMesh(parts);
```
