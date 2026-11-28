#define MAX_DIRECTIONAL_LIGHTS 4
#define IBL_PREFILTERED_MIP_COUNT 6

float4x4 World;
float4x4 WorldViewProjection;
float3 CameraPosition;

// xyz = unit vector toward the light, w = intensity.
float4 DirectionalLightDirections[MAX_DIRECTIONAL_LIGHTS];
// rgb = linear light color.
float4 DirectionalLightColors[MAX_DIRECTIONAL_LIGHTS];
int DirectionalLightCount;

float4 BaseColorFactor;
float MetallicFactor;
float RoughnessFactor;
float HasBaseColorTexture;
float HasMetallicRoughnessTexture;
float HasNormalTexture;
float NormalScale;
float HasOcclusionTexture;
float OcclusionStrength;
float HasEmissiveTexture;
float3 EmissiveFactor;
float EmissiveStrength;
float AlphaCutoff;
float UseBaseColorAlpha;
float UseUV1BaseColor;
float UseUV1MetallicRoughness;
float UseUV1Normal;
float UseUV1Occlusion;
float UseUV1Emissive;

// xy = offset, zw = scale.
float4 BaseColorTransform;
float BaseColorRotation;
float4 MetallicRoughnessTransform;
float MetallicRoughnessRotation;
float4 NormalTransform;
float NormalRotation;
float4 OcclusionTransform;
float OcclusionRotation;
float4 EmissiveTransform;
float EmissiveRotation;

// x = wrap U (0 repeat, 1 clamp, 2 mirror), y = wrap V, z = point filtering.
float4 BaseColorSamplerInfo;
float2 BaseColorTextureSize;
float4 MetallicRoughnessSamplerInfo;
float2 MetallicRoughnessTextureSize;
float4 NormalSamplerInfo;
float2 NormalTextureSize;
float4 OcclusionSamplerInfo;
float2 OcclusionTextureSize;
float4 EmissiveSamplerInfo;
float2 EmissiveTextureSize;

int DebugView;

Texture2D BaseColorTexture;
sampler2D BaseColorSampler = sampler_state
{
	Texture = <BaseColorTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

Texture2D MetallicRoughnessTexture;
sampler2D MetallicRoughnessSampler = sampler_state
{
	Texture = <MetallicRoughnessTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

Texture2D NormalTexture;
sampler2D NormalSampler = sampler_state
{
	Texture = <NormalTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

Texture2D OcclusionTexture;
sampler2D OcclusionSampler = sampler_state
{
	Texture = <OcclusionTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

Texture2D EmissiveTexture;
sampler2D EmissiveSampler = sampler_state
{
	Texture = <EmissiveTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

TextureCube IrradianceTexture;
samplerCUBE IrradianceSampler = sampler_state
{
	Texture = <IrradianceTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

TextureCube PrefilteredSpecularTexture;
samplerCUBE PrefilteredSpecularSampler = sampler_state
{
	Texture = <PrefilteredSpecularTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
};

Texture2D BrdfLutTexture;
sampler2D BrdfLutSampler = sampler_state
{
	Texture = <BrdfLutTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

float3 FresnelSchlickRoughness(float cosTheta, float3 f0, float roughness)
{
	float3 f90 = max(
		float3(1.0 - roughness, 1.0 - roughness, 1.0 - roughness),
		f0);
	return f0 + (f90 - f0) * pow(1.0 - cosTheta, 5.0);
}

float2 ApplyTextureTransform(float2 uv, float4 offsetScale, float rotation)
{
	// glTF applies scale, then rotation, then translation:
	// uv' = R * (S * uv) + offset, with R using the glTF sign convention.
	float sinRotation = sin(rotation);
	float cosRotation = cos(rotation);
	float2 scaled = uv * offsetScale.zw;
	float2 rotated = float2(
		scaled.x * cosRotation + scaled.y * sinRotation,
		-scaled.x * sinRotation + scaled.y * cosRotation);

	return rotated + offsetScale.xy;
}

float WrapTextureCoordinate(float coordinate, float wrapMode)
{
	if (wrapMode < 0.5)
	{
		// The sampler_state already uses Wrap. Leaving the coordinate unchanged
		// preserves UV derivatives and avoids a mip-level seam at integer edges.
		return coordinate;
	}
	else if (wrapMode < 1.5)
	{
		return saturate(coordinate);
	}

	float mirrored = frac(coordinate * 0.5);
	return 1.0 - abs(mirrored * 2.0 - 1.0);
}

float2 ApplySamplerSettings(
	float2 uv,
	float4 samplerInfo,
	float2 textureSize)
{
	uv.x = WrapTextureCoordinate(uv.x, samplerInfo.x);
	uv.y = WrapTextureCoordinate(uv.y, samplerInfo.y);

	if (samplerInfo.z > 0.5)
	{
		float2 texelCenters = (floor(uv * textureSize) + 0.5) / textureSize;
		float2 minUV = 0.5 / textureSize;
		float2 maxUV = (textureSize - 0.5) / textureSize;

		if (samplerInfo.x > 0.5)
		{
			texelCenters.x = clamp(texelCenters.x, minUV.x, maxUV.x);
		}

		if (samplerInfo.y > 0.5)
		{
			texelCenters.y = clamp(texelCenters.y, minUV.y, maxUV.y);
		}

		uv = texelCenters;
	}

	return uv;
}

float4 GetBaseColor(
	float2 texCoord,
	float2 texCoord1,
	float4 vertexColor)
{
	float2 baseColorUV = UseUV1BaseColor > 0.5 ? texCoord1 : texCoord;
#ifdef TEXTURE_SETTINGS
	baseColorUV = ApplyTextureTransform(
		baseColorUV,
		BaseColorTransform,
		BaseColorRotation);
	baseColorUV = ApplySamplerSettings(
		baseColorUV,
		BaseColorSamplerInfo,
		BaseColorTextureSize);
#endif

	float4 baseColor = BaseColorFactor;

	if (HasBaseColorTexture > 0.5)
	{
		float4 textureColor = tex2D(BaseColorSampler, baseColorUV);
		baseColor.rgb *= pow(saturate(textureColor.rgb), 2.2);
		baseColor.a *= textureColor.a;
	}

	baseColor.rgb *= vertexColor.rgb;
	baseColor.a *= vertexColor.a;
	return baseColor;
}

float4 ShadeSurface(
	float3 worldPosition,
	float3 normal,
	float2 texCoord,
	float2 texCoord1,
	float facing,
	float4 vertexColor)
{
	float4 baseColor = GetBaseColor(texCoord, texCoord1, vertexColor);

	if (baseColor.a < AlphaCutoff)
	{
		discard;
	}

	float2 metallicRoughnessUV = UseUV1MetallicRoughness > 0.5 ? texCoord1 : texCoord;
	float2 occlusionUV = UseUV1Occlusion > 0.5 ? texCoord1 : texCoord;
	float2 emissiveUV = UseUV1Emissive > 0.5 ? texCoord1 : texCoord;

#ifdef TEXTURE_SETTINGS
	metallicRoughnessUV = ApplyTextureTransform(
		metallicRoughnessUV,
		MetallicRoughnessTransform,
		MetallicRoughnessRotation);
	occlusionUV = ApplyTextureTransform(
		occlusionUV,
		OcclusionTransform,
		OcclusionRotation);
	emissiveUV = ApplyTextureTransform(
		emissiveUV,
		EmissiveTransform,
		EmissiveRotation);
	metallicRoughnessUV = ApplySamplerSettings(
		metallicRoughnessUV,
		MetallicRoughnessSamplerInfo,
		MetallicRoughnessTextureSize);
	occlusionUV = ApplySamplerSettings(
		occlusionUV,
		OcclusionSamplerInfo,
		OcclusionTextureSize);
	emissiveUV = ApplySamplerSettings(
		emissiveUV,
		EmissiveSamplerInfo,
		EmissiveTextureSize);
#endif

	float metallic = MetallicFactor;
	float roughness = RoughnessFactor;

	if (HasMetallicRoughnessTexture > 0.5)
	{
		float4 metallicRoughness = tex2Dlod(
			MetallicRoughnessSampler,
			float4(metallicRoughnessUV, 0.0, 2.0));
		roughness *= metallicRoughness.g;
		metallic *= metallicRoughness.b;
	}

	metallic = saturate(metallic);
	roughness = clamp(roughness, 0.04, 1.0);

	normal = normalize(normal);

	if (facing < 0.0)
	{
		normal = -normal;
	}

	float occlusion = 1.0;

	if (HasOcclusionTexture > 0.5)
	{
		float sampledOcclusion = tex2D(OcclusionSampler, occlusionUV).r;
		occlusion = lerp(1.0, sampledOcclusion, OcclusionStrength);
	}

	float3 emissive = EmissiveFactor * EmissiveStrength;

	if (HasEmissiveTexture > 0.5)
	{
		float3 emissiveTexture = tex2D(EmissiveSampler, emissiveUV).rgb;
		emissive *= pow(saturate(emissiveTexture), 2.2);
	}

	if (DebugView == 1)
	{
		return float4(baseColor.rgb, 1.0);
	}
	else if (DebugView == 2)
	{
		return float4(metallic, metallic, metallic, 1.0);
	}
	else if (DebugView == 3)
	{
		return float4(roughness, roughness, roughness, 1.0);
	}
	else if (DebugView == 4)
	{
		return float4(normal * 0.5 + 0.5, 1.0);
	}
	else if (DebugView == 5)
	{
		return float4(occlusion, occlusion, occlusion, 1.0);
	}
	else if (DebugView == 6)
	{
		return float4(emissive, 1.0);
	}

	float3 view = normalize(CameraPosition - worldPosition);
	float nDotV = saturate(dot(normal, view));

	float alpha = roughness * roughness;
	float alphaSquared = alpha * alpha;
	float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
	float3 f0 = lerp(float3(0.04, 0.04, 0.04), baseColor.rgb, metallic);

	float3 directDiffuse = 0.0;
	float3 directSpecular = 0.0;

	for (int i = 0; i < MAX_DIRECTIONAL_LIGHTS; ++i)
	{
		if (i >= DirectionalLightCount)
		{
			break;
		}

		float3 light = normalize(DirectionalLightDirections[i].xyz);
		float3 lightColor = DirectionalLightColors[i].rgb
			* DirectionalLightDirections[i].w;
		float3 halfVector = normalize(view + light);

		float nDotL = saturate(dot(normal, light));
		float nDotH = saturate(dot(normal, halfVector));
		float vDotH = saturate(dot(view, halfVector));

		float denominator = nDotH * nDotH * (alphaSquared - 1.0) + 1.0;
		float distribution = alphaSquared
			/ (3.14159265 * denominator * denominator);
		float geometry = (nDotL / (nDotL * (1.0 - k) + k))
			* (nDotV / (nDotV * (1.0 - k) + k));
		float3 fresnel = f0 + (1.0 - f0) * pow(1.0 - vDotH, 5.0);

		float3 specular = distribution * geometry * fresnel
			/ max(4.0 * nDotL * nDotV, 0.001);
		float3 diffuse = (1.0 - fresnel) * (1.0 - metallic)
			* baseColor.rgb / 3.14159265;

		directDiffuse += diffuse * nDotL * lightColor;
		directSpecular += specular * nDotL * lightColor;
	}

	float3 fresnelIbl = FresnelSchlickRoughness(nDotV, f0, roughness);
	float3 diffuseWeight = (1.0 - fresnelIbl) * (1.0 - metallic);

	float3 irradiance = texCUBE(IrradianceSampler, normal).rgb;
	float3 iblDiffuse = irradiance * baseColor.rgb;

	float3 reflection = reflect(-view, normal);
	float mipLevel = roughness * (IBL_PREFILTERED_MIP_COUNT - 1);
	float3 prefiltered = texCUBElod(
		PrefilteredSpecularSampler,
		float4(reflection, mipLevel)).rgb;
	float2 brdf = tex2D(BrdfLutSampler, float2(nDotV, roughness)).rg;
	float3 iblSpecular = prefiltered * (fresnelIbl * brdf.x + brdf.y);

	if (DebugView == 7)
	{
		return float4(directDiffuse, 1.0);
	}
	else if (DebugView == 8)
	{
		return float4(directSpecular, 1.0);
	}
	else if (DebugView == 9)
	{
		return float4(iblDiffuse * diffuseWeight, 1.0);
	}
	else if (DebugView == 10)
	{
		return float4(iblSpecular, 1.0);
	}

	float3 ambient = (iblDiffuse * diffuseWeight + iblSpecular) * occlusion;
	float3 color = directDiffuse + directSpecular + ambient + emissive;

	float outputAlpha = UseBaseColorAlpha > 0.5 ? baseColor.a : 1.0;
	return float4(color, outputAlpha);
}
