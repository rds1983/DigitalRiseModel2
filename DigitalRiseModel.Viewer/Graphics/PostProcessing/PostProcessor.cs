using DigitalRiseModel.Viewer.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics.PostProcessing;

/// <summary>
/// Owns the HDR scene target, optional bloom targets and the resolve pass with
/// exposure, ACES tone mapping and gamma output.
/// </summary>
internal sealed class PostProcessor : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly Effect _effect;
	private readonly VertexBuffer _vertexBuffer;
	private readonly EffectParameter _exposure;
	private readonly EffectParameter _bloomIntensity;
	private readonly EffectParameter _texelSize;
	private readonly EffectParameter _debugView;
	private readonly EffectParameter? _sceneTexture;
	private readonly EffectParameter? _bloomTexture;

	private RenderTarget2D? _sceneTarget;
	private RenderTarget2D? _bloomTargetA;
	private RenderTarget2D? _bloomTargetB;
	private int _width;
	private int _height;
	private float _bloomIntensityValue;

	public PostProcessor(GraphicsDevice device, ContentManager content)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(content);

		_device = device;
		_effect = content.Load<Effect>("Shaders/PostProcess");
		_exposure = _effect.Parameters["Exposure"];
		_bloomIntensity = _effect.Parameters["BloomIntensity"];
		_texelSize = _effect.Parameters["TexelSize"];
		_debugView = _effect.Parameters["DebugView"];
		_sceneTexture = FindTextureParameter(_effect, "SceneTexture");
		_bloomTexture = FindTextureParameter(_effect, "BloomTexture");

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

		DisposeTargets();

		_sceneTarget = CreateTarget(width, height, DepthFormat.Depth24);
		int bloomWidth = Math.Max(1, width / 2);
		int bloomHeight = Math.Max(1, height / 2);
		_bloomTargetA = CreateTarget(bloomWidth, bloomHeight, DepthFormat.None);
		_bloomTargetB = CreateTarget(bloomWidth, bloomHeight, DepthFormat.None);
		_width = width;
		_height = height;
	}

	public void SetSettings(ShaderSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		_exposure.SetValue(settings.Exposure);
		_bloomIntensity.SetValue(settings.BloomIntensity);
		_debugView.SetValue((int)settings.DebugView);
		_bloomIntensityValue = settings.BloomIntensity;
	}

	/// <summary>
	/// Resolves the scene target to the back buffer. The caller owns and restores
	/// device state and render targets.
	/// </summary>
	public void Draw()
	{
		_sceneTexture?.SetValue(SceneTarget);
		_bloomTexture?.SetValue(_bloomTargetA ?? SceneTarget);

		if (_bloomIntensityValue > 0f && _bloomTargetA is not null
			&& _bloomTargetB is not null)
		{
			DrawPass(_bloomTargetA, "BrightPass", SceneTarget, Vector2.Zero);
			DrawPass(
				_bloomTargetB,
				"BlurHorizontal",
				_bloomTargetA,
				GetTexelSize(_bloomTargetA));
			DrawPass(
				_bloomTargetA,
				"BlurVertical",
				_bloomTargetB,
				GetTexelSize(_bloomTargetA));
			DrawPass(
				_bloomTargetB,
				"BlurHorizontal",
				_bloomTargetA,
				GetTexelSize(_bloomTargetA));
			DrawPass(
				_bloomTargetA,
				"BlurVertical",
				_bloomTargetB,
				GetTexelSize(_bloomTargetA));
			_bloomTexture?.SetValue(_bloomTargetA);
		}

		DrawPass(null, "Composite", SceneTarget, Vector2.Zero);
	}

	public void Dispose()
	{
		DisposeTargets();
		_vertexBuffer.Dispose();
	}

	private void DrawPass(
		RenderTarget2D? target,
		string technique,
		Texture2D source,
		Vector2 texelSize)
	{
		_device.SetRenderTarget(target);
		_device.Viewport = target is null
			? new Viewport(0, 0, _width, _height)
			: new Viewport(0, 0, target.Width, target.Height);
		_device.BlendState = BlendState.Opaque;
		_device.DepthStencilState = DepthStencilState.None;
		_device.RasterizerState = RasterizerState.CullNone;
		_device.SamplerStates[0] = SamplerState.LinearClamp;
		_device.SetVertexBuffer(_vertexBuffer);

		_sceneTexture?.SetValue(source);
		_texelSize.SetValue(texelSize);

		_effect.CurrentTechnique = _effect.Techniques[technique];

		foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
		}
	}

	private RenderTarget2D CreateTarget(
		int width,
		int height,
		DepthFormat depthFormat)
	{
		return new RenderTarget2D(
			_device,
			width,
			height,
			false,
			SurfaceFormat.HalfVector4,
			depthFormat,
			0,
			RenderTargetUsage.PreserveContents);
	}

	private static Vector2 GetTexelSize(Texture2D texture)
	{
		return new Vector2(1f / texture.Width, 1f / texture.Height);
	}

	private void DisposeTargets()
	{
		_sceneTarget?.Dispose();
		_bloomTargetA?.Dispose();
		_bloomTargetB?.Dispose();
		_sceneTarget = null;
		_bloomTargetA = null;
		_bloomTargetB = null;
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
