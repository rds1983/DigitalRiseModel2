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

- `--shader-test` — start with the fullscreen shader smoke test enabled.
- `--debug-view <name>` — start with a PBR debug view: `shaded`, `basecolor`, `metallic`, `roughness`, `normal`, `occlusion`, `emissive`, `diffuse`, `specular`, `ibldiffuse` or `iblspecular`.
- `--exposure <value>` — set the shaded-output exposure scale.
- `--bloom <intensity>` — add HDR bloom before tone mapping; `0` disables it.
- `--fxaa` — apply FXAA to the shaded output.
- `--screenshot <file>` — render a few frames, save a PNG and exit.

The main viewer always uses the custom PBR path. It combines a fixed three-point studio light rig with image-based lighting generated at startup from a procedural environment, renders to an HDR scene target and resolves it with exposure, ACES tone mapping and gamma. `Samples/SimpleViewer` remains the stock-effect reference.

`F2` toggles the shader smoke test; `F3` cycles PBR debug views and updates the window title; `F5` reloads compiled effect binaries after a content rebuild. A top-right stats panel shows smoothed FPS, frame time, draw calls and triangle count.

## Controls

- Left-drag: orbit the camera.
- Mouse wheel: zoom.
- Escape: exit.

Camera orbit and zoom are ignored while the mouse is over a UI panel.

The animation panel appears only when the model has animations. It selects a clip, plays/pauses/stops playback, seeks through the current clip, toggles looping and reverse playback, and adjusts playback speed. Animated models start with their first clip selected and playing automatically.

## Rendering

The viewer loads assets through `AssetManager.LoadDrModel`, so external buffers and textures resolve relative to the model file.

Low-level rendering is isolated in `DigitalRiseModel.Viewer/Graphics/Renderer.cs`:

- It owns the stock effects, device state, lighting and draw calls.
- Non-skinned parts are drawn with `BasicEffect`.
- Parts with skin data are drawn with `SkinnedEffect`.
- `DrMaterial` supplies the base color, base-color texture and alpha-blend state.
- Basic ambient and directional lighting keeps the model visible.
- glTF winding is normalized using the node global-transform determinant and drawn with `CullCounterClockwise`; double-sided materials use `CullNone`.

`ViewerGame` owns model loading, camera input, bounds and the Myra UI. The camera frames the merged model bounds on startup.

Custom shader content is built through `DigitalRiseModel.Viewer/Content/Content.mgcb`. A trivial fullscreen shader smoke test can be started with `--shader-test` or toggled with `F2`.

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
