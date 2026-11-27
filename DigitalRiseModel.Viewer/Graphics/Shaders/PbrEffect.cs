using DigitalRiseModel;
using DigitalRiseModel.Viewer.Graphics.Lighting;
using Microsoft.Xna.Framework;
using DirectionalLight = DigitalRiseModel.Viewer.Graphics.Lighting.DirectionalLight;
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
	private readonly EffectParameter _emissiveStrength;
	private readonly EffectParameter _alphaCutoff;
	private readonly EffectParameter _useBaseColorAlpha;
	private readonly EffectParameter _useUV1BaseColor;
	private readonly EffectParameter _useUV1MetallicRoughness;
	private readonly EffectParameter? _useUV1Normal;
	private readonly EffectParameter _useUV1Occlusion;
	private readonly EffectParameter _useUV1Emissive;
	private readonly EffectParameter _debugView;
	private readonly EffectParameter? _baseColorTexture;
	private readonly EffectParameter? _metallicRoughnessTexture;
	private readonly EffectParameter? _normalTexture;
	private readonly EffectParameter? _occlusionTexture;
	private readonly EffectParameter? _emissiveTexture;
	private readonly EffectParameter _directionalLightDirections;
	private readonly EffectParameter _directionalLightColors;
	private readonly EffectParameter _directionalLightCount;
	private readonly EffectParameter? _irradianceTexture;
	private readonly EffectParameter? _prefilteredSpecularTexture;
	private readonly EffectParameter? _brdfLutTexture;
	private readonly Vector4[] _packedLightDirections =
		new Vector4[LightSet.MaxDirectionalLights];
	private readonly Vector4[] _packedLightColors =
		new Vector4[LightSet.MaxDirectionalLights];

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
		_emissiveStrength = _effect.Parameters["EmissiveStrength"];
		_alphaCutoff = _effect.Parameters["AlphaCutoff"];
		_useBaseColorAlpha = _effect.Parameters["UseBaseColorAlpha"];
		_useUV1BaseColor = _effect.Parameters["UseUV1BaseColor"];
		_useUV1MetallicRoughness = _effect.Parameters["UseUV1MetallicRoughness"];
		_useUV1Normal = _effect.Parameters["UseUV1Normal"];
		_useUV1Occlusion = _effect.Parameters["UseUV1Occlusion"];
		_useUV1Emissive = _effect.Parameters["UseUV1Emissive"];
		_debugView = _effect.Parameters["DebugView"];
		_baseColorTexture = FindTextureParameter(_effect, "BaseColorTexture");
		_metallicRoughnessTexture = FindTextureParameter(_effect, "MetallicRoughnessTexture");
		_normalTexture = FindTextureParameter(_effect, "NormalTexture");
		_occlusionTexture = FindTextureParameter(_effect, "OcclusionTexture");
		_emissiveTexture = FindTextureParameter(_effect, "EmissiveTexture");
		_directionalLightDirections = _effect.Parameters["DirectionalLightDirections"];
		_directionalLightColors = _effect.Parameters["DirectionalLightColors"];
		_directionalLightCount = _effect.Parameters["DirectionalLightCount"];
		_irradianceTexture = FindTextureParameter(_effect, "IrradianceTexture");
		_prefilteredSpecularTexture = FindTextureParameter(
			_effect,
			"PrefilteredSpecularTexture");
		_brdfLutTexture = FindTextureParameter(_effect, "BrdfLutTexture");
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
	}

	public void SetLights(LightSet lights)
	{
		ArgumentNullException.ThrowIfNull(lights);

		int count = 0;

		foreach (DirectionalLight light in lights.DirectionalLights)
		{
			if (count >= LightSet.MaxDirectionalLights)
			{
				break;
			}

			if (light.Intensity <= 0f)
			{
				continue;
			}

			Vector3 direction = light.Direction;

			if (direction.LengthSquared() < 1e-8f)
			{
				continue;
			}

			direction.Normalize();
			_packedLightDirections[count] = new Vector4(direction, light.Intensity);
			_packedLightColors[count] = new Vector4(light.Color, 0f);
			++count;
		}

		for (int i = count; i < LightSet.MaxDirectionalLights; ++i)
		{
			_packedLightDirections[i] = Vector4.Zero;
			_packedLightColors[i] = Vector4.Zero;
		}

		_directionalLightDirections.SetValue(_packedLightDirections);
		_directionalLightColors.SetValue(_packedLightColors);
		_directionalLightCount.SetValue(count);
	}

	public void SetIbl(IblResources ibl)
	{
		ArgumentNullException.ThrowIfNull(ibl);

		_irradianceTexture?.SetValue(ibl.Irradiance);
		_prefilteredSpecularTexture?.SetValue(ibl.PrefilteredSpecular);
		_brdfLutTexture?.SetValue(ibl.BrdfLut);
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
		_emissiveStrength.SetValue(material?.EmissiveStrength ?? 1f);
		_alphaCutoff.SetValue(material?.AlphaCutoff ?? 0.5f);
		_useBaseColorAlpha.SetValue(
			material?.AlphaMode == DrAlphaMode.Blend ? 1f : 0f);
		_useUV1BaseColor.SetValue(material?.BaseColorTexture is { TexCoord: 1 } ? 1f : 0f);
		_useUV1MetallicRoughness.SetValue(
			material?.MetallicRoughnessTexture is { TexCoord: 1 } ? 1f : 0f);
		_useUV1Normal?.SetValue(material?.NormalTexture is { TexCoord: 1 } ? 1f : 0f);
		_useUV1Occlusion.SetValue(material?.OcclusionTexture is { TexCoord: 1 } ? 1f : 0f);
		_useUV1Emissive.SetValue(material?.EmissiveTexture is { TexCoord: 1 } ? 1f : 0f);
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
