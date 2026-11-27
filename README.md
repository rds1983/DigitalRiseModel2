# DigitalRiseModel.MonoGame

Data-only 3D model library for **MonoGame** on **.NET 8**. Provides XNA-shaped `DrModel`, `DrModelBone`, `DrModelMesh`, `DrModelMeshPart`, materials, skins, animation clips and model instances. It does not render; draw calls belong to your renderer.

## Load glTF

```csharp
using AssetManagementBase;
using DigitalRiseModel;

var assets = AssetManager.CreateFileAssetManager("Content");
DrModel model = assets.LoadDrModel(graphicsDevice, "models/robot.glb");
```

## Animate

```csharp
using DigitalRiseModel.Animation;

var instance = new DrModelInstance(model);
var animation = new AnimationController(instance);
animation.Play("Walk");

animation.Update(elapsedTime);
```

## Viewer

```bash
dotnet run --project DigitalRiseModel.Viewer -- "path/to/model.glb"
```

Left-drag orbits, the mouse wheel zooms and Escape exits. Animated models play their first clip automatically.

## Build

```bash
dotnet build DigitalRiseModel.MonoGame.sln
dotnet test DigitalRiseModel.MonoGame.sln
```

## Documentation

- [Introduction](docfx/articles/introduction.md)
- [Loading glTF](docfx/articles/gltf-loading.md)
- [Model data](docfx/articles/model-data.md)
- [Agent instructions](AGENTS.md)

## Metrics

42 C# files, 6,193 lines (4,553 code), 163.5 KB. 19 public types, 126 tests.

## License

MIT. See [LICENSE](LICENSE).
