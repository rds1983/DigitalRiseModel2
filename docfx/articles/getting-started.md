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

## Create a primitive instead of building geometry

`MeshPrimitives` creates a ready-to-render `DrModelMeshPart` for common shapes. Each factory uploads write-only vertex and index buffers to the `GraphicsDevice`, fills `NumVertices`, `PrimitiveCount` and `BoundingSphere`, and uses the `VertexPositionNormalTexture` layout:

```csharp
DrModelMeshPart part = MeshPrimitives.CreateSphere(
    graphicsDevice,
    radius: 1f,
    slices: 32,
    stacks: 16);

root.Mesh = new DrModelMesh(new[] { part });
```

Available primitives:

- `CreatePlane(graphicsDevice, width, depth, segmentsX, segmentsZ, cullClockwise)`
- `CreateBox(graphicsDevice, size, cullClockwise)`
- `CreateSphere(graphicsDevice, radius, slices, stacks, cullClockwise)`
- `CreateCylinder(graphicsDevice, radius, height, slices, cullClockwise)`
- `CreateCone(graphicsDevice, radius, height, slices, cullClockwise)`
- `CreateCapsule(graphicsDevice, radius, height, slices, stacks, cullClockwise)`
- `CreateTorus(graphicsDevice, majorRadius, minorRadius, majorSlices, minorSlices, cullClockwise)`

Primitives are centered on the origin and use Y as up. Their front faces are clockwise by default, matching MonoGame's `RasterizerState.CullCounterClockwise`. Pass `cullClockwise: true` to emit counter-clockwise front faces for `RasterizerState.CullClockwise`. Every factory also has a `*Lines` counterpart, such as `CreateBoxLines`, that returns the same shape as a `VertexPosition` wireframe using `PrimitiveType.LineList`. The returned part has no `Material`; assign one before drawing if you need color or textures. See [Mesh primitives](mesh-primitives.md) for winding, tessellation, materials and rendering details.

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
