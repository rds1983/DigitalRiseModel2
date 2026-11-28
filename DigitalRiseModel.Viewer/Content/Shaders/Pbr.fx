#include "PbrCommon.fxh"

#ifdef SKINNED
float4x4 Bones[72];
#endif

struct VertexInput
{
	float4 Position : POSITION;
#ifdef HAS_NORMAL
	float3 Normal : NORMAL;
#endif
#ifdef HAS_TANGENT
	float4 Tangent : TANGENT;
#endif
#ifdef HAS_COLOR
	float4 Color : COLOR0;
#endif
	float2 TexCoord : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
#ifdef SKINNED
	int4 BlendIndices : BLENDINDICES0;
	float4 BlendWeights : BLENDWEIGHT0;
#endif
};

struct VertexOutput
{
	float4 Position : SV_Position;
	float3 WorldPosition : TEXCOORD0;
#ifdef HAS_NORMAL
	float3 Normal : TEXCOORD1;
#endif
#ifdef HAS_TANGENT
	float3 Tangent : TEXCOORD2;
	float3 Bitangent : TEXCOORD3;
#endif
#ifdef HAS_COLOR
	float4 Color : COLOR0;
#endif
	float2 TexCoord : TEXCOORD4;
	float2 TexCoord1 : TEXCOORD5;
};

VertexOutput MainVS(VertexInput input)
{
	float4 position = input.Position;
	float3 normal = float3(0.0, 1.0, 0.0);
	float3 tangent = float3(1.0, 0.0, 0.0);

#ifdef SKINNED
	float4x4 skinning = 0;
	skinning += Bones[input.BlendIndices.x] * input.BlendWeights.x;
	skinning += Bones[input.BlendIndices.y] * input.BlendWeights.y;
	skinning += Bones[input.BlendIndices.z] * input.BlendWeights.z;
	skinning += Bones[input.BlendIndices.w] * input.BlendWeights.w;

	position = mul(input.Position, skinning);
#ifdef HAS_NORMAL
	normal = mul(input.Normal, (float3x3)skinning);
#endif
#ifdef HAS_TANGENT
	tangent = mul(input.Tangent.xyz, (float3x3)skinning);
#endif
#else
#ifdef HAS_NORMAL
	normal = input.Normal;
#endif
#ifdef HAS_TANGENT
	tangent = input.Tangent.xyz;
#endif
#endif

	float4 worldPosition = mul(position, World);

	VertexOutput output;
	output.Position = mul(position, WorldViewProjection);
	output.WorldPosition = worldPosition.xyz;

#ifdef HAS_NORMAL
	output.Normal = mul(normal, (float3x3)World);
#endif
#ifdef HAS_TANGENT
	output.Tangent = mul(tangent, (float3x3)World);
	output.Bitangent = cross(output.Normal, output.Tangent) * input.Tangent.w;
#endif

#ifdef HAS_COLOR
	output.Color = input.Color;
#endif

	output.TexCoord = input.TexCoord;
	output.TexCoord1 = input.TexCoord1;
	return output;
}

#ifdef UNLIT
float4 MainPS(VertexOutput input, float facing : VFACE) : SV_Target
{
#if defined(HAS_COLOR)
	float4 vertexColor = input.Color;
#else
	float4 vertexColor = float4(1.0, 1.0, 1.0, 1.0);
#endif

	float4 baseColor = GetBaseColor(
		input.TexCoord,
		input.TexCoord1,
		vertexColor);

	if (baseColor.a < AlphaCutoff)
	{
		discard;
	}

	float outputAlpha = UseBaseColorAlpha > 0.5 ? baseColor.a : 1.0;
	return float4(baseColor.rgb, outputAlpha);
}
#else
float4 MainPS(VertexOutput input, float facing : VFACE) : SV_Target
{
	float3 normal = float3(0.0, 1.0, 0.0);

#ifdef HAS_NORMAL
	normal = normalize(input.Normal);
#endif

#ifdef HAS_TANGENT
	if (HasNormalTexture > 0.5)
	{
		float2 normalUV = UseUV1Normal > 0.5 ? input.TexCoord1 : input.TexCoord;
#ifdef TEXTURE_SETTINGS
		normalUV = ApplyTextureTransform(
			normalUV,
			NormalTransform,
			NormalRotation);
		normalUV = ApplySamplerSettings(
			normalUV,
			NormalSamplerInfo,
			NormalTextureSize);
#endif
		float3 sampledNormal = tex2D(NormalSampler, normalUV).xyz * 2.0 - 1.0;
		sampledNormal.xy *= NormalScale;
		float3 tangent = normalize(input.Tangent);
		float3 bitangent = normalize(input.Bitangent);
		float3x3 tangentSpace = float3x3(tangent, bitangent, normal);
		normal = normalize(mul(sampledNormal, tangentSpace));
	}
#endif

#ifdef HAS_COLOR
	float4 vertexColor = input.Color;
#else
	float4 vertexColor = float4(1.0, 1.0, 1.0, 1.0);
#endif

	return ShadeSurface(
		input.WorldPosition,
		normal,
		input.TexCoord,
		input.TexCoord1,
		facing,
		vertexColor);
}
#endif

technique
{
	pass
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}
