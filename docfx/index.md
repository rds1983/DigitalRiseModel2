# DigitalRiseModel.MonoGame

A data-only 3D model library for **MonoGame**, targeting **.NET 8** and **C# 12**.

DigitalRiseModel provides `Dr`-prefixed counterparts to the XNA Framework Graphics model classes. It stores bones, meshes, mesh parts, transforms and bounds, but it does not draw anything and does not own effects or rendering state.

## Start here

- [Introduction](articles/introduction.md)
- [Getting started](articles/getting-started.md)
- [Model data concepts](articles/model-data.md)
- [API reference](xref:DigitalRiseModel)

## At a glance

```csharp
using DigitalRiseModel;
using Microsoft.Xna.Framework;

var root = new DrModelBone("Root");
var child = new DrModelBone("Child");
root.Children = [child];

var model = new DrModel(root);

Matrix[] transforms = new Matrix[model.Bones.Length];
model.CopyAbsoluteBoneTransformsTo(transforms);
```

The model arrays are plain data. Use your own MonoGame renderer to bind vertex/index buffers, apply effects and issue draw calls.
