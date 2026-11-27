using DigitalRiseModel.Viewer.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics.PostProcessing;

/// <summary>
/// Owns the HDR scene target and resolves it to the back buffer with exposure,
/// ACES tone mapping and gamma output.
/// </summary>
internal sealed class PostProcessor : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly Effect _effect;
	private readonly VertexBuffer _vertexBuffer;
	private readonly EffectParameter _exposure;
	private readonly EffectParameter _debugView;
	private readonly EffectParameter? _sceneTexture;

	private RenderTarget2D? _sceneTarget;
	private int _width;
	private int _height;

	public PostProcessor(GraphicsDevice device, ContentManager content)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(content);

		_device = device;
		_effect = content.Load<Effect>("Shaders/PostProcess");
		_exposure = _effect.Parameters["Exposure"];
		_debugView = _effect.Parameters["DebugView"];
		_sceneTexture = FindTextureParameter(_effect, "SceneTexture");

		_vertexBuffer = new VertexBuffer(
			device,
			VertexPositionTexture.VertexDeclaration,
			4,
			BufferUsage.WriteOnly);
		_vertexBuffer.SetData(
		[
			new VertexPositionTexture(new Vector3(-1f, 1f, 0f), new Vector2(0f, 0f)),
			new VertexPositionTexture(new Vector3(1f, 1f, 0f), new Vector2(1f, 0f)),
			new VertexPositionTexture(new Vector3(-1f, -1f, 0f), new Vector2(0f, 1f)),
			new VertexPositionTexture(new Vector3(1f, -1f, 0f), new Vector2(1f, 1f))
		]);
	}

	public RenderTarget2D SceneTarget =>
		_sceneTarget ?? throw new InvalidOperationException(
			"The post processor has no scene target; call EnsureSize first.");

	public void EnsureSize(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(width),
				"The scene target size must be positive.");
		}

		if (_sceneTarget is not null && _width == width && _height == height)
		{
			return;
		}

		_sceneTarget?.Dispose();
		_sceneTarget = new RenderTarget2D(
			_device,
			width,
			height,
			false,
			SurfaceFormat.HalfVector4,
			DepthFormat.Depth24,
			0,
			RenderTargetUsage.PreserveContents);
		_width = width;
		_height = height;
	}

	public void SetSettings(ShaderSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		_exposure.SetValue(settings.Exposure);
		_debugView.SetValue((int)settings.DebugView);
	}

	/// <summary>
	/// Resolves the scene target to the back buffer. The caller owns and restores
	/// device state and render targets.
	/// </summary>
	public void Draw()
	{
		_device.SetRenderTarget(null);
		_device.Viewport = new Viewport(0, 0, _width, _height);
		_device.BlendState = BlendState.Opaque;
		_device.DepthStencilState = DepthStencilState.None;
		_device.RasterizerState = RasterizerState.CullNone;
		_device.SamplerStates[0] = SamplerState.PointClamp;
		_device.SetVertexBuffer(_vertexBuffer);

		_sceneTexture?.SetValue(SceneTarget);

		foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
		}
	}

	public void Dispose()
	{
		_sceneTarget?.Dispose();
		_vertexBuffer.Dispose();
	}

	private static EffectParameter? FindTextureParameter(
		Effect effect,
		string textureName)
	{
		foreach (EffectParameter parameter in effect.Parameters)
		{
			if (parameter.ParameterType == EffectParameterType.Texture2D
				&& parameter.Name.EndsWith(textureName, StringComparison.Ordinal))
			{
				return parameter;
			}
		}

		return null;
	}
}
