using DigitalRiseModel.Viewer.Graphics.Materials;
using DigitalRiseModel.Viewer.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics;

/// <summary>
/// Low-level 3D drawing service: PBR effects, device state and draw calls.
/// It owns no scene content beyond the loaded model instance and never draws UI.
/// </summary>
internal sealed class Renderer : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly DrModelInstance _instance;
	private readonly DrModel _model;
	private readonly PbrEffect[] _pbrEffects;
	private readonly Texture2D _fallbackTexture;
	private readonly Dictionary<DrMaterial, MaterialBinding> _materialBindings = [];

	private Matrix _viewProjection;
	private Vector3 _cameraPosition;
	private bool _frameOpen;

	private BlendState _savedBlendState = BlendState.Opaque;
	private DepthStencilState _savedDepthStencilState = DepthStencilState.Default;
	private RasterizerState _savedRasterizerState = RasterizerState.CullCounterClockwise;
	private SamplerState _savedSamplerState = SamplerState.LinearWrap;
	private Rectangle _savedScissorRectangle;

	public ShaderSettings Settings { get; } = new();

	public Renderer(
		GraphicsDevice device,
		DrModelInstance instance,
		ContentManager content)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(instance);
		ArgumentNullException.ThrowIfNull(content);

		_device = device;
		_instance = instance;
		_model = instance.Model;

		_pbrEffects = new PbrEffect[Enum.GetValues<PbrVariant>().Length];

		(PbrVariant Variant, string Asset)[] variants =
		[
			(PbrVariant.Basic, "Shaders/PbrBasic"),
			(PbrVariant.BasicColor, "Shaders/PbrBasicVertexColor"),
			(PbrVariant.NoTangent, "Shaders/PbrNoTangent"),
			(PbrVariant.NoTangentColor, "Shaders/PbrNoTangentVertexColor"),
			(PbrVariant.Full, "Shaders/Pbr"),
			(PbrVariant.FullColor, "Shaders/PbrVertexColor"),
			(PbrVariant.SkinnedBasic, "Shaders/PbrSkinnedBasic"),
			(PbrVariant.SkinnedBasicColor, "Shaders/PbrSkinnedBasicVertexColor"),
			(PbrVariant.Skinned, "Shaders/PbrSkinned"),
			(PbrVariant.SkinnedColor, "Shaders/PbrSkinnedVertexColor")
		];

		foreach ((PbrVariant variant, string asset) in variants)
		{
			_pbrEffects[(int)variant] = new PbrEffect(content, asset);
		}

		_fallbackTexture = new Texture2D(device, 1, 1);
		_fallbackTexture.SetData([Color.White]);
	}

	public int DrawCalls { get; private set; }

	public int Triangles { get; private set; }

	/// <summary>
	/// Saves device state and starts a 3D frame. Must be paired with <see cref="EndFrame"/>.
	/// </summary>
	public void BeginFrame(Matrix view, Matrix projection)
	{
		if (_frameOpen)
		{
			throw new InvalidOperationException("A renderer frame is already open.");
		}

		DrawCalls = 0;
		Triangles = 0;
		_frameOpen = true;
		_viewProjection = view * projection;
		_cameraPosition = Matrix.Invert(view).Translation;

		foreach (PbrEffect effect in _pbrEffects)
		{
			effect.SetSettings(Settings);
		}

		_savedBlendState = _device.BlendState;
		_savedDepthStencilState = _device.DepthStencilState;
		_savedRasterizerState = _device.RasterizerState;
		_savedSamplerState = _device.SamplerStates[0];
		_savedScissorRectangle = _device.ScissorRectangle;

		_device.BlendState = BlendState.Opaque;
		_device.DepthStencilState = DepthStencilState.Default;
		_device.RasterizerState = RasterizerState.CullCounterClockwise;
		_device.SamplerStates[0] = SamplerState.LinearWrap;
		_device.ScissorRectangle = _device.Viewport.Bounds;
	}

	public void Clear(Color color)
	{
		EnsureFrameOpen();
		_device.Clear(color);
	}

	/// <summary>
	/// Draws the whole model using the instance's current bone pose.
	/// </summary>
	public void DrawModel()
	{
		EnsureFrameOpen();

		Matrix[] absoluteBoneTransforms = _instance.AbsoluteBoneTransforms;
		var opaqueItems = new List<RenderItem>();
		var transparentItems = new List<RenderItem>();

		foreach (DrModelMesh mesh in _model.Meshes)
		{
			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				if (part.VertexBuffer is null || part.IndexBuffer is null)
				{
					continue;
				}

				Matrix world = GetMeshWorld(part, absoluteBoneTransforms);
				Matrix[]? skinTransforms = part.Skin is { } skin
					? _instance.GetSkinTransforms(skin.Index)
					: null;
				Vector3 center = Vector3.Transform(part.BoundingSphere.Center, world);
				MaterialBinding material = part.Material is { } sourceMaterial
					? GetMaterialBinding(sourceMaterial)
					: MaterialBinding.Default;
				var item = new RenderItem
				{
					Part = part,
					Material = material,
					World = world,
					SkinTransforms = skinTransforms,
					DistanceSquared = Vector3.DistanceSquared(center, _cameraPosition)
				};

				if (item.Material.AlphaMode == DrAlphaMode.Blend)
				{
					transparentItems.Add(item);
				}
				else
				{
					opaqueItems.Add(item);
				}
			}
		}

		transparentItems.Sort((a, b) => b.DistanceSquared.CompareTo(a.DistanceSquared));

		foreach (RenderItem item in opaqueItems)
		{
			DrawItem(item);
		}

		foreach (RenderItem item in transparentItems)
		{
			DrawItem(item);
		}
	}

	public PbrEffect GetEffect(
		bool skinned,
		bool hasNormal,
		bool hasTangent,
		bool hasColor)
	{
		PbrVariant variant;

		if (skinned)
		{
			variant = hasNormal
				? hasColor ? PbrVariant.SkinnedColor : PbrVariant.Skinned
				: hasColor ? PbrVariant.SkinnedBasicColor : PbrVariant.SkinnedBasic;
		}
		else if (hasNormal && hasTangent)
		{
			variant = hasColor ? PbrVariant.FullColor : PbrVariant.Full;
		}
		else if (hasNormal)
		{
			variant = hasColor ? PbrVariant.NoTangentColor : PbrVariant.NoTangent;
		}
		else
		{
			variant = hasColor ? PbrVariant.BasicColor : PbrVariant.Basic;
		}

		return _pbrEffects[(int)variant];
	}

	private void DrawItem(RenderItem item)
	{
		DrModelMeshPart part = item.Part;
		MaterialBinding material = item.Material;

		_device.BlendState = material.AlphaMode == DrAlphaMode.Blend
			? BlendState.AlphaBlend
			: BlendState.Opaque;
		_device.DepthStencilState = DepthStencilState.Default;

		// The glTF loader converts front-facing triangles to MonoGame's clockwise
		// convention, so the standard counter-clockwise cull state is correct.
		_device.RasterizerState = material.DoubleSided
			? RasterizerState.CullNone
			: RasterizerState.CullCounterClockwise;

		VertexDeclaration declaration = part.VertexDeclaration
			?? part.VertexBuffer!.VertexDeclaration;

		bool hasNormal = HasElement(declaration, VertexElementUsage.Normal);
		bool hasTangent = HasElement(declaration, VertexElementUsage.Tangent);
		bool hasColor = HasElement(declaration, VertexElementUsage.Color);
		bool hasTexCoord = HasElement(declaration, VertexElementUsage.TextureCoordinate);
		bool hasBlend = HasElement(declaration, VertexElementUsage.BlendIndices)
			&& HasElement(declaration, VertexElementUsage.BlendWeight);
		bool skinned = item.SkinTransforms is not null && hasBlend && hasTexCoord;

		PbrEffect pbr = GetEffect(skinned, hasNormal, hasTangent, hasColor);

		if (skinned)
		{
			pbr.SetWorld(Matrix.Identity);
			pbr.SetWorldViewProjection(_viewProjection);
			pbr.SetSkinTransforms(item.SkinTransforms!);
		}
		else
		{
			pbr.SetWorld(item.World);
			pbr.SetWorldViewProjection(item.World * _viewProjection);
		}

		pbr.SetCameraPosition(_cameraPosition);
		pbr.SetMaterial(material, _fallbackTexture);

		_device.SetVertexBuffer(part.VertexBuffer);
		_device.Indices = part.IndexBuffer;

		foreach (EffectPass pass in pbr.Effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_device.DrawIndexedPrimitives(
				PrimitiveType.TriangleList,
				part.VertexOffset,
				part.StartIndex,
				part.PrimitiveCount);

			++DrawCalls;
			Triangles += part.PrimitiveCount;
		}
	}

	public void EndFrame()
	{
		EnsureFrameOpen();

		_frameOpen = false;

		_device.BlendState = _savedBlendState;
		_device.DepthStencilState = _savedDepthStencilState;
		_device.RasterizerState = _savedRasterizerState;
		_device.SamplerStates[0] = _savedSamplerState;
		_device.ScissorRectangle = _savedScissorRectangle;
	}

	public BoundingSphere ComputeBounds()
	{
		return _instance.BoundingSphere;
	}

	public void Dispose()
	{
		_fallbackTexture.Dispose();
	}

	private MaterialBinding GetMaterialBinding(DrMaterial material)
	{
		if (!_materialBindings.TryGetValue(material, out MaterialBinding? binding))
		{
			binding = new MaterialBinding(material);
			_materialBindings.Add(material, binding);
		}

		return binding;
	}

	private static Matrix GetMeshWorld(
		DrModelMeshPart part,
		Matrix[] absoluteBoneTransforms)
	{
		DrModelBone? parentBone = part.Mesh?.ParentBone;

		return parentBone is not null
			&& (uint)parentBone.Index < (uint)absoluteBoneTransforms.Length
			? absoluteBoneTransforms[parentBone.Index]
			: Matrix.Identity;
	}

	private static bool HasElement(
		VertexDeclaration declaration,
		VertexElementUsage usage,
		int usageIndex = 0)
	{
		foreach (VertexElement element in declaration.GetVertexElements())
		{
			if (element.VertexElementUsage == usage && element.UsageIndex == usageIndex)
			{
				return true;
			}
		}

		return false;
	}

	private void EnsureFrameOpen()
	{
		if (!_frameOpen)
		{
			throw new InvalidOperationException(
				"This operation requires an open renderer frame.");
		}
	}

	private enum PbrVariant
	{
		Basic,
		BasicColor,
		NoTangent,
		NoTangentColor,
		Full,
		FullColor,
		SkinnedBasic,
		SkinnedBasicColor,
		Skinned,
		SkinnedColor
	}

	private sealed class RenderItem
	{
		public required DrModelMeshPart Part { get; init; }

		public required MaterialBinding Material { get; init; }

		public Matrix World { get; init; }

		public Matrix[]? SkinTransforms { get; init; }

		public float DistanceSquared { get; init; }
	}
}
