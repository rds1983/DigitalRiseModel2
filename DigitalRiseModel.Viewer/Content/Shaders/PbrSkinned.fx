float4x4 World;
float4x4 WorldViewProjection;
float3 CameraPosition;
float4x4 Bones[72];

float4 BaseColorFactor;
float MetallicFactor;
float RoughnessFactor;
float HasBaseColorTexture;
float HasMetallicRoughnessTexture;
float HasOcclusionTexture;
float OcclusionStrength;
float HasEmissiveTexture;
float3 EmissiveFactor;
float AlphaCutoff;
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

struct VertexInput
{
	float4 Position : POSITION;
	float3 Normal : NORMAL;
	float2 TexCoord : TEXCOORD0;
	int4 BlendIndices : BLENDINDICES0;
	float4 BlendWeights : BLENDWEIGHT0;
};

struct VertexOutput
{
	float4 Position : SV_Position;
	float3 WorldPosition : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float2 TexCoord : TEXCOORD2;
};

VertexOutput MainVS(VertexInput input)
{
	float4x4 skinning = 0;

	skinning += Bones[input.BlendIndices.x] * input.BlendWeights.x;
	skinning += Bones[input.BlendIndices.y] * input.BlendWeights.y;
	skinning += Bones[input.BlendIndices.z] * input.BlendWeights.z;
	skinning += Bones[input.BlendIndices.w] * input.BlendWeights.w;

	float4 skinnedPosition = mul(input.Position, skinning);
	float3 skinnedNormal = mul(input.Normal, (float3x3)skinning);
	float4 worldPosition = mul(skinnedPosition, World);

	VertexOutput output;
	output.Position = mul(skinnedPosition, WorldViewProjection);
	output.WorldPosition = worldPosition.xyz;
	output.Normal = mul(skinnedNormal, (float3x3)World);
	output.TexCoord = input.TexCoord;
	return output;
}

float4 MainPS(VertexOutput input, float facing : VFACE) : SV_Target
{
	float4 baseColor = BaseColorFactor;

	if (HasBaseColorTexture > 0.5)
	{
		float4 textureColor = tex2D(BaseColorSampler, input.TexCoord);
		baseColor.rgb *= pow(saturate(textureColor.rgb), 2.2);
		baseColor.a *= textureColor.a;
	}

	if (baseColor.a < AlphaCutoff)
	{
		discard;
	}

	float metallic = MetallicFactor;
	float roughness = RoughnessFactor;

	if (HasMetallicRoughnessTexture > 0.5)
	{
		float4 metallicRoughness = tex2D(MetallicRoughnessSampler, input.TexCoord);
		roughness *= metallicRoughness.g;
		metallic *= metallicRoughness.b;
	}

	metallic = saturate(metallic);
	roughness = clamp(roughness, 0.04, 1.0);

	float3 normal = normalize(input.Normal);

	if (facing < 0.0)
	{
		normal = -normal;
	}

	float occlusion = 1.0;

	if (HasOcclusionTexture > 0.5)
	{
		float sampledOcclusion = tex2D(OcclusionSampler, input.TexCoord).r;
		occlusion = lerp(1.0, sampledOcclusion, OcclusionStrength);
	}

	float3 emissive = EmissiveFactor;

	if (HasEmissiveTexture > 0.5)
	{
		float3 emissiveTexture = tex2D(EmissiveSampler, input.TexCoord).rgb;
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

	float3 view = normalize(CameraPosition - input.WorldPosition);
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

	float3 color = (diffuse + specular) * nDotL;
	color += baseColor.rgb * 0.12 * occlusion;
	color += emissive;
	color = pow(saturate(color * Exposure), 1.0 / 2.2);

	return float4(color, 1.0);
}

technique
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}
