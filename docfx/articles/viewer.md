# Viewer

`DigitalRiseModel.Viewer` is a small MonoGame window viewer for glTF and GLB models. It is also packaged as the `drm-viewer` .NET tool.

```bash
dotnet run --project DigitalRiseModel.Viewer -- "path/to/model.glb"
```

```bash
dotnet pack DigitalRiseModel.Viewer -c Release -o artifacts
dotnet tool install --global DigitalRiseModel.Viewer --add-source artifacts
drm-viewer "path/to/model.glb"
```

## Controls

- Left-drag: orbit the camera.
- Mouse wheel: zoom.
- Escape: exit.

## Rendering

The viewer loads assets through `AssetManager.LoadDrModel`, so external buffers and textures resolve relative to the model file.

Low-level rendering is isolated in `DigitalRiseModel.Viewer/Graphics/Renderer.cs`:

- It owns the stock effects, device state, lighting and draw calls.
- Non-skinned parts are drawn with `BasicEffect`.
- Parts with skin data are drawn with `SkinnedEffect`.
- `DrMaterial` supplies the base color, base-color texture and alpha-blend state.
- Basic ambient and directional lighting keeps the model visible.
- Backface culling is disabled so rooms and other single-sided scenes do not look transparent when viewed from outside.

`ViewerGame` owns model loading, camera input, bounds and the Myra UI. The camera frames the merged model bounds on startup.

## Controls overlay

The controls block is a Myra UI defined in `DigitalRiseModel.Viewer/UI/ControlsPanel.xmmp` as a `VerticalStackPanel` of labels. It is exported to C# with:

```bash
myra-export DigitalRiseModel.Viewer/UI/ControlsPanel.xmmp \
  --namespace DigitalRiseModel.Viewer.UI \
  --class ControlsPanel \
  --output DigitalRiseModel.Viewer/UI
```

`ViewerGame` hosts the generated panel on a Myra `Desktop`.

The viewer uses the library's dynamic vertex declarations directly with the stock effects.
