using DigitalRiseModel.Viewer.Graphics.PostProcessing;
using DigitalRiseModel.Viewer.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics;

/// <summary>
/// Low-level 3D draw service. Owns the HDR/post-process targets and device state,
/// draws individual mesh parts and tracks frame statistics. It does not know about
/// models, effect selection, culling, sorting or material caching.
/// </summary>
internal sealed class Renderer : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly PostProcessor _postProcessor;
	private readonly Texture2D _fallbackTexture;

	private Matrix _viewProjection;
	private Vector3 _cameraPosition;
	private bool _frameOpen;

	private RenderTargetBinding[] _savedRenderTargets = [];
	private Viewport _savedViewport;
	private BlendState _savedBlendState = BlendState.Opaque;
	private DepthStencilState _savedDepthStencilState = DepthStencilState.Default;
	private RasterizerState _savedRasterizerState = RasterizerState.CullCounterClockwise;
	private SamplerState _savedSamplerState = SamplerState.LinearWrap;
	private Rectangle _savedScissorRectangle;

	public Renderer(GraphicsDevice device, ContentManager content)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(content);

		_device = device;
		_postProcessor = new PostProcessor(device, content);

		_fallbackTexture = new Texture2D(device, 1, 1);
		_fallbackTexture.SetData([Color.White]);
	}

	/// <summary>
	/// Number of draw calls issued in the current frame, including post-processing
	/// passes. Reset by <see cref="BeginFrame"/>.
	/// </summary>
	public int DrawCalls { get; private set; }

	/// <summary>
	/// Number of triangles submitted through <see cref="DrawMeshPart"/> in the
	/// current frame. Reset by <see cref="BeginFrame"/>.
	/// </summary>
	public int Triangles { get; private set; }

	/// <summary>
	/// Saves device state and starts a 3D frame. Must be paired with
	/// <see cref="EndFrame"/>.
	/// </summary>
	public void BeginFrame(Matrix view, Matrix projection, ShaderSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		if (_frameOpen)
		{
			throw new InvalidOperationException("A renderer frame is already open.");
		}

		DrawCalls = 0;
		Triangles = 0;
		_frameOpen = true;
		_viewProjection = view * projection;
		_cameraPosition = Matrix.Invert(view).Translation;

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
		_postProcessor.SetSettings(settings);
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
	/// Draws one mesh part with the supplied effect and world transform. Skin
	/// transforms and any other effect state must be set by the caller before this
	/// call; pass <see cref="Matrix.Identity"/> for skinned parts.
	/// </summary>
	public void DrawMeshPart(
		DrModelMeshPart part,
		PbrEffect effect,
		Matrix transform)
	{
		ArgumentNullException.ThrowIfNull(part);
		ArgumentNullException.ThrowIfNull(effect);
		EnsureFrameOpen();

		if (part.VertexBuffer is null || part.IndexBuffer is null)
		{
			return;
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

		effect.SetWorld(transform);
		effect.SetWorldViewProjection(transform * _viewProjection);
		effect.SetCameraPosition(_cameraPosition);
		effect.SetMaterial(material, _fallbackTexture);

		_device.SetVertexBuffer(part.VertexBuffer);
		_device.Indices = part.IndexBuffer;

		foreach (EffectPass pass in effect.Effect.CurrentTechnique.Passes)
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

	/// <summary>
	/// Resolves the scene target with post-processing and restores device state.
	/// </summary>
	public void EndFrame()
	{
		EnsureFrameOpen();

		_frameOpen = false;

		try
		{
			_postProcessor.Draw();
			DrawCalls += _postProcessor.DrawCalls;
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

	public void Dispose()
	{
		_fallbackTexture.Dispose();
		_postProcessor.Dispose();
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
