using DigitalRiseModel.Viewer.Graphics.Lighting;
using DigitalRiseModel.Viewer.Graphics.PostProcessing;
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
	private const int ColorFlag = 1;
	private const int NormalFlag = 2;
	private const int TangentFlag = 4;
	private const int SkinnedFlag = 8;
	private const int UnlitFlag = 16;

	private readonly GraphicsDevice _device;
	private readonly DrModelInstance _instance;
	private readonly DrModel _model;
	private readonly ContentManager _content;
	private readonly Dictionary<int, PbrEffect> _pbrEffects = [];
	private readonly Dictionary<DrModelMeshPart, PartDrawInfo> _partDrawInfo = [];
	private readonly List<RenderItem> _opaqueItems = [];
	private readonly List<RenderItem> _transparentItems = [];
	private readonly List<RenderItem> _renderItemPool = [];
	private readonly IblResources _ibl;
	private readonly PostProcessor _postProcessor;
	private readonly Texture2D _fallbackTexture;

	private static readonly Comparison<RenderItem> OpaqueComparison =
		CompareOpaque;
	private static readonly Comparison<RenderItem> TransparentComparison =
		CompareTransparent;

	private Matrix _viewProjection;
	private BoundingFrustum _frustum = new(Matrix.Identity);
	private Vector3 _cameraPosition;
	private int _renderItemCount;
	private ShaderDebugView? _lastDebugView;
	private bool _frameOpen;

	private RenderTargetBinding[] _savedRenderTargets = [];
	private Viewport _savedViewport;
	private BlendState _savedBlendState = BlendState.Opaque;
	private DepthStencilState _savedDepthStencilState = DepthStencilState.Default;
	private RasterizerState _savedRasterizerState = RasterizerState.CullCounterClockwise;
	private SamplerState _savedSamplerState = SamplerState.LinearWrap;
	private Rectangle _savedScissorRectangle;

	public ShaderSettings Settings { get; } = new();

	public LightSet Lights { get; } = LightSet.CreateStudio();

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
		_content = content;
		_ibl = new IblResources(device);
		_postProcessor = new PostProcessor(device, content);

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
		_frustum = new BoundingFrustum(_viewProjection);
		_cameraPosition = Matrix.Invert(view).Translation;

		// Lights and IBL resources are immutable for the viewer lifetime and are
		// uploaded when each effect is created. Debug view changes are rare, so only
		// update the cached effects when the selected view actually changes.
		if (_lastDebugView != Settings.DebugView)
		{
			foreach (PbrEffect effect in _pbrEffects.Values)
			{
				effect.SetSettings(Settings);
			}

			_lastDebugView = Settings.DebugView;
		}

		_savedRenderTargets = _device.GetRenderTargets();
		_savedViewport = _device.Viewport;
		_savedBlendState = _device.BlendState;
		_savedDepthStencilState = _device.DepthStencilState;
		_savedRasterizerState = _device.RasterizerState;
		_savedSamplerState = _device.SamplerStates[0];
		_savedScissorRectangle = _device.ScissorRectangle;

		int width = _device.PresentationParameters.BackBufferWidth;
		int height = _device.PresentationParameters.BackBufferHeight;

		_postProcessor.EnsureSize(width, height);
		_postProcessor.SetSettings(Settings);
		_device.SetRenderTarget(_postProcessor.SceneTarget);
		_device.Viewport = new Viewport(0, 0, width, height);

		_device.BlendState = BlendState.Opaque;
		_device.DepthStencilState = DepthStencilState.Default;
		_device.RasterizerState = RasterizerState.CullCounterClockwise;
		_device.SamplerStates[0] = SamplerState.LinearWrap;
		_device.ScissorRectangle = new Rectangle(0, 0, width, height);
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
		_opaqueItems.Clear();
		_transparentItems.Clear();
		_renderItemCount = 0;

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
				BoundingSphere worldBounds = part.BoundingSphere.Transform(world);

				// Skinned bounds can exceed the bind pose, so only cull static parts.
				if (skinTransforms is null && !_frustum.Intersects(worldBounds))
				{
					continue;
				}

				PartDrawInfo drawInfo = GetPartDrawInfo(part, skinTransforms);
				RenderItem item = GetRenderItem();
				item.Part = part;
				item.Material = part.Material;
				item.World = world;
				item.SkinTransforms = skinTransforms;
				item.DistanceSquared = Vector3.DistanceSquared(
					worldBounds.Center,
					_cameraPosition);
				item.Effect = drawInfo.Effect;
				item.Skinned = drawInfo.Skinned;

				if (item.Material?.AlphaMode == DrAlphaMode.Blend)
				{
					_transparentItems.Add(item);
				}
				else
				{
					_opaqueItems.Add(item);
				}
			}
		}

		_opaqueItems.Sort(OpaqueComparison);
		_transparentItems.Sort(TransparentComparison);

		foreach (RenderItem item in _opaqueItems)
		{
			DrawItem(item);
		}

		foreach (RenderItem item in _transparentItems)
		{
			DrawItem(item);
		}
	}

	private PartDrawInfo GetPartDrawInfo(
		DrModelMeshPart part,
		Matrix[]? skinTransforms)
	{
		if (_partDrawInfo.TryGetValue(part, out PartDrawInfo? drawInfo))
		{
			return drawInfo;
		}

		VertexDeclaration declaration = part.VertexDeclaration
			?? part.VertexBuffer!.VertexDeclaration;

		bool hasNormal = HasElement(declaration, VertexElementUsage.Normal);
		bool hasTangent = HasElement(declaration, VertexElementUsage.Tangent);
		bool hasColor = HasElement(declaration, VertexElementUsage.Color);
		bool hasTexCoord = HasElement(
			declaration,
			VertexElementUsage.TextureCoordinate);
		bool hasBlend = HasElement(declaration, VertexElementUsage.BlendIndices)
			&& HasElement(declaration, VertexElementUsage.BlendWeight);
		bool skinned = skinTransforms is not null && hasBlend && hasTexCoord;
		bool unlit = part.Material?.Unlit == true;

		drawInfo = new PartDrawInfo
		{
			Skinned = skinned,
			Effect = GetEffect(
				skinned,
				hasNormal,
				hasTangent,
				hasColor,
				unlit)
		};
		_partDrawInfo[part] = drawInfo;
		return drawInfo;
	}

	private RenderItem GetRenderItem()
	{
		if (_renderItemCount == _renderItemPool.Count)
		{
			_renderItemPool.Add(new RenderItem());
		}

		return _renderItemPool[_renderItemCount++];
	}

	public PbrEffect GetEffect(
		bool skinned,
		bool hasNormal,
		bool hasTangent,
		bool hasColor,
		bool unlit)
	{
		int flags = 0;

		if (unlit)
		{
			flags |= UnlitFlag;
			hasNormal = false;
			hasTangent = false;
		}

		if (hasColor)
		{
			flags |= ColorFlag;
		}

		if (hasNormal)
		{
			flags |= NormalFlag;

			if (hasTangent)
			{
				flags |= TangentFlag;
			}
		}

		if (skinned)
		{
			flags |= SkinnedFlag;
		}

		if (!_pbrEffects.TryGetValue(flags, out PbrEffect? effect))
		{
			effect = new PbrEffect(_content, "Shaders/" + GetEffectName(flags));
			effect.SetSettings(Settings);
			effect.SetLights(Lights);
			effect.SetIbl(_ibl);
			_pbrEffects[flags] = effect;
		}

		return effect;
	}

	/// <summary>
	/// Builds the effect asset name from a feature bitmask. Defines are sorted
	/// alphabetically to match the content pipeline output.
	/// </summary>
	private static string GetEffectName(int flags)
	{
		string name = "Pbr";

		if ((flags & ColorFlag) != 0)
		{
			name += "_HAS_COLOR";
		}

		if ((flags & NormalFlag) != 0)
		{
			name += "_HAS_NORMAL";
		}

		if ((flags & TangentFlag) != 0)
		{
			name += "_HAS_TANGENT";
		}

		if ((flags & SkinnedFlag) != 0)
		{
			name += "_SKINNED";
		}

		if ((flags & UnlitFlag) != 0)
		{
			name += "_UNLIT";
		}

		return name;
	}

	private void DrawItem(RenderItem item)
	{
		DrModelMeshPart part = item.Part;
		DrMaterial? material = item.Material;

		_device.BlendState = material?.AlphaMode == DrAlphaMode.Blend
			? BlendState.AlphaBlend
			: BlendState.Opaque;
		_device.DepthStencilState = DepthStencilState.Default;

		// The glTF loader converts front-facing triangles to MonoGame's clockwise
		// convention, so the standard counter-clockwise cull state is correct.
		_device.RasterizerState = material?.DoubleSided == true
			? RasterizerState.CullNone
			: RasterizerState.CullCounterClockwise;

		PbrEffect pbr = item.Effect;

		if (item.Skinned)
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

		try
		{
			_postProcessor.Draw();
		}
		finally
		{
			if (_savedRenderTargets.Length == 0)
			{
				_device.SetRenderTarget(null);
			}
			else
			{
				_device.SetRenderTargets(_savedRenderTargets);
			}

			_device.Viewport = _savedViewport;
			_device.BlendState = _savedBlendState;
			_device.DepthStencilState = _savedDepthStencilState;
			_device.RasterizerState = _savedRasterizerState;
			_device.SamplerStates[0] = _savedSamplerState;
			_device.ScissorRectangle = _savedScissorRectangle;
		}
	}

	public BoundingSphere ComputeBounds()
	{
		return _instance.BoundingSphere;
	}

	public void Dispose()
	{
		_fallbackTexture.Dispose();
		_ibl.Dispose();
		_postProcessor.Dispose();
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

	private static int CompareOpaque(RenderItem a, RenderItem b)
	{
		return a.DistanceSquared.CompareTo(b.DistanceSquared);
	}

	private static int CompareTransparent(RenderItem a, RenderItem b)
	{
		return b.DistanceSquared.CompareTo(a.DistanceSquared);
	}

	private sealed class PartDrawInfo
	{
		public required PbrEffect Effect { get; init; }

		public bool Skinned { get; init; }
	}

	private sealed class RenderItem
	{
		public DrModelMeshPart Part { get; set; } = null!;

		public DrMaterial? Material { get; set; }

		public Matrix World { get; set; }

		public Matrix[]? SkinTransforms { get; set; }

		public float DistanceSquared { get; set; }

		public PbrEffect Effect { get; set; } = null!;

		public bool Skinned { get; set; }
	}
}
