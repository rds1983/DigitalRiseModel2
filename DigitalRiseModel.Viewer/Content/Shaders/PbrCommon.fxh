#define MAX_DIRECTIONAL_LIGHTS 4
#define IBL_PREFILTERED_MIP_COUNT 7
#define INV_TWO_PI 0.15915494309

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

Texture2D IrradianceTexture;
sampler2D IrradianceSampler = sampler_state
{
	Texture = <IrradianceTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Clamp;
};

Texture2D PrefilteredSpecularTexture;
sampler2D PrefilteredSpecularSampler = sampler_state
{
	Texture = <PrefilteredSpecularTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Clamp;
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

float FastAtan2(float y, float x)
{
	float absY = abs(y);
	float absX = abs(x);
	float a = min(absX, absY) / max(max(absX, absY), 1e-6);
	float s = a * a;
	float r = ((-0.0464964749 * s + 0.15931422) * s - 0.327622764) * s * a + a;

	if (absY > absX)
	{
		r = 1.57079637 - r;
	}

	if (x < 0.0)
	{
		r = 3.14159274 - r;
	}

	if (y < 0.0)
	{
		r = -r;
	}

	return r;
}

// Keep this inverse mapping in sync with IblResources.EquirectUVToDirection.
float2 DirectionToEquirectUV(float3 direction)
{
	direction = normalize(direction);

	float u = FastAtan2(direction.z, direction.x) * INV_TWO_PI + 0.5;
	float v = 0.5 - 0.5 * direction.y;
	return float2(u, v);
}

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

float4 GetBaseColor(
	float2 texCoord,
	float2 texCoord1,
	float4 vertexColor)
{
	float2 baseColorUV = UseUV1BaseColor > 0.5 ? texCoord1 : texCoord;
	baseColorUV = ApplyTextureTransform(
		baseColorUV,
		BaseColorTransform,
		BaseColorRotation);

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
	metallicRoughnessUV = ApplyTextureTransform(
		metallicRoughnessUV,
		MetallicRoughnessTransform,
		MetallicRoughnessRotation);
	float2 occlusionUV = UseUV1Occlusion > 0.5 ? texCoord1 : texCoord;
	occlusionUV = ApplyTextureTransform(
		occlusionUV,
		OcclusionTransform,
		OcclusionRotation);
	float2 emissiveUV = UseUV1Emissive > 0.5 ? texCoord1 : texCoord;
	emissiveUV = ApplyTextureTransform(
		emissiveUV,
		EmissiveTransform,
		EmissiveRotation);

	float metallic = MetallicFactor;
	float roughness = RoughnessFactor;

	if (HasMetallicRoughnessTexture > 0.5)
	{
		float4 metallicRoughness = tex2D(MetallicRoughnessSampler, metallicRoughnessUV);
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

	float2 irradianceUV = DirectionToEquirectUV(normal);
	float3 irradiance = tex2D(IrradianceSampler, irradianceUV).rgb;
	float3 iblDiffuse = irradiance * baseColor.rgb;

	float3 reflection = reflect(-view, normal);
	float2 reflectionUV = DirectionToEquirectUV(reflection);
	float mipLevel = roughness * (IBL_PREFILTERED_MIP_COUNT - 1);
	float3 prefiltered = tex2Dlod(
		PrefilteredSpecularSampler,
		float4(reflectionUV, 0.0, mipLevel)).rgb;
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
