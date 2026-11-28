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

## Primitives

```csharp
DrModelMeshPart part = MeshPrimitives.CreateSphere(graphicsDevice, radius: 1f);
root.Mesh = new DrModelMesh(new[] { part });
```

`MeshPrimitives` creates plane, box, sphere, cylinder, cone, capsule and torus parts with write-only buffers and clockwise front faces. Pass `cullClockwise: true` for `RasterizerState.CullClockwise`. The parts carry no material, so assign one in your renderer.

## Viewer

```bash
dotnet run --project DigitalRiseModel.Viewer -- "path/to/model.glb"
```

The built executable is `drm-viewer`. Left-drag orbits, the mouse wheel zooms and Escape exits. Animated models play their first clip automatically. The animation panel selects clips, controls play/pause/stop, seeks time, toggles looping and reverse playback, and adjusts speed. A graphics panel exposes FXAA, bloom and exposure based on the matching command-line arguments.

`Samples/SimpleViewer` is a stock-effect reference viewer built from the same code. The viewer always renders through the custom metallic-roughness shader with a three-point studio light rig and image-based lighting, normal, occlusion and emissive maps, vertex colors, alpha mask/blend and double-sided materials. The scene renders to an HDR target and is resolved with exposure, ACES tone mapping and gamma. `--bloom <intensity>` adds optional HDR bloom; FXAA is enabled by default and `--no-fxaa` disables it. A stats panel shows FPS, frame time, draw calls (including post-processing) and triangles. `--debug-view` and `--exposure` inspect the PBR channels, `F3` cycles debug views, and `--screenshot <file>` captures a frame.

## Shader support

Supported:

- Core metallic-roughness materials with base color, metallic-roughness, normal, occlusion and emissive maps.
- Vertex colors, alpha opaque/mask/blend, double-sided materials and skinning.
- UV0/UV1 selection and texture sampler wrap/filter settings.
- `KHR_materials_emissive_strength`, `KHR_texture_transform` and `KHR_materials_unlit`.
- Three-point studio lighting, procedural cubemap IBL, HDR rendering, ACES tone mapping, bloom and FXAA.

Not implemented:

- `KHR_lights_punctual`, `KHR_materials_clearcoat/sheen/transmission`, `KHR_texture_basisu`, Draco compression and morph targets. Clearcoat/sheen/transmission are deferred to a separate milestone.
- Normal maps require authored `TANGENT` attributes; tangent generation is not implemented.
- Shadows, HDR environment loading and runtime post-processing toggles are not available.
- Transparent parts are sorted per part, and skinned parts are not frustum-culled.

Khronos reference comparisons are qualitative because the viewer uses a procedural studio environment. See [Shader validation](docfx/articles/shader-validation.md) for the asset-by-asset notes.

## Build

```bash
dotnet build DigitalRiseModel.MonoGame.sln
dotnet test DigitalRiseModel.MonoGame.sln
```

## Documentation

- [Introduction](docfx/articles/introduction.md)
- [Loading glTF](docfx/articles/gltf-loading.md)
- [Model data](docfx/articles/model-data.md)
- [Mesh primitives](docfx/articles/mesh-primitives.md)
- [Viewer and shaders](docfx/articles/viewer.md)
- [Shader validation](docfx/articles/shader-validation.md)
- [Agent instructions](AGENTS.md)

## Metrics

| Area | Files | Lines | Code | Size |
| --- | ---: | ---: | ---: | ---: |
| Library | 34 | 5,113 | 3,459 | 139.5 KB |
| Unit tests | 16 | 2,897 | 2,355 | 78.3 KB |
| Viewer | 18 | 2,773 | 2,261 | 75.5 KB |
| Samples | 7 | 853 | 687 | 21.0 KB |
| **Total** | **75** | **11,636** | **8,762** | **314.3 KB** |

22 public types, 198 tests.

## License

MIT. See [LICENSE](LICENSE).
