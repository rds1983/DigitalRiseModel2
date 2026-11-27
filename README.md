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

The built executable is `drm-viewer`. Left-drag orbits, the mouse wheel zooms and Escape exits. Animated models play their first clip automatically. The animation panel selects clips, controls play/pause/stop, seeks time, toggles looping and reverse playback, and adjusts speed.

`Samples/SimpleViewer` is a stock-effect reference viewer built from the same code. The viewer always renders through the custom metallic-roughness shader, with normal, occlusion and emissive maps, vertex colors, alpha mask/blend and double-sided materials. `--debug-view` and `--exposure` inspect the PBR channels, `F3` cycles debug views, and `--screenshot <file>` captures a frame.

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

56 C# files, 8,287 lines (6,245 code), 217.3 KB. 19 public types, 142 tests.

## License

MIT. See [LICENSE](LICENSE).
