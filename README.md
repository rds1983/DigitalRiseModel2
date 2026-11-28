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

The built executable is `drm-viewer`. Left-drag orbits, the mouse wheel zooms and Escape exits. `F5` reloads compiled effect binaries after a content rebuild; in a development checkout, `.fx`/`.fxh` edits trigger an automatic rebuild and reload. Animated models play their first clip automatically. The animation panel selects clips, controls play/pause/stop, seeks time, toggles looping and reverse playback, and adjusts speed.

`Samples/SimpleViewer` is a stock-effect reference viewer built from the same code. The viewer always renders through the custom metallic-roughness shader with a three-point studio light rig and image-based lighting, normal, occlusion and emissive maps, vertex colors, alpha mask/blend and double-sided materials. The scene renders to an HDR target and is resolved with exposure, ACES tone mapping and gamma. `--bloom <intensity>` adds optional HDR bloom and `--fxaa` enables FXAA. A stats panel shows FPS, frame time, draw calls and triangles. `--debug-view` and `--exposure` inspect the PBR channels, `F3` cycles debug views, and `--screenshot <file>` captures a frame.

## Shader support

Supported:

- Core metallic-roughness materials with base color, metallic-roughness, normal, occlusion and emissive maps.
- Vertex colors, alpha opaque/mask/blend, double-sided materials and skinning.
- UV0/UV1 selection and texture sampler wrap/filter settings.
- `KHR_materials_emissive_strength`, `KHR_texture_transform` and `KHR_materials_unlit`.
- Three-point studio lighting, procedural cubemap IBL, HDR rendering, ACES tone mapping, bloom and FXAA.

Not implemented:

- `KHR_lights_punctual`, `KHR_materials_clearcoat/sheen/transmission`, `KHR_texture_basisu`, Draco compression and morph targets.
- Normal maps require authored `TANGENT` attributes; tangent generation is not implemented.
- Shadows, HDR environment loading and runtime post-processing toggles are not available.
- Transparent parts are sorted per part, and skinned parts are not frustum-culled.

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

62 C# files, 10,177 lines (7,780 code), 268.5 KB. 21 public types, 148 tests.

## License

MIT. See [LICENSE](LICENSE).
