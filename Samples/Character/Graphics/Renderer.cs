using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character;

/// <summary>
/// Low-level 3D drawing service built on stock MonoGame effects. It owns no scene content:
/// <see cref="DrModelMeshPart.Skin"/> selects <see cref="SkinnedEffect"/>, otherwise
/// <see cref="BasicEffect"/>, mirroring the viewer's renderer. The same <see cref="BasicEffect"/>
/// also draws unskinned geometry such as the field.
/// </summary>
internal sealed class Renderer : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly BasicEffect _basicEffect;
	private readonly SkinnedEffect _skinnedEffect;
	private readonly Texture2D _fallbackTexture;

	private Matrix _view;
	private Matrix _projection;
	private bool _frameOpen;

	public Renderer(GraphicsDevice device)
	{
		ArgumentNullException.ThrowIfNull(device);

		_device = device;

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

	/// <summary>
	/// Gets the per-frame statistics. Reset by <see cref="BeginFrame"/>.
	/// </summary>
	public RenderStatistics Statistics { get; } = new();

	/// <summary>
	/// Starts a 3D frame. Must be paired with <see cref="EndFrame"/>.
	/// </summary>
	public void BeginFrame(Matrix view, Matrix projection)
	{
		if (_frameOpen)
		{
			throw new InvalidOperationException("A renderer frame is already open.");
		}

		Statistics.Reset();
		_frameOpen = true;
		_view = view;
		_projection = projection;
	}

	public void EndFrame()
	{
		EnsureFrameOpen();
		_frameOpen = false;
	}

	/// <summary>
	/// Draws a whole model instance, including every mesh part.
	/// </summary>
	public void DrawInstance(DrModelInstance instance)
	{
		ArgumentNullException.ThrowIfNull(instance);

		Matrix[] absoluteBoneTransforms = instance.AbsoluteBoneTransforms;

		foreach (DrModelMesh mesh in instance.Model.Meshes)
		{
			foreach (DrModelMeshPart part in mesh.MeshParts)
			{
				Matrix[]? boneMatrices = part.Skin is { } skin
					? instance.GetSkinTransforms(skin.Index)
					: null;

				DrawPart(part, GetMeshWorld(part, absoluteBoneTransforms), boneMatrices);
			}
		}
	}

	/// <summary>
	/// Draws one mesh part with the effect that matches its skinning state.
	/// </summary>
	/// <param name="part">The mesh part to draw.</param>
	/// <param name="world">The world matrix for static parts.</param>
	/// <param name="boneMatrices">
	/// The skinning transforms for a skinned part, as returned by
	/// <see cref="DrModelInstance.GetSkinTransforms"/>. Pass <see langword="null"/> for static
	/// parts.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="part"/> is <see langword="null"/>.</exception>
	/// <exception cref="InvalidOperationException">
	/// <paramref name="part"/> has a <see cref="DrModelMeshPart.Skin"/> but
	/// <paramref name="boneMatrices"/> is <see langword="null"/>.
	/// </exception>
	public void DrawPart(DrModelMeshPart part, Matrix world, Matrix[]? boneMatrices = null)
	{
		ArgumentNullException.ThrowIfNull(part);
		EnsureFrameOpen();

		if (part.VertexBuffer is null || part.IndexBuffer is null)
		{
			return;
		}

		if (part.Skin is not null && boneMatrices is null)
		{
			throw new InvalidOperationException(
				"The mesh part is skinned, so bone matrices are required. " +
				"Pass instance.GetSkinTransforms(part.Skin.Index).");
		}

		_device.BlendState = BlendState.Opaque;
		_device.DepthStencilState = DepthStencilState.Default;
		_device.RasterizerState = part.Material?.DoubleSided == true
			? RasterizerState.CullNone
			: RasterizerState.CullCounterClockwise;

		Vector4 color = part.Material?.BaseColorFactor ?? Vector4.One;
		var diffuse = new Vector3(color.X, color.Y, color.Z);

		_device.SetVertexBuffer(part.VertexBuffer);
		_device.Indices = part.IndexBuffer;

		if (boneMatrices is not null)
		{
			// Skinned parts are placed by their bone transforms, which include root motion,
			// so the world matrix stays identity. This matches the viewer.
			_skinnedEffect.World = Matrix.Identity;
			_skinnedEffect.View = _view;
			_skinnedEffect.Projection = _projection;
			_skinnedEffect.DiffuseColor = diffuse;
			_skinnedEffect.Alpha = color.W;
			_skinnedEffect.Texture = _fallbackTexture;
			_skinnedEffect.SetBoneTransforms(boneMatrices);

			DrawPasses(_skinnedEffect, part);
			return;
		}

		_basicEffect.World = world;
		_basicEffect.View = _view;
		_basicEffect.Projection = _projection;
		_basicEffect.DiffuseColor = diffuse;
		_basicEffect.Alpha = color.W;
		_basicEffect.LightingEnabled = HasElement(
			part.VertexDeclaration,
			VertexElementUsage.Normal);
		_basicEffect.TextureEnabled = false;

		DrawPasses(_basicEffect, part);
	}

	private void DrawPasses(Effect effect, DrModelMeshPart part)
	{
		foreach (EffectPass pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_device.DrawIndexedPrimitives(
				part.PrimitiveType,
				part.VertexOffset,
				part.StartIndex,
				part.PrimitiveCount);

			int triangles = part.PrimitiveType == PrimitiveType.TriangleList
				? part.PrimitiveCount
				: 0;
			Statistics.AddDrawCall(triangles);
		}
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
		lights.DirectionalLight0.Direction =
			Vector3.Normalize(new Vector3(-0.5f, -1f, -0.4f));
		lights.DirectionalLight0.DiffuseColor = Vector3.One;
		lights.DirectionalLight0.SpecularColor = Vector3.Zero;
		lights.DirectionalLight1.Enabled = false;
		lights.DirectionalLight2.Enabled = false;
	}

	private static bool HasElement(VertexDeclaration? declaration, VertexElementUsage usage)
	{
		if (declaration is null)
		{
			return false;
		}

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

	public void Dispose()
	{
		_basicEffect.Dispose();
		_skinnedEffect.Dispose();
		_fallbackTexture.Dispose();
	}
}
