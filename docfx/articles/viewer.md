# Viewer

`DigitalRiseModel.Viewer` is a small MonoGame window viewer for glTF and GLB models. Its executable is `drm-viewer`, and it is also packaged as the `drm-viewer` .NET tool.

```bash
dotnet run --project DigitalRiseModel.Viewer -- "path/to/model.glb"
```

```bash
dotnet pack DigitalRiseModel.Viewer -c Release -o artifacts
dotnet tool install --global DigitalRiseModel.Viewer --add-source artifacts
drm-viewer "path/to/model.glb"
```

`Samples/SimpleViewer` is a stock-effect reference viewer. It mirrors the main viewer while the main viewer gains the custom shader system.

Command-line options:

- `--debug-view <name>` — start with a PBR debug view: `shaded`, `basecolor`, `metallic`, `roughness`, `normal`, `occlusion`, `emissive`, `diffuse`, `specular`, `ibldiffuse` or `iblspecular`.
- `--exposure <value>` — set the shaded-output exposure scale.
- `--bloom <intensity>` — add HDR bloom before tone mapping; `0` disables it.
- `--fxaa` — apply FXAA to the shaded output.
- `--screenshot <file>` — render a few frames, save a PNG and exit.

The main viewer always uses the custom PBR path. It combines a fixed three-point studio light rig with image-based lighting generated at startup from a procedural environment, renders to an HDR scene target and resolves it with exposure, ACES tone mapping and gamma. `Samples/SimpleViewer` remains the stock-effect reference.

`F3` cycles PBR debug views and updates the window title; `F5` reloads compiled effect binaries after a content rebuild. In a development checkout, shader source changes trigger an automatic MGCB rebuild and reload. A top-right stats panel shows smoothed FPS, frame time, draw calls and triangle count.

## Controls

- Left-drag: orbit the camera.
- Mouse wheel: zoom.
- Escape: exit.

Camera orbit and zoom are ignored while the mouse is over a UI panel.

The animation panel appears only when the model has animations. It selects a clip, plays/pauses/stops playback, seeks through the current clip, toggles looping and reverse playback, and adjusts playback speed. Animated models start with their first clip selected and playing automatically.

## Rendering

The viewer loads assets through `AssetManager.LoadDrModel`, so external buffers and textures resolve relative to the model file.

Low-level rendering is isolated in `DigitalRiseModel.Viewer/Graphics/Renderer.cs`:

- It owns the cached PBR effects, IBL resources, effect reload, device state and draw queues.
- `Pbr.fx` variants handle static, skinned, vertex-color, normal-mapped and unlit parts.
- `DrMaterial` supplies metallic-roughness values, texture references, texture transforms, sampler settings and alpha state.
- A three-point studio rig provides direct lighting; procedural cubemap IBL provides diffuse and prefiltered specular lighting.
- The scene renders to an HDR target and resolves with exposure, ACES tone mapping, gamma, optional bloom and optional FXAA.
- Opaque parts sort front-to-back; blended parts sort back-to-front. Static parts are frustum-culled.
- glTF winding is normalized using the node global-transform determinant and drawn with `CullCounterClockwise`; double-sided materials use `CullNone`.

`ViewerGame` owns model loading, camera input, bounds and the Myra UI. The camera frames the merged model bounds on startup.

Custom shader content is built through `DigitalRiseModel.Viewer/Content/Content.mgcb`.

## Supported features and limitations

Supported:

- Core metallic-roughness materials with base color, metallic-roughness, normal, occlusion and emissive maps.
- Vertex colors, alpha opaque/mask/blend, double-sided materials and skinning.
- UV0/UV1 selection and sampler wrap/filter settings.
- `KHR_materials_emissive_strength`, `KHR_texture_transform` and `KHR_materials_unlit`.

Not implemented:

- `KHR_lights_punctual`, clearcoat/sheen/transmission, Basis Universal textures, Draco compression and morph targets. Clearcoat/sheen/transmission are deferred to a separate milestone.
- Normal maps require authored `TANGENT` attributes; tangent generation is not implemented.
- Shadows and HDR environment loading are not available.
- Transparent parts are sorted per part; skinned parts are not frustum-culled.
- Bloom and FXAA are startup options rather than runtime toggles.

## Troubleshooting

### Shader compilation

- The DesktopGL effect compiler accepts SM 3.0 shaders (`vs_3_0` / `ps_3_0`). SM 4-only intrinsics fail to compile.
- MGCB does not prune stale `.xnb` files. After renaming effect outputs, delete `Content/bin`, `Content/obj` and the viewer output `Content` folder.
- `TEXTURE_SETTINGS` doubles the PBR variant set. Full content rebuilds can take several minutes.
- The source watcher and `F5` reload require a development checkout and restored local tools (`dotnet tool restore`).

### Color and brightness

- Base color and emissive textures are sRGB and are linearized in the shader. Normal, metallic-roughness and occlusion textures remain linear.
- Rendering is HDR/linear; exposure, ACES tone mapping and gamma are applied only by the post-process pass. Do not apply gamma again in material shaders.
- Debug views bypass tone mapping, so their brightness is not representative of the shaded output.
- Bloom and FXAA are selected at startup with `--bloom` and `--fxaa`.

## UI panels

The overlay is Myra UI defined in `DigitalRiseModel.Viewer/UI/*.xmmp`:

- `ControlsPanel` shows camera and exit hints.
- `AnimationPanel` provides clip selection, playback buttons, time and speed sliders, plus loop and reverse-playback toggles.

Regenerate a panel after editing its `.xmmp`:

```bash
myra-export DigitalRiseModel.Viewer/UI/AnimationPanel.xmmp \
  --namespace DigitalRiseModel.Viewer.UI \
  --class AnimationPanel \
  --output DigitalRiseModel.Viewer/UI
```

Only `*.Generated.cs` is overwritten by the exporter. Keep event handlers and other custom behavior in the companion `.cs` file.

`ViewerGame` hosts both panels on a Myra `Desktop`.

The viewer uses the library's dynamic vertex declarations directly with the stock effects.
