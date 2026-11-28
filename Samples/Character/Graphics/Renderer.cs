using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character;

/// <summary>
/// Low-level 3D drawing service built on stock MonoGame effects. It owns no scene content:
/// <see cref="DrModelMeshPart.Skin"/> selects <see cref="SkinnedEffect"/>, otherwise
/// <see cref="BasicEffect"/>, mirroring the viewer's renderer. The same <see cref="BasicEffect"/>
/// also draws the unskinned ground.
/// </summary>
internal sealed class Renderer : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly BasicEffect _basicEffect;
	private readonly SkinnedEffect _skinnedEffect;
	private readonly Texture2D _fallbackTexture;

	private Matrix _view;
	private Matrix _projection;

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

	public void SetCamera(Matrix view, Matrix projection)
	{
		_view = view;
		_projection = projection;
	}

	/// <summary>
	/// Draws one mesh part with the effect that matches its skinning state. The caller supplies
	/// the world matrix and the instance whose pose drives skinning, so the renderer stays
	/// scene-agnostic.
	/// </summary>
	public void DrawPart(DrModelMeshPart part, Matrix world, DrModelInstance? instance)
	{
		ArgumentNullException.ThrowIfNull(part);

		if (part.VertexBuffer is null || part.IndexBuffer is null)
		{
			return;
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

		if (part.Skin is { } skin && instance is not null)
		{
			// Skinned parts are placed by their bone transforms, which include root motion,
			// so the world matrix stays identity. This matches the viewer.
			_skinnedEffect.World = Matrix.Identity;
			_skinnedEffect.View = _view;
			_skinnedEffect.Projection = _projection;
			_skinnedEffect.DiffuseColor = diffuse;
			_skinnedEffect.Alpha = color.W;
			_skinnedEffect.Texture = _fallbackTexture;
			_skinnedEffect.SetBoneTransforms(instance.GetSkinTransforms(skin.Index));

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
		}
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

	public void Dispose()
	{
		_basicEffect.Dispose();
		_skinnedEffect.Dispose();
		_fallbackTexture.Dispose();
	}
}
