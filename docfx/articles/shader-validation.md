# Shader validation

This page records qualitative comparisons against the Khronos glTF Sample Assets
for the viewer's PBR path. The viewer uses a procedural studio environment and a
fixed camera framing, so comparisons are channel-by-channel rather than
pixel-exact.

## Reproducing a render

```bash
dotnet run --project DigitalRiseModel.Viewer -- \
    "path/to/model.gltf" --screenshot render.png
```

Useful options:

- `--debug-view basecolor|metallic|roughness|normal|occlusion|emissive`
- `--exposure <value>`
- `--bloom <intensity>`
- `--fxaa`

## Core PBR assets

| Asset | Khronos reference | Channels checked | Result | Notes |
| --- | --- | --- | --- | --- |
| `BoomBox` | [screenshot_large.jpg](https://github.com/KhronosGroup/glTF-Sample-Assets/blob/main/Models/BoomBox/screenshot/screenshot_large.jpg) | Base color, metallic-roughness, normal, occlusion, emissive | Pass | Material channels match. Reflections differ because the viewer uses a procedural environment. A close-up reflection seam was fixed by switching IBL to cubemaps. |
| `DamagedHelmet` | [screenshot.png](https://github.com/KhronosGroup/glTF-Sample-Assets/blob/main/Models/DamagedHelmet/screenshot/screenshot.png) | Base color, metallic-roughness, occlusion, emissive | Pass with limitation | Material channels match. The asset has no `TANGENT` attributes, so the tangent-space normal map is not applied. |
| `MetalRoughSpheres` | [screenshot.png](https://github.com/KhronosGroup/glTF-Sample-Assets/blob/main/Models/MetalRoughSpheres/screenshot/screenshot.png) | Metallic-roughness response, IBL reflections | Pass | Roughness and metallic response are monotonic across the grid. Exact reflections differ with the procedural environment. |
| `FlightHelmet` | [screenshot.jpg](https://github.com/KhronosGroup/glTF-Sample-Assets/blob/main/Models/FlightHelmet/screenshot/screenshot.jpg) | Multiple materials and textures | Pass | All material sets and textures load and render through the cached PBR variants. |

## Known differences from the Khronos viewer

- The viewer uses a fixed three-point studio rig and a procedural cubemap
  environment instead of a bundled HDR studio environment.
- Camera framing, exposure and background are not matched to the reference
  screenshots.
- Normal mapping requires authored `TANGENT` attributes; tangent generation is
  not implemented.
- Bloom and FXAA are optional startup flags and are disabled unless requested.
