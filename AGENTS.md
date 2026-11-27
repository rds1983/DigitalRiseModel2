# AGENTS.md

Instructions for AI coding agents and human contributors working on **DigitalRiseModel.MonoGame**.

## What this project is

DigitalRiseModel.MonoGame is a 3D model library for **MonoGame** on .NET 8 / C# 12. It provides `Dr`-prefixed counterparts to the XNA 4.0 Graphics model classes — `DrModel`, `DrModelMesh`, `DrModelMeshPart`, `DrModelBone` and their collections — using MonoGame's own `Microsoft.Xna.Framework` types.

The project is an independent implementation. It mirrors the documented XNA API shape for familiarity, but it is not a port of MonoGame, XNA or DigitalRiseModel/DigitalRune source code.

## Repository layout

```text
DigitalRiseModel.MonoGame.sln              # solution root
DigitalRiseModel/
  DigitalRiseModel.MonoGame.csproj         # library project
  *.cs                                     # root model types at the project root
  Gltf/                                    # DigitalRiseModel.Gltf (internal loader)
  Vertices/                                # DigitalRiseModel.Vertices (VertexSkinned)
  Animation/                               # DigitalRiseModel.Animation (clips, channels, samplers)
DigitalRiseModel.Tests/                    # xUnit test project
DigitalRiseModel.Viewer/                   # MonoGame glTF viewer and drm-viewer tool
Samples/
  SimpleViewer/                            # stock-effect reference viewer
docfx/                                     # DocFX config and articles
README.md
AGENTS.md
LICENSE
```

- Namespace root: `DigitalRiseModel`.
- Model types use the `DigitalRiseModel` namespace. Library source files live directly in the project root; do not add a `code/` or `Graphics/` source subfolder.
- glTF-specific types use the `DigitalRiseModel.Gltf` namespace and live under `Gltf/`.
- Animation types use the `DigitalRiseModel.Animation` namespace and live under `Animation/`.
- One public type per file, file name matches the type name.
- Test sources live directly in `DigitalRiseModel.Tests/` and use the `DigitalRiseModel.Tests` namespace.

## Golden rules

1. **Data only.** Model classes must not expose `Draw`, `Effect`, shader, render-state or renderer-callback APIs. `GltfLoader` may create vertex/index buffers using a caller-supplied `GraphicsDevice`, but it must not issue draw calls. Rendering is the developer's responsibility.
2. **MonoGame data types only.** Use MonoGame data types such as `VertexBuffer`, `IndexBuffer`, `Matrix` and `BoundingSphere`. Do not add Stride, SharpDX, Silk.NET, OpenGL, Vulkan, SDL or custom engine abstractions.
3. **Mirror XNA behavior, not source.** The public API shape follows the XNA Game Studio 4.0 model classes. Do not copy, port or transliterate source, comments or internal designs from MonoGame, XNA, DigitalRiseModel or DigitalRune. Write the implementation independently and keep it simple.
4. **Keep dependencies minimal.** The library's package dependencies are `MonoGame.Framework.DesktopGL` (3.8.6.1), Khronos `glTF2Loader` (2.0.0) and `XNAssets.MonoGame` (0.8.9). Tests may use xUnit and the test SDK. Ask before adding another package or changing a platform dependency.
5. **Public API is a contract.** Document it, test it, and avoid breaking it. Add `[Obsolete]` with guidance before removing or changing a public member.
6. **Respect scope.** Make the smallest change that solves the task. Do not refactor unrelated code or reformat whole files.

## Build and test

```bash
dotnet restore DigitalRiseModel.MonoGame.sln
dotnet build DigitalRiseModel.MonoGame.sln -c Debug
dotnet test DigitalRiseModel.MonoGame.sln
```

Documentation:

```bash
dotnet tool restore
dotnet docfx metadata docfx/docfx.json
dotnet docfx build docfx/docfx.json
```

Delete `docfx/api` before regenerating metadata after removing public types; DocFX does not prune stale API YAML files.

Useful variants:

```bash
dotnet test --filter "FullyQualifiedName~DrModelTests"
dotnet build -c Release
dotnet format DigitalRiseModel.MonoGame.sln --verify-no-changes
```

Rules:

- The build must produce **zero warnings**. Both projects treat warnings as errors.
- All tests must pass before a task is considered complete.
- Never commit `bin/`, `obj/`, `TestResults/`, `docfx/_site/`, `.vs/` or IDE caches.

## Project and language conventions

- Target framework: `net8.0` everywhere (library, tests, samples).
- `LangVersion` 12, `Nullable` enabled, implicit usings enabled, nullable reference types annotated correctly.
- File-scoped namespaces; tab indentation; braces on their own lines.
- Use `var` when the type is obvious from the right-hand side; otherwise name the type.
- Private fields: `_camelCase`. Constants: `PascalCase`. Interfaces: `I` prefix. Async methods: `Async` suffix and accept a `CancellationToken` when cancellable work is involved.
- Use MonoGame math types (`Vector2/3/4`, `Matrix`, `Quaternion`, `BoundingSphere`, `BoundingBox`) in the public API. Do not introduce a parallel math API or `System.Numerics` equivalents in MonoGame-facing types.
- Argument validation: `ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException.ThrowIfNegative`, and similar BCL helpers. Validate public entry points; trust internal callers.
- Malformed asset data: throw `InvalidDataException` (or a derived, documented exception) with file offset/context. Do not silently skip bad data unless the caller opted in.
- Disposal: `DrModelMeshPart.Dispose` disposes its assigned `VertexBuffer` and `IndexBuffer`; `DrModelMesh.Dispose` disposes its parts. `Dispose` must be idempotent and clear owned references. Do not dispose resources the model type does not own.
- No `async void`, no blocking `.Result`/`.Wait()`, no `Thread.Sleep` in library code.
- Avoid `#region`; regions hide structure. Avoid static mutable state.

## API design guidelines

- Keep the `Dr` prefix on model types. Do not shadow or replace MonoGame's `Model`, `ModelMesh` or `ModelMeshPart`.
- Model types are plain data. Do not add rendering members, effects, graphics-device references or callbacks that issue draw calls.
- `DrModel.Bones`, `DrModel.Meshes`, `DrModelBone.Children` and `DrModelMesh.MeshParts` are arrays; do not add wrapper collection types or collection-change tracking. Array setters clear old back-references and assign the new ones: `Children` updates `Parent`, and `MeshParts` updates `Mesh`.
- `DrModelBone.Mesh` and `DrModelMesh.ParentBone` are two sides of the same link. The `Mesh` setter must update `ParentBone`; the `ParentBone` setter is internal so callers cannot break the invariant.
- Store only references and ranges needed by a consumer: vertex/index buffers, primitive counts and offsets. `DrMaterial` describes material data, but effects and draw calls belong to the caller's renderer.
- Cache `DrModelMesh.BoundingSphere` and invalidate it when `MeshParts` changes or a part's `BoundingSphere` changes. Do not expose a public invalidation method without a concrete need.
- `DrModel` is constructed from a non-null root bone. Its constructor traverses the hierarchy, collects bones and `DrModelBone.Mesh` references, and assigns sequential bone indices by default (`autoAssignIndices: true`).
- `DrModelInstance` wraps a `DrModel` and owns a per-instance local pose, accessed with `GetLocalTransform(int boneIndex)` and `SetLocalTransform(int boneIndex, Matrix matrix)`. The setter marks absolute transforms, skin transforms and the bounding sphere dirty. Each derived value recalculates lazily on its next query; skin and bounding-sphere queries first ensure absolute transforms are current. `BoundingSphere` transforms each mesh bound by its parent bone's absolute transform and merges them. `GetSkinTransforms(int skinIndex)` returns the cached per-skin `Matrix[]`. The constructor copies the model's current local transforms and collects skins from the mesh parts. Do not put animation playback or sampling APIs on it.
- `DrModelMeshPart` carries `DrMaterial` and `DrSkin` references. `DrMaterial` mirrors glTF metallic-roughness data. `DrSkin.Index` is its index in the source skin collection; `DrSkin.Joints` is a `DrSkinJoint[]`, each pairing a `DrModelBone` with its inverse bind matrix; `DrSkin.Skeleton` is the optional skeleton root.
- `GltfLoader` is internal. Public glTF loading is `AssetManager.LoadDrModel(device, path)`, which must use `AssetManager.UseLoader` so buffers and textures resolve recursively relative to the model asset. Do not expose `GltfLoader` publicly.
- `GltfLoader` uses Khronos `glTF2Loader` for parsing only. Decode accessors and create buffers in this repository; do not add a second glTF parser.
- `GltfLoader` is `partial`. Keep animation conversion in `GltfLoader.Animation.cs`; do not put it back in the main loader file.
- `DrModel.Animations` maps clip names to `AnimationClip`. Each `AnimationChannel` references a bone by `BoneIndex` and stores full `SrtTransform` poses in `AnimationChannelKeyframe` values; keep `TimeSpan` times and per-path interpolation modes. glTF TRS tracks are resampled at the union of their key times. Animation data is keyframes only: do not add sampling, blending, playback or bone-mutation APIs to the model classes. Clip duration is the maximum keyframe time; duplicate names get `_1`, `_2`, suffixes. Morph target weight animation is out of scope.
- `AnimationChannel.Sample(TimeSpan)` owns keyframe lookup and pose interpolation; the controller only advances time and applies poses. `AnimationController` owns single-clip playback for a `DrModelInstance`: `Play(name)` looks up `DrModel.Animations`, `Update(TimeSpan)` samples channels and writes local transforms, and `Stop` restores the bind pose. Treat `CubicSpline` channels as linear between their baked keyframes. Keep blending out of the controller until a concrete design is needed.
- `SrtTransform.Interpolate` owns pose interpolation: `Step` holds the start component; `Linear` and `CubicSpline` interpolate (rotation uses slerp). `SrtTransform.ToMatrix` composes `scale * rotation * translation`. Do not duplicate interpolation or TRS-to-matrix logic in animation code.
- Textures are loaded through XNAssets (`AssetManager.LoadTexture2D`). External images use the recursive model manager; embedded glTF images use an in-memory `IAssetAccessor`. Assign the result to `DrTextureInfo.Texture`.
- `ModelLoadFlags.None` creates `BufferUsage.WriteOnly` buffers. `ModelLoadFlags.ReadableBuffers` creates `BufferUsage.None` buffers so tests and debug tools can call `GetData`. `ModelLoadFlags.KeepSourceGeometry` fills `DrModelMeshPart.SourceIndices` and `SourceVertices`; those properties stay null otherwise. `ModelLoadFlags.EnsureUVs` adds a zero `TEXCOORD_0` channel when missing. Include the flags in the `AssetManager` cache key.
- `GltfLoader.Load(path)` is the internal data-only path: it fills vertex declarations, counts, bounds, materials, skins and animations but leaves `VertexBuffer`/`IndexBuffer` null. Use it only for tests and inspection.
- glTF fixtures live in `DigitalRiseModel.Tests/TestData` and are copied to the test output. Keep them tiny and offline; do not add external binary dependencies.
- Preserve XNA semantics for `CopyBoneTransformsTo`, `CopyBoneTransformsFrom` and `CopyAbsoluteBoneTransformsTo`. Parent bones must appear before their children; document and enforce this.
- Every public type and member gets XML documentation. Explain units, coordinate conventions and handedness when relevant.
- Document vertex layouts, index formats and texture formats explicitly. No implicit conventions.
- Keep constructors and methods deterministic. Sorting, hashing and deduplication must produce stable results across runs.

## Testing conventions

- Framework: xUnit. One test class per production type, named `<Type>Tests`.
- Test names: `MethodName_Scenario_ExpectedResult`, e.g. `CopyAbsoluteBoneTransformsTo_ParentAfterChild_ThrowsInvalidOperationException`.
- Cover edge cases: null arguments, missing names, duplicate children, hierarchy cycles and undersized transform arrays.
- Tests must not require a `GraphicsDevice`, window or GPU. Test data-side behavior only.
- Use precise assertions for geometry: compare vectors and matrices component-wise with an explicit tolerance, not exact float equality, except where values are exactly representable.
- Tests must be deterministic, offline, and independent of machine culture (use `CultureInfo.InvariantCulture` in formatting tests).
- Prefer small inline test assets or tiny generated data over large binaries. If a binary fixture is necessary, keep it under a few kilobytes and document its origin.
- Do not use a mocking framework unless a seam genuinely requires it; prefer simple fakes.

## Serialization rules

- Any native container format is versioned. A reader must accept older versions when possible and reject newer incompatible ones with an actionable error.
- Never reorder or reinterpret existing fields without a version bump.
- Validate counts and lengths before allocating. Reject negative/overflowing sizes.
- Add round-trip tests (write → read → compare) for every format change.

## Performance rules

- Hot paths (vertex processing, animation sampling) should not allocate per element. Use `Span<T>`/`stackalloc` for small scratch buffers.
- Do not add `Task`/`async` to CPU-bound geometry algorithms.
- Optimize only with a benchmark or profile in hand; correctness and clarity come first.
- Keep memory ownership explicit (who owns the buffer, who may mutate it).

## Samples and documentation

- Samples live under `Samples/` and are separate runnable MonoGame projects that reference the library project.
- `Samples/SimpleViewer` is the stock-effect reference viewer. Keep it runnable as a baseline while the main viewer moves to the custom shader system.
- `DigitalRiseModel.Viewer` takes one glTF/GLB path argument and renders with stock effects. Its executable/assembly name is `drm-viewer`; it is also packaged as the `drm-viewer` tool (`PackAsTool`, `ToolCommandName=drm-viewer`). Keep it runnable and packable with the current public API.
- Keep low-level 3D rendering in `DigitalRiseModel.Viewer/Graphics/Renderer.cs`: effects, device state, lighting, draw calls and counters. The renderer owns reusable `BasicEffect` and `SkinnedEffect` instances and selects one per `DrModelMeshPart` based on whether `Skin` is null. `ViewerGame` owns model loading, the `DrModelInstance`/`AnimationController`, camera input, bounds and Myra UI. The animation panel appears only for animated models, auto-plays the first clip and provides clip selection, play/pause/stop, seek, loop, reverse-playback and speed controls. Camera orbit and zoom are ignored while `Desktop.IsMouseOverGUI` is true. The renderer draws from the instance's current pose and recomputes skinned bone transforms every frame.
- glTF winding is normalized by the loader using the node global-transform determinant (`positive` = glTF CCW, `negative` = glTF CW). The viewer then uses `CullCounterClockwise`, or `CullNone` for double-sided materials.
- The viewer UI is Myra. Edit the `.xmmp` sources in `DigitalRiseModel.Viewer/UI` and regenerate with `myra-export`. Only the `*.Generated.cs` file is overwritten; keep custom behavior in the companion `.cs` file and do not hand-edit generated files.
- The viewer uses Myra `1.6.8` in addition to the library dependencies.
- Custom shader content is built through `DigitalRiseModel.Viewer/Content/Content.mgcb`; `dotnet-mgcb` is pinned in `.config/dotnet-tools.json`. Put shader sources under `Content/Shaders` and register them in `Content.mgcb`. The main viewer always uses the PBR path; `Samples/SimpleViewer` is the stock-effect reference. PBR variants are compiled from one macro-driven `Pbr.fx` using EffectProcessor `Defines`: `Pbr` (tangent normal mapping), `PbrNoTangent`, `PbrBasic`, `PbrSkinned` and `PbrSkinnedBasic`; `ModelLoadFlags.EnsureUVs` guarantees UV0. `--shader-test` (or `F2`) draws the fullscreen shader smoke test. Opaque and mask parts draw first; blended parts are sorted back-to-front. Declare PBR texture samplers with `sampler_state` in the effect; do not hardcode sampler slots in the renderer. Shared PBR material/lighting code lives in `PbrCommon.fxh`; keep only vertex, skinning and normal-map differences in the `.fx` files. `ShaderSettings` carries exposure and the debug view; `--debug-view`/`F3` select base color, metallic, roughness, normal, occlusion or emissive output, and `--exposure` scales HDR output before ACES tone mapping. `--screenshot <file>` captures a frame and exits for visual validation.
- Samples must compile and run with the current API; update them when the API changes.
- Public API changes require matching XML docs and, when user-visible behavior changes, a README or DocFX update.
- Keep README claims honest: mark unimplemented features as planned, not shipped.

## Definition of done

Before finishing a task, confirm:

- [ ] `dotnet build DigitalRiseModel.MonoGame.sln` succeeds with zero warnings.
- [ ] `dotnet test DigitalRiseModel.MonoGame.sln` passes.
- [ ] New or changed public API has XML documentation and tests.
- [ ] The change stays MonoGame-native and does not copy upstream source.
- [ ] No unnecessary dependencies or engine backends were added.
- [ ] Samples and docs still reflect reality.
- [ ] The diff contains only changes relevant to the task.

## When in doubt

Ask before making a decision that affects public API shape, the on-disk format, project dependencies or the repository layout. Small, reversible, well-tested changes are always preferred.
