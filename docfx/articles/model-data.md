# Model data concepts

## DrModel

`DrModel` is the root data container. Construct it from a non-null root bone:

```csharp
var model = new DrModel(root);
```

The constructor traverses the bone hierarchy and fills:

- `Bones` — every reachable bone, in pre-order.
- `Meshes` — the `DrModelBone.Mesh` of every bone that has one, in the same order.
- `Root` — the supplied root bone.

```csharp
public DrModelBone[] Bones { get; }
public DrModelMesh[] Meshes { get; }
public DrModelBone? Root { get; set; }
public object? Tag { get; set; }
```

Bone indices are assigned sequentially by default. Pass `autoAssignIndices: false` to keep existing indices.

## DrModelBone

```csharp
public DrModelBone(string name, DrModelMesh? mesh = null)
```

A bone stores:

- `Name`
- `Children` — a `DrModelBone[]`
- `Parent` — internal setter, maintained by `Children`
- `Mesh` — optional associated mesh
- `Transform` — local transform relative to the parent
- `Index`
- `Tag`

Assigning `Children` clears the old children's `Parent` and sets this bone as the parent of the new children.

Assigning `Mesh` clears the old mesh's `ParentBone` and sets the new mesh's `ParentBone` to this bone. Moving a mesh between bones also clears the previous bone's `Mesh`.

## DrModelMesh

```csharp
public DrModelMesh(DrModelMeshPart[] meshParts)
```

A mesh stores:

- `MeshParts` — a `DrModelMeshPart[]`
- `BoundingSphere` — calculated on demand and cached
- `ParentBone` — internal setter
- `Name`
- `Tag`

The bounding sphere cache is invalidated when `MeshParts` changes or when a contained part's `BoundingSphere` changes.

## DrModelMeshPart

A mesh part stores:

- `VertexBuffer` and `IndexBuffer` references
- `NumVertices`, `PrimitiveCount`, `StartIndex`, `VertexOffset`
- `BoundingSphere`
- `Mesh` — internal setter, maintained by `DrModelMesh.MeshParts`
- `Tag`

Parts are owned by a mesh and are disposed with it.

## Arrays and ownership

`Bones`, `Meshes`, `Children` and `MeshParts` are arrays. Replacing an array through its property setter updates the related back-references. Mutating an array element directly bypasses that logic, so reassign the array when the relationship changes.

## No game-loop dependency

None of the model types derive from MonoGame's `Game` or from `DrawableGameComponent`. They are plain data classes, so they can be created, tested, serialized and processed without a graphics device.
