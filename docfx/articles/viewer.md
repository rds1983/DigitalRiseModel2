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

- Non-skinned parts are drawn with `BasicEffect`.
- Parts with skin data are drawn with `SkinnedEffect`.
- `DrMaterial` supplies the base color, base-color texture and double-sided/alpha-blend state.
- The camera frames the merged model bounds on startup.

The viewer uses the library's dynamic vertex declarations directly with the stock effects.
