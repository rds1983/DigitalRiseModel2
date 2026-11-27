using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics.Shaders;

/// <summary>
/// Minimal custom shader smoke test: draws a full-screen color.
/// </summary>
internal sealed class FullscreenColorEffect : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly Effect _effect;
	private readonly VertexBuffer _vertexBuffer;

	public FullscreenColorEffect(GraphicsDevice device, ContentManager content)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(content);

		_device = device;
		_effect = content.Load<Effect>("Shaders/FullscreenColor");

		_vertexBuffer = new VertexBuffer(
			device,
			VertexPositionColor.VertexDeclaration,
			6,
			BufferUsage.WriteOnly);

		var color = new Color(32, 64, 128);
		_vertexBuffer.SetData(
			[
				new VertexPositionColor(new Vector3(-1, -1, 0), color),
				new VertexPositionColor(new Vector3(-1, 1, 0), color),
				new VertexPositionColor(new Vector3(1, 1, 0), color),
				new VertexPositionColor(new Vector3(-1, -1, 0), color),
				new VertexPositionColor(new Vector3(1, 1, 0), color),
				new VertexPositionColor(new Vector3(1, -1, 0), color)
			]);
	}

	public void Draw()
	{
		BlendState blendState = _device.BlendState;
		DepthStencilState depthStencilState = _device.DepthStencilState;
		RasterizerState rasterizerState = _device.RasterizerState;

		try
		{
			_device.BlendState = BlendState.Opaque;
			_device.DepthStencilState = DepthStencilState.None;
			_device.RasterizerState = RasterizerState.CullNone;
			_device.SetVertexBuffer(_vertexBuffer);

			foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
			}
		}
		finally
		{
			_device.BlendState = blendState;
			_device.DepthStencilState = depthStencilState;
			_device.RasterizerState = rasterizerState;
		}
	}

	public void Dispose()
	{
		_vertexBuffer.Dispose();
	}
}
