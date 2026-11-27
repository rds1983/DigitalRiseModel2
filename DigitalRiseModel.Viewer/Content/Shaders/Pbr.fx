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

struct VertexInput
{
	float4 Position : POSITION;
	float3 Normal : NORMAL;
	float4 Tangent : TANGENT;
	float2 TexCoord : TEXCOORD0;
};

struct VertexOutput
{
	float4 Position : SV_Position;
	float3 WorldPosition : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float3 Tangent : TEXCOORD2;
	float3 Bitangent : TEXCOORD3;
	float2 TexCoord : TEXCOORD4;
};

VertexOutput MainVS(VertexInput input)
{
	VertexOutput output;
	float4 worldPosition = mul(input.Position, World);
	float3 normal = mul(input.Normal, (float3x3)World);
	float3 tangent = mul(input.Tangent.xyz, (float3x3)World);

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = worldPosition.xyz;
	output.Normal = normal;
	output.Tangent = tangent;
	output.Bitangent = cross(normal, tangent) * input.Tangent.w;
	output.TexCoord = input.TexCoord;
	return output;
}

float4 MainPS(VertexOutput input) : SV_Target
{
	float3 baseColor = BaseColorFactor.rgb;

	if (HasBaseColorTexture > 0.5)
	{
		float3 textureColor = tex2D(BaseColorSampler, input.TexCoord).rgb;
		baseColor *= pow(saturate(textureColor), 2.2);
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

	if (HasNormalTexture > 0.5)
	{
		float3 sampledNormal = tex2D(NormalSampler, input.TexCoord).xyz * 2.0 - 1.0;
		sampledNormal.xy *= NormalScale;
		float3 tangent = normalize(input.Tangent);
		float3 bitangent = normalize(input.Bitangent);
		float3x3 tangentSpace = float3x3(tangent, bitangent, normal);
		normal = normalize(mul(sampledNormal, tangentSpace));
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

	float3 f0 = lerp(float3(0.04, 0.04, 0.04), baseColor, metallic);
	float3 fresnel = f0 + (1.0 - f0) * pow(1.0 - vDotH, 5.0);

	float3 specular = distribution * geometry * fresnel
		/ max(4.0 * nDotL * nDotV, 0.001);
	float3 diffuse = (1.0 - fresnel) * (1.0 - metallic) * baseColor / 3.14159265;

	float3 color = (diffuse + specular) * nDotL;
	color += baseColor * 0.12 * occlusion;
	color += emissive;
	color = pow(saturate(color), 1.0 / 2.2);

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
