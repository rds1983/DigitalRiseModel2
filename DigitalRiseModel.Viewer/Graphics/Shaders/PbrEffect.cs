using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DigitalRiseModel.Viewer.Graphics.Shaders;

/// <summary>
/// Wrapper for the custom PBR effects with cached parameter references.
/// </summary>
internal sealed class PbrEffect
{
	private readonly Effect _effect;
	private readonly EffectParameter? _world;
	private readonly EffectParameter _worldViewProjection;
	private readonly EffectParameter _cameraPosition;
	private readonly EffectParameter? _bones;
	private readonly EffectParameter _baseColorFactor;
	private readonly EffectParameter _metallicFactor;
	private readonly EffectParameter _roughnessFactor;
	private readonly EffectParameter _hasBaseColorTexture;
	private readonly EffectParameter _hasMetallicRoughnessTexture;
	private readonly EffectParameter? _hasNormalTexture;
	private readonly EffectParameter? _normalScale;
	private readonly EffectParameter _hasOcclusionTexture;
	private readonly EffectParameter _occlusionStrength;
	private readonly EffectParameter _hasEmissiveTexture;
	private readonly EffectParameter _emissiveFactor;
	private readonly EffectParameter _alphaCutoff;
	private readonly EffectParameter _debugView;
	private readonly EffectParameter _exposure;
	private readonly EffectParameter? _baseColorTexture;
	private readonly EffectParameter? _metallicRoughnessTexture;
	private readonly EffectParameter? _normalTexture;
	private readonly EffectParameter? _occlusionTexture;
	private readonly EffectParameter? _emissiveTexture;

	public PbrEffect(ContentManager content, string effectName)
	{
		ArgumentNullException.ThrowIfNull(content);

		_effect = content.Load<Effect>(effectName);
		_world = _effect.Parameters["World"];
		_worldViewProjection = _effect.Parameters["WorldViewProjection"];
		_cameraPosition = _effect.Parameters["CameraPosition"];
		_bones = _effect.Parameters["Bones"];
		_baseColorFactor = _effect.Parameters["BaseColorFactor"];
		_metallicFactor = _effect.Parameters["MetallicFactor"];
		_roughnessFactor = _effect.Parameters["RoughnessFactor"];
		_hasBaseColorTexture = _effect.Parameters["HasBaseColorTexture"];
		_hasMetallicRoughnessTexture = _effect.Parameters["HasMetallicRoughnessTexture"];
		_hasNormalTexture = _effect.Parameters["HasNormalTexture"];
		_normalScale = _effect.Parameters["NormalScale"];
		_hasOcclusionTexture = _effect.Parameters["HasOcclusionTexture"];
		_occlusionStrength = _effect.Parameters["OcclusionStrength"];
		_hasEmissiveTexture = _effect.Parameters["HasEmissiveTexture"];
		_emissiveFactor = _effect.Parameters["EmissiveFactor"];
		_alphaCutoff = _effect.Parameters["AlphaCutoff"];
		_debugView = _effect.Parameters["DebugView"];
		_exposure = _effect.Parameters["Exposure"];
		_baseColorTexture = FindTextureParameter(_effect, "BaseColorTexture");
		_metallicRoughnessTexture = FindTextureParameter(_effect, "MetallicRoughnessTexture");
		_normalTexture = FindTextureParameter(_effect, "NormalTexture");
		_occlusionTexture = FindTextureParameter(_effect, "OcclusionTexture");
		_emissiveTexture = FindTextureParameter(_effect, "EmissiveTexture");
	}

	public Effect Effect => _effect;

	public void SetWorld(Matrix world)
	{
		_world?.SetValue(world);
	}

	public void SetWorldViewProjection(Matrix matrix)
	{
		_worldViewProjection.SetValue(matrix);
	}

	public void SetCameraPosition(Vector3 position)
	{
		_cameraPosition.SetValue(position);
	}

	public void SetSkinTransforms(Matrix[] transforms)
	{
		_bones?.SetValue(transforms);
	}

	public void SetSettings(ShaderSettings settings)
	{
		_debugView.SetValue((int)settings.DebugView);
		_exposure.SetValue(settings.Exposure);
	}

	public void SetMaterial(DrMaterial? material, Texture2D fallbackTexture)
	{
		Texture2D? baseColorTexture = material?.BaseColorTexture?.Texture;
		Texture2D? metallicRoughnessTexture = material?.MetallicRoughnessTexture?.Texture;
		Texture2D? normalTexture = material?.NormalTexture?.Texture;
		Texture2D? occlusionTexture = material?.OcclusionTexture?.Texture;
		Texture2D? emissiveTexture = material?.EmissiveTexture?.Texture;

		_baseColorFactor.SetValue(material?.BaseColorFactor ?? Vector4.One);
		_metallicFactor.SetValue(material?.MetallicFactor ?? 1f);
		_roughnessFactor.SetValue(material?.RoughnessFactor ?? 1f);
		_hasBaseColorTexture.SetValue(baseColorTexture is not null ? 1f : 0f);
		_hasMetallicRoughnessTexture.SetValue(metallicRoughnessTexture is not null ? 1f : 0f);
		_hasNormalTexture?.SetValue(normalTexture is not null ? 1f : 0f);
		_normalScale?.SetValue(material?.NormalTexture?.Scale ?? 1f);
		_hasOcclusionTexture.SetValue(occlusionTexture is not null ? 1f : 0f);
		_occlusionStrength.SetValue(material?.OcclusionTexture?.Strength ?? 1f);
		_hasEmissiveTexture.SetValue(emissiveTexture is not null ? 1f : 0f);
		_emissiveFactor.SetValue(material?.EmissiveFactor ?? Vector3.Zero);
		_alphaCutoff.SetValue(material?.AlphaCutoff ?? 0.5f);
		_baseColorTexture?.SetValue(baseColorTexture ?? fallbackTexture);
		_metallicRoughnessTexture?.SetValue(metallicRoughnessTexture ?? fallbackTexture);
		_normalTexture?.SetValue(normalTexture ?? fallbackTexture);
		_occlusionTexture?.SetValue(occlusionTexture ?? fallbackTexture);
		_emissiveTexture?.SetValue(emissiveTexture ?? fallbackTexture);
	}

	private static EffectParameter? FindTextureParameter(Effect effect, string textureName)
	{
		// MonoGame combines a Texture2D with the SamplerState used to sample it,
		// producing a name like "BaseColorSampler+BaseColorTexture".
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
