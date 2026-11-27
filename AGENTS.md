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
  *.cs                                     # library sources at the project root
DigitalRiseModel.Tests/                    # xUnit test project
docfx/                                     # DocFX config and articles
Samples/
  <SampleName>/                            # one self-contained sample per folder
README.md
AGENTS.md
LICENSE
```

- Namespace root: `DigitalRiseModel`.
- Model types use the `DigitalRiseModel` namespace. Library source files live directly in the project root; do not add a `code/` or `Graphics/` source subfolder.
- One public type per file, file name matches the type name.
- Test sources live directly in `DigitalRiseModel.Tests/` and use the `DigitalRiseModel.Tests` namespace.

## Golden rules

1. **Data only.** Model classes must not expose `Draw`, `Effect`, `GraphicsDevice`, shader, render-state or renderer-callback APIs. Rendering is the developer's responsibility.
2. **MonoGame data types only.** Use MonoGame data types such as `VertexBuffer`, `IndexBuffer`, `Matrix` and `BoundingSphere`. Do not add Stride, SharpDX, Silk.NET, OpenGL, Vulkan, SDL or custom engine abstractions.
3. **Mirror XNA behavior, not source.** The public API shape follows the XNA Game Studio 4.0 model classes. Do not copy, port or transliterate source, comments or internal designs from MonoGame, XNA, DigitalRiseModel or DigitalRune. Write the implementation independently and keep it simple.
4. **Keep dependencies minimal.** The library's only package dependency is `MonoGame.Framework.DesktopGL` (currently 3.8.6.1). Tests may use xUnit and the test SDK. Ask before adding another package or changing the MonoGame platform package.
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
- Store only references and ranges needed by a consumer: vertex/index buffers, primitive counts and offsets. Materials and effects belong to the caller's renderer.
- Cache `DrModelMesh.BoundingSphere` and invalidate it when `MeshParts` changes or a part's `BoundingSphere` changes. Do not expose a public invalidation method without a concrete need.
- `DrModel` is constructed from a non-null root bone. Its constructor traverses the hierarchy, collects bones and `DrModelBone.Mesh` references, and assigns sequential bone indices by default (`autoAssignIndices: true`).
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

- Each sample is a separate runnable MonoGame project under `Samples/<Name>/` and references the library project.
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
