using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimpleViewer.Graphics;

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
	private readonly Texture2D _fallbackTexture;

	private bool _frameOpen;

	private BlendState _savedBlendState = BlendState.Opaque;
	private DepthStencilState _savedDepthStencilState = DepthStencilState.Default;
	private RasterizerState _savedRasterizerState = RasterizerState.CullCounterClockwise;
	private SamplerState _savedSamplerState = SamplerState.LinearWrap;
	private Rectangle _savedScissorRectangle;

	public Renderer(GraphicsDevice device, DrModelInstance instance)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(instance);

		_device = device;
		_instance = instance;
		_model = instance.Model;

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

		_basicEffect.View = view;
		_basicEffect.Projection = projection;
		_skinnedEffect.View = view;
		_skinnedEffect.Projection = projection;

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

		foreach (DrModelMesh mesh in _model.Meshes)
		{
			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				if (part.VertexBuffer is null || part.IndexBuffer is null)
				{
					continue;
				}

				DrMaterial? material = part.Material;
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

				Effect effect;

				if (part.Skin is { } skin)
				{
					_skinnedEffect.World = Matrix.Identity;
					_skinnedEffect.DiffuseColor = new Vector3(color.X, color.Y, color.Z);
					_skinnedEffect.Alpha = color.W;
					_skinnedEffect.Texture = texture ?? _fallbackTexture;
					_skinnedEffect.SetBoneTransforms(
						_instance.GetSkinTransforms(skin.Index));
					effect = _skinnedEffect;
				}
				else
				{
					VertexDeclaration declaration = part.VertexDeclaration
						?? part.VertexBuffer.VertexDeclaration;

					_basicEffect.World = GetMeshWorld(part, absoluteBoneTransforms);
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

	private static bool HasElement(VertexDeclaration declaration, VertexElementUsage usage)
	{
		foreach (VertexElement element in declaration.GetVertexElements())
		{
			if (element.VertexElementUsage == usage)
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
}
