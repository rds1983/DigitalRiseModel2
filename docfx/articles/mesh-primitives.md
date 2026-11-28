# Mesh primitives

## How do I create a shape without a model file?

Call one of the `MeshPrimitives.Create*` methods with a graphics device. Each returns a `DrModelMeshPart` you can attach to a bone:

```csharp
using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

DrModelMeshPart part = MeshPrimitives.CreateSphere(graphicsDevice, radius: 1f);
rootBone.Mesh = new DrModelMesh(new[] { part });
```

The part is ready to draw: buffers are uploaded, counts and bounds are filled. Assign a `DrMaterial` if you want it shaded, then draw it like any other mesh part.

Use primitives for prototypes, gizmos, collision debug shapes and test scenes when you do not want to author or load a model.

### Solid or wireframe?

Every shape has a `*Lines` twin that returns the same geometry as a wireframe:

```csharp
DrModelMeshPart wireframe = MeshPrimitives.CreateBoxLines(graphicsDevice, Vector3.One);
```

### Which shape do I need?

| Factory | Shape |
| --- | --- |
| `CreatePlane(device, width, depth, segmentsX = 1, segmentsZ = 1)` | Rectangle in the XZ plane facing +Y |
| `CreateBox(device, size)` | Axis-aligned box with per-face normals and UVs |
| `CreateSphere(device, radius, slices = 32, stacks = 16)` | UV sphere |
| `CreateCylinder(device, radius, height, slices = 32)` | Y-aligned cylinder with flat caps |
| `CreateCone(device, radius, height, slices = 32)` | Y-aligned cone, apex up, flat base |
| `CreateCapsule(device, radius, height, slices = 32, stacks = 16)` | Cylinder plus two hemispherical caps |
| `CreateTorus(device, majorRadius, minorRadius, majorSlices = 32, minorSlices = 16)` | Torus in the XZ plane |

All primitives are centered on the origin and use Y as up.

## Requirements

Each factory needs a `GraphicsDevice` because it creates the vertex and index buffers up front:

- Vertex buffer: `BufferUsage.WriteOnly`
- Index buffer: `BufferUsage.WriteOnly`, 16-bit when the part has 65,536 vertices or fewer, otherwise 32-bit
- Vertex layout: `VertexPositionNormalTexture` (position, unit normal, texture coordinate)

The library never draws. It fills the part and returns it; your renderer issues the draw call.

## The returned part

| Member | Value |
| --- | --- |
| `VertexBuffer` | New buffer containing the geometry |
| `IndexBuffer` | New triangle-list index buffer |
| `VertexDeclaration` | `VertexPositionNormalTexture.VertexDeclaration` |
| `NumVertices` | Number of vertices |
| `PrimitiveCount` | Number of triangles |
| `StartIndex`, `VertexOffset` | `0` |
| `BoundingSphere` | Fits every vertex, centered on the AABB center |
| `Material`, `Skin` | `null` — assign your own material |

Because a part owns its buffers, dispose it (or its owning `DrModelMesh`) when you are done.

`CreateCapsule` treats `height` as the total height including both caps, so it must be at least `2 * radius`. `CreateTorus` measures `majorRadius` to the center of the tube and `minorRadius` as the tube radius.

Every factory validates its arguments: non-positive sizes and slice counts below the documented minimums throw `ArgumentOutOfRangeException`.

## Line primitives

Every factory has a `*Lines` counterpart:

| Lines factory | Solid factory |
| --- | --- |
| `CreatePlaneLines(device, width, depth, segmentsX = 1, segmentsZ = 1)` | `CreatePlane` |
| `CreateBoxLines(device, size)` | `CreateBox` |
| `CreateSphereLines(device, radius, slices = 32, stacks = 16)` | `CreateSphere` |
| `CreateCylinderLines(device, radius, height, slices = 32)` | `CreateCylinder` |
| `CreateConeLines(device, radius, height, slices = 32)` | `CreateCone` |
| `CreateCapsuleLines(device, radius, height, slices = 32, stacks = 16)` | `CreateCapsule` |
| `CreateTorusLines(device, majorRadius, minorRadius, majorSlices = 32, minorSlices = 16)` | `CreateTorus` |

Line parts differ from solid parts in three ways:

- Vertex layout is `VertexPosition` only, because lines have no surface to light or texture.
- `PrimitiveType` is `PrimitiveType.LineList`, so the part must be drawn with that topology.
- `PrimitiveCount` is the number of line segments, not triangles.

The wireframe is derived from the solid geometry, so it matches the same shape and tessellation parameters. Edges shared by adjacent triangles are emitted once, and internal quad diagonals are skipped, so `CreateBoxLines` produces exactly 12 edges. Vertices are welded by position, so the 24 box vertices collapse to 8 corner vertices.

```csharp
DrModelMeshPart wireframe = MeshPrimitives.CreateBoxLines(
    graphicsDevice,
    new Vector3(1f, 1f, 1f));

// wireframe.PrimitiveCount == 12
```

`*Lines` factories have no `cullClockwise` parameter: culling does not apply to lines.

## Winding and culling

Primitives emit clockwise front faces by default. This matches MonoGame's default `RasterizerState.CullCounterClockwise` and the winding that `AssetManager.LoadDrModel` produces for glTF models, so the same rasterizer state works for both.

If your renderer uses `RasterizerState.CullClockwise`, pass `cullClockwise: true`:

```csharp
DrModelMeshPart part = MeshPrimitives.CreateBox(
    graphicsDevice,
    new Vector3(1f, 2f, 1f),
    cullClockwise: true);
```

The flag reverses the triangle winding. It does not touch positions, normals or UVs, so lighting and textures are unaffected.

| Rasterizer state | `cullClockwise` |
| --- | --- |
| `RasterizerState.CullCounterClockwise` (default) | `false` |
| `RasterizerState.CullClockwise` | `true` |
| `RasterizerState.CullNone` | either |

For double-sided materials, either value works because nothing is culled.

## Tessellation and detail

Slices and stacks control triangle density:

```csharp
// Coarse sphere for a debug gizmo.
var coarse = MeshPrimitives.CreateSphere(graphicsDevice, 1f, slices: 8, stacks: 4);

// Smooth sphere for a hero asset.
var smooth = MeshPrimitives.CreateSphere(graphicsDevice, 1f, slices: 64, stacks: 32);
```

Two details are worth knowing:

- Pole rows produce single triangles rather than degenerate quads, so sphere and capsule poles have no zero-area triangles.
- `CreateCapsule` with `height == 2 * radius` skips the duplicated equator row and produces the same geometry as the equivalent sphere.

## Combine primitives into a model

Each call returns one part. A `DrModelMesh` can hold several parts, which is useful for building compound shapes:

```csharp
var body = MeshPrimitives.CreateBox(graphicsDevice, new Vector3(1f, 0.5f, 1f));
var head = MeshPrimitives.CreateSphere(graphicsDevice, 0.3f);

var mesh = new DrModelMesh(new[] { body, head });
rootBone.Mesh = mesh;
```

If you want the parts to move independently, put them on separate bones instead:

```csharp
var root = new DrModelBone("Root");
var headBone = new DrModelBone("Head")
{
    Transform = Matrix.CreateTranslation(0f, 0.75f, 0f)
};
headBone.Mesh = new DrModelMesh(new[] { head });
root.Children = [headBone];

var model = new DrModel(root);
```

## Materials

Primitives return plain geometry with no material. To get a shaded result, assign a `DrMaterial` before drawing:

```csharp
part.Material = new DrMaterial
{
    BaseColorFactor = new Vector4(0.8f, 0.2f, 0.2f, 1f),
    MetallicFactor = 0f,
    RoughnessFactor = 0.6f
};
```

The viewer and samples read `DrMaterial` when binding their shader. See <xref:DigitalRiseModel.DrMaterial> for the material fields.

## Render a primitive

Rendering is the same as for any other mesh part. Use `part.PrimitiveType` so solid and line parts both draw correctly:

```csharp
graphicsDevice.SetVertexBuffer(part.VertexBuffer);
graphicsDevice.Indices = part.IndexBuffer;

foreach (EffectPass pass in effect.CurrentTechnique.Passes)
{
    pass.Apply();
    graphicsDevice.DrawIndexedPrimitives(
        part.PrimitiveType,
        part.VertexOffset,
        part.StartIndex,
        part.PrimitiveCount);
}
```

Bind your effect, set world/view/projection, and choose a rasterizer state that matches the part's winding. `part.BoundingSphere` is available for culling.

## Limitations

- Solid parts use only the `VertexPositionNormalTexture` layout: no tangents, vertex colors, second UV set or skinning weights. Line parts use `VertexPosition`.
- Normal maps that require tangents are not supported unless your renderer derives tangents.
- The parts are not skinned. Assigning `part.Skin` without matching blend attributes will not animate them.

If you need a different vertex layout or skinned geometry, build the buffers yourself and fill a `DrModelMeshPart` directly.

## Runnable sample

`Samples/PrimitivesDemo` builds a scene from every primitive, solid and wireframe, with no asset files:

```bash
dotnet run --project Samples/PrimitivesDemo
```

It demonstrates materials, `cullClockwise`, drawing by `part.PrimitiveType`, lighting solid vs. line parts and spinning shapes through `DrModelInstance.SetLocalTransform`.
