using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics.Shaders;

/// <summary>
/// Wrapper for the custom PBR effect with cached parameter references.
/// </summary>
internal sealed class PbrEffect
{
	private readonly Effect _effect;
	private readonly EffectParameter _worldViewProjection;
	private readonly EffectParameter _baseColorFactor;

	public PbrEffect(ContentManager content)
	{
		ArgumentNullException.ThrowIfNull(content);

		_effect = content.Load<Effect>("Shaders/Pbr");
		_worldViewProjection = _effect.Parameters["WorldViewProjection"];
		_baseColorFactor = _effect.Parameters["BaseColorFactor"];
	}

	public Effect Effect => _effect;

	public void SetWorldViewProjection(Matrix matrix)
	{
		_worldViewProjection.SetValue(matrix);
	}

	public void SetBaseColor(Vector4 color)
	{
		_baseColorFactor.SetValue(color);
	}
}
