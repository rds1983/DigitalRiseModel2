using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics;

/// <summary>
/// Low-level 3D drawing service: stock effects, device state and draw calls.
/// It owns no scene content beyond the loaded model and never draws UI.
/// </summary>
internal sealed class Renderer : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly DrModel _model;
	private readonly List<Drawable> _drawables = [];
	private readonly Matrix[] _absoluteBoneTransforms;
	private readonly Texture2D _fallbackTexture;

	private Matrix _view;
	private Matrix _projection;
	private bool _frameOpen;

	private BlendState _savedBlendState = BlendState.Opaque;
	private DepthStencilState _savedDepthStencilState = DepthStencilState.Default;
	private RasterizerState _savedRasterizerState = RasterizerState.CullCounterClockwise;
	private SamplerState _savedSamplerState = SamplerState.LinearWrap;
	private Rectangle _savedScissorRectangle;

	public Renderer(GraphicsDevice device, DrModel model)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(model);

		_device = device;
		_model = model;
		_absoluteBoneTransforms = new Matrix[model.Bones.Length];

		_fallbackTexture = new Texture2D(device, 1, 1);
		_fallbackTexture.SetData([Color.White]);

		if (model.Bones.Length > 0)
		{
			model.CopyAbsoluteBoneTransformsTo(_absoluteBoneTransforms);
		}

		CreateDrawables();
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

		_view = view;
		_projection = projection;
		DrawCalls = 0;
		Triangles = 0;
		_frameOpen = true;

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
	/// Draws the whole model using its current bind pose.
	/// </summary>
	public void DrawModel()
	{
		EnsureFrameOpen();

		_model.CopyAbsoluteBoneTransformsTo(_absoluteBoneTransforms);

		foreach (Drawable drawable in _drawables)
		{
			DrModelMeshPart part = drawable.Part;

			if (part.VertexBuffer is null || part.IndexBuffer is null)
			{
				continue;
			}

			DrMaterial? material = part.Material;

			_device.BlendState = material?.AlphaMode == DrAlphaMode.Blend
				? BlendState.AlphaBlend
				: BlendState.Opaque;
			_device.DepthStencilState = DepthStencilState.Default;

			// The glTF loader converts front-facing triangles to MonoGame's clockwise
			// convention, so the standard counter-clockwise cull state is correct.
			_device.RasterizerState = material?.DoubleSided == true
				? RasterizerState.CullNone
				: RasterizerState.CullCounterClockwise;

			if (drawable.IsSkinned)
			{
				var effect = (SkinnedEffect)drawable.Effect;
				effect.World = Matrix.Identity;
				effect.View = _view;
				effect.Projection = _projection;
				effect.SetBoneTransforms(drawable.BoneTransforms!);
			}
			else
			{
				var effect = (BasicEffect)drawable.Effect;
				effect.World = GetMeshWorld(part);
				effect.View = _view;
				effect.Projection = _projection;
			}

			_device.SetVertexBuffer(part.VertexBuffer);
			_device.Indices = part.IndexBuffer;

			foreach (EffectPass pass in drawable.Effect.CurrentTechnique.Passes)
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
		_model.CopyAbsoluteBoneTransformsTo(_absoluteBoneTransforms);

		BoundingSphere result = default;
		bool hasBounds = false;

		foreach (DrModelMesh mesh in _model.Meshes)
		{
			Matrix meshTransform = mesh.ParentBone is { } parentBone
				&& (uint)parentBone.Index < (uint)_absoluteBoneTransforms.Length
				? _absoluteBoneTransforms[parentBone.Index]
				: Matrix.Identity;

			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				BoundingSphere sphere = part.BoundingSphere.Transform(meshTransform);
				result = hasBounds
					? BoundingSphere.CreateMerged(result, sphere)
					: sphere;
				hasBounds = true;
			}
		}

		return result;
	}

	public void Dispose()
	{
		foreach (Drawable drawable in _drawables)
		{
			drawable.Effect.Dispose();
		}

		_drawables.Clear();
		_fallbackTexture.Dispose();
	}

	private void CreateDrawables()
	{
		foreach (DrModelMesh mesh in _model.Meshes)
		{
			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				if (part.VertexBuffer is null || part.IndexBuffer is null)
				{
					continue;
				}

				_drawables.Add(CreateDrawable(part));
			}
		}
	}

	private Drawable CreateDrawable(DrModelMeshPart part)
	{
		Texture2D? texture = part.Material?.BaseColorTexture?.Texture;
		Vector4 color = part.Material?.BaseColorFactor ?? Vector4.One;
		VertexDeclaration declaration = part.VertexDeclaration
			?? part.VertexBuffer!.VertexDeclaration;

		bool hasNormals = HasElement(declaration, VertexElementUsage.Normal);
		Matrix[]? boneTransforms = CreateBoneTransforms(part);
		bool isSkinned = boneTransforms is not null
			&& HasElement(declaration, VertexElementUsage.BlendIndices)
			&& HasElement(declaration, VertexElementUsage.BlendWeight);

		if (isSkinned)
		{
			var effect = new SkinnedEffect(_device)
			{
				DiffuseColor = new Vector3(color.X, color.Y, color.Z),
				Alpha = color.W,
				Texture = texture ?? _fallbackTexture,
				PreferPerPixelLighting = true,
				World = Matrix.Identity
			};

			ConfigureLights(effect);
			return new Drawable(part, effect, true, boneTransforms);
		}

		var basicEffect = new BasicEffect(_device)
		{
			DiffuseColor = new Vector3(color.X, color.Y, color.Z),
			Alpha = color.W,
			LightingEnabled = hasNormals,
			TextureEnabled = texture is not null,
			Texture = texture,
			PreferPerPixelLighting = true
		};

		ConfigureLights(basicEffect);
		return new Drawable(part, basicEffect, false, null);
	}

	private Matrix[]? CreateBoneTransforms(DrModelMeshPart part)
	{
		DrSkin? skin = part.Skin;

		if (skin is null
			|| skin.Joints.Length == 0
			|| skin.Joints.Length > SkinnedEffect.MaxBones
			|| skin.InverseBindMatrices.Length != skin.Joints.Length)
		{
			return null;
		}

		var transforms = new Matrix[skin.Joints.Length];

		for (int i = 0; i < transforms.Length; i++)
		{
			int boneIndex = skin.Joints[i].Index;

			if ((uint)boneIndex >= (uint)_absoluteBoneTransforms.Length)
			{
				return null;
			}

			transforms[i] = skin.InverseBindMatrices[i] * _absoluteBoneTransforms[boneIndex];
		}

		return transforms;
	}

	private Matrix GetMeshWorld(DrModelMeshPart part)
	{
		DrModelBone? parentBone = part.Mesh?.ParentBone;

		return parentBone is not null
			&& (uint)parentBone.Index < (uint)_absoluteBoneTransforms.Length
			? _absoluteBoneTransforms[parentBone.Index]
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

	private sealed class Drawable
	{
		public Drawable(
			DrModelMeshPart part,
			Effect effect,
			bool isSkinned,
			Matrix[]? boneTransforms)
		{
			Part = part;
			Effect = effect;
			IsSkinned = isSkinned;
			BoneTransforms = boneTransforms;
		}

		public DrModelMeshPart Part { get; }

		public Effect Effect { get; }

		public bool IsSkinned { get; }

		public Matrix[]? BoneTransforms { get; }
	}
}
