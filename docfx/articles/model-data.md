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

## DrModelInstance

`DrModelInstance` gives a model an independent mutable pose while sharing its mesh and buffer data:

```csharp
var instance = new DrModelInstance(model);

public DrModel Model { get; }
public int BoneCount { get; }
public Matrix GetLocalTransform(int boneIndex);
public void SetLocalTransform(int boneIndex, Matrix matrix);
public Matrix[] AbsoluteBoneTransforms { get; }
public BoundingSphere BoundingSphere { get; }
```

The constructor copies the model's current bone transforms as the initial local pose. `GetLocalTransform` and `SetLocalTransform` access the local transform of a bone, relative to its parent. Setting a transform marks absolute transforms, skin transforms and the bounding sphere dirty.

Each derived value recalculates lazily on its next query. Skin-transform and bounding-sphere queries first ensure `AbsoluteBoneTransforms` are current. The bounding sphere transforms each mesh's bounding sphere with its parent bone's absolute transform and merges the spheres. Bone indices refer to positions in `DrModel.Bones`.

`AbsoluteBoneTransforms` returns the instance-owned array. Treat it as read-only.

`DrModelInstance` does not contain animation playback or sampling APIs. Animation code writes local transforms with `SetLocalTransform` and reads absolute transforms or bounds when needed.

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
- `Material` — optional `DrMaterial`
- `Skin` — optional `DrSkin`
- `Tag`

Parts are owned by a mesh and are disposed with it.

## DrMaterial and DrSkin

`DrMaterial` mirrors glTF metallic-roughness material data: base color, metallic/roughness factors, texture references, emissive color, alpha mode and double-sided flag.

`DrSkin` mirrors glTF skinning data: an index, a `DrSkinJoint[]` and an optional skeleton root. Each `DrSkinJoint` pairs a joint bone with its inverse bind matrix. `DrModelInstance.GetSkinTransforms(skin.Index)` produces the current skinning matrices for that instance.

See [Loading glTF](gltf-loading.md) for the mapping details.

## Arrays and ownership

`Bones`, `Meshes`, `Children` and `MeshParts` are arrays. Replacing an array through its property setter updates the related back-references. Mutating an array element directly bypasses that logic, so reassign the array when the relationship changes.

## No game-loop dependency

None of the model types derive from MonoGame's `Game` or from `DrawableGameComponent`. They are plain data classes, so they can be created, tested, serialized and processed without a graphics device.
