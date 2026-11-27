float4x4 World;
float4x4 WorldViewProjection;
float3 CameraPosition;

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
float AlphaCutoff;
float UseBaseColorAlpha;
float UseUV1BaseColor;
float UseUV1MetallicRoughness;
float UseUV1Normal;
float UseUV1Occlusion;
float UseUV1Emissive;
int DebugView;
float Exposure;

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

float3 ACESFilm(float3 color)
{
	return saturate((color * (2.51 * color + 0.03))
		/ (color * (2.43 * color + 0.59) + 0.14));
}

float4 ShadeSurface(
	float3 worldPosition,
	float3 normal,
	float2 texCoord,
	float2 texCoord1,
	float facing,
	float4 vertexColor)
{
	float2 baseColorUV = UseUV1BaseColor > 0.5 ? texCoord1 : texCoord;
	float2 metallicRoughnessUV = UseUV1MetallicRoughness > 0.5 ? texCoord1 : texCoord;
	float2 occlusionUV = UseUV1Occlusion > 0.5 ? texCoord1 : texCoord;
	float2 emissiveUV = UseUV1Emissive > 0.5 ? texCoord1 : texCoord;

	float4 baseColor = BaseColorFactor;

	if (HasBaseColorTexture > 0.5)
	{
		float4 textureColor = tex2D(BaseColorSampler, baseColorUV);
		baseColor.rgb *= pow(saturate(textureColor.rgb), 2.2);
		baseColor.a *= textureColor.a;
	}

	baseColor.rgb *= vertexColor.rgb;
	baseColor.a *= vertexColor.a;

	if (baseColor.a < AlphaCutoff)
	{
		discard;
	}

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

	float3 emissive = EmissiveFactor;

	if (HasEmissiveTexture > 0.5)
	{
		float3 emissiveTexture = tex2D(EmissiveSampler, emissiveUV).rgb;
		emissive *= pow(saturate(emissiveTexture), 2.2);
	}

	if (DebugView == 1)
	{
		return float4(pow(saturate(baseColor.rgb), 1.0 / 2.2), 1.0);
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
		return float4(pow(saturate(emissive), 1.0 / 2.2), 1.0);
	}

	float3 view = normalize(CameraPosition - worldPosition);
	float3 light = normalize(float3(0.5, 1.0, 0.5));
	float3 halfVector = normalize(view + light);

	float nDotL = saturate(dot(normal, light));
	float nDotV = saturate(dot(normal, view));
	float nDotH = saturate(dot(normal, halfVector));
	float vDotH = saturate(dot(view, halfVector));

	float alpha = roughness * roughness;
	float alphaSquared = alpha * alpha;
	float denominator = nDotH * nDotH * (alphaSquared - 1.0) + 1.0;
	float distribution = alphaSquared / (3.14159265 * denominator * denominator);

	float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
	float geometry = (nDotL / (nDotL * (1.0 - k) + k))
		* (nDotV / (nDotV * (1.0 - k) + k));

	float3 f0 = lerp(float3(0.04, 0.04, 0.04), baseColor.rgb, metallic);
	float3 fresnel = f0 + (1.0 - f0) * pow(1.0 - vDotH, 5.0);

	float3 specular = distribution * geometry * fresnel
		/ max(4.0 * nDotL * nDotV, 0.001);
	float3 diffuse = (1.0 - fresnel) * (1.0 - metallic) * baseColor.rgb / 3.14159265;

	float up = normal.y * 0.5 + 0.5;
	float3 skyColor = float3(0.18, 0.22, 0.30);
	float3 groundColor = float3(0.04, 0.035, 0.03);
	float3 ambient = lerp(groundColor, skyColor, up) * baseColor.rgb * occlusion;

	float3 color = (diffuse + specular) * nDotL;
	color += ambient;
	color += emissive;
	color = ACESFilm(color * Exposure);
	color = pow(saturate(color), 1.0 / 2.2);

	float outputAlpha = UseBaseColorAlpha > 0.5 ? baseColor.a : 1.0;
	return float4(color, outputAlpha);
}
