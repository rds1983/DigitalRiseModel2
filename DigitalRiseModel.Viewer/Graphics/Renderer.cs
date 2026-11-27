using DigitalRiseModel.Viewer.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics;

/// <summary>
/// Low-level 3D drawing service: stock effects, device state and draw calls.
/// It owns no scene content beyond the loaded model instance and never draws UI.
/// </summary>
internal sealed class Renderer : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly DrModelInstance _instance;
	private readonly DrModel _model;
	private readonly BasicEffect _basicEffect;
	private readonly SkinnedEffect _skinnedEffect;
	private readonly PbrEffect? _pbrEffect;
	private readonly PbrEffect? _pbrSkinnedEffect;
	private readonly Texture2D _fallbackTexture;

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
		ContentManager? content = null,
		bool usePbr = false)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(instance);

		_device = device;
		_instance = instance;
		_model = instance.Model;

		if (usePbr)
		{
			ArgumentNullException.ThrowIfNull(content);
			_pbrEffect = new PbrEffect(content, "Shaders/Pbr");
			_pbrSkinnedEffect = new PbrEffect(content, "Shaders/PbrSkinned");
		}

		_basicEffect = new BasicEffect(device)
		{
			PreferPerPixelLighting = true
		};

		ConfigureLights(_basicEffect);

		_skinnedEffect = new SkinnedEffect(device)
		{
			PreferPerPixelLighting = true
		};

		ConfigureLights(_skinnedEffect);

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

		_basicEffect.View = view;
		_basicEffect.Projection = projection;
		_skinnedEffect.View = view;
		_skinnedEffect.Projection = projection;
		_pbrEffect?.SetSettings(Settings);
		_pbrSkinnedEffect?.SetSettings(Settings);

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
				var item = new RenderItem
				{
					Part = part,
					Material = part.Material,
					World = world,
					SkinTransforms = skinTransforms,
					DistanceSquared = Vector3.DistanceSquared(center, _cameraPosition)
				};

				if (item.Material?.AlphaMode == DrAlphaMode.Blend)
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

	private void DrawItem(RenderItem item)
	{
		DrModelMeshPart part = item.Part;
		DrMaterial? material = item.Material;
		Vector4 color = material?.BaseColorFactor ?? Vector4.One;
		Texture2D? texture = material?.BaseColorTexture?.Texture;

		_device.BlendState = material?.AlphaMode == DrAlphaMode.Blend
			? BlendState.AlphaBlend
			: BlendState.Opaque;
		_device.DepthStencilState = DepthStencilState.Default;

		// The glTF loader converts front-facing triangles to MonoGame's clockwise
		// convention, so the standard counter-clockwise cull state is correct.
		_device.RasterizerState = material?.DoubleSided == true
			? RasterizerState.CullNone
			: RasterizerState.CullCounterClockwise;

		VertexDeclaration declaration = part.VertexDeclaration
			?? part.VertexBuffer!.VertexDeclaration;
		Effect effect;

		if (item.SkinTransforms is not null)
		{
			if (_pbrSkinnedEffect is not null
				&& UsesUv0(material?.BaseColorTexture)
				&& UsesUv0(material?.MetallicRoughnessTexture)
				&& UsesUv0(material?.OcclusionTexture)
				&& UsesUv0(material?.EmissiveTexture)
				&& HasElement(declaration, VertexElementUsage.Normal)
				&& HasElement(declaration, VertexElementUsage.BlendIndices)
				&& HasElement(declaration, VertexElementUsage.BlendWeight)
				&& HasElement(declaration, VertexElementUsage.TextureCoordinate))
			{
				_pbrSkinnedEffect.SetWorld(Matrix.Identity);
				_pbrSkinnedEffect.SetWorldViewProjection(_viewProjection);
				_pbrSkinnedEffect.SetCameraPosition(_cameraPosition);
				_pbrSkinnedEffect.SetMaterial(material, _fallbackTexture);
				_pbrSkinnedEffect.SetSkinTransforms(item.SkinTransforms);
				effect = _pbrSkinnedEffect.Effect;
			}
			else
			{
				_skinnedEffect.World = Matrix.Identity;
				_skinnedEffect.DiffuseColor = new Vector3(color.X, color.Y, color.Z);
				_skinnedEffect.Alpha = color.W;
				_skinnedEffect.Texture = texture ?? _fallbackTexture;
				_skinnedEffect.SetBoneTransforms(item.SkinTransforms);
				effect = _skinnedEffect;
			}
		}
		else if (_pbrEffect is not null
			&& UsesUv0(material?.BaseColorTexture)
			&& UsesUv0(material?.MetallicRoughnessTexture)
			&& UsesUv0(material?.NormalTexture)
			&& UsesUv0(material?.OcclusionTexture)
			&& UsesUv0(material?.EmissiveTexture)
			&& HasElement(declaration, VertexElementUsage.Normal)
			&& HasElement(declaration, VertexElementUsage.Tangent)
			&& HasElement(declaration, VertexElementUsage.TextureCoordinate))
		{
			_pbrEffect.SetWorld(item.World);
			_pbrEffect.SetWorldViewProjection(item.World * _viewProjection);
			_pbrEffect.SetCameraPosition(_cameraPosition);
			_pbrEffect.SetMaterial(material, _fallbackTexture);
			effect = _pbrEffect.Effect;
		}
		else
		{
			_basicEffect.World = item.World;
			_basicEffect.DiffuseColor = new Vector3(color.X, color.Y, color.Z);
			_basicEffect.Alpha = color.W;
			_basicEffect.LightingEnabled = HasElement(
				declaration,
				VertexElementUsage.Normal);
			_basicEffect.TextureEnabled = texture is not null;
			_basicEffect.Texture = texture;
			effect = _basicEffect;
		}

		_device.SetVertexBuffer(part.VertexBuffer);
		_device.Indices = part.IndexBuffer;

		foreach (EffectPass pass in effect.CurrentTechnique.Passes)
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
		_basicEffect.Dispose();
		_skinnedEffect.Dispose();
		_fallbackTexture.Dispose();
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

	private static void ConfigureLights(IEffectLights lights)
	{
		lights.AmbientLightColor = new Vector3(0.35f);
		lights.DirectionalLight0.Enabled = true;
		lights.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.5f));
		lights.DirectionalLight0.DiffuseColor = Vector3.One;
		lights.DirectionalLight0.SpecularColor = Vector3.Zero;
		lights.DirectionalLight1.Enabled = false;
		lights.DirectionalLight2.Enabled = false;
	}

	private static bool UsesUv0(DrTextureInfo? texture)
	{
		return texture is null || texture.TexCoord == 0;
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

	private sealed class RenderItem
	{
		public required DrModelMeshPart Part { get; init; }

		public DrMaterial? Material { get; init; }

		public Matrix World { get; init; }

		public Matrix[]? SkinTransforms { get; init; }

		public float DistanceSquared { get; init; }
	}
}
